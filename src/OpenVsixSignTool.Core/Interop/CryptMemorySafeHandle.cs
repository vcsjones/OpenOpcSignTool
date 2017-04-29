namespace OpenVsixSignTool.Core.Interop
{
    internal sealed class CryptMemorySafeHandle : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        public CryptMemorySafeHandle(bool ownsHandle) : base(ownsHandle)
        {
        }

        public CryptMemorySafeHandle() : this(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Crypt32.CryptMemFree(handle);
            return true;
        }
    }
}
