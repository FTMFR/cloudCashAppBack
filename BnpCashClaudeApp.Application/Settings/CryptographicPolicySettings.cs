namespace BnpCashClaudeApp.Application.Settings
{
    /// <summary>
    /// Configurable cryptographic policy used to enforce approved algorithms and key lengths.
    /// </summary>
    public class CryptographicPolicySettings
    {
        /// <summary>
        /// Approved AES key lengths for encryption operations.
        /// </summary>
        public int[] ApprovedAesKeyLengthsBits { get; set; } = { 128, 192, 256 };

        /// <summary>
        /// Approved HMAC key lengths for integrity/signature operations.
        /// </summary>
        public int[] ApprovedHmacKeyLengthsBits { get; set; } = { 256, 384, 512 };

        /// <summary>
        /// Minimum key length accepted for integrity protection key material.
        /// </summary>
        public int MinimumIntegrityKeyLengthBits { get; set; } = 256;

        /// <summary>
        /// Required master key length for key wrapping/encryption operations.
        /// </summary>
        public int RequiredMasterKeyLengthBits { get; set; } = 256;

        /// <summary>
        /// Algorithm used for integrity HMAC.
        /// Allowed values: HMACSHA256, HMACSHA384, HMACSHA512.
        /// </summary>
        public string IntegrityHmacAlgorithm { get; set; } = "HMACSHA256";
    }
}
