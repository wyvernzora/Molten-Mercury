using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MoltenMercury.DataModel
{
    public class CharaSetGroup : IList<CharaSet>
    {
        List<CharaSet> m_sets = new List<CharaSet>();
        String m_name;
        Boolean m_multiselect;

        public CharaSetGroup(String name, Boolean multisel)
        {
            m_name = name;
            m_multiselect = multisel;
        }

        public String Name
        {
            get { return m_name; }
            set { m_name = value; }
        }
        public Boolean Multiselect
        {
            get { return m_multiselect; }
            set { m_multiselect = value; }
        }

        public CharacterResourceManager ResourceManager
        { get; set; }

        public CharaSet GetSetByName(String name)
        {
           return m_sets.Find(new Predicate<CharaSet>((CharaSet s) => { return s.Name == name; }));
        }

        public static CharaSetGroup FromXML(XmlNode node)
        {
            if (node.Name != "CharaSetGroup")
                throw new InvalidOperationException("Unexpected XML Node Name!");

            String name = node.Attributes["name"].InnerText;
            Boolean multisel = Boolean.Parse(node.Attributes["multiselect"].InnerText);

            CharaSetGroup setg = new CharaSetGroup(name, multisel);

            foreach (XmlNode setNode in node.SelectNodes("CharaSet"))
            {
                CharaSet cset = CharaSet.FromXML(setNode);
                cset.Parent = setg;
                setg.Add(cset);
            }

            setg.SortSets();

            return setg;
        }
        public void ToXml(XmlWriter xw)
        {
            xw.WriteStartElement("CharaSetGroup");
            xw.WriteAttributeString("name", Name);
            xw.WriteAttributeString("multiselect", Multiselect.ToString());

            foreach (CharaSet cset in this)
                cset.ToXml(xw);

            xw.WriteEndElement();
        }

        public void SortSets()
        {
            m_sets.Sort(new Comparison<CharaSet>((CharaSet a, CharaSet b) =>
            { return a.Name.CompareTo(b.Name); }));
        }

        public override string ToString()
        {
            return this.Name;
        }

        #region IList Members

        public IEnumerator<CharaSet> GetEnumerator()
        {
            return m_sets.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_sets.GetEnumerator();
        }

        public int IndexOf(CharaSet item)
        {
            return m_sets.IndexOf(item);
        }

        public void Insert(int index, CharaSet item)
        {
            if (IsReadOnly)
                throw new InvalidOperationException();

            item.Parent = this;
            m_sets.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            if (IsReadOnly)
                throw new InvalidOperationException();

            m_sets.RemoveAt(index);
        }

        public CharaSet this[int index]
        {
            get
            {
                return m_sets[index];
            }
            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException();

                value.Parent = this;
                m_sets[index] = value;
            }
        }

        public void Add(CharaSet item)
        {

            if (IsReadOnly)
                throw new InvalidOperationException();

            item.Parent = this;
            m_sets.Add(item);
        }

        public void Clear()
        {
            if (IsReadOnly)
                throw new InvalidOperationException();

            m_sets.Clear();
        }

        public bool Contains(CharaSet item)
        {
            return m_sets.Contains(item);
        }

        public void CopyTo(CharaSet[] array, int arrayIndex)
        {
            m_sets.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return m_sets.Count; }
        }

        public bool IsReadOnly
        {
            get
            {
                if (ResourceManager != null)
                    return !ResourceManager.AllowChange;
                else
                    return false;
            }
        }

        public bool Remove(CharaSet item)
        {
            if (IsReadOnly)
                throw new InvalidOperationException();

            return m_sets.Remove(item);
        }

        #endregion
    }
}
