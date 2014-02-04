/*===================================================
 * LibHex2\\IO\\Packaging\\PackageEntry.cs
 * -------------------------------------------------------------------------------
 * Copyright (C) Aragorn Wyvernzora 2011
 *      MODIFIED FROM THE Firefly.Core.Packaging.FileDB CLASS
 *      ORIGINALLY WRITTEN BY F.R.C. IN 2010
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
    public class PackageEntry
    {
        public enum EntryType
        { File = 0, Directory = 1 }

        protected String m_name;
        protected EntryType m_type = EntryType.File;
        protected Int64 m_len;
        protected Int64 m_ofs;
        protected String m_title;
        protected Dictionary<String, Object> m_additional = new Dictionary<string, Object>(StringComparer.CurrentCultureIgnoreCase);

        public Object GetAdditinalProperty(String name)
        {
            if (m_additional.ContainsKey(name))
                return m_additional[name];
            else return null;
        }
        public void SetAdditionalProperty(String name, Object value)
        {
            if (m_additional.ContainsKey(name))
                m_additional.Remove(name);
            m_additional.Add(name, value);
        }

        public virtual String Name
        { get { return m_name; } set { m_name = value; } }
        public virtual EntryType Type
        { get { return m_type; } }
        public virtual Int64 Offset
        { get { return m_ofs; } set { m_ofs = value; } }
        public virtual Int64 Length
        { get { return m_len; } set { m_len =  value; } }
        public virtual String Title
        { get { return m_title; } set { m_title = value; } }

        public PackageEntry Parent { get; set; }
        public List<PackageEntry> SubEntries { get; set; }
        public Dictionary<String, PackageEntry> SubEntriesMap { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PackageEntry()
        {
            SubEntriesMap = new Dictionary<string, PackageEntry>(StringComparer.CurrentCultureIgnoreCase);
            SubEntries = new List<PackageEntry>();
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PackageEntry(String name, EntryType type, Int64 len, Int64 ofs, String title = "")
            : this()
        {
            m_name = name;
            m_type = type;
            m_len = len;
            m_ofs = ofs;
            m_title = title;
        }

        public String Path
        {
            get
            {
                PackageEntry parent = Parent;
                String ret = m_name;
                while (parent != null)
                {
                    ret = libWyvernzora.Path.GetPath(parent.Name, ret);
                    parent = parent.Parent;
                }
                return ret;
            }
        }

        public static PackageEntry CreateFile(String name, Int64 len, Int64 ofs, String title = "")
        {
            return new PackageEntry(name, EntryType.File, len, ofs, title);
        }
        public static PackageEntry CreateDir(String name, String title = "")
        {
            return new PackageEntry(name, EntryType.Directory, 0, -1, title);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class PackageEntryAddressComparer : IComparer<PackageEntry>
    {
        public int Compare(PackageEntry x, PackageEntry y)
        {
            if (x.Offset < y.Offset) return -1;
            if (x.Offset > y.Offset) return 1;
            if (x.Length < y.Length) return -1;
            if (x.Length > y.Length) return 1;
            return String.Compare(x.Name, y.Name);
        }
    }
}
