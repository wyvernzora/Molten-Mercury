using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MoltenMercury.Interop
{
    public class INILoader
    {
        const String GROUP_REGEX = @"\[(?<groupname>.+)\]";
        const String VALUE_REGEX = @"(?<key>[^ =]+?)([ ]*)=([ ]*)(?<value>.+)";

        public class INIGroup
        {
            internal Dictionary<String, String> m_values = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

            public String Name { get; set; }
            public String this[String key]
            {
                get
                {
                    if (m_values.ContainsKey(key))
                        return m_values[key];
                    else
                        throw new KeyNotFoundException();
                }
            }
            
        }

        private Dictionary<String, INIGroup> m_groups = new Dictionary<string, INIGroup>();

        public INILoader(String[] iniLines)
        {
            String currentGroup = "Default";

            foreach (String line in iniLines)
            {
                if (line.Replace(" ", "").Length == 0) continue;
                if (Regex.IsMatch(line, VALUE_REGEX))
                {
                    Match m = Regex.Match(line, VALUE_REGEX);
                    String key = m.Result("${key}");
                    String val = m.Result("${value}");

                    if (!m_groups.ContainsKey(currentGroup))
                        m_groups.Add(currentGroup, new INIGroup() { Name = currentGroup });

                    m_groups[currentGroup].m_values.Add(key, val);
                }
                else if (Regex.IsMatch(line, GROUP_REGEX))
                {
                    Match m = Regex.Match(line, GROUP_REGEX);
                    currentGroup = m.Result("${groupname}");
                }
            }
        }

        public INIGroup this[String index]
        {
            get
            {
                return m_groups[index];
            }
        }
    }
}
