using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;


/* ============================================================================
 * CharaSet.cs
 * ----------------------------------------------------------------------------
 * 2012 (C) Jieni Luchijinzhou a.k.a Aragorn Wyvernzora
 *   
 * This is the class representing a set of bitmaps that form one single part of
 * a character. 
 */

namespace MoltenMercury.DataModel
{
    /// <summary>
    /// Represents a set of Character Parts
    /// </summary>
    public class CharaSet : IList<CharaPart>
    {
        String m_name;      // Name if the set
        Boolean m_selected = false; // Flag indicating whether the set has been selected
        List<CharaPart> m_parts = new List<CharaPart>();    // Parts contained in this set

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">name of the set</param>
        public CharaSet(String name)
        {
            m_name = name;
            LayerAdjustment = 0;
        }
          
        /// <summary>
        /// Gets or sets CharaSetGroup containing this set
        /// </summary>
        public CharaSetGroup Parent
        { get; set; }
        /// <summary>
        /// Gets a value indicating whether this set has been selcted
        /// </summary>
        public Boolean Selected
        {
            get
            { return m_selected; }
            set
            {
                if (value == m_selected) return;

                if (value)
                    this.Parent.ResourceManager.SetSelected(this);
                else
                    this.Parent.ResourceManager.SetDeselected(this);

                m_selected = value;
            }
        }
        public String Name
        {
            get { return m_name; }
            set { m_name = value; }
        }
        public Int32 LayerAdjustment
        { get; set; }

        public static CharaSet FromXML(XmlNode node)
        {
            if (node.Name != "CharaSet")
                throw new InvalidOperationException("Unexpected XML node name!");

            String name = node.Attributes["name"].InnerText;
            CharaSet cset = new CharaSet(name);

            foreach (XmlNode partNode in node.SelectNodes("CharaPart"))
            {
                CharaPart cpart = CharaPart.FromXml(partNode);
                cpart.Parent = cset;
                cset.Add(cpart);
            }

            return cset;
        }
        public void ToXml(XmlWriter xw)
        {
            xw.WriteStartElement("CharaSet");
            xw.WriteAttributeString("name", Name);

            foreach (CharaPart cpart in m_parts)
                cpart.ToXml(xw);

            xw.WriteEndElement();
        }

        #region IList Members

        public int IndexOf(CharaPart item)
        {
            return m_parts.IndexOf(item);
        }

        public void Insert(int index, CharaPart item)
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Cannot modify a locked CharaSet!");

            item.Parent = this;
            m_parts.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Cannot modify a locked CharaSet!");

            m_parts.RemoveAt(index);
        }

        public CharaPart this[int index]
        {
            get
            {
                return m_parts[index];
            }
            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException("Cannot modify a locked CharaSet!");

                value.Parent = this;
                m_parts[index] = value;
            }
        }

        public void Add(CharaPart item)
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Cannot modify a locked CharaSet!");

            item.Parent = this;
            m_parts.Add(item);            
        }

        public void Clear()
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Cannot modify a locked CharaSet!");

            m_parts.Clear();
        }

        public bool Contains(CharaPart item)
        {
            return m_parts.Contains(item);
        }

        public void CopyTo(CharaPart[] array, int arrayIndex)
        {
            m_parts.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return m_parts.Count; }
        }

        public bool IsReadOnly
        {
            get
            {
                if (Parent != null && Parent.ResourceManager != null)
                    return !Parent.ResourceManager.AllowChange;
                else
                    return false;
            }
        }

        public bool Remove(CharaPart item)
        {
            if (IsReadOnly)
                throw new InvalidOperationException("Cannot modify a locked CharaSet!");

            return m_parts.Remove(item);
        }

        public IEnumerator<CharaPart> GetEnumerator()
        {
            return m_parts.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return m_parts.GetEnumerator();
        }

        #endregion

    }
}
