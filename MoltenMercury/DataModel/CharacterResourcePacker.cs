using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using libWyvernzora.IO.Packaging;
using System.IO;
using System.Xml;
using MoltenMercury.ImageProcessing;

namespace MoltenMercury.DataModel
{
    public class CharacterResourcePacker
    {
        private AFSPackageCreator m_afsCreator;
        private CharacterResourceManager m_resources;
        private Dictionary<String, String> m_fileNameMap = new Dictionary<string, string>();
        private Dictionary<String, ImageProcessor> m_processors = null;

        private Boolean m_lockChara = false;
        private Boolean m_saveState = true;
        private Boolean m_trim = false;
        private Boolean m_patch = false;
        public Boolean SaveState
        {
            get { return m_saveState; }
            set { m_saveState = value; }
        }
        public Boolean LockCharacter
        {
            get { return m_lockChara; }
            set { m_lockChara = value; }
        }
        public Boolean OmitUnusedResources
        {
            get { return m_trim; }
            set { m_trim = value; }
        }
        public Boolean IsPatch
        {
            get { return m_patch; }
            set { m_patch = value; }
        }
        public String CharacterName
        { get; set; }
        public AFSPackageCreator ArchiveGenerator
        { get { return m_afsCreator; } }

        public Dictionary<String, ImageProcessor> ImageProcessors
        {
            get { return m_processors; }
            set { m_processors = value; }
        }

        public CharacterResourcePacker(String archivePath, CharacterResourceManager resources)
        {
            m_afsCreator = new AFSPackageCreator(archivePath);
            m_resources = resources;
            CharacterName = Path.GetFileNameWithoutExtension(archivePath);
            m_processors = resources.Processors;
        }

        public void PackResources()
        {
            if (m_trim && !m_saveState)
                throw new InvalidOperationException();

            // Hasher
            MD5 hasher = MD5.Create();

            // Generate Naming Map and add resource files
            for (int gid = 0; gid < m_resources.Count; gid++)
            {
                for (int sid = 0; sid < m_resources[gid].Count; sid++)
                {
                    if (!m_trim || m_resources[gid][sid].Selected)
                    {
                        for (int pid = 0; pid < m_resources[gid][sid].Count; pid++)
                        {
                            CharaPart cpart = m_resources[gid][sid][pid];


                            String oriName = m_resources[gid][sid][pid].FileName;
                            String newName = libWyvernzora.HexTools.Byte2String(hasher.ComputeHash(Encoding.UTF8.GetBytes(oriName))).ToUpper();

                            if (!m_fileNameMap.ContainsKey(oriName))
                            {

                                PackageEntry entry = new PackageEntry();
                                Stream data = m_resources.FileSystemProxy.GetBitmapStream(cpart.FileName);
                                entry.SetAdditionalProperty("Data", data);
                                entry.Length = data.Length;
                                entry.Name = newName;
                                m_afsCreator.AddFile(entry);

                                m_fileNameMap.Add(oriName, newName);
                            }
                        }
                    }

                }
            }

            // Generate New Descriptor
            MemoryStream ms = new MemoryStream();
            XmlWriter xw = XmlTextWriter.Create(ms, new XmlWriterSettings() { Encoding = Encoding.UTF8 });

            xw.WriteStartDocument();
            xw.WriteStartElement("MoltenMercury");
            xw.WriteAttributeString("name", CharacterName);
            xw.WriteAttributeString("width", m_resources.Width.ToString());
            xw.WriteAttributeString("height", m_resources.Height.ToString());

            if (!m_trim)
            {
                foreach (KeyValuePair<String, List<ColorPreset>> kvp in m_resources.Presets)
                {
                    xw.WriteStartElement("ColorProcessorPresets");
                    xw.WriteAttributeString("colorgroup", kvp.Key);

                    foreach (ColorPreset preset in kvp.Value)
                    {
                        xw.WriteStartElement("Preset");
                        xw.WriteAttributeString("name", preset.Name);
                        xw.WriteString(preset.Preset);
                        xw.WriteEndElement();
                    }

                    xw.WriteEndElement();
                }
            }

            foreach (CharaSetGroup group in m_resources)
            {
                xw.WriteStartElement("CharaSetGroup");
                xw.WriteAttributeString("name", group.Name);
                xw.WriteAttributeString("multiselect", group.Multiselect.ToString());

                foreach (CharaSet set in group)
                {
                    if (!m_trim || set.Selected)
                    {
                        xw.WriteStartElement("CharaSet");
                        xw.WriteAttributeString("name", set.Name);

                        foreach (CharaPart part in set)
                        {
                            xw.WriteStartElement("CharaPart");
                            xw.WriteAttributeString("layer", part.Layer.ToString());
                            xw.WriteAttributeString("filename", m_fileNameMap[part.FileName]);
                            xw.WriteAttributeString("color", part.ColorScheme);
                            xw.WriteEndElement();
                        }

                        xw.WriteEndElement();
                    }
                }

                xw.WriteEndElement();
            }

            xw.WriteEndElement();
            xw.WriteEndDocument();
            xw.Flush();

            ms.Position = 0;
            PackageEntry descriptorEntry = new PackageEntry()
            {
             Name = "character.mcres",
             Length = ms.Length
            };
            descriptorEntry.SetAdditionalProperty("Data", ms);
            m_afsCreator.AddFile(descriptorEntry);

            // Save state if requested
            if (m_saveState)
            {
                if (m_processors == null)
                    throw new InvalidOperationException("Can't save state without image processor info!");

                MemoryStream state = new MemoryStream();
                using (XmlWriter statew = XmlTextWriter.Create(state, new XmlWriterSettings() { Encoding = Encoding.UTF8, Indent = true, IndentChars = "\t" }))
                {
                    statew.WriteStartDocument();

                    statew.WriteStartElement("MoltenMercuryState");
                    statew.WriteAttributeString("name", CharacterName);

                    foreach (KeyValuePair<String, ImageProcessor> kvp in m_processors)
                    {
                        statew.WriteStartElement("ImageProcessor");
                        statew.WriteAttributeString("colorgroup", kvp.Key);
                        statew.WriteString(kvp.Value.EncodeSettings());
                        statew.WriteEndElement();
                    }

                    foreach (CharaSet set in m_resources.SelectedSets)
                    {
                        statew.WriteStartElement("Selection");
                        statew.WriteAttributeString("group", set.Parent.Name);
                        statew.WriteAttributeString("set", set.Name);
                        if (set.Parent.Multiselect)
                            statew.WriteAttributeString("adjustment", set.LayerAdjustment.ToString());
                        statew.WriteEndElement();
                    }

                    statew.WriteEndDocument();
                }

                PackageEntry stateEntry = new PackageEntry() { Length = state.Length, Name = "character.mcs" };
                state.Position = 0;
                stateEntry.SetAdditionalProperty("Data", state);
                m_afsCreator.AddFile(stateEntry);
            }

            // If lock chara, add a lock file
            if (m_lockChara)
            {
                MemoryStream lockms = new MemoryStream(new Byte[10]);
                PackageEntry lockentry = new PackageEntry() { Name = ".lock", Length = 10 };
                lockentry.SetAdditionalProperty("Data", lockms);
                m_afsCreator.AddFile(lockentry);
            }

            // If this is a patch, add a patch file (file name map)
            if (m_patch)
            {
                MemoryStream patchmap = new MemoryStream();
                XmlTextWriter pw = new XmlTextWriter(patchmap, Encoding.UTF8);
                pw.WriteStartDocument();
                pw.WriteStartElement("FileNameMap");
                foreach (KeyValuePair<String, String> kvp in m_fileNameMap)
                {
                    pw.WriteStartElement("Mapping");
                    pw.WriteAttributeString("key", kvp.Value);
                    pw.WriteString(kvp.Key);
                    pw.WriteEndElement();
                }
                pw.WriteEndDocument();
                pw.Flush();
                patchmap.Position = 0;

                PackageEntry patchentry = new PackageEntry() { Name = ".patch", Length = patchmap.Length };
                patchentry.SetAdditionalProperty("Data", patchmap);
                m_afsCreator.AddFile(patchentry);
            }

            // Save archive
            m_afsCreator.Flush();
            m_afsCreator.Dispose();

        }
    }
}
