namespace OpenVsixSignTool.Core
{
    /// <summary>
    /// Sets the mode of the package when opened.
    /// </summary>
    public enum OpcPackageFileMode
    {
        /// <summary>
        /// The package will be opened in read-only mode.
        /// </summary>
        Read,

        /// <summary>
        /// The package will be opened for reading and writing.
        /// </summary>
        ReadWrite
    }
}
