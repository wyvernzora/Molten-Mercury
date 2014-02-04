/*===================================================
 * LibHex2\\Core\\Range.cs
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
using libWyvernzora.IO;
namespace libWyvernzora
{
    public class Range : 
        IComparable<Range>, IEquatable<Range>
    {
        public Int64 Lower { get; set; }
        public Int64 Higher { get; set; }
        public Int64 Length
        {
            get
            {
                return Higher - Lower;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException();
                Higher = Lower + value;
            }
        }

        public Range()
        { Lower = 0; Higher= 0; }
        public Range(Int64 low, Int64 high)
        { Lower = low; Higher = high; }

        //IComparable<Range>
        public int CompareTo(Range other)
        {
            if (Lower > other.Lower) return 1;
            else if (Lower < other.Lower) return -1;
            else
            {
                if (Higher > other.Higher) return 1;
                else if (Higher < other.Higher) return -1;
                else return 0;
            }
        }

        //IEquatable<Range>
        public bool Equals(Range other)
        {
            return (Lower == other.Lower && Higher == other.Higher);
        }

        //IBinaryConverter<Range>
        public byte[] ToBytes(Range obj)
        {
            Byte[] res_buff = new Byte[16];
            HexTools.I64toHEX(Lower).CopyTo(res_buff, 0);
            HexTools.I64toHEX(Higher).CopyTo(res_buff, 8);
            return res_buff;
        }
        public Range FromBytes(Byte[] obj)
        {
            Range res_rng = new Range(0,0);
            Byte[] lower = new Byte[8];
            Byte[] higher = new Byte[8];
            Array.Copy(obj, lower, 8);
            Array.Copy(obj, 8, higher, 0, 8);

            res_rng.Lower = HexTools.HEXtoI64(lower);
            res_rng.Higher = HexTools.HEXtoI64(higher);
            return res_rng;
        }
        public void WriteToStream(Range obj, System.IO.Stream s)
        {
            StreamTools.WriteBytes(s, obj.ToBytes(this));
        }
        public Range ReadFromStream(System.IO.Stream s)
        {
            return this.FromBytes(StreamTools.ReadBytes(s, 16));
        }

        //Functions
        public Boolean HasIntersection(Range other)
        {
            if (this.Lower == other.Lower) return true;
            else if (this.Lower > other.Lower)
            {
                return other.Higher > this.Lower;
            }
            else
            {
                return this.Higher > other.Lower;
            }
        }
        public static Boolean operator *(Range lhs, Range rhs)
        {
            return lhs.HasIntersection(rhs);
        }
    }

    public class RangeCollection : ICollection<Range>
    {
        protected SortedDictionary<Int64, Range> m_data = new SortedDictionary<long, Range>();
        protected Boolean m_allowIntersection = false;
        public Boolean AllowIntersection
        { get { return m_allowIntersection; } set { m_allowIntersection = value; } }


        public void Add(Range item)
        {
            if (!AllowIntersection && HasIntersection(item))
                throw new ArgumentException("INTERSECTION");
            m_data.Add(item.Lower, item);
        }
        public void Clear()
        {
            m_data.Clear();
        }
        public bool Contains(Range item)
        {
            if (!m_data.ContainsKey(item.Lower)) return false;
            else return item.Equals(m_data[item.Lower]);
        }
        public void CopyTo(Range[] array, int arrayIndex)
        {
            Int32 t_pt = arrayIndex;
            foreach (Range r in m_data.Values)
            { array[t_pt] = r; t_pt++; if (t_pt == array.Length) break; }
        }
        public int Count
        {
            get { return m_data.Count; }
        }
        public bool IsReadOnly
        {
            get { return false; }
        }
        public bool Remove(Range item)
        {
            if (!m_data.ContainsKey(item.Lower)) return false;
            else if (m_data[item.Lower].Equals(item))
            { m_data.Remove(item.Lower); return true; }
            return false;
        }
        public IEnumerator<Range> GetEnumerator()
        {
            return m_data.Values.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_data.Values.GetEnumerator();
        }

        public Boolean HasIntersection(Range obj)
        {
            foreach (Range rng in m_data.Values)
            { if (rng * obj) return true; }
            return false;
        }
        public Boolean HasIntersection()
        {
            foreach (Range rng0 in m_data.Values)
            {
                foreach (Range rng1 in m_data.Values)
                {
                    if (rng0 * rng1) return true;
                }
            }
            return false;
        }

    }
}
