using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Domain.Entities.ManagementSubsystem;
using BnpCashClaudeApp.Persistence.Migrations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// Provides encrypted database connection settings for dynamic database routing.
    /// AES-256-CBC is used for at-rest encrypted values.
    /// </summary>
    public class DatabaseConnectionService : IDatabaseConnectionService
    {
        private const string EncryptionV2Prefix = "enc:v2:";

        private readonly NavigationDbContext _context;
        private readonly ILogger<DatabaseConnectionService> _logger;
        private readonly byte[] _encryptionKey;
        private readonly IReadOnlyList<byte[]> _decryptionKeys;

        public DatabaseConnectionService(
            NavigationDbContext context,
            IConfiguration configuration,
            ILogger<DatabaseConnectionService> logger)
        {
            _context = context;
            _logger = logger;

            var keyMaterial = configuration["Encryption:Key"];
            if (string.IsNullOrWhiteSpace(keyMaterial))
            {
                throw new InvalidOperationException(
                    "Encryption:Key is not configured. Set Encryption__Key for runtime.");
            }

            _encryptionKey = DerivePrimaryEncryptionKey(keyMaterial);
            _decryptionKeys = BuildDecryptionKeys(keyMaterial, _encryptionKey);
        }

        /// <inheritdoc/>
        public async Task<string?> GetConnectionStringAsync(string dbCode)
        {
            try
            {
                var db = await _context.tblDbs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.DbCode == dbCode && d.Status == 1);

                if (db == null)
                {
                    _logger.LogWarning("Database with code '{DbCode}' not found or inactive", dbCode);
                    return null;
                }

                return BuildConnectionStringFromEntity(db);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connection string for DbCode: {DbCode}", dbCode);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<string?> GetConnectionStringByIdAsync(long dbId)
        {
            try
            {
                var db = await _context.tblDbs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == dbId && d.Status == 1);

                if (db == null)
                {
                    _logger.LogWarning("Database with Id '{DbId}' not found or inactive", dbId);
                    return null;
                }

                return BuildConnectionStringFromEntity(db);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connection string for DbId: {DbId}", dbId);
                return null;
            }
        }

        private string BuildConnectionStringFromEntity(tblDb db)
        {
            if (!string.IsNullOrEmpty(db.EncryptedConnectionString))
            {
                try
                {
                    return DecryptConnectionString(db.EncryptedConnectionString);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to decrypt encrypted connection string for DbCode: {DbCode}. Falling back to encrypted parts.",
                        db.DbCode);
                }
            }

            string? password = null;
            if (!string.IsNullOrEmpty(db.EncryptedPassword))
            {
                try
                {
                    password = DecryptPassword(db.EncryptedPassword);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to decrypt password for DbCode: {DbCode}", db.DbCode);
                }
            }

            bool integratedSecurity = string.IsNullOrEmpty(db.Username);
            return BuildConnectionString(db.ServerName, db.Port, db.DatabaseName, db.Username, password, integratedSecurity);
        }

        /// <inheritdoc/>
        public string BuildConnectionString(
            string serverName,
            int? port,
            string databaseName,
            string? username,
            string? password,
            bool integratedSecurity = false)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = port.HasValue && port.Value != 1433
                    ? $"{serverName},{port.Value}"
                    : serverName,
                InitialCatalog = databaseName,
                TrustServerCertificate = true,
                MultipleActiveResultSets = true,
                Encrypt = true
            };

            if (integratedSecurity || string.IsNullOrEmpty(username))
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = username;
                builder.Password = password ?? string.Empty;
            }

            return builder.ConnectionString;
        }

        /// <inheritdoc/>
        public string EncryptPassword(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword))
                return string.Empty;

            return Encrypt(plainPassword);
        }

        /// <inheritdoc/>
        public string DecryptPassword(string encryptedPassword)
        {
            if (string.IsNullOrEmpty(encryptedPassword))
                return string.Empty;

            return Decrypt(encryptedPassword);
        }

        /// <inheritdoc/>
        public string EncryptConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return string.Empty;

            return Encrypt(connectionString);
        }

        /// <inheritdoc/>
        public string DecryptConnectionString(string encryptedConnectionString)
        {
            if (string.IsNullOrEmpty(encryptedConnectionString))
                return string.Empty;

            return Decrypt(encryptedConnectionString);
        }

        private string Encrypt(string plainText)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var iv = RandomNumberGenerator.GetBytes(16);

            try
            {
                using var aes = CreateAesCipher(_encryptionKey, iv);
                using var encryptor = aes.CreateEncryptor();
                var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                return $"{EncryptionV2Prefix}{Convert.ToBase64String(iv)}:{Convert.ToBase64String(cipherBytes)}";
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plainBytes);
                CryptographicOperations.ZeroMemory(iv);
            }
        }

        private string Decrypt(string cipherText)
        {
            if (TryDecryptV2(cipherText, out var plainText))
                return plainText;

            if (TryDecryptLegacyFixedIv(cipherText, out plainText))
                return plainText;

            if (TryDecryptLegacyIvPrefixed(cipherText, out plainText))
                return plainText;

            throw new CryptographicException("Encrypted payload format is invalid or key is not compatible.");
        }

        private bool TryDecryptV2(string cipherText, out string plainText)
        {
            plainText = string.Empty;
            if (!cipherText.StartsWith(EncryptionV2Prefix, StringComparison.Ordinal))
                return false;

            var payload = cipherText.Substring(EncryptionV2Prefix.Length);
            var separatorIndex = payload.IndexOf(':');
            if (separatorIndex <= 0 || separatorIndex == payload.Length - 1)
                return false;

            var ivBase64 = payload.Substring(0, separatorIndex);
            var cipherBase64 = payload.Substring(separatorIndex + 1);

            if (!TryFromBase64(ivBase64, out var iv) || iv.Length != 16)
                return false;

            if (!TryFromBase64(cipherBase64, out var cipherBytes) || cipherBytes.Length == 0)
                return false;

            foreach (var key in _decryptionKeys)
            {
                if (TryDecryptWithKey(cipherBytes, key, iv, out plainText))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryDecryptLegacyFixedIv(string cipherText, out string plainText)
        {
            plainText = string.Empty;
            if (!TryFromBase64(cipherText, out var cipherBytes) || cipherBytes.Length == 0)
                return false;

            foreach (var key in _decryptionKeys)
            {
                var iv = new byte[16];
                Buffer.BlockCopy(key, 0, iv, 0, 16);

                if (TryDecryptWithKey(cipherBytes, key, iv, out plainText))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryDecryptLegacyIvPrefixed(string cipherText, out string plainText)
        {
            plainText = string.Empty;
            if (!TryFromBase64(cipherText, out var fullCipher) || fullCipher.Length <= 16)
                return false;

            var iv = new byte[16];
            var cipherBytes = new byte[fullCipher.Length - 16];
            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);

            foreach (var key in _decryptionKeys)
            {
                if (TryDecryptWithKey(cipherBytes, key, iv, out plainText))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryDecryptWithKey(byte[] cipherBytes, byte[] key, byte[] iv, out string plainText)
        {
            plainText = string.Empty;
            byte[]? plainBytes = null;

            try
            {
                using var aes = CreateAesCipher(key, iv);
                using var decryptor = aes.CreateDecryptor();
                plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                plainText = Encoding.UTF8.GetString(plainBytes);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
            finally
            {
                if (plainBytes != null && plainBytes.Length > 0)
                {
                    CryptographicOperations.ZeroMemory(plainBytes);
                }
            }
        }

        private static Aes CreateAesCipher(byte[] key, byte[] iv)
        {
            var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.KeySize = 256;
            aes.Key = key;
            aes.IV = iv;
            return aes;
        }

        private static byte[] DerivePrimaryEncryptionKey(string keyMaterial)
        {
            if (TryParseBase64Key(keyMaterial, out var base64Key))
            {
                if (base64Key.Length == 32)
                    return base64Key;

                using var sha256 = SHA256.Create();
                return sha256.ComputeHash(base64Key);
            }

            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(keyMaterial));
            }
        }

        private static IReadOnlyList<byte[]> BuildDecryptionKeys(string keyMaterial, byte[] primaryKey)
        {
            var keys = new List<byte[]>
            {
                Clone(primaryKey)
            };

            var managementLegacyKey = DeriveLegacyManagementKey(keyMaterial);
            if (managementLegacyKey.Length == 32 && !keys.Any(k => k.SequenceEqual(managementLegacyKey)))
            {
                keys.Add(managementLegacyKey);
            }

            if (TryParseBase64Key(keyMaterial, out var rawKey) &&
                rawKey.Length == 32 &&
                !keys.Any(k => k.SequenceEqual(rawKey)))
            {
                keys.Add(rawKey);
            }

            return keys;
        }

        private static byte[] DeriveLegacyManagementKey(string keyMaterial)
        {
            return Encoding.UTF8.GetBytes(keyMaterial.PadRight(32).Substring(0, 32));
        }

        private static bool TryParseBase64Key(string keyMaterial, out byte[] keyBytes)
        {
            keyBytes = Array.Empty<byte>();
            try
            {
                keyBytes = Convert.FromBase64String(keyMaterial);
                return keyBytes.Length > 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool TryFromBase64(string value, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            try
            {
                bytes = Convert.FromBase64String(value);
                return bytes.Length > 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static byte[] Clone(byte[] input)
        {
            var copy = new byte[input.Length];
            Buffer.BlockCopy(input, 0, copy, 0, input.Length);
            return copy;
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string? ErrorMessage)> TestConnectionAsync(string connectionString)
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Connection test failed");
                return (false, ex.Message);
            }
        }

        /// <inheritdoc/>
        public async Task<(bool Success, string? ErrorMessage)> TestConnectionByCodeAsync(string dbCode)
        {
            var connectionString = await GetConnectionStringAsync(dbCode);
            if (string.IsNullOrEmpty(connectionString))
            {
                return (false, $"Database with code '{dbCode}' not found or inactive");
            }

            return await TestConnectionAsync(connectionString);
        }
    }
}
