/*===================================================
 * LibHex2\\Core\\HexTools.cs
 *      This class was introduced as an alternative to the BitConverter class.
 *      The main edge over the BitConverter is that HexTools supports BE and LE.
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
using System.Runtime.InteropServices;

namespace libWyvernzora
{
    public enum BitSequence
    { LittleEndian = 0, BigEndian = 1 }

    public static class HexTools
    {
        //Signed <-> Unsigned
       public static SByte Sign(Byte i)
       { return (SByte)i; }
       public static Int16 Sign(UInt16 i)
       { return (Int16)i; }
       public static Int32 Sign(UInt32 i)
       { return (Int32)i; }
       public static Int64 Sign(UInt64 i)
       { return (Int64)i; }
       public static Byte Unsign(SByte i)
       { return (Byte)i; }
       public static UInt16 Unsign(Int16 i)
       { return (UInt16)i; }
       public static UInt32 Unsign(Int32 i)
       { return (UInt32)i; }
       public static UInt64 Unsign(Int64 i)
       { return (UInt64)i; }

        //Decimal -> Hexadecimalo
        [StructLayout(LayoutKind.Explicit)]
        private struct MidSingle
        {
            [FieldOffset(0)]
            public UInt32 intValue;
            [FieldOffset(0)]
            public Single singleValue;
            public static MidSingle Init()
            { MidSingle m; m.intValue = 0; m.singleValue = 0; return m; }
        }
        [StructLayout(LayoutKind.Explicit)]
        private struct MidDouble
        {
            [FieldOffset(0)]
            public UInt64 intValue;
            [FieldOffset(0)]
            public Double doubleValue;
            public static MidDouble Init()
            { MidDouble m; m.intValue = 0; m.doubleValue = 0; return m; }
        }

        public static Byte[] U16toHEX(UInt16 src, BitSequence seq = BitSequence.LittleEndian)
        {
            Byte[] result = new Byte[2];
            result[1] = Convert.ToByte(src & 0xFF);
            result[0] = Convert.ToByte((src >> 8) & 0xFF);

            if (seq == BitSequence.LittleEndian)
            { Array.Reverse(result); }
            return result;
        }
        public static Byte[] I16toHEX(Int16 src, BitSequence seq = BitSequence.LittleEndian)
        { return U16toHEX((UInt16)src, seq); }
        public static Byte[] U32toHEX(UInt32 src, BitSequence seq = BitSequence.LittleEndian)
        {
            Byte[] result = new Byte[4];
            for (int i = 0; i < 4; i++)
            { result[3 - i] = Convert.ToByte(src & 0xFF); src >>= 8; }

            if (seq == BitSequence.LittleEndian)
            { Array.Reverse(result); }
            return result;
        }
        public static Byte[] I32toHEX(Int32 src, BitSequence seq = BitSequence.LittleEndian)
        { return U32toHEX(Unsign(src), seq); }
        public static Byte[] U64toHEX(UInt64 src, BitSequence seq = BitSequence.LittleEndian)
        {
            Byte[] result = new Byte[8];
            for (int i = 0; i < 8; i++)
            { result[7 - i] = Convert.ToByte(src & 0xFF); src >>= 8; }

            if (seq == BitSequence.LittleEndian)
            { Array.Reverse(result); }
            return result;
        }
        public static Byte[] I64toHEX(Int64 src, BitSequence seq = BitSequence.LittleEndian)
        { return U64toHEX(Unsign(src), seq); }
        public static Byte[] F32toHEX(Single src, BitSequence seq = BitSequence.LittleEndian)
        {
            MidSingle m = MidSingle.Init();
            m.singleValue = src;
            return U32toHEX(m.intValue, seq);
        }
        public static Byte[] F64toHEX(Double src, BitSequence seq = BitSequence.LittleEndian)
        {
            MidDouble m = MidDouble.Init();
            m.doubleValue = src;
            return U64toHEX(m.intValue, seq);
        }

        //Hexadecimal -> Decimal
        public static UInt16 HEXtoU16(Byte[] src, BitSequence seq = BitSequence.LittleEndian)
        {
            if (src.Length != 2)
            { throw new ArgumentOutOfRangeException("LibHex.Core.HexTools.HEXtoU16 : Source array must contain 2 elements."); }
            if (seq == BitSequence.LittleEndian) { Array.Reverse(src); }

            UInt16 result = src[1];
            result |= Convert.ToUInt16(Convert.ToUInt16(src[0]) << 8);

            return result;
        }
        public static Int16 HEXtoI16(Byte[] src, BitSequence seq = BitSequence.LittleEndian)
        {
            if (src.Length != 2)
            { throw new ArgumentOutOfRangeException("LibHex.Core.HexTools.HEXtoI16 : Source array must contain 2 elements."); }
            return Sign(HEXtoU16(src, seq));
        }
        public static UInt32 HEXtoU32(Byte[] src, BitSequence seq = BitSequence.LittleEndian)
        {
            if (src.Length != 4)
            { throw new ArgumentOutOfRangeException("LibHex.Core.HexTools.HEXtoU32 : Source array must contain 4 elements."); }
            if (seq == BitSequence.LittleEndian) { Array.Reverse(src); }
            UInt32 result = src[3];
            for (int i = 1; i < 4; i++)
            { result |= Convert.ToUInt32(Convert.ToUInt32(src[3 - i]) << (i * 8)); }
            return result;
        }
        public static Int32 HEXtoI32(Byte[] src, BitSequence seq = BitSequence.LittleEndian)
        {
            if (src.Length != 4)
            { throw new ArgumentOutOfRangeException("LibHex.Core.HexTools.HEXtoI32 : Source array must contain 4 elements."); }
            return Sign(HEXtoU32(src, seq));
        }
        public static UInt64 HEXtoU64(Byte[] src, BitSequence seq = BitSequence.LittleEndian)
        {
            if (src.Length != 8)
            { throw new ArgumentOutOfRangeException("LibHex.Core.HexTools.HEXtoU64 : Source array must contain 8 elements."); }
            if (seq == BitSequence.LittleEndian) { Array.Reverse(src); }

            UInt64 result = src[7];
            for (int i = 1; i < 8; i++)
            { result |= Convert.ToUInt64(Convert.ToUInt64(src[7 - i]) << (i * 8)); }

            return result;
        }
        public static Int64 HEXtoI64(Byte[] src, BitSequence seq = BitSequence.LittleEndian)
        {
            if (src.Length != 8)
            { throw new ArgumentOutOfRangeException("LibHex.Core.HexTools.HEXtoI64 : Source array must contain 8 elements."); }
            return Sign(HEXtoU64(src, seq));
        }
        public static Single HEXtoF32(Byte[] src, BitSequence seq = BitSequence.LittleEndian)
        {
            if (src.Length != 4)
            { throw new ArgumentOutOfRangeException("LibHex.Core.HexTools.HEXtoF32 : Source array must contain 4 elements."); }
            MidSingle m = MidSingle.Init();
            m.intValue = HEXtoU32(src, seq);
            return m.singleValue;
        }
        public static Double HEXtoF64(Byte[] src, BitSequence seq = BitSequence.LittleEndian)
        {
            if (src.Length != 8)
            { throw new ArgumentOutOfRangeException("LibHex.Core.HexTools.HEXtoF64 : Source array must contain 8 elements."); }
            MidDouble m = MidDouble.Init();
            m.intValue = HEXtoU64(src, seq);
            return m.doubleValue;
        }

        //Other stuff
        //String -> Hexadecimal | Decimal -> String
        public static Byte[] String2Byte(String src)
        {
            while (src.Length % 2 != 0) { src = "0" + src; }
            Byte[] result = new Byte[src.Length / 2];
            for (int i = 0; i < (src.Length / 2); i++)
            { result[i] = Convert.ToByte(src.Substring(i * 2, 2), 16); }
            return result;
        }
        public static String Byte2String(Byte[] src)
        {
            string s = String.Empty;
            foreach (byte b in src)
            { s += ToHexString(b, 2); }
            return s;
        }
        //public static ExArrayString<Byte> String2Byte(String src)
        //{
        //    while (src.Length % 2 != 0) { src = "0" + src; }
        //    ExArrayString<Byte> result = new ExArrayString<Byte>(src.Length / 2);
        //    for (int i = 0; i < (src.Length / 2); i++)
        //    { result[i] = Convert.ToByte(src.Substring(i * 2, 2), 16); }
        //    return result;
        //}
        public static String ToHexString(Int64 src, Int32 padding)
        {
            string r = Convert.ToString(src, 16).ToUpper();
            while (r.Length < padding) r = "0" + r;
            return r;
        }
    }
}