using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenVsixSignTool.Core.Interop
{
    internal static class Mssign32
    {
        [method: DllImport("mssign32.dll", CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Error)]
        public static extern int SignerTimeStampEx2(
            //note: this parameter is famously misdocumented on MSDN.
            //[param: In, MarshalAs(UnmanagedType.LPStr)] string algorithm,


        );
    }

    [type: StructLayout(LayoutKind.Sequential)]
    internal struct SIGNER_SUBJECT_INFO
    {
        public uint cbSize;
        public IntPtr pdwIndex;
        public SubjectChoice dwSubjectChoice;
        public SIGNER_SUBJECT_INFO_UNION signerInfoUnion;

    }

    [type: StructLayout(LayoutKind.Explicit)]
    internal struct SIGNER_SUBJECT_INFO_UNION
    {
        [field: FieldOffset(0)]
        public IntPtr pSignerFileInfo;
        [field: FieldOffset(0)]
        public IntPtr pSignerBlobInfo;
    }



    internal enum SubjectChoice : uint
    {
        SIGNER_SUBJECT_BLOB = 0x02,
        SIGNER_SUBJECT_FILE = 0x01
    }
}
