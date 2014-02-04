using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace libWyvernzora.IO.Packaging
{
    public class AFSPackageCreator : IDisposable 
    {
        //Mainly depends on the PackageEntry.GetAdditionalProperty(...) func.

        const int ALIGN = 0x800;
        Queue<PackageEntry> fileEntries = new Queue<PackageEntry>();
        string npath;
        public Action<Double> NotifyProgress { get; set; }
        public Action<String> NotifyDetails { get; set; }
        public Boolean HasErrors { get; set; }

        public AFSPackageCreator(string path)
        { npath = path; }
        public void AddFile(PackageEntry macro)
        {
            if (macro.Name.Length <= 32)
            {
                fileEntries.Enqueue(macro);
            }
            else
            {
                NotifyDetails(String.Format("Error :> Filename overflow :> \"{0}\"", macro.Name));
                HasErrors = true;
            }
        }
        public void Flush()
        {
            if (NotifyProgress != null) NotifyProgress(0);
            if (NotifyDetails != null) NotifyDetails("Building Header...");
            string cpath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "temp");
            if (!System.IO.Directory.Exists(cpath)) System.IO.Directory.CreateDirectory(cpath);

            //Calculate Header Length

            //4 bytes Magic Number + 4 bytes F_Count
            //4 bytes Descriptor Offset + 4 Bytes Descriptor Length
            //                                                      = 16 Bytes
            Int32 hdrlen = 16 + fileEntries.Count * 8;
            Int32 descLen = fileEntries.Count * 0x30;
            hdrlen = calcCapacity(hdrlen);
            descLen = calcCapacity(descLen);
            if (NotifyDetails != null) NotifyDetails("Done!\n");
            if (NotifyDetails != null) NotifyDetails("Writing Header...");
            using (ExStream fs = new ExStream(npath, FileMode.Create, FileAccess.ReadWrite))
            {
                //Write Identifier
                StreamTools.WriteASCII(fs, "AFS\0");
                StreamTools.WriteI32(fs, fileEntries.Count, BitSequence.LittleEndian);

                //update Header
                Int32 c_offset = hdrlen;
                Int32 fp = 0;

                //Calc Header and write it
                foreach (PackageEntry m in fileEntries)
                {
                    fp++;
                    m.Offset = c_offset;
                    c_offset += calcCapacity((int)m.Length);
                    StreamTools.WriteI32(fs, (int)m.Offset, BitSequence.LittleEndian);
                    StreamTools.WriteI32(fs, (int)m.Length, BitSequence.LittleEndian);
                }

                //Pad Header
                fs.WriteBytes(new Byte[hdrlen - fs.Length]);
                if (NotifyDetails != null) NotifyDetails("Done!\n");

                //!!!!! DESCRIPTOR INFO NOT INCLUDED !!!!

                if (NotifyDetails != null) NotifyDetails("Writing Files\n");
                //Write Data
                int pt = 0;
                foreach (PackageEntry entr in fileEntries)
                {
                    if (NotifyDetails != null) NotifyDetails("\tWriting File [" + entr.Name + "]...");
                    pt++;
                    Stream datas = (Stream)entr.GetAdditinalProperty("Data");
                    if ((datas == null)) throw new InvalidOperationException();
                    StreamTools.WriteTo(datas, fs);
                    //Pad
                    fs.WriteBytes(new Byte[(int)(calcCapacity((int)fs.Length) - fs.Length)]);
                    if (NotifyProgress != null) NotifyProgress(pt / fileEntries.Count);
                    if (NotifyDetails != null) NotifyDetails("Done!\n");
                }

                if (NotifyDetails != null) NotifyDetails("Building Descriptor...");
                //Save Descriptor Address
                Int32 descAddr = (int)fs.Position;

                //Write Descriptor
                foreach (PackageEntry entr in fileEntries)
                {
                    StreamTools.WriteASCII(fs, entr.Name);
                    StreamTools.WriteBytes(fs, new Byte[32 - entr.Name.Length]);

                    DateTime? dt = entr.GetAdditinalProperty("Data") as DateTime?;
                    if (!dt.HasValue) dt = DateTime.Now;

                    StreamTools.WriteI16(fs, (short)dt.Value.Year, BitSequence.LittleEndian);
                    StreamTools.WriteI16(fs, (short)dt.Value.Month, BitSequence.LittleEndian);
                    StreamTools.WriteI16(fs, (short)dt.Value.Day, BitSequence.LittleEndian);
                    StreamTools.WriteI16(fs, (short)dt.Value.Hour, BitSequence.LittleEndian);
                    StreamTools.WriteI16(fs, (short)dt.Value.Minute, BitSequence.LittleEndian);
                    StreamTools.WriteI16(fs, (short)dt.Value.Second, BitSequence.LittleEndian);
                    StreamTools.WriteI32(fs, (int)entr.Length, BitSequence.LittleEndian);

                }

                fs.WriteBytes(new Byte[calcCapacity((int)fs.Length) - fs.Length]);

                fs.Position = 8 + fileEntries.Count * 8;
                fs.WriteInt(descAddr);
                fs.WriteInt(descLen);
                if (NotifyDetails != null) NotifyDetails("Done!\n");

                if (NotifyProgress != null) NotifyProgress(1);
                if (NotifyDetails != null) NotifyDetails("Package Created Successfully!");
            }
        }

        private int calcCapacity(int i)
        {
            return (i % ALIGN == 0) ? i : (i / ALIGN + 1) * ALIGN;
        }

        public void Dispose()
        {
            foreach (PackageEntry entry in fileEntries)
            {
                ((Stream)entry.GetAdditinalProperty("Data")).Close();
            }
        }
    }
}
