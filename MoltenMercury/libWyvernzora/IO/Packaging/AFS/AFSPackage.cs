using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace libWyvernzora.IO.Packaging
{
    public class AFSPackageLoader : PackageContinuous 
    {
        public AFSPackageLoader(ExStream exs, String title = null)
            : base(exs)
        {
            if (StreamTools.ReadASCII(m_bs, 4) != "AFS\0") throw new InvalidDataException();
            Int32 fcount = StreamTools.ReadI32(m_bs, BitSequence.LittleEndian);
            List<PackageEntry> files = new List<PackageEntry>();

            for (int i = 0; i < fcount; i++)
            {
                PackageEntry macro = new PackageEntry();
                macro.Offset = StreamTools.ReadI32(m_bs, BitSequence.LittleEndian);
                macro.Length = StreamTools.ReadI32(m_bs, BitSequence.LittleEndian);
                files.Add(macro);
            }

            Int32 indx_start = StreamTools.ReadI32(m_bs, BitSequence.LittleEndian);
            Int32 indx_len = m_bs.ReadInt(BitSequence.LittleEndian);

            m_bs.Position = indx_start;

            for (int i = 0; i < fcount; i++)
            {
                files[i].Name = StreamTools.ReadASCII(m_bs, 0x20).Replace("\0", "");

                Int16 f_dy = StreamTools.ReadI16(m_bs, BitSequence.LittleEndian);
                Int16 f_dd = StreamTools.ReadI16(m_bs, BitSequence.LittleEndian);
                Int16 f_dm = StreamTools.ReadI16(m_bs, BitSequence.LittleEndian);
                Int16 f_th = StreamTools.ReadI16(m_bs, BitSequence.LittleEndian);
                Int16 f_tm = StreamTools.ReadI16(m_bs, BitSequence.LittleEndian);
                Int16 f_ts = StreamTools.ReadI16(m_bs, BitSequence.LittleEndian);
                Int32 f_rsize = StreamTools.ReadI32(m_bs, BitSequence.LittleEndian);

                files[i].SetAdditionalProperty("Date", new DateTime(f_dy, f_dd, f_dm, f_th, f_tm, f_ts));
                files[i].SetAdditionalProperty("Data", f_rsize.ToString());
            }

            foreach (PackageEntry entry in files)
            { PushEntry(entry); GetInputStream(entry); }

            m_root.Title = title;
        }

        public AFSPackageLoader(String path, String title = null)
            : this(new ExStream(path, FileMode.Open, FileAccess.Read), 
            (title == null) ? Path.GetMainFileName(path) : title) { }

        public override long GetEntryLengthInPhysicalFileDB(PackageEntry entry)
        {
            Int32 index = m_map[entry];
            Int64 pos = index * 12 + 8;
            m_bs.Position = pos;
            return m_bs.ReadInt(BitSequence.LittleEndian);
        }

        public override void SetEntryLengthInPhysicalFileDB(PackageEntry entry, long value)
        {
            Int32 index = m_map[entry];
            Int64 pos = index * 12 + 8;
            m_bs.Position = pos;
            m_bs.WriteInt((int)value, BitSequence.LittleEndian);
        }

        protected override long GetSpace(long len)
        {
            return ((len + 0x800 - 1) / 0x800) * 0x800;
        }

        public static void CreatePackage(string pkDir, string outPath, Action<Double> progress, Action<String> details)
        {
            AFSPackageCreator creator = new AFSPackageCreator(outPath);
            creator.NotifyProgress = progress;
            creator.NotifyDetails = details;
            Queue<PackageEntry> flist = ReadonlyPackageBase.BuildFileList(pkDir, pkDir, new Queue<PackageEntry>());
            foreach (PackageEntry entr in flist) creator.AddFile(entr);
            creator.Flush();
            creator.Dispose();
        }

        public static Boolean CanCreatepackage(string pkDir, Action<String> details)
        {
            bool result = true;
            Queue<PackageEntry> flist = ReadonlyPackageBase.BuildFileList(pkDir, pkDir, new Queue<PackageEntry>());
            foreach (PackageEntry entr in flist)
            {
                if (entr.Name.Length > 32)
                    result = false;
                if (details != null) details(String.Format("Error :> Name Overflow [{0}] :> \"{1}\"", entr.Name.Length, entr.Name));
            }
            return result;

        }

        public override string ConfigurationInfo
        {
            get { return "Andrew File System"; }
        }
    }
}
