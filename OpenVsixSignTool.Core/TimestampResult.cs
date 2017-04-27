namespace OpenVsixSignTool
{
    /// <summary>
    /// Indicates the status of the timestamp operation.
    /// </summary>
    public enum TimestampResult
    {
        /// <summary>
        /// The timestamp operation was a success.
        /// </summary>
        Success = 1,

        /// <summary>
        /// The package could not be timestamped because it does not have an existing signature.
        /// </summary>
        PackageNotSigned = 2,

        /// <summary>
        /// The timestamp operation failed.
        /// </summary>
        Failed = 3
    }
}
