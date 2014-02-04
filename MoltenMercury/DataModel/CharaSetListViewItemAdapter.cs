using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MoltenMercury.DataModel
{
    class CharaSetListViewItemAdapter : ListViewItem
    {
        CharaSet m_set;

        public CharaSetListViewItemAdapter(CharaSet cset)
            : base(new String[] { 
                cset.Name, 
                cset.Count.ToString(), 
                cset.Parent.Multiselect ? cset.LayerAdjustment.ToString() : "N/A"
            })
        {
            if (cset == null) throw new ArgumentNullException();
            m_set = cset;
            this.Checked = cset.Selected;
        }

        public CharaSet CharaSet
        { get { return m_set; } }
    }
}
