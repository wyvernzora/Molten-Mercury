using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MoltenMercury.Localization
{
    /* Example of language XML:
     * 
     * <localization>
     *      <language name="English" code="en-us">
     *          <string key="STRING_NAME">String Content</string>
     *      </language>
     * </localization>
     * 
     * 
     */

    /// <summary>
    /// Simple multi-language dictilnary loader.
    /// </summary>
    public class LocalizationDictionary
    {
        private static LocalizationDictionary m_instance;
        public static LocalizationDictionary Instance
        { get { return m_instance; } }
        public static void Initialize(XmlDocument load)
        {
            m_instance = new LocalizationDictionary();
            m_instance.LoadXml(load);
        }


        private Dictionary<String, String> m_locales = new Dictionary<string, string>(); // Locale Code to Locale Name mapping (en-us -> English)
        private Dictionary<String, Dictionary<String, String>> m_data = new Dictionary<string, Dictionary<string, string>>(); // Locale code to actual dictionary mapping
        private String m_currentLocale = "en-us";

        private EventHandler m_onChangeLocale;
        public event EventHandler OnLocaleChanged
        { add { m_onChangeLocale += value; } remove { m_onChangeLocale -= value; } }
        private void raiseLocaleChanged()
        {
            if (m_onChangeLocale != null)
                m_onChangeLocale(this, new EventArgs());
        }

        public IEnumerable<KeyValuePair<String, String>> Locales
        { get { return m_locales; } }
        public void ChangeLocale(String loc)
        {
            if (!m_locales.ContainsKey(loc)) throw new InvalidOperationException();
            if (loc != m_currentLocale)
            {
                m_currentLocale = loc;
                raiseLocaleChanged();
            }
        }
        public Boolean HasLocale(String loc)
        {
            return m_locales.ContainsKey(loc);
        }

        public void AddLocale(XmlNode node)
        {
            // Check for invalid input
            if (node.Name != "locale") throw new InvalidOperationException();

            // Get locale name and locale code
            String locName = node.Attributes["name"].Value;
            String locCode = node.Attributes["code"].Value;
            // if locale is already there, skip
            if (m_locales.ContainsKey(locName)) return;

            Dictionary<String, String> locale = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (XmlNode strnode in node.SelectNodes("string"))
            {
                String key = strnode.Attributes["key"].InnerText;
                String value = strnode.InnerText.Replace("\\n", "\n");
                locale.Add(key, value);
            }

            m_locales.Add(locCode, locName);
            m_data.Add(locCode, locale);
        }
        public void LoadXml(XmlDocument doc)
        {
            foreach (XmlNode localenode in doc.SelectNodes("localization/locale"))
                AddLocale(localenode);
        }

        public string this[string key]
        {
            get
            {
                if (m_data[m_currentLocale].ContainsKey(key))
                    return m_data[m_currentLocale][key];
                else
                    return String.Format("ERR-NF:{0}", key.ToUpper());
            }
        }
 
    }
}
