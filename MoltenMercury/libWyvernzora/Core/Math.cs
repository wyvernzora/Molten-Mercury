/*===================================================
 * LibHex2\\Core\\Math.cs
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

namespace libWyvernzora
{
    public static class Math
    {
        public static T Min<T>(T a, T b) where T : IComparable<T>
        { return (a.CompareTo(b) > 0) ? b : a; }
        public static T Min<T>(IEnumerable<T> a) where T : IComparable<T>
        {
            T res = default(T);
            foreach (T item in a)
            { if (res.Equals(null)) res = item; res = (res.CompareTo(item) > 0) ? item : res; }
            return res;
        }
        public static T Min<T>(params T[] a) where T : IComparable<T>
        {
            T res = default(T);
            foreach (T item in a)
            { if (res.Equals(null)) res = item; res = (res.CompareTo(item) > 0) ? item : res; }
            return res;
        }
        public static T Max<T>(T a, T b) where T : IComparable<T>
        { return (a.CompareTo(b) > 0) ? a : b; }
        public static T Max<T>(IEnumerable<T> a) where T : IComparable<T>
        {
            T res = default(T);
            foreach (T i in a)
            { if (res.Equals(null)) res = i; res = (res.CompareTo(i) > 0) ? res : i; }
            return res;
        }
        public static T Max<T>(params T[] a) where T : IComparable<T>
        {
            T res = default(T);
            foreach (T i in a)
            { if (res.Equals(null)) res = i; res = (res.CompareTo(i) > 0) ? res : i; }
            return res;
        }
        public static void Swap<T>(T a, T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }
    }
}
