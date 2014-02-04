using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using MoltenMercury.DataModel;

namespace MoltenMercury.Interop
{
    public class CharaxDirHelper
    {
        #region Constants

        // Layer Directories used by Charax
        // Z order from back to front
        readonly Dictionary<String, Int32> LAYER_MAP = new Dictionary<string, int>() {
                                      { "accessory_back", 0 },
                                      { "hair_back", 10000 },
                                      { "hair_back_accessory", 20000 },
                                      {"body_back", 30000 },
                                      {"accessory_underwear", 40000 },
                                      {"body_front", 50000 },
                                      {"body_front_color", 60000 },
                                      {"accessory_middle_back", 70000 },
                                      {"head", 80000 },
                                      {"accessory_middle_front", 90000 },
                                      {"face_back", 100000 },
                                      {"hair_front", 110000 },
                                      {"hair_front_accessory", 120000 },
                                      {"face_front", 130000 },
                                      {"eye", 140000 },
                                      {"accessory_front", 150000 }
                                  };

        readonly Dictionary<String, String> COLOR_MAP = new Dictionary<string, String>() {
                                      { "accessory_back", "none" },
                                      { "hair_back", "Hair" },
                                      { "hair_back_accessory", "none" },
                                      { "body_back", "Skin" },
                                      { "accessory_underwear", "none" },
                                      { "body_front", "none" },
                                      { "body_front_color", "Clothes" },
                                      { "accessory_middle_back", "none" },
                                      { "head", "Skin" },
                                      { "accessory_middle_front", "none" },
                                      { "face_back", "none" },
                                      { "hair_front", "Hair" },
                                      { "hair_front_accessory", "none" },
                                      { "face_front", "none" },
                                      { "eye", "Eyes" },
                                      { "accessory_front", "none" }
                                  };

        readonly Dictionary<String, String> SUFFIX_MAP = new Dictionary<string, string>()
        {
                                      { "", "none" },
                                      { "(髪)", "Hair" },
                                      { "(瞳)", "Eyes" },
                                      { "(服)", "Clothes" },
                                      { "(肌)", "Skin" }
        };

        readonly String[] ACCESSORY_DIRS = new String[] {
                                      "accessory_back",
                                      "accessory_front",
                                      "accessory_middle_front",
                                      "accessory_middle_back",
                                      "accessory_underwear"
                                  };

        readonly String DEFAULT_PRESETS =
            "<ColorProcessorPresets colorgroup=\"Hair\">" +
            "<Preset name=\"Default\">MM|ST0.1|BC-8550435|HX0|SX0|LX0|AA1</Preset>" +
            "<Preset name=\"Violet\">MM|ST0.1|BC-8550435|HA0.833|S+-0.114|L+0.046|AA1</Preset>" +
            "<Preset name=\"Brown\">MM|ST0.1|BC-8550435|HA0|S+-0.252|L+0|AA1</Preset>" +
            "<Preset name=\"Yellow\">MM|ST0.1|BC-8550435|HA0.166|S+-0.16|L+0.072|AA1</Preset>" +
            "<Preset name=\"Green\">MM|ST0.1|BC-8550435|HA0.333|SM0.48|L+0.062|AA1</Preset>" +
            "<Preset name=\"Azure\">MM|ST0.1|BC-8550435|HA0.5|SM0.57|L+0.108|AA1</Preset>" +
            "<Preset name=\"Black\">MM|ST0.1|BC-8550435|HX0|SA0|LM0.868|AA1</Preset>" +
            "<Preset name=\"White\">MM|ST0.1|BC-8550435|HX0|SA0|L+0.17|AA1</Preset>" +
            "<Preset name=\"Brown\">MM|ST0.1|BC-8550435|HX0|S+-0.252|LX0|AA1</Preset>" +
            "<Preset name=\"Orange\">MM|ST0.1|BC-8550435|HA0.083|S+-0.084|L+0.078|AA1</Preset>" +
            "<Preset name=\"Pink\">MM|ST0.1|BC-11511925|HA0.833|S+0.098|L+0.232|AA1</Preset>" +
            "</ColorProcessorPresets><ColorProcessorPresets colorgroup=\"Eyes, Clothes\">" +
            "<Preset name=\"Default\">MM|ST0.1|BC-8550435|HX0|SX0|LX0|AA1</Preset>" +
            "<Preset name=\"Purple\">MM|ST0.1|BC-8550435|HA0.83333|S+0.414|LX0|AA1</Preset>" +
            "<Preset name=\"Red\">MM|ST0.1|BC-8550435|HA0|S+0.41463|LX0|AA1</Preset>" +
            "<Preset name=\"Yellow\">MM|ST0.1|BC-8550435|HA0.166|S+0.206|L+-0.094|AA1</Preset>" +
            "<Preset name=\"Green\">MM|ST0.1|BC-8550435|HA0.333|S+0.114|L+0|AA1</Preset>" +
            "<Preset name=\"Azure\">MM|ST0.1|BC-8550435|HA0.5|SX0.174|LX-0.016|AA1</Preset>" +
            "<Preset name=\"Black\">MM|ST0.1|BC-8550435|HA0|SA0.00008|L+-0.14|AA1</Preset>" +
            "<Preset name=\"White\">MM|ST0.1|BC-8550435|HX0|SA0|LM1.302|AA1</Preset>" +
            "</ColorProcessorPresets><ColorProcessorPresets colorgroup=\"Skin\">" +
            "<Preset name=\"Default\">MM|ST0.295|BC-338486|HX0|SX0|LX0|AA1</Preset>" +
            "<Preset name=\"Brown\">MM|ST0.295|BC-338486|HX0|S+-0.144|L+-0.124|AA1</Preset>" +
            "<Preset name=\"Yellow\">MM|ST0.295|BC-404792|H+0.057|SX0|LX0|AA1</Preset>" +
            "<Preset name=\"Sunburn\">MM|ST0.295|BC-269865|HX0.057|S+0.098|L+-0.032|AA1</Preset>" +
            "<Preset name=\"Unhealthy\">MM|ST0.357|BC-269865|HX0.057|S+-0.054|L+0.032|AA1</Preset>" +
            "</ColorProcessorPresets>";

        Regex filenameExpr = new Regex(@"(?<name>.+?)(|(?<col>\((髪|瞳|服|肌)\)))\.png");

        #endregion

        private string root;
        private Int32 width;
        private Int32 height;

        public String CharaName
        { get; set; }
        public Encoding Encoding
        { get; set; }

        public CharaxDirHelper(String charaini)
        {
            root = Path.GetDirectoryName(charaini);

            INILoader inifile = new INILoader(File.ReadAllLines(charaini));

            width = Int32.Parse(inifile["Size"]["size_x"]);
            height = Int32.Parse(inifile["Size"]["size_y"]);
        }
        public CharaxDirHelper(String root, Int32 width, Int32 height)
        {
            this.root = root;
            this.width = width;
            this.height = height;
        }
        public CharaxDirHelper(String root, CharacterResourceManager res)
            : this(root, res.Width, res.Height)
        {
        }

        public void GenerateDescriptor(String path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create))
                GenerateDescriptor(fs);
        }
        public void GenerateDescriptor(Stream s)
        {
            using (XmlWriter xw = XmlTextWriter.Create(s, new XmlWriterSettings()
            {
                Indent = true,
                IndentChars = "\t",
                Encoding = Encoding.UTF8,
                NewLineChars = "\n",
                NewLineHandling = NewLineHandling.None
            }))
            {
                // Molten Mercury Metadata
                xw.WriteStartDocument();
                xw.WriteStartElement("MoltenMercury");
                xw.WriteAttributeString("name", CharaName == null ? Path.GetFileNameWithoutExtension(root) : CharaName);
                xw.WriteAttributeString("width", width.ToString());
                xw.WriteAttributeString("height", height.ToString());

                xw.WriteRaw(DEFAULT_PRESETS);

                WriteCharaSetGroupXML(xw, "髪型（後ろ）", false, "hair_back", "hair_back_accessory");
                WriteCharaSetGroupXML(xw, "髪型（手前）", false, "hair_front", "hair_front_accessory");
                WriteCharaSetGroupXML(xw, "頭", false, "head");
                WriteCharaSetGroupXML(xw, "表情", false, "face_front", "face_back");
                WriteCharaSetGroupXML(xw, "目", false, "eye");
                WriteCharaSetGroupXML(xw, "身体", false, "body_back", "body_front", "body_front_color");
                WriteCharaSetGroupXML(xw, "アクセサリー", true, ACCESSORY_DIRS);

            }
        }

        #region Utility methods

        private void WriteCharaSetGroupXML(XmlWriter xw, String name, Boolean multiselect, params String[] dirs)
        {

            Dictionary<String, List<String>> m_groups = new Dictionary<string, List<string>>();

            foreach (String dir in dirs)
            {
                if (!Directory.Exists(Path.Combine(root, dir))) continue;

                foreach (String file in Directory.GetFiles(Path.Combine(root, dir), "*.png"))
                {
                    String filename = Path.GetFileName(file);
                    Match m = filenameExpr.Match(filename);

                    String sname = m.Success ? m.Result("${name}") : Path.GetFileNameWithoutExtension(file);

                    if (!m_groups.ContainsKey(sname))
                        m_groups.Add(sname, new List<string>());

                    m_groups[sname].Add(file);
                }
            }

            if (m_groups.Count == 0) return;

            xw.WriteStartElement("CharaSetGroup");
            xw.WriteAttributeString("name", name);
            xw.WriteAttributeString("multiselect", multiselect.ToString().ToLower());
            
            foreach (String setname in m_groups.Keys)
            {
                xw.WriteStartElement("CharaSet");
                xw.WriteAttributeString("name", Path.GetFileNameWithoutExtension(setname));

                foreach (String fname in m_groups[setname])
                {
                    String dir = Path.GetFileName(Path.GetDirectoryName(fname));

                        xw.WriteStartElement("CharaPart");
                        xw.WriteAttributeString("layer", LAYER_MAP[dir].ToString());
                        xw.WriteAttributeString("filename", Path.Combine(dir, Path.GetFileName(fname)));
                        xw.WriteAttributeString("color", GetColorMapping(fname));
                        xw.WriteEndElement();
                }

                xw.WriteEndElement();
            }

            xw.WriteEndElement();
            
        }

        private string GetColorMapping(String filename)
        {
            String pFileName = Path.GetFileNameWithoutExtension(filename);
            if (pFileName.Contains("(瞳)")) return "Eyes";
            if (pFileName.Contains("(髪)")) return "Hair";
            if (pFileName.Contains("(肌)")) return "Skin";
            if (pFileName.Contains("(服)")) return "Clothes";

            String rootdir = Path.GetFileNameWithoutExtension(Path.GetDirectoryName(filename));
            return COLOR_MAP[rootdir];
        }

        #endregion

    }
}
