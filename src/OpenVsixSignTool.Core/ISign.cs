using System;
using System.Security.Cryptography;

namespace OpenVsixSignTool.Core
{
    internal interface ISign : IDisposable
    {
        byte[] SignDigest(byte[] digest, HashAlgorithmName pkcsAlgorithm);
        bool VerifyDigest(byte[] digest, byte[] signature, HashAlgorithmName pkcsAlgorithm);
    }
}