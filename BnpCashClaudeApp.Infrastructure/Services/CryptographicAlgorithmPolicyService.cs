using BnpCashClaudeApp.Application.Interfaces;
using BnpCashClaudeApp.Application.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace BnpCashClaudeApp.Infrastructure.Services
{
    /// <summary>
    /// Central policy service to enforce approved cryptographic algorithms and key sizes.
    /// </summary>
    public class CryptographicAlgorithmPolicyService : ICryptographicAlgorithmPolicyService
    {
        private readonly ILogger<CryptographicAlgorithmPolicyService> _logger;
        private readonly HashSet<int> _approvedAesKeyLengths;
        private readonly HashSet<int> _approvedHmacKeyLengths;
        private readonly HashSet<int> _approvedManagedKeyLengths;
        private readonly int _minimumIntegrityKeyLengthBits;
        private readonly int _requiredMasterKeyLengthBits;
        private readonly string _integrityHmacAlgorithm;

        public CryptographicAlgorithmPolicyService(
            IOptions<CryptographicPolicySettings> options,
            ILogger<CryptographicAlgorithmPolicyService> logger)
        {
            _logger = logger;

            var settings = options?.Value ?? new CryptographicPolicySettings();

            _approvedAesKeyLengths = BuildPolicySet(
                settings.ApprovedAesKeyLengthsBits,
                new[] { 128, 192, 256 });

            _approvedHmacKeyLengths = BuildPolicySet(
                settings.ApprovedHmacKeyLengthsBits,
                new[] { 256, 384, 512 });

            _approvedManagedKeyLengths = _approvedAesKeyLengths
                .Concat(_approvedHmacKeyLengths)
                .ToHashSet();

            _minimumIntegrityKeyLengthBits = settings.MinimumIntegrityKeyLengthBits > 0
                ? settings.MinimumIntegrityKeyLengthBits
                : 256;

            _requiredMasterKeyLengthBits = settings.RequiredMasterKeyLengthBits > 0
                ? settings.RequiredMasterKeyLengthBits
                : 256;

            _integrityHmacAlgorithm = NormalizeAlgorithmName(settings.IntegrityHmacAlgorithm);
        }

        public bool IsApprovedAesKeyLength(int keyLengthBits)
        {
            return _approvedAesKeyLengths.Contains(keyLengthBits);
        }

        public bool IsApprovedHmacKeyLength(int keyLengthBits)
        {
            return _approvedHmacKeyLengths.Contains(keyLengthBits);
        }

        public bool IsApprovedManagedKeyLength(int keyLengthBits)
        {
            return _approvedManagedKeyLengths.Contains(keyLengthBits);
        }

        public string ResolveManagedKeyAlgorithm(int keyLengthBits)
        {
            if (_approvedAesKeyLengths.Contains(keyLengthBits))
            {
                return $"AES-{keyLengthBits}";
            }

            if (_approvedHmacKeyLengths.Contains(keyLengthBits))
            {
                return keyLengthBits switch
                {
                    256 => "HMAC-SHA256",
                    384 => "HMAC-SHA384",
                    512 => "HMAC-SHA512",
                    _ => throw new InvalidOperationException(
                        $"Approved HMAC key length '{keyLengthBits}' has no algorithm mapping.")
                };
            }

            throw new InvalidOperationException(
                $"Key length '{keyLengthBits}' is not approved by cryptographic policy. Approved values: {string.Join(", ", GetApprovedManagedKeyLengths())}");
        }

        public void ValidateMasterKey(byte[] masterKey)
        {
            if (masterKey == null || masterKey.Length == 0)
            {
                throw new InvalidOperationException("Master key is null or empty.");
            }

            int keyLengthBits = masterKey.Length * 8;

            if (keyLengthBits != _requiredMasterKeyLengthBits)
            {
                throw new InvalidOperationException(
                    $"Master key length must be exactly {_requiredMasterKeyLengthBits} bits. Current: {keyLengthBits} bits.");
            }

            if (!IsApprovedAesKeyLength(keyLengthBits))
            {
                throw new InvalidOperationException(
                    $"Master key length '{keyLengthBits}' is not approved for AES policy.");
            }
        }

        public void ValidateIntegrityKey(byte[] integrityKey)
        {
            if (integrityKey == null || integrityKey.Length == 0)
            {
                throw new InvalidOperationException("Integrity key is null or empty.");
            }

            int keyLengthBits = integrityKey.Length * 8;

            if (keyLengthBits < _minimumIntegrityKeyLengthBits)
            {
                throw new InvalidOperationException(
                    $"Integrity key length must be at least {_minimumIntegrityKeyLengthBits} bits. Current: {keyLengthBits} bits.");
            }

            if (!IsApprovedHmacKeyLength(keyLengthBits))
            {
                _logger.LogWarning(
                    "Integrity key length {KeyLengthBits} is not in approved HMAC key-length list ({ApprovedList}) but accepted because it meets minimum policy.",
                    keyLengthBits,
                    string.Join(", ", _approvedHmacKeyLengths.OrderBy(x => x)));
            }
        }

        public HMAC CreateIntegrityHmac(byte[] integrityKey)
        {
            ValidateIntegrityKey(integrityKey);

            return _integrityHmacAlgorithm switch
            {
                "HMACSHA256" => new HMACSHA256(integrityKey),
                "HMACSHA384" => new HMACSHA384(integrityKey),
                "HMACSHA512" => new HMACSHA512(integrityKey),
                _ => throw new InvalidOperationException(
                    $"Unsupported integrity HMAC algorithm policy: {_integrityHmacAlgorithm}")
            };
        }

        public Aes CreateAesForKeyWrapping(byte[] masterKey, byte[]? iv = null)
        {
            ValidateMasterKey(masterKey);

            var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = masterKey;
            aes.KeySize = masterKey.Length * 8;

            if (iv == null || iv.Length == 0)
            {
                aes.GenerateIV();
            }
            else
            {
                if (iv.Length != aes.BlockSize / 8)
                {
                    aes.Dispose();
                    throw new InvalidOperationException(
                        $"IV length must be exactly {aes.BlockSize / 8} bytes for AES-CBC.");
                }

                aes.IV = iv;
            }

            return aes;
        }

        public IReadOnlyCollection<int> GetApprovedManagedKeyLengths()
        {
            return _approvedManagedKeyLengths.OrderBy(x => x).ToArray();
        }

        private static HashSet<int> BuildPolicySet(int[]? configuredValues, int[] defaultValues)
        {
            var values = (configuredValues == null || configuredValues.Length == 0)
                ? defaultValues
                : configuredValues;

            return values
                .Where(v => v > 0)
                .Distinct()
                .ToHashSet();
        }

        private static string NormalizeAlgorithmName(string? algorithmName)
        {
            if (string.IsNullOrWhiteSpace(algorithmName))
            {
                return "HMACSHA256";
            }

            return algorithmName
                .Trim()
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .ToUpperInvariant();
        }
    }
}
