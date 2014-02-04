/*===================================================
 * LibHex2\\Core\\StreamTools.cs
 * -------------------------------------------------------------------------------
 * Copyright (C) Aragorn Wyvernzora 2011
 * -------------------------------------------------------------------------------
 *  This program is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * ===================================================
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace libWyvernzora
{
    /// <summary>
    /// Class that extends I/O ability of Stream-derived classes
    /// </summary>
    public static class StreamTools
    {
        public static Byte[] ReadBytes( Stream s, Int32 count)
        {
            Byte[] buffer = new Byte[count];
            s.Read(buffer, 0, count);
            return buffer;
        }
        public static void WriteBytes( Stream s, Byte[] src)
        { s.Write(src, 0, src.Length); }

        //Advanced reading functions that require explicit byte-sequence param
        public static SByte ReadSByte( Stream s)
        { return HexTools.Sign(Convert.ToByte(s.ReadByte())); }
        public static Int16 ReadI16( Stream s, BitSequence seq = BitSequence.LittleEndian)
        { return HexTools.HEXtoI16(ReadBytes(s, 2), seq); }
        public static UInt16 ReadU16(Stream s, BitSequence seq = BitSequence.LittleEndian)
        { return HexTools.HEXtoU16(ReadBytes(s,2), seq); }
        public static Int32 ReadI32(Stream s, BitSequence seq = BitSequence.LittleEndian)
        { return HexTools.HEXtoI32(ReadBytes(s,4), seq); }
        public static UInt32 ReadU32(Stream s, BitSequence seq = BitSequence.LittleEndian)
        { return HexTools.HEXtoU32(ReadBytes(s,4), seq); }
        public static Int64 ReadI64(Stream s, BitSequence seq = BitSequence.LittleEndian)
        { return HexTools.HEXtoI64(ReadBytes(s,8), seq); }
        public static UInt64 ReadU64(Stream s, BitSequence seq = BitSequence.LittleEndian)
        { return HexTools.HEXtoU64(ReadBytes(s,8), seq); }
        public static Single ReadF32(Stream s, BitSequence seq = BitSequence.LittleEndian)
        { return HexTools.HEXtoF32(ReadBytes(s,4), seq); }
        public static Double ReadF64(Stream s, BitSequence seq = BitSequence.LittleEndian)
        { return HexTools.HEXtoF64(ReadBytes(s,8), seq); }

        //Advanced writing functions that require eplicit byte-sequence param
        public static void WriteSByte( Stream s, SByte i)
        { s.WriteByte(HexTools.Unsign(i)); }
        public static void WriteI16(Stream s, Int16 i, BitSequence seq = BitSequence.LittleEndian)
        { s.Write(HexTools.I16toHEX(i, seq), 0, 2); }
        public static void WriteU16(Stream s, UInt16 i, BitSequence seq = BitSequence.LittleEndian)
        { s.Write(HexTools.U16toHEX(i, seq), 0, 2); }
        public static void WriteI32(Stream s, Int32 i, BitSequence seq = BitSequence.LittleEndian)
        { s.Write(HexTools.I32toHEX(i, seq), 0, 4); }
        public static void WriteU32(Stream s, UInt32 i, BitSequence seq = BitSequence.LittleEndian)
        { s.Write(HexTools.U32toHEX(i, seq), 0, 4); }
        public static void WriteI64(Stream s, Int64 i, BitSequence seq = BitSequence.LittleEndian)
        { s.Write(HexTools.I64toHEX(i, seq), 0, 8); }
        public static void WriteU64(Stream s, UInt64 i, BitSequence seq = BitSequence.LittleEndian)
        { s.Write(HexTools.U64toHEX(i, seq), 0, 8); }
        public static void WriteF32(Stream s, Single i, BitSequence seq = BitSequence.LittleEndian)
        { s.Write(HexTools.F32toHEX(i, seq), 0, 4); }
        public static void WriteF64(Stream s, Double i, BitSequence seq = BitSequence.LittleEndian)
        { s.Write(HexTools.F64toHEX(i, seq), 0, 8); }

        //String-related functions, basically I/O for ASCII and UTF-8 strings
        //TO BE FINISHED LATER
        public static String ReadASCII( Stream s, Int32 count)
        {
            return new string(Encoding.ASCII.GetChars(ReadBytes(s,count)));
        }
        public static Int32 WriteASCII( Stream s, String src)
        {
            Byte[] tmp = Encoding.ASCII.GetBytes(src);
            s.Write(tmp, 0, tmp.Length);
            return tmp.Length;
        }
        public static String ReadString( Stream s, Int32 count)
        { return new string(Encoding.UTF8.GetChars(ReadBytes(s,count))); }
        public static Int32 WriteString( Stream s, string src)
        {
            Byte[] tmp = Encoding.UTF8.GetBytes(src);
            s.Write(tmp, 0, tmp.Length);
            return tmp.Length;
        }
        public static void WriteStringWithMetadata( Stream s, String src)
        {
            Byte[] tmp = Encoding.UTF8.GetBytes(src);
            WriteU16(s, (UInt16)tmp.Length);
            s.Write(tmp, 0, tmp.Length);
        }
        public static String ReadStringWithMetadata( Stream s)
        {
            UInt16 count = ReadU16(s);
            return ReadString(s, count);
        }
        public static Int32 Peek( Stream s, Byte[] array, Int32 offset, Int32 count)
        {
            Int64 c_pos = s.Position;
            Int32 response = s.Read(array, offset, count);
            s.Position = c_pos;
            return response;
        }

        //Inter-stream I/O
        public static void WriteTo( Stream s, Stream d)
        {
            if (s.CanSeek) { s.Seek(0, SeekOrigin.Begin); }
            Byte[] buffer = new Byte[1000];
            while (true)
            {
                Int32 count = s.Read(buffer, 0, 1000);
                d.Write(buffer, 0, count);
                if (count < 1000)
                    break;
            }
        }
        public static void WriteFrom( Stream s, Stream d)
        {
            if (d.CanSeek) { d.Seek(0, SeekOrigin.Begin); }
            Byte[] buffer = new Byte[1000];
            while (true)
            {
                Int32 count = d.Read(buffer, 0, 1000);
                s.Write(buffer, 0, count);
                if (count < 1000) break;
            }
        }
    }
}
