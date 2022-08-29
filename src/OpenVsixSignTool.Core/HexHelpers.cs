using System;

namespace OpenVsixSignTool.Core
{
    internal static class HexHelpers
    {
        private static ReadOnlySpan<byte> LookupTable => new byte[]
        {
            (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4',
            (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9',
            (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e',
            (byte)'f',
        };

        public static bool TryHexEncode(ReadOnlySpan<byte> data, Span<char> buffer)
        {
            var charsRequired = data.Length * 2;
            if (buffer.Length < charsRequired)
            {
                return false;
            }
            for (int i = 0, j = 0; i < data.Length; i++, j += 2)
            {
                var value = data[i];
                buffer[j] = (char)LookupTable[(value & 0xF0) >> 4];
                buffer[j+1] = (char)LookupTable[value & 0x0F];
            }
            return true;
        }
    }
}
