using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using MoltenMercury.ImageProcessing;
using libWyvernzora.IO;
using libWyvernzora.IO.Packaging;
using System.Security.Cryptography;

/* ============================================================================
 * CharacterResourceManager.cs
 * ----------------------------------------------------------------------------
 * 2012 (C) Jieni Luchijinzhou a.k.a Aragorn Wyvernzora
 *   
 * This is the resource manager class that manages resources.
 * It loads resource descriptor (an XML file) and arranges resources accordingly.
 * 
 * 
 */
namespace MoltenMercury.DataModel
{
    public class CharacterResourceManager : IList<CharaSetGroup>
    {
        List<CharaSetGroup> m_groups = new List<CharaSetGroup>();       // Chara Set Groups
        Dictionary<String, List<ColorPreset>> m_presets =               // Color Presets for each color group
            new Dictionary<string, List<ColorPreset>>();
        Dictionary<String, ImageProcessor> m_processors =               // Image Processors
            new Dictionary<string, ImageProcessor>();
        List<CharaSet> m_selection = new List<CharaSet>();              // Selected chara sets


        Int32 m_width;  // Bitmap width
        Int32 m_height; // Bitmap height
        String m_name;  // character name
        Boolean m_allowChange = true;   // allow changes to be made. In other words, character NOT locked

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">Character Name</param>
        /// <param name="width">Bitmap Width</param>
        /// <param name="height">Bitmap Height</param>
        public CharacterResourceManager(String name, Int32 width, Int32 height)
        {
            m_name = name;
            m_width = width;
            m_height = height;
        }

        public FileSystemProxy FileSystemProxy
        { get; set; }

        #region Metadata

        public Int32 Width
        {
            get { return m_width; }
            set { m_width = value; }
        }
        public Int32 Height
        {
            get { return m_height; }
            set { m_height = value; }
        }
        public String Name
        {
            get { return m_name; }
            set { m_name = value; }
        }
        public Boolean AllowChange
        {
            get { return m_allowChange; }
            set { m_allowChange = value; }
        }
        public Boolean IsPatch
        {
            get
            {
                AFSFileSystemProxy proxy = this.FileSystemProxy as AFSFileSystemProxy;
                if (proxy == null) return false;
                else return proxy.IsPatch;
            }
        }

        #endregion

        #region Color Processors

        /// <summary>
        /// Gets presets for the specified color group
        /// </summary>
        /// <param name="group">Color Group Name</param>
        /// <returns></returns>
        public IEnumerable<ColorPreset> GetPresets(String group)
        {
            if (m_presets.ContainsKey(group))
                return m_presets[group];
            else if (m_presets.ContainsKey("*"))
                return m_presets["*"];
            else
                return new ColorPreset[0];
        }
        /// <summary>
        /// Gets all presets as they are stored in the manager
        /// </summary>
        public Dictionary<String, List<ColorPreset>> Presets
        { get { return m_presets; } }


        public void UpdateColorProcessorList()
        {
            // List of requested processors
            Dictionary<String, ImageProcessor> processorList = new Dictionary<string, ImageProcessor>();

            // Get all processors from selection
            foreach (CharaSet cset in SelectedSets)
            {
                foreach (CharaPart part in cset)
                {
                    // Get Color Scheme of the selected item
                    String cScheme = part.ColorScheme;

                    // if it does not exist...
                    if (!processorList.ContainsKey(cScheme))
                    {
                        if (m_processors.ContainsKey(cScheme))
                            processorList.Add(cScheme, m_processors[cScheme]);
                        else
                        {
                            if (cScheme == "none") continue;
                            if (cScheme == "skin")
                                processorList.Add(cScheme, (ImageProcessor)ImageProcessor.DEFAULT_SKIN_PROCESSOR.Clone());
                            else
                                processorList.Add(cScheme, (ImageProcessor)ImageProcessor.DEFALT_PROCESSOR.Clone());
                        }
                    }
                }
            }

            // Get all deleted processors and debug print them
            String[] deleted
                = (from val in m_processors
                   where (m_processors.ContainsKey(val.Key) && !processorList.ContainsKey(val.Key))
                   select val.Key).ToArray();

            if (deleted.Length > 0)
                DebugHelper.DebugPrint("Deleted following processors: {0}", String.Join(", ", deleted));

            // Set current processor dictionary to the new one
            m_processors = processorList;
        }

        public Dictionary<String, ImageProcessor> Processors
        { get { return m_processors; } }


        #endregion

        #region Groups and Selection

        public CharaSetGroup GetGroupByName(String groupName)
        {
            return m_groups.Find(new Predicate<CharaSetGroup>((CharaSetGroup g) => { return g.Name == groupName; }));
        }

        public void SetSelected(CharaSet set)
        {
            DebugHelper.DebugPrint("Selected Set: {0}", set.Name);
            if (!m_selection.Contains(set))
                m_selection.Add(set);
        }
        public void SetDeselected(CharaSet set)
        {
            DebugHelper.DebugPrint("Deselected Set: {0}", set.Name);
            m_selection.Remove(set);
        }
        public void ClearSelection()
        {
            DebugHelper.DebugPrint("Selection Cleared");
            List<CharaSet> duplList = new List<CharaSet>(m_selection);
            foreach (CharaSet cset in duplList) cset.Selected = false;
        }
        public IEnumerable<CharaSet> SelectedSets
        { get { return m_selection; } }

        #endregion

        /// <summary>
        /// Merges another resource manager into this one
        /// </summary>
        /// <param name="mgr"></param>
        public void Merge(CharacterResourceManager mgr)
        {
            if (mgr.FileSystemProxy is DefaultFileSystemProxy)
                MergeDefault(mgr);
            else if (mgr.FileSystemProxy is AFSFileSystemProxy)
                MergeAFS(mgr);
        }

        private void MergeDefault(CharacterResourceManager mgr)
        {
            DefaultFileSystemProxy proxy = this.FileSystemProxy as DefaultFileSystemProxy;
            if (proxy == null)
                throw new InvalidOperationException();

            // Merge Presets
            foreach (String colorg in mgr.Presets.Keys)
            {
                if (!Presets.ContainsKey(colorg))
                    Presets.Add(colorg, mgr.Presets[colorg]);
            }

            // Merge Chara Parts
            foreach (CharaSetGroup csg in mgr)
            {
                // Set up the group
                CharaSetGroup loccsg = this.GetGroupByName(csg.Name);
                if (loccsg == null)
                {
                    loccsg = new CharaSetGroup(csg.Name, csg.Multiselect);
                    this.Add(loccsg);
                }

                foreach (CharaSet cset in csg)
                {
                    CharaSet loccset = loccsg.GetSetByName(cset.Name);
                    if (loccset != null) continue;      // Merge happens on set level, if set exists, ignore it

                    loccset = new CharaSet(cset.Name);
                    loccsg.Add(loccset);

                    foreach (CharaPart cpart in cset)
                    {
                        loccset.Add(cpart);

                        String tgtFilename = cpart.FileName;

                        Int32 suffixIndex = 1;
                        while (File.Exists(Path.Combine(proxy.Root, tgtFilename)))
                        {
                            tgtFilename = String.Format("{0}({1}).{2}", Path.GetFileNameWithoutExtension(cpart.FileName), suffixIndex++, "png");
                        }

                        using (FileStream fs = new FileStream(Path.Combine(proxy.Root, tgtFilename), FileMode.CreateNew))
                        {
                            libWyvernzora.StreamTools.WriteTo(mgr.FileSystemProxy.GetBitmapStream(cpart.FileName), fs);
                        }

                        cpart.FileName = tgtFilename;
                    }
                }

            }

            // Save Resources
            using (XmlWriter xw = XmlTextWriter.Create(Path.Combine(proxy.Root, "character.mcres"),
                new XmlWriterSettings() { Indent = true, IndentChars = "\t", Encoding = Encoding.UTF8 }))
            {
                this.ToXml(xw);
                xw.Flush();
            }
        }

        private void MergeAFS(CharacterResourceManager mgr)
        {
            DefaultFileSystemProxy proxy = this.FileSystemProxy as DefaultFileSystemProxy;
            AFSFileSystemProxy afsProxy = (AFSFileSystemProxy)mgr.FileSystemProxy;

            if (proxy == null)
                throw new InvalidOperationException();

            // Merge Presets
            foreach (String colorg in mgr.Presets.Keys)
            {
                if (!Presets.ContainsKey(colorg))
                    Presets.Add(colorg, mgr.Presets[colorg]);
            }

            // Attempt to load file name mapping
            Dictionary<String, String> fileNameMap = null;
            PackageEntry fmapentry = afsProxy.Archive.TryGetEntry(".patch");
            if (fmapentry != null)
            {
                fileNameMap = new Dictionary<string, string>();
                XmlDocument fmapdoc = new XmlDocument();
                using (ExStream fmaps = new ExStream())
                {
                    afsProxy.Archive.Extract(fmapentry, fmaps);
                    fmaps.Position = 0;
                    fmapdoc.Load(fmaps);
                }
                foreach (XmlNode mapNode in fmapdoc.SelectNodes("FileNameMap/Mapping"))
                {
                    String hashed = mapNode.Attributes["key"].InnerText;
                    String original = mapNode.InnerText;

                    fileNameMap.Add(hashed, original);
                }

            }
            
            // Extract all available parts
            foreach (PackageEntry entry in afsProxy.Archive.Entries)
            {
                if (entry.Type == PackageEntry.EntryType.Directory ||
                    entry.Name.StartsWith(".") || entry.Name == "character.mcres" || entry.Name == "character.mcs")
                    continue;

                String filename = fileNameMap != null && fileNameMap.ContainsKey(entry.Name) ? fileNameMap[entry.Name] : entry.Name;
                filename = Path.Combine(proxy.Root, filename);

                if (File.Exists(filename))
                {
                    // Verify SHA if there is a collision
                    using (ExStream exs = new ExStream())
                    {
                        afsProxy.Archive.Extract(entry, exs);
                        exs.Position = 0;

                        SHA256 hasher = SHA256.Create();
                        String newFileHash = libWyvernzora.HexTools.Byte2String(hasher.ComputeHash(exs));

                        String oldFileHash;
                        using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                            oldFileHash = libWyvernzora.HexTools.Byte2String(hasher.ComputeHash(fs));

                        if (StringComparer.CurrentCultureIgnoreCase.Equals(oldFileHash, newFileHash))
                            continue; // hashes of the new file and the old file are the same, so no worries

                        // Otherwise, backup the original and overwrite
                        File.Move(filename, Path.ChangeExtension(filename, ".bak"));
                        using (FileStream fs = new FileStream(filename, FileMode.Create))
                            afsProxy.Archive.Extract(entry, new ExStream(fs));
                    }
                }
                else
                {
                    using (FileStream fs = new FileStream(filename, FileMode.Create))
                        afsProxy.Archive.Extract(entry, new ExStream(fs));
                }

            }

            // Merge Chara Parts
            foreach (CharaSetGroup csg in mgr)
            {
                // Set up the group
                CharaSetGroup loccsg = this.GetGroupByName(csg.Name);
                if (loccsg == null)
                {
                    loccsg = new CharaSetGroup(csg.Name, csg.Multiselect);
                    this.Add(loccsg);
                }

                foreach (CharaSet cset in csg)
                {
                    CharaSet loccset = loccsg.GetSetByName(cset.Name);
                    if (loccset != null) continue;

                    loccset = new CharaSet(cset.Name);
                    loccsg.Add(loccset);

                    foreach (CharaPart cpart in cset)
                    {
                        if (fileNameMap != null && fileNameMap.ContainsKey(cpart.FileName))
                            cpart.FileName = fileNameMap[cpart.FileName];

                        loccset.Add(cpart);
                    }
                }

            }

            // Save Resources
            using (XmlWriter xw = XmlTextWriter.Create(Path.Combine(proxy.Root, "character.mcres"),
                new XmlWriterSettings() { Indent = true, IndentChars = "\t", Encoding = Encoding.UTF8 }))
            {
                this.ToXml(xw);
                xw.Flush();
            }
        }

        public static CharacterResourceManager FromXML(Stream xmlstream)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlstream);

            XmlNode rootNode = doc.SelectSingleNode("MoltenMercury");
            Int32 w = Int32.Parse(rootNode.Attributes["width"].InnerText);
            Int32 h = Int32.Parse(rootNode.Attributes["height"].InnerText);
            string name = rootNode.Attributes["name"].InnerText;

            CharacterResourceManager res = new CharacterResourceManager(name, w, h);

            foreach (XmlNode groupNode in rootNode.SelectNodes("CharaSetGroup"))
            {
                CharaSetGroup csg = CharaSetGroup.FromXML(groupNode);
                csg.ResourceManager = res;
                res.m_groups.Add(csg);
            }

            foreach (XmlNode presetNode in rootNode.SelectNodes("ColorProcessorPresets"))
            {
                String[] groups = presetNode.Attributes["colorgroup"].InnerText.Split(',');
                List<ColorPreset> setList = new List<ColorPreset>();

                foreach (XmlNode pNode in presetNode.SelectNodes("Preset"))
                {
                    String pname = pNode.Attributes["name"].InnerText;
                    String set = pNode.InnerText;
                    setList.Add(new ColorPreset() { Name = pname, Preset = set });
                }

                foreach (String group in groups)
                {
                    res.m_presets.Add(group.Trim(), setList);
                }
            }

            return res;
        }
        public static CharacterResourceManager FromXML(String xmlpath)
        {
            CharacterResourceManager res;
            using (FileStream fs = new FileStream(xmlpath, FileMode.Open))
                res = CharacterResourceManager.FromXML(fs);
            return res;
        }
        public void ToXml(XmlWriter xw)
        {
            xw.WriteStartDocument();
            xw.WriteStartElement("MoltenMercury");
            xw.WriteAttributeString("name", Name);
            xw.WriteAttributeString("width", Width.ToString());
            xw.WriteAttributeString("height", Height.ToString());

            foreach (String presetg in m_presets.Keys)
            {
                xw.WriteStartElement("ColorProcessorPresets");
                xw.WriteAttributeString("colorgroup", presetg);

                foreach (ColorPreset cp in m_presets[presetg])
                {
                    xw.WriteStartElement("Preset");
                    xw.WriteAttributeString("name", cp.Name);
                    xw.WriteString(cp.Preset);
                    xw.WriteEndElement();
                }

                xw.WriteEndElement();
            }

            foreach (CharaSetGroup csg in this)
                csg.ToXml(xw);

            xw.WriteEndDocument();
        }

        #region IList Members

        public IEnumerator<CharaSetGroup> GetEnumerator()
        {
            return m_groups.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_groups.GetEnumerator();
        }

        public int IndexOf(CharaSetGroup item)
        {
            return m_groups.IndexOf(item);
        }

        public void Insert(int index, CharaSetGroup item)
        {
            if (!AllowChange)
                throw new InvalidOperationException();

            item.ResourceManager = this;
            m_groups.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            if (!AllowChange)
                throw new InvalidOperationException();

            m_groups.RemoveAt(index);
        }

        public CharaSetGroup this[int index]
        {
            get
            {
                return m_groups[index];
            }
            set
            {
                if (!AllowChange)
                    throw new InvalidOperationException();

                value.ResourceManager = this;
                m_groups[index] = value;
            }
        }

        public void Add(CharaSetGroup item)
        {
            if (!AllowChange)
                throw new InvalidOperationException();

            item.ResourceManager = this;
            m_groups.Add(item);
        }

        public void Clear()
        {
            if (!AllowChange)
                throw new InvalidOperationException();
            m_groups.Clear();
        }

        public bool Contains(CharaSetGroup item)
        {
            return m_groups.Contains(item);
        }

        public void CopyTo(CharaSetGroup[] array, int arrayIndex)
        {
            m_groups.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return m_groups.Count; }
        }

        public bool IsReadOnly
        {
            get { return !AllowChange; }
        }

        public bool Remove(CharaSetGroup item)
        {
            if (!AllowChange)
                throw new InvalidOperationException();
            return m_groups.Remove(item);
        }

        #endregion
    }
}
