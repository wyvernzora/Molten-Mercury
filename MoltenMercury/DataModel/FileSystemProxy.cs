using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using libWyvernzora.IO.Packaging;
using libWyvernzora.IO;

namespace MoltenMercury.DataModel
{
    
    public abstract class FileSystemProxy
    {
        //private static FileSystemProxy m_loader;
        //public static FileSystemProxy Loader
        //{
        //    get { return m_loader; }
        //    set { m_loader = value; }
        //}

        public abstract Bitmap LoadBitmap(String path);
        public abstract Stream GetBitmapStream(String path);

        public abstract Boolean CanSaveState
        { get; }
        public abstract void WriteSavedState(Stream source);
        public abstract Stream GetSavedStateStream();

        public abstract String LoadedPath
        { get; }
    }

    public class DefaultFileSystemProxy : FileSystemProxy
    {
        const String STATE_FILE = "character.mcs";
        const Int32 BUFFER_SIZE = 4096;

        private String rootDir;
        private String loadedPath;

        public String Root
        { get { return rootDir; } }

        public DefaultFileSystemProxy(String rootDir)
        {
            this.rootDir = rootDir;
            if (File.Exists(Path.Combine(rootDir, "character.mcres")))
                loadedPath = Path.Combine(rootDir, "character.mcres");
            else if (File.Exists(Path.Combine(rootDir, "character.ini")))
                loadedPath = Path.Combine(rootDir, "character.ini");
        }


        public override Bitmap LoadBitmap(string path)
        {
            String imgPath = Path.Combine(rootDir, path);
            if (File.Exists(imgPath))
                return new Bitmap(Image.FromFile(imgPath));
            else
                return null;
        }
        
        public override void WriteSavedState(Stream source)
        {
            source.Position = 0;
            using (FileStream fs = new FileStream(Path.Combine(rootDir, STATE_FILE), FileMode.Create, FileAccess.Write))
            {
                while (true)
                {
                    Byte[] buffer = new Byte[BUFFER_SIZE];
                    Int32 br = source.Read(buffer, 0, BUFFER_SIZE);
                    fs.Write(buffer, 0, br);
                    if (br < BUFFER_SIZE) break;
                }
                
            }
        }
        public override Stream GetSavedStateStream()
        {
            if (!File.Exists(Path.Combine(rootDir, STATE_FILE))) return null;

            MemoryStream ms = new MemoryStream();
            using (FileStream fs = new FileStream(Path.Combine(rootDir, STATE_FILE), FileMode.Open, FileAccess.Read))
            {
                while (true)
                {
                    Byte[] buffer = new Byte[BUFFER_SIZE];
                    Int32 br = fs.Read(buffer, 0, BUFFER_SIZE);
                    ms.Write(buffer, 0, br);
                    if (br < BUFFER_SIZE) break;
                }
            }
            ms.Position = 0;
            return ms;
        }
        public override bool CanSaveState
        {
            get { return true; }
        }

        public override Stream GetBitmapStream(string path)
        {
            String imgPath = Path.Combine(rootDir, path);
            if (File.Exists(imgPath))
                return new FileStream(imgPath, FileMode.Open, FileAccess.Read);
            else
                return null;
        }

        public override string LoadedPath
        {
            get { return loadedPath; }
        }
    }

    public class AFSFileSystemProxy : FileSystemProxy
    {
        const String STATE_FILE = "character.mcs";
        const String ROOT_FILE = "character.mcres";
        const String PATCH_FILE = ".patch";

        AFSPackageLoader afs;
        Boolean canSaveState;
        Boolean isPatch;
        String afsPath;

        public AFSPackageLoader Archive
        { get { return afs; } }

        public AFSFileSystemProxy(String archivePath)
        {
            afs = new AFSPackageLoader(archivePath);
            afsPath = archivePath;
            canSaveState = (afs.TryGetEntry(STATE_FILE) == null) && (afs.TryGetEntry(PATCH_FILE) == null);
            isPatch = afs.TryGetEntry(PATCH_FILE) != null;
        }

        public override Bitmap LoadBitmap(string path)
        {
            PackageEntry entry = afs.TryGetEntry(path);
            if (entry == null)
                throw new KeyNotFoundException();

            Bitmap result;
            using (ExStream exs = new ExStream())
            {
                afs.Extract(entry, exs);
                exs.Position = 0;
                result = new Bitmap(Image.FromStream(exs));
            }
            return result;
        }

        public override bool CanSaveState
        {
            get { return canSaveState; }
        }

        public bool IsPatch
        {
            get { return isPatch; }
        }

        public override void WriteSavedState(Stream source)
        {
            if (canSaveState)
            {
                using (FileStream fs = new FileStream(Path.ChangeExtension(afsPath, ".mcs"), FileMode.Create))
                    libWyvernzora.StreamTools.WriteTo(source, fs);
            }
        }

        public override Stream GetSavedStateStream()
        {
            PackageEntry entry = afs.TryGetEntry("character.mcs");
            ExStream exs = new ExStream();
            if (entry != null)
            {
                afs.Extract(entry, exs);
                exs.Position = 0;
                return exs;
            }
            else
            {
                if (File.Exists(Path.ChangeExtension(afsPath, ".mcs")))
                {
                    using (FileStream fs = new FileStream(Path.ChangeExtension(afsPath, ".mcs"), FileMode.Open))
                    {
                        libWyvernzora.StreamTools.WriteTo(fs, exs);
                    }
                    exs.Position = 0;
                    return exs;
                }
            }
            return null;
        }

        public override Stream GetBitmapStream(string path)
        {
            PackageEntry entry = afs.TryGetEntry(path);
            if (entry == null)
                throw new KeyNotFoundException();

            ExStream exs = new ExStream();
            afs.Extract(entry, exs);
            exs.Position = 0;
            
            return exs;
        }

        public override string LoadedPath
        {
            get { return afsPath; }
        }
    }
     
}
