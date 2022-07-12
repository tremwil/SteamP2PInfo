using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SteamP2PInfo
{
    public class ReverseTextReader
    {
        private const int LineFeedLf = 10;
        private const int LineFeedCr = 13;
        private readonly Stream _stream;
        private readonly Encoding _encoding;

        public bool EndOfStream => _stream.Position == 0;

        public ReverseTextReader(Stream stream, Encoding encoding)
        {
            _stream = stream;
            _encoding = encoding;
            _stream.Position = _stream.Length;
        }

        public string ReadLine()
        {
            if (_stream.Position == 0) return null;

            var line = new List<byte>();
            var endOfLine = false;
            while (!endOfLine)
            {
                var b = _stream.ReadByteFromBehind();

                if (b == -1 || b == LineFeedLf)
                {
                    endOfLine = true;
                }
                line.Add(Convert.ToByte(b));
            }

            line.Reverse();
            return _encoding.GetString(line.ToArray());
        }
    }

    public static class StreamExtensions
    {
        public static int ReadByteFromBehind(this Stream stream)
        {
            if (stream.Position == 0) return -1;

            stream.Position = stream.Position - 1;
            var value = stream.ReadByte();
            stream.Position = stream.Position - 1;
            return value;
        }
    }
}
