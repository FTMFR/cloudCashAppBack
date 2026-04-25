using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Common;
using BnpCashClaudeApp.Domain.Entities.SecuritySubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// سرویس مدیریت چرخه حیات کلید رمزنگاری
    /// پیاده‌سازی الزامات FCS_CKM از استاندارد ISO 15408
    /// 
    /// ویژگی‌ها:
    /// - ذخیره‌سازی امن کلیدها (رمزنگاری با Master Key + Encrypt-then-MAC)
    /// - Key Rotation با دوره انتقال
    /// - تخریب امن کلیدها (FCS_CKM.4)
    /// - ثبت Audit Log برای تمام عملیات
    /// </summary>
    public class KeyManagementService : IKeyManagementService, IDisposable
    {
        private readonly NavigationDbContext _dbContext;
        private readonly IKeyGenerationService _keyGenerationService;
        private readonly ICryptographicAlgorithmPolicyService _cryptographicAlgorithmPolicyService;
        private readonly ISecureMemoryService _secureMemoryService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<KeyManagementService> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IConfiguration _configuration;

        private readonly byte[] _masterKey;
        private readonly byte[] _macKey;
        private bool _disposed;

        public KeyManagementService(
            NavigationDbContext dbContext,
            IKeyGenerationService keyGenerationService,
            ICryptographicAlgorithmPolicyService cryptographicAlgorithmPolicyService,
            ISecureMemoryService secureMemoryService,
            IAuditLogService auditLogService,
            ILogger<KeyManagementService> logger,
            IHostEnvironment hostEnvironment,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _keyGenerationService = keyGenerationService;
            _cryptographicAlgorithmPolicyService = cryptographicAlgorithmPolicyService;
            _secureMemoryService = secureMemoryService;
            _auditLogService = auditLogService;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _configuration = configuration;

            string? masterKeyBase64 = _configuration["Security:MasterKey"];
            if (string.IsNullOrWhiteSpace(masterKeyBase64))
            {
                if (!_hostEnvironment.IsDevelopment())
                {
                    throw new InvalidOperationException(
                        "Missing required configuration: Security:MasterKey. Configure a stable 256-bit Base64 key.");
                }

                _masterKey = CreateDeterministicDevelopmentMasterKey();
                _logger.LogWarning(
                    "No Security:MasterKey configured. Using deterministic development fallback key tied to machine identity. " +
                    "Configure Security:MasterKey for shared/stable environments.");
            }
            else
            {
                try
                {
                    _masterKey = Convert.FromBase64String(masterKeyBase64.Trim());
                }
                catch (FormatException ex)
                {
                    throw new InvalidOperationException(
                        "Security:MasterKey must be a valid Base64 string for a 256-bit key.",
                        ex);
                }
            }

            _cryptographicAlgorithmPolicyService.ValidateMasterKey(_masterKey);
            _macKey = DeriveMacKey(_masterKey);
        }

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _secureMemoryService.ClearBytes(_masterKey);
                _secureMemoryService.ClearBytes(_macKey);
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        #endregion

        #region Key Storage & Retrieval

        /// <summary>
        /// ذخیره کلید جدید
        /// </summary>
        public async Task<Guid> StoreKeyAsync(
            string keyPurpose,
            byte[] keyValue,
            DateTime? expiresAt = null,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(keyPurpose);
            ArgumentNullException.ThrowIfNull(keyValue);
            if (keyValue.Length == 0)
                throw new ArgumentException("Key value must not be empty.", nameof(keyValue));

            var (encryptedKey, iv, mac) = EncryptKey(keyValue);
            string keyHash = ComputeKeyHash(keyValue);

            var keyEntity = new CryptographicKeyEntity
            {
                KeyId = Guid.NewGuid(),
                Purpose = keyPurpose,
                EncryptedKeyValue = Convert.ToBase64String(encryptedKey),
                EncryptionIV = Convert.ToBase64String(iv),
                EncryptionMAC = Convert.ToBase64String(mac),
                KeyLengthBits = keyValue.Length * 8,
                Algorithm = _cryptographicAlgorithmPolicyService.ResolveManagedKeyAlgorithm(keyValue.Length * 8),
                Status = (int)KeyStatus.Active,
                ActivatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                KeyHash = keyHash,
                Version = 1
            };

            keyEntity.SetZamanInsert(DateTime.Now);

            _dbContext.Set<CryptographicKeyEntity>().Add(keyEntity);
            await _dbContext.SaveChangesAsync(ct);

            _secureMemoryService.ClearBytes(encryptedKey);
            _secureMemoryService.ClearBytes(iv);
            _secureMemoryService.ClearBytes(mac);

            await _auditLogService.LogEventAsync(
                eventType: "KeyCreated",
                entityType: "CryptographicKey",
                entityId: keyEntity.KeyId.ToString(),
                isSuccess: true,
                description: $"New cryptographic key created for purpose: {keyPurpose}, Length: {keyEntity.KeyLengthBits} bits, Algorithm: {keyEntity.Algorithm}");

            _logger.LogInformation(
                "Created new cryptographic key {KeyId} for purpose {Purpose}",
                keyEntity.KeyId,
                keyPurpose);

            return keyEntity.KeyId;
        }

        /// <summary>
        /// دریافت کلید فعال بر اساس هدف
        /// </summary>
        public async Task<CryptographicKey?> GetActiveKeyAsync(
            string keyPurpose,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(keyPurpose);

            var keyEntities = await _dbContext.Set<CryptographicKeyEntity>()
                .Where(k => k.Purpose == keyPurpose && k.Status == (int)KeyStatus.Active)
                .Where(k => k.ExpiresAt == null || k.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(k => k.Version)
                .ToListAsync(ct);

            if (keyEntities.Count == 0)
                return null;

            foreach (var keyEntity in keyEntities)
            {
                try
                {
                    var key = DecryptAndMapKey(keyEntity);

                    keyEntity.LastUsedAt = DateTime.UtcNow;
                    keyEntity.UsageCount++;
                    await _dbContext.SaveChangesAsync(ct);

                    return key;
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Skipping non-decryptable active key {KeyId} for purpose {Purpose}.",
                        keyEntity.KeyId,
                        keyPurpose);
                }
            }

            return null;
        }

        /// <summary>
        /// دریافت کلید بر اساس شناسه
        /// </summary>
        public async Task<CryptographicKey?> GetKeyByIdAsync(
            Guid keyId,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();

            var keyEntity = await _dbContext.Set<CryptographicKeyEntity>()
                .FirstOrDefaultAsync(k => k.KeyId == keyId, ct);

            if (keyEntity == null)
                return null;

            return DecryptAndMapKey(keyEntity);
        }

        /// <summary>
        /// دریافت تمام کلیدهای یک هدف
        /// </summary>
        public async Task<List<CryptographicKey>> GetKeysByPurposeAsync(
            string keyPurpose,
            bool includeExpired = false,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(keyPurpose);

            var query = _dbContext.Set<CryptographicKeyEntity>()
                .Where(k => k.Purpose == keyPurpose);

            if (!includeExpired)
            {
                query = query.Where(k => k.Status != (int)KeyStatus.Expired &&
                                         k.Status != (int)KeyStatus.Destroyed);
            }

            var keyEntities = await query
                .OrderByDescending(k => k.Version)
                .ToListAsync(ct);

            var result = new List<CryptographicKey>();
            foreach (var entity in keyEntities)
            {
                try
                {
                    result.Add(DecryptAndMapKey(entity));
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Skipping non-decryptable key {KeyId} for purpose {Purpose}.",
                        entity.KeyId,
                        keyPurpose);
                }
            }

            return result;
        }

        #endregion

        #region Key Rotation

        /// <summary>
        /// چرخش کلید (ایجاد کلید جدید و غیرفعال کردن کلید قبلی)
        /// </summary>
        public async Task<Guid> RotateKeyAsync(
            string keyPurpose,
            byte[] newKeyValue,
            int gracePeriodMinutes = 60,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(keyPurpose);
            ArgumentNullException.ThrowIfNull(newKeyValue);
            if (newKeyValue.Length == 0)
                throw new ArgumentException("New key value must not be empty.", nameof(newKeyValue));

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.RepeatableRead, ct);

            try
            {
                var currentKey = await _dbContext.Set<CryptographicKeyEntity>()
                    .Where(k => k.Purpose == keyPurpose && k.Status == (int)KeyStatus.Active)
                    .OrderByDescending(k => k.Version)
                    .FirstOrDefaultAsync(ct);

                var (encryptedKey, iv, mac) = EncryptKey(newKeyValue);
                string keyHash = ComputeKeyHash(newKeyValue);

                var newKeyEntity = new CryptographicKeyEntity
                {
                    KeyId = Guid.NewGuid(),
                    Purpose = keyPurpose,
                    EncryptedKeyValue = Convert.ToBase64String(encryptedKey),
                    EncryptionIV = Convert.ToBase64String(iv),
                    EncryptionMAC = Convert.ToBase64String(mac),
                    KeyLengthBits = newKeyValue.Length * 8,
                    Algorithm = _cryptographicAlgorithmPolicyService.ResolveManagedKeyAlgorithm(newKeyValue.Length * 8),
                    Status = (int)KeyStatus.Active,
                    ActivatedAt = DateTime.UtcNow,
                    KeyHash = keyHash,
                    Version = (currentKey?.Version ?? 0) + 1,
                    PreviousKeyId = currentKey?.KeyId
                };

                newKeyEntity.SetZamanInsert(DateTime.Now);

                if (currentKey != null)
                {
                    currentKey.Status = (int)KeyStatus.Inactive;
                    currentKey.DeactivatedAt = DateTime.UtcNow;
                    currentKey.DeactivationReason = "Key rotated";
                    currentKey.ReplacedByKeyId = newKeyEntity.KeyId;
                    currentKey.GracePeriodEndsAt = DateTime.UtcNow.AddMinutes(gracePeriodMinutes);
                    currentKey.SetZamanLastEdit(DateTime.Now);
                }

                _dbContext.Set<CryptographicKeyEntity>().Add(newKeyEntity);
                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _secureMemoryService.ClearBytes(encryptedKey);
                _secureMemoryService.ClearBytes(iv);
                _secureMemoryService.ClearBytes(mac);

                await _auditLogService.LogEventAsync(
                    eventType: "KeyRotated",
                    entityType: "CryptographicKey",
                    entityId: newKeyEntity.KeyId.ToString(),
                    isSuccess: true,
                    description: $"Key rotated for purpose: {keyPurpose}, Previous: {currentKey?.KeyId}, Grace period: {gracePeriodMinutes} min");

                _logger.LogInformation(
                    "Rotated key for purpose {Purpose}. New key: {NewKeyId}, Previous key: {PreviousKeyId}",
                    keyPurpose,
                    newKeyEntity.KeyId,
                    currentKey?.KeyId);

                return newKeyEntity.KeyId;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        /// <summary>
        /// چرخش خودکار کلید (تولید کلید جدید)
        /// </summary>
        public async Task<Guid> AutoRotateKeyAsync(
            string keyPurpose,
            int keyLengthBits = 256,
            int gracePeriodMinutes = 60,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(keyPurpose);

            byte[] newKey = _keyGenerationService.GenerateSymmetricKey(keyLengthBits);

            try
            {
                return await RotateKeyAsync(keyPurpose, newKey, gracePeriodMinutes, ct);
            }
            finally
            {
                _secureMemoryService.ClearBytes(newKey);
            }
        }

        /// <summary>
        /// بررسی نیاز به چرخش کلید
        /// </summary>
        public async Task<bool> NeedsRotationAsync(
            string keyPurpose,
            int maxAgeDays = 90,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(keyPurpose);

            var activeKey = await _dbContext.Set<CryptographicKeyEntity>()
                .Where(k => k.Purpose == keyPurpose && k.Status == (int)KeyStatus.Active)
                .OrderByDescending(k => k.Version)
                .FirstOrDefaultAsync(ct);

            if (activeKey?.ActivatedAt == null)
                return true;

            var keyAge = DateTime.UtcNow - activeKey.ActivatedAt.Value;
            return keyAge.TotalDays > maxAgeDays;
        }

        #endregion

        #region Key Destruction (FCS_CKM.4)

        /// <summary>
        /// تخریب امن کلید
        /// </summary>
        public async Task DestroyKeyAsync(
            Guid keyId,
            string reason,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();

            var keyEntity = await _dbContext.Set<CryptographicKeyEntity>()
                .FirstOrDefaultAsync(k => k.KeyId == keyId, ct);

            if (keyEntity == null)
            {
                _logger.LogWarning("Attempted to destroy non-existent key {KeyId}", keyId);
                return;
            }

            ZeroizeKeyEntity(keyEntity, reason);
            await _dbContext.SaveChangesAsync(ct);

            await _auditLogService.LogEventAsync(
                eventType: "KeyDestroyed",
                entityType: "CryptographicKey",
                entityId: keyId.ToString(),
                isSuccess: true,
                description: $"Key destroyed for purpose: {keyEntity.Purpose}, Reason: {reason}");

            _logger.LogInformation(
                "Destroyed key {KeyId} for purpose {Purpose}. Reason: {Reason}",
                keyId,
                keyEntity.Purpose,
                reason);
        }

        /// <summary>
        /// تخریب امن تمام کلیدهای منقضی
        /// </summary>
        public async Task<int> DestroyExpiredKeysAsync(CancellationToken ct = default)
        {
            ThrowIfDisposed();

            var expiredKeys = await _dbContext.Set<CryptographicKeyEntity>()
                .Where(k => k.Status != (int)KeyStatus.Destroyed)
                .Where(k => (k.ExpiresAt != null && k.ExpiresAt < DateTime.UtcNow) ||
                           (k.GracePeriodEndsAt != null && k.GracePeriodEndsAt < DateTime.UtcNow && k.Status == (int)KeyStatus.Inactive))
                .ToListAsync(ct);

            if (expiredKeys.Count == 0)
                return 0;

            foreach (var key in expiredKeys)
            {
                ZeroizeKeyEntity(key, "Key expired");
            }

            await _dbContext.SaveChangesAsync(ct);

            foreach (var key in expiredKeys)
            {
                await _auditLogService.LogEventAsync(
                    eventType: "KeyDestroyed",
                    entityType: "CryptographicKey",
                    entityId: key.KeyId.ToString(),
                    isSuccess: true,
                    description: $"Key destroyed for purpose: {key.Purpose}, Reason: Key expired");
            }

            _logger.LogInformation("Destroyed {Count} expired keys", expiredKeys.Count);
            return expiredKeys.Count;
        }

        /// <summary>
        /// تخریب امن تمام کلیدهای یک هدف
        /// </summary>
        public async Task<int> DestroyKeysByPurposeAsync(
            string keyPurpose,
            string reason,
            CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(keyPurpose);

            var keys = await _dbContext.Set<CryptographicKeyEntity>()
                .Where(k => k.Purpose == keyPurpose && k.Status != (int)KeyStatus.Destroyed)
                .ToListAsync(ct);

            if (keys.Count == 0)
                return 0;

            foreach (var key in keys)
            {
                ZeroizeKeyEntity(key, reason);
            }

            await _dbContext.SaveChangesAsync(ct);

            foreach (var key in keys)
            {
                await _auditLogService.LogEventAsync(
                    eventType: "KeyDestroyed",
                    entityType: "CryptographicKey",
                    entityId: key.KeyId.ToString(),
                    isSuccess: true,
                    description: $"Key destroyed for purpose: {key.Purpose}, Reason: {reason}");
            }

            _logger.LogInformation("Destroyed {Count} keys for purpose {Purpose}", keys.Count, keyPurpose);
            return keys.Count;
        }

        #endregion

        #region Key Lifecycle

        /// <summary>
        /// فعال‌سازی کلید
        /// </summary>
        public async Task ActivateKeyAsync(Guid keyId, CancellationToken ct = default)
        {
            ThrowIfDisposed();

            var keyEntity = await _dbContext.Set<CryptographicKeyEntity>()
                .FirstOrDefaultAsync(k => k.KeyId == keyId, ct)
                ?? throw new InvalidOperationException($"Key {keyId} not found");

            keyEntity.Status = (int)KeyStatus.Active;
            keyEntity.ActivatedAt = DateTime.UtcNow;
            keyEntity.SetZamanLastEdit(DateTime.Now);

            await _dbContext.SaveChangesAsync(ct);

            await _auditLogService.LogEventAsync(
                eventType: "KeyActivated",
                entityType: "CryptographicKey",
                entityId: keyId.ToString(),
                isSuccess: true,
                description: $"Key activated for purpose: {keyEntity.Purpose}");
        }

        /// <summary>
        /// غیرفعال‌سازی کلید
        /// </summary>
        public async Task DeactivateKeyAsync(Guid keyId, string reason, CancellationToken ct = default)
        {
            ThrowIfDisposed();

            var keyEntity = await _dbContext.Set<CryptographicKeyEntity>()
                .FirstOrDefaultAsync(k => k.KeyId == keyId, ct)
                ?? throw new InvalidOperationException($"Key {keyId} not found");

            keyEntity.Status = (int)KeyStatus.Inactive;
            keyEntity.DeactivatedAt = DateTime.UtcNow;
            keyEntity.DeactivationReason = reason;
            keyEntity.SetZamanLastEdit(DateTime.Now);

            await _dbContext.SaveChangesAsync(ct);

            await _auditLogService.LogEventAsync(
                eventType: "KeyDeactivated",
                entityType: "CryptographicKey",
                entityId: keyId.ToString(),
                isSuccess: true,
                description: $"Key deactivated for purpose: {keyEntity.Purpose}, Reason: {reason}");
        }

        /// <summary>
        /// دریافت وضعیت کلید
        /// </summary>
        public async Task<KeyStatus?> GetKeyStatusAsync(Guid keyId, CancellationToken ct = default)
        {
            ThrowIfDisposed();

            var keyEntity = await _dbContext.Set<CryptographicKeyEntity>()
                .FirstOrDefaultAsync(k => k.KeyId == keyId, ct);

            if (keyEntity == null)
                return null;

            return (KeyStatus)keyEntity.Status;
        }

        /// <summary>
        /// دریافت آمار کلیدها
        /// </summary>
        public async Task<KeyStatistics> GetKeyStatisticsAsync(CancellationToken ct = default)
        {
            ThrowIfDisposed();

            var keySet = _dbContext.Set<CryptographicKeyEntity>();

            int activeStatus = (int)KeyStatus.Active;
            int expiredStatus = (int)KeyStatus.Expired;
            int destroyedStatus = (int)KeyStatus.Destroyed;

            var totalKeys = await keySet.CountAsync(ct);
            var activeKeys = await keySet.CountAsync(k => k.Status == activeStatus, ct);
            var expiredKeys = await keySet.CountAsync(k => k.Status == expiredStatus, ct);
            var destroyedKeys = await keySet.CountAsync(k => k.Status == destroyedStatus, ct);

            var purposeGroups = await keySet
                .GroupBy(k => k.Purpose)
                .Select(g => new { Purpose = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var keysByPurpose = purposeGroups.ToDictionary(x => x.Purpose, x => x.Count);

            var oldestActiveKeyDate = await keySet
                .Where(k => k.Status == activeStatus && k.ActivatedAt != null)
                .OrderBy(k => k.ActivatedAt)
                .Select(k => k.ActivatedAt)
                .FirstOrDefaultAsync(ct);

            var newestKeyInsert = await keySet
                .OrderByDescending(k => k.Id)
                .Select(k => k.ZamanInsert)
                .FirstOrDefaultAsync(ct);

            return new KeyStatistics
            {
                TotalKeys = totalKeys,
                ActiveKeys = activeKeys,
                ExpiredKeys = expiredKeys,
                DestroyedKeys = destroyedKeys,
                KeysByPurpose = keysByPurpose,
                OldestActiveKeyDate = oldestActiveKeyDate,
                NewestKeyDate = !string.IsNullOrEmpty(newestKeyInsert)
                    ? BaseEntity.ToGregorianDateTime(newestKeyInsert)
                    : null
            };
        }

        #endregion

        #region Private Methods

        private static byte[] CreateDeterministicDevelopmentMasterKey()
        {
            string identitySeed = $"{Environment.MachineName}|{Environment.UserName}|BnpCashClaudeApp|DevMasterKeyV1";
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(identitySeed));
        }

        private static byte[] DeriveMacKey(byte[] masterKey)
        {
            using var hmac = new HMACSHA256(masterKey);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes("BnpCashClaudeApp:KeyWrapping:MAC"));
        }

        /// <summary>
        /// رمزنگاری کلید با AES-256-CBC + Encrypt-then-MAC
        /// </summary>
        private (byte[] encryptedKey, byte[] iv, byte[] mac) EncryptKey(byte[] keyValue)
        {
            using var aes = _cryptographicAlgorithmPolicyService.CreateAesForKeyWrapping(_masterKey);
            using var encryptor = aes.CreateEncryptor();
            byte[] encryptedKey = encryptor.TransformFinalBlock(keyValue, 0, keyValue.Length);
            byte[] iv = aes.IV;

            byte[] mac = ComputeEncryptionMac(iv, encryptedKey);

            return (encryptedKey, iv, mac);
        }

        /// <summary>
        /// رمزگشایی کلید با تایید MAC
        /// </summary>
        private byte[] DecryptKey(string encryptedKeyBase64, string ivBase64, string? macBase64)
        {
            byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);
            byte[] iv = Convert.FromBase64String(ivBase64);

            try
            {
                if (!string.IsNullOrEmpty(macBase64))
                {
                    byte[] storedMac = Convert.FromBase64String(macBase64);
                    byte[] computedMac = ComputeEncryptionMac(iv, encryptedKey);

                    bool macValid = CryptographicOperations.FixedTimeEquals(storedMac, computedMac);

                    _secureMemoryService.ClearBytes(storedMac);
                    _secureMemoryService.ClearBytes(computedMac);

                    if (!macValid)
                    {
                        throw new CryptographicException(
                            "Key material MAC verification failed. Data may have been tampered with.");
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Key does not have Encrypt-then-MAC. Consider re-encrypting for tamper detection.");
                }

                using var aes = _cryptographicAlgorithmPolicyService.CreateAesForKeyWrapping(_masterKey, iv);
                using var decryptor = aes.CreateDecryptor();
                return decryptor.TransformFinalBlock(encryptedKey, 0, encryptedKey.Length);
            }
            catch (CryptographicException ex) when (!ex.Message.Contains("MAC", StringComparison.Ordinal))
            {
                throw new CryptographicException(
                    "Failed to decrypt key material. Security:MasterKey may be different from the one used during encryption.",
                    ex);
            }
            finally
            {
                _secureMemoryService.ClearBytes(encryptedKey);
                _secureMemoryService.ClearBytes(iv);
            }
        }

        /// <summary>
        /// محاسبه HMAC-SHA256 روی IV || Ciphertext
        /// </summary>
        private byte[] ComputeEncryptionMac(byte[] iv, byte[] ciphertext)
        {
            using var hmac = new HMACSHA256(_macKey);

            byte[] dataToMac = new byte[iv.Length + ciphertext.Length];
            Buffer.BlockCopy(iv, 0, dataToMac, 0, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, dataToMac, iv.Length, ciphertext.Length);

            byte[] mac = hmac.ComputeHash(dataToMac);
            _secureMemoryService.ClearBytes(dataToMac);

            return mac;
        }

        /// <summary>
        /// محاسبه هش کلید (برای اعتبارسنجی)
        /// </summary>
        private static string ComputeKeyHash(byte[] keyValue)
        {
            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(keyValue);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// تایید هش کلید پس از رمزگشایی
        /// </summary>
        private bool VerifyKeyHash(byte[] decryptedKey, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
                return true;

            string computedHash = ComputeKeyHash(decryptedKey);
            return string.Equals(computedHash, storedHash, StringComparison.Ordinal);
        }

        /// <summary>
        /// رمزگشایی و تبدیل Entity به Model
        /// </summary>
        private CryptographicKey DecryptAndMapKey(CryptographicKeyEntity entity)
        {
            byte[] keyValue;

            if (entity.Status == (int)KeyStatus.Destroyed)
            {
                keyValue = Array.Empty<byte>();
            }
            else
            {
                try
                {
                    keyValue = DecryptKey(entity.EncryptedKeyValue, entity.EncryptionIV, entity.EncryptionMAC);
                }
                catch (Exception ex) when (ex is CryptographicException || ex is FormatException)
                {
                    _logger.LogError(
                        ex,
                        "Failed to decrypt cryptographic key {KeyId} for purpose {Purpose}.",
                        entity.KeyId,
                        entity.Purpose);

                    throw new InvalidOperationException(
                        $"Unable to decrypt key '{entity.KeyId}'. Ensure Security:MasterKey is stable and unchanged.",
                        ex);
                }

                if (!VerifyKeyHash(keyValue, entity.KeyHash))
                {
                    _secureMemoryService.ClearBytes(keyValue);

                    _logger.LogError(
                        "Key hash mismatch for key {KeyId} purpose {Purpose}. Key material may be corrupted.",
                        entity.KeyId,
                        entity.Purpose);

                    throw new InvalidOperationException(
                        $"Key hash verification failed for key '{entity.KeyId}'. Key material may be corrupted or tampered with.");
                }
            }

            return new CryptographicKey
            {
                KeyId = entity.KeyId,
                Purpose = entity.Purpose,
                KeyValue = keyValue,
                Status = (KeyStatus)entity.Status,
                CreatedAt = entity.GetZamanInsertAsGregorian(),
                ExpiresAt = entity.ExpiresAt,
                LastUsedAt = entity.LastUsedAt
            };
        }

        /// <summary>
        /// پاکسازی امن داده‌های کلید (Zeroization) بدون نیاز به کوئری مجدد
        /// </summary>
        private void ZeroizeKeyEntity(CryptographicKeyEntity keyEntity, string reason)
        {
            byte[] randomData = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomData);
            }

            keyEntity.EncryptedKeyValue = Convert.ToBase64String(randomData);
            keyEntity.EncryptionIV = Convert.ToBase64String(randomData.AsSpan(0, 16).ToArray());
            keyEntity.EncryptionMAC = null;
            keyEntity.KeyHash = string.Empty;
            keyEntity.Status = (int)KeyStatus.Destroyed;
            keyEntity.DestroyedAt = DateTime.UtcNow;
            keyEntity.DestructionReason = reason;
            keyEntity.SetZamanLastEdit(DateTime.Now);

            _secureMemoryService.ClearBytes(randomData);
        }

        #endregion
    }
}
