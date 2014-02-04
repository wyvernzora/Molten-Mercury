using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace libWyvernzora.IO
{
    public abstract class ExStreamBase : Stream
    {
        //Base Stream
        protected Stream m_base;
        
        //Concrete Functions
        public Byte[] ReadBytes(Int32 count)
        {
            Byte[] t_b = new Byte[count];
            this.Read(t_b, 0, count);
            return t_b;
        }
        public new Byte ReadByte()
        {
            Byte[] t_b = ReadBytes(1);
            return t_b[0];
        }
        public SByte ReadSByte()
        { return HexTools.Sign(ReadByte()); }
        public UInt16 ReadUShort(BitSequence seq = BitSequence.LittleEndian)
        { return HexTools.HEXtoU16(ReadBytes(2), seq); }
        public Int16 ReadShort(BitSequence seq = BitSequence.LittleEndian)
        { return HexTools.HEXtoI16(ReadBytes(2), seq); }
        public UInt32 ReadUInt(BitSequence seq = BitSequence.LittleEndian)
        { return HexTools.HEXtoU32(ReadBytes(4), seq); }
        public Int32 ReadInt(BitSequence seq = BitSequence.LittleEndian)
        { return HexTools.HEXtoI32(ReadBytes(4), seq); }
        public UInt64 ReadULong(BitSequence seq = BitSequence.LittleEndian)
        { return HexTools.HEXtoU64(ReadBytes(8), BitSequence.LittleEndian); }
        public Int64 ReadLong(BitSequence seq = BitSequence.LittleEndian)
        { return HexTools.HEXtoI64(ReadBytes(8), seq); }

        public void WriteBytes(Byte[] b)
        { Write(b, 0, b.Length); }
        public void WriteSByte(SByte b)
        { WriteByte(HexTools.Unsign(b)); }
        public void WriteUShort(UInt16 b, BitSequence seq = BitSequence.LittleEndian)
        { Write(HexTools.U16toHEX(b, seq), 0, 2); }
        public void WriteShort(Int16 b, BitSequence seq = BitSequence.LittleEndian)
        { Write(HexTools.I16toHEX(b, seq), 0, 2); }
        public void WriteUInt(UInt32 b, BitSequence seq = BitSequence.LittleEndian)
        { Write(HexTools.U32toHEX(b, seq), 0, 4); }
        public void WriteInt(Int32 b, BitSequence seq = BitSequence.LittleEndian)
        { Write(HexTools.I32toHEX(b, seq), 0, 4); }
        public void WriteULong(UInt64 b, BitSequence seq = BitSequence.LittleEndian)
        { Write(HexTools.U64toHEX(b, seq), 0, 8); }
        public void WriteLong(Int64 b, BitSequence seq = BitSequence.LittleEndian)
        { Write(HexTools.I64toHEX(b, seq), 0, 8); }

        public void WriteTo(Stream dest, int buffer = 0x1000)
        {
            if (this.CanSeek) this.Seek(0, SeekOrigin.Begin);
            Byte[] BUFFER;
            while (true)
            {
                BUFFER = new Byte[buffer];
                Int32 READ_COUNT = Read(BUFFER, 0, buffer);
                dest.Write(BUFFER, 0, READ_COUNT);
                if (READ_COUNT < buffer) break;
            }
        }

        public override void Close()
        { if (m_base != null) m_base.Close(); }
        public new void Dispose()
        { if (m_base != null)  m_base.Close(); }
    }
}
