using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace libWyvernzora.IO
{
    public class ExPartStream : ExStreamBase
    {
        //Operational Variables
        protected ExStreamBase _baseStream;
        protected Int64 _startPosition = 0;
        protected Int64 _length = 0;
        protected Int64 _position = 0;
        protected FileAccess _access = FileAccess.ReadWrite;

        //Constructors
        public ExPartStream(ExStreamBase b, Int64 start, Int64 len, FileAccess ac = FileAccess.ReadWrite)
        {
            if (len < 0) { throw new ArgumentOutOfRangeException(); }
            if (start < 0) { throw new ArgumentOutOfRangeException(); }
            _baseStream = b;
            _startPosition = start;
            _length = len;
            _access = ac;
        }
        public ExPartStream(ExStreamBase b, Range rng, FileAccess ac= FileAccess.ReadWrite)
        {
            _baseStream = b;
            _startPosition = rng.Lower;
            _length = rng.Higher - rng.Lower;
            _access = ac;
        }

        //Position management support
        protected void RefreshPosition()
        {
            //no validation logic, everything's done on site
            _baseStream.Position = _startPosition + _position;
        }

        #region System.IO.Stream Members
        public override bool CanRead
        {
            get { return _baseStream.CanRead; }
        }
        public override bool CanSeek
        {
            get { return _baseStream.CanSeek; }
        }
        public override bool CanWrite
        {
            get { return _baseStream.CanWrite; }
        }
        public override void Flush()
        {
            _baseStream.Flush();
        }
        public override long Length
        {
            get { return _length; }
        }
        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                if (value < 0) { throw new ArgumentOutOfRangeException(); }
                _position = value;
                RefreshPosition();
            }
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_access == FileAccess.Write) throw new InvalidOperationException();
            RefreshPosition();
            if (count > buffer.Length) { throw new ArgumentOutOfRangeException(); }
            if (offset > buffer.Length - count) { throw new ArgumentOutOfRangeException(); }

            if (_position + count <= _length)
            {
                _baseStream.Read(buffer, offset, count);
                _position += count;
                RefreshPosition();
                return count;
            }
            else
            {
                Int32 c = (int)(_length - _position);
                _baseStream.Read(buffer, 0, c);
                _position += c;
                RefreshPosition();
                return c;
            }
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            RefreshPosition();
            Int64 t_offset = 0;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    t_offset = offset;
                    break;
                case SeekOrigin.Current:
                    t_offset = _position + offset;
                    break;
                case SeekOrigin.End:
                    t_offset = _length - offset;
                    break;
            }
            if (t_offset < 0) { throw new ArgumentOutOfRangeException(); }
            _position = t_offset;
            RefreshPosition();
            return t_offset;
        }
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_access == FileAccess.Read) throw new InvalidOperationException();
            RefreshPosition();
            if (offset < 0 || count < 0) { throw new ArgumentOutOfRangeException(); }
            if (count + offset > buffer.Length) { throw new ArgumentOutOfRangeException(); }
            if (_position + count > _length)
            { throw new NotSupportedException("ExStream does not support length expansion !"); }
            _baseStream.Write(buffer, offset, count);
            _position += count;
            RefreshPosition();
        }
        #endregion
    }
}
