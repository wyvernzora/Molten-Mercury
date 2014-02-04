/*===================================================
 * LibHex2\\IO\\Packaging\\PackageContinuous.cs
 * -------------------------------------------------------------------------------
 * Copyright (C) Aragorn Wyvernzora 2011
 *      MODIFIED FROM Firefly.Core.Packaging.PackageContinuous class
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

namespace libWyvernzora.IO.Packaging
{
    /// <summary>
    /// Continuous Package Class
    /// Usually applied to the packages where file info and data must be in the same order.
    /// If it is not neccessary to maintain the order, please consider using PackageDiscrete instead.
    /// Current implementation assumes that the indexer is at the beginning of the file, thus it doesn't need backup features.
    /// If not so, some behaviour of this class should be modified.
    /// </summary>
    public abstract class PackageContinuous : ReadonlyPackageBase
    {
        /// <summary>
        /// Sorted list of entries
        /// </summary>
        protected SortedList<PackageEntry, Int64> m_sortedEntries = new SortedList<PackageEntry, long>(new PackageEntryAddressComparer());
        /// <summary>
        /// Progress notification delegate
        /// </summary>
        public Action<Double> NotifyProgress { get; set; }


        public abstract Int64 GetEntryLengthInPhysicalFileDB(PackageEntry entry);
        public abstract void SetEntryLengthInPhysicalFileDB(PackageEntry entry, Int64 value);
        /// <summary>
        /// Returns the physical space occupied by a file.
        /// e.g. if alignment is 0x800, then it will return ((len + 0x800 - 1) / 0x800) * 0x800
        /// </summary>
        protected abstract Int64 GetSpace(Int64 len);

        protected PackageContinuous() : base() { }
        public PackageContinuous(ExStream exs)
            : base(exs) { }

        protected override void PushEntry(PackageEntry f, PackageEntry dir)
        {
            base.PushEntry(f, dir);
            m_sortedEntries.Add(f, f.Offset);
        }
    }
}
