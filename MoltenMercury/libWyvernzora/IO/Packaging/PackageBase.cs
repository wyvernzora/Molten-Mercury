/*===================================================
 * LibHex2\\IO\\Packaging\\PackageBase.cs
 * Base class for all the archive managing classes.
 * -------------------------------------------------------------------------------
 * Copyright (C) Aragorn Wyvernzora 2011
 *      MODIFIED FROM Firefly.Core.Packaging.PackageBase class
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
using System.IO;

namespace libWyvernzora.IO.Packaging
{
    public abstract class ReadonlyPackageBase : IDisposable
    {
        /// <summary>
        /// Archive Base Class
        /// </summary>
        protected ExStreamBase m_bs;

        protected List<PackageEntry> m_entries = new List<PackageEntry>();
        protected Dictionary<PackageEntry, Int32> m_map = new Dictionary<PackageEntry, int>();

        protected ReadonlyPackageBase() { }
        protected ReadonlyPackageBase(ExStreamBase bs)
        {
            m_bs = bs;
        }

        protected PackageEntry m_root = new PackageEntry("", PackageEntry.EntryType.Directory, -1, 0);

        public PackageEntry Root
        { get { return m_root; } }
        public IEnumerable<PackageEntry> Entries
        { get { return m_entries; } }
        public Int32 EntriesCount { get { return m_entries.Count; } }

        protected virtual void PushEntry(PackageEntry f)
        {
            PushEntry(f, m_root);
        }
        protected virtual void PushEntry(PackageEntry f, PackageEntry dir)
        {
            PushEntryToDir(f, dir);

            Int32 n = m_entries.Count;
            m_entries.Add(f);
            m_map.Add(f, n);
        }

        protected void PushEntryToDir(PackageEntry f, PackageEntry dir)
        {
            String d = "";
            String tn = f.Name;
            if (f.Name.Contains("\\") || f.Name.Contains("/"))
            { d = Path.PopFirstDir(ref tn); f.Name = tn; }

            if (d == "")
            {
                dir.SubEntries.Add(f);
                dir.SubEntriesMap.Add(f.Name, f);
                f.Parent = dir;
            }
            else
            {
                if (!dir.SubEntriesMap.ContainsKey(d))
                {
                    PackageEntry dir_entry = PackageEntry.CreateDir(d);
                    dir.SubEntries.Add(dir_entry);
                    dir.SubEntriesMap.Add(dir_entry.Name, dir_entry);
                    dir_entry.Parent = dir;
                }
                PushEntryToDir(f, dir.SubEntriesMap[d]);
            }
        }
        public PackageEntry TryGetEntry(String path)
        {
            string p = path;
            PackageEntry ret = m_root;
            String d = "";
            if (p == "") return m_root;
            if (m_root.Name != "")
            {
                d = Path.PopFirstDir(ref p);
                if (d != m_root.Name) p = path;
            }
            while (ret != null)
            {
                d = Path.PopFirstDir(ref p);
                if (d == "") return ret;
                if (ret.SubEntriesMap.ContainsKey(d))
                    ret = ret.SubEntriesMap[d];
                else
                    return null;
            }
            return null;
        }

        protected virtual void ExtractInner(PackageEntry entry, ExStreamBase exs)
        {
            using (ExPartStream fout = new ExPartStream(m_bs, new Range() { Lower = entry.Offset, Length = entry.Length }))
            {
                fout.WriteTo(exs);
            }
        }
        public void Extract(PackageEntry entry, ExStreamBase exs)
        {
            if (entry.Type != PackageEntry.EntryType.File) throw new InvalidOperationException("PackageBase.Extract(...) :> Cannot directly extract directories");
            ExtractInner(entry, exs);
        }
        protected virtual void ExtractSingleInner(PackageEntry entry, String path)
        {
            using (ExStream exs = new ExStream(path, System.IO.FileMode.Create))
            {
                Extract(entry, exs);
            }
        }
        public void ExtractSingle(PackageEntry entry, String path)
        {
            String dir = Path.GetFileDirectory(path);
            if (dir != "" && !System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            ExtractSingleInner(entry, path);
        }
        protected virtual void ExtractMultipleInner(PackageEntry[] entries, String[] paths)
        {
            for (int i = 0; i < entries.Length - 1; i++)
            {
                ExtractSingle(entries[i], paths[i]);
            }
        }
        public void ExtractMultiple(PackageEntry[] entries, String[] paths)
        {
            if (entries.Length != paths.Length) throw new ArgumentException();
            for (int i = 0; i < paths.Length; i++)
            {
                String dir = Path.GetFileDirectory(paths[i]);
                if (dir != "" && !System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            }
            ExtractMultipleInner(entries, paths);
        }
        protected virtual ExPartStream GetInputStreamInner(PackageEntry entry)
        {
            return new ExPartStream(m_bs, new Range() { Lower = entry.Offset, Length = entry.Length }, System.IO.FileAccess.Read);
        }
        public ExPartStream GetInputStream(PackageEntry entry)
        {
            if (entry.Type != PackageEntry.EntryType.File) throw new InvalidOperationException("PackageBase.Extract(...) :> Cannot directly extract directories");
            return GetInputStreamInner(entry);
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (m_bs != null) m_bs.Dispose();
            m_bs = null;
        }

        public abstract String ConfigurationInfo
        {
            get;
        }
        public String PackageSize
        { get { return libWyvernzora.FileLengthUtility.GetFileLengthString(m_bs.Length); } }

        //============================
        public static Queue<PackageEntry> BuildFileList(string bpath, string path, Queue<PackageEntry> q)
        {
            string[] sfs = System.IO.Directory.GetFiles(path);
            foreach (string ds in sfs)
            {
                FileInfo fi = new FileInfo(ds);
                PackageEntry entry = new PackageEntry();
                entry.Name = Path.GetRelativePath(ds, bpath);
                entry.Length = fi.Length;
                entry.SetAdditionalProperty("Date", fi.LastWriteTime);
                entry.SetAdditionalProperty("Data", new ExStream(ds, FileMode.Open, FileAccess.Read));
                q.Enqueue(entry);
            }
            string[] sds = System.IO.Directory.GetDirectories(path);
            foreach (string dds in sds)
            {
                q = BuildFileList(bpath, dds, q);
            }
            return q;
        }
    }
}
