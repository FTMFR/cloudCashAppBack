using System.Collections.Generic;
using System.Security.Cryptography;

namespace BnpCashClaudeApp.Application.Interfaces
{
    /// <summary>
    /// Security policy service for approved cryptographic algorithms and key sizes.
    /// </summary>
    public interface ICryptographicAlgorithmPolicyService
    {
        /// <summary>
        /// Returns true when the key length is approved for AES operations.
        /// </summary>
        bool IsApprovedAesKeyLength(int keyLengthBits);

        /// <summary>
        /// Returns true when the key length is approved for HMAC operations.
        /// </summary>
        bool IsApprovedHmacKeyLength(int keyLengthBits);

        /// <summary>
        /// Returns true when the key length is approved for managed key lifecycle operations.
        /// </summary>
        bool IsApprovedManagedKeyLength(int keyLengthBits);

        /// <summary>
        /// Returns approved algorithm name for a managed key length or throws when unsupported.
        /// </summary>
        string ResolveManagedKeyAlgorithm(int keyLengthBits);

        /// <summary>
        /// Validates master key policy and throws when policy is violated.
        /// </summary>
        void ValidateMasterKey(byte[] masterKey);

        /// <summary>
        /// Validates integrity key policy and throws when policy is violated.
        /// </summary>
        void ValidateIntegrityKey(byte[] integrityKey);

        /// <summary>
        /// Creates HMAC instance for integrity operations according to policy.
        /// </summary>
        HMAC CreateIntegrityHmac(byte[] integrityKey);

        /// <summary>
        /// Creates AES-CBC instance for key wrapping according to policy.
        /// </summary>
        Aes CreateAesForKeyWrapping(byte[] masterKey, byte[]? iv = null);

        /// <summary>
        /// Gets the approved key lengths for managed key lifecycle operations.
        /// </summary>
        IReadOnlyCollection<int> GetApprovedManagedKeyLengths();
    }
}
