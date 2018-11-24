namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// Indicicates a signing algorithm.
    /// </summary>
    public enum SigningAlgorithm
    {
        /// <summary>
        /// The signing algorithm is unknown.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The signing algorithm is RSA.
        /// </summary>
        RSA,

        /// <summary>
        /// The signing algorithm is ECDSA.
        /// </summary>
        ECDSA
    }
}