using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Xml;
using System.IO;
using MoltenMercury.ImageProcessing;


/* ============================================================================
 * Charapart.cs
 * ----------------------------------------------------------------------------
 * 2012 (C) Jieni Luchijinzhou a.k.a Aragorn Wyvernzora
 *   
 * This is the class representing a character part made of one single bitmap.
 * It stores color scheme, layer info and information needed for loading the bitmap
 * when needed.
 * 
 */

namespace MoltenMercury.DataModel
{
    /// <summary>
    /// Character Part made of one single bitmap
    /// </summary>
    public class CharaPart
    {
        /* Unlike CharaEX, where layers are arranged as folders, Molten Mercury
         * assigns a layer index to each character part. When character image is
         * generated, all selected parts are sorted by layer index and composited
         * starting from the lowest index. In other words, the smallest layer index
         * means bitmap is closest to the background; the biggest layer index means
         * the bitmap is closest to the foreground (viewer)
         */

        private Int32 m_layer;      // layer index
        private String m_filename;  // file name (NOT full path, may be a relative path)
        private String m_color;     // color scheme name (or color group name)

        private Bitmap m_buffer;    // associated bitmap, not loaded until requested

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="layer">Layer Index of the Part</param>
        /// <param name="filename">Filename or path relative to mount root</param>
        /// <param name="color">Color scheme (group) name</param>
        public CharaPart(Int32 layer, String filename, String color)
        {
            m_layer = layer;
            m_filename = filename;
            m_color = color;
        }

        /// <summary>
        /// Gets character part set containing this part
        /// </summary>
        public CharaSet Parent
        { get; set; }
        public Int32 Layer
        {
            get { return m_layer + (Parent != null ? Parent.LayerAdjustment : 0); }
            set { m_layer = value; }
        }
        public Int32 UnadjustedLayer
        { get { return m_layer; } }
        public String FileName
        {
            get { return m_filename; }
            set { m_filename = value; }
        }
        public String ColorScheme
        {
            get { return m_color; }
            set { m_color = value; }
        }

        /// <summary>
        /// Gets bitmap associated with the chara part.
        /// Loads bitmap if necessary
        /// </summary>
        public Bitmap Bitmap
        {
            get
            {
                try
                {
                    if (m_buffer == null)
                        m_buffer = Parent.Parent.ResourceManager.FileSystemProxy.LoadBitmap(FileName);
                }
                catch
                {
                    m_buffer = new Bitmap(Parent.Parent.ResourceManager.Width, Parent.Parent.ResourceManager.Height);
                }

                return m_buffer;
            }
        }

        private Bitmap m_processBuffer = null;      // Already processed bitmap, to avoid repetitive work
        private Int32 m_processorVer = -1;          // Signature of the image processor that generated the bitmap above
        public Bitmap ProcessedBuffer
        {
            get { return m_processBuffer; }
            set
            {
                m_processBuffer = value;
            }
        }
        public Int32 ProcessedBufferVersion
        {
            get { return m_processorVer; }
            set { m_processorVer = value; }
        }

        /// <summary>
        /// Creates a CharaPart object from XML description
        /// </summary>
        /// <param name="node">XMLNode containing information about Character Part</param>
        /// <returns></returns>
        public static CharaPart FromXml(XmlNode node)
        {
            if (node.Name != "CharaPart")
                throw new InvalidOperationException("Unexpected XML node name!");

            Int32 layer = Int32.Parse(node.Attributes["layer"].InnerText);
            String filename = node.Attributes["filename"].InnerText;
            String color = node.Attributes["color"].InnerText;

            return new CharaPart(layer, filename, color);
        }
        /// <summary>
        /// Writes chara part information into an xml stream.
        /// </summary>
        /// <param name="xw"></param>
        public void ToXml(XmlWriter xw)
        {
            xw.WriteStartElement("CharaPart");
            xw.WriteAttributeString("layer", UnadjustedLayer.ToString());
            xw.WriteAttributeString("filename", FileName);
            xw.WriteAttributeString("color", ColorScheme);
            xw.WriteEndElement();
        }
    }
}
