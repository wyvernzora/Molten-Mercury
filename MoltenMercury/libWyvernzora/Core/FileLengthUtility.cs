/*===================================================
 * LibHex2\\Core\\FileLengthUtility.cs
 * -------------------------------------------------------------------------------
 * Copyright (C) Aragorn Wyvernzora 2011
 *      PARTIALLY MODIFIED FROM Firefly.Core.Packaging.PackageBase class
 *      ORIGINALLY WRITTEN BY F.R.C IN 2010
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
    public static class FileLengthUtility
    {
        public static long[] GetAddressSummation(long baseAddr, long[] lengths)
        {
            long[] ret = new long[lengths.Length];
            long addr = baseAddr;
            for (int n = 0; n < lengths.Length; n++)
            {
                if (lengths[n] == 0) ret[n] = 0;
                else { ret[n] = addr; addr += lengths[n]; }
            }
            return ret;
        }

        public static String GetFileLengthString(long length)
        {
            if (length < 1024) //b
            {
                return length.ToString() + ((length == 1) ? " Byte" : " Bytes");
            }
            else if (length >= 1024 && length < System.Math.Pow(1024, 2)) //kb
            {
                decimal ln = System.Math.Round((decimal)(length / 1024));
                return ln.ToString() + " KiB";
            }
            else if (length >= System.Math.Pow(1024, 2) && length < System.Math.Pow(1024, 3)) //mb
            {
                decimal ln = System.Math.Round((decimal)(length / System.Math.Pow(1024, 2)));
                return ln.ToString() + " MiB";
            }
            else if (length >= System.Math.Pow(1024, 3) && length < System.Math.Pow(1024, 4)) //gb
            {
                decimal ln = System.Math.Round((decimal)(length / System.Math.Pow(1024, 3)));
                return ln.ToString() + " GiB";
            }
            else if (length >= System.Math.Pow(1024, 4) && length < System.Math.Pow(1024, 5)) //tb
            {
                decimal ln = System.Math.Round((decimal)(length / System.Math.Pow(1024, 4)));
                return ln.ToString() + " TiB";
            }
            else throw new Exception("Greater numbers not supported...yet");
        }
    }
}
