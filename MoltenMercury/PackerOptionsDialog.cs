using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MoltenMercury.DataModel;

/* ============================================================================
 * PackerOptionsDialog.cs
 * ----------------------------------------------------------------------------
 * 2012 (C) Jieni Luchijinzhou a.k.a Aragorn Wyvernzora
 *   
 * Simple dialog that pops up when a MCPAK is generated.
 * 
 */

namespace MoltenMercury
{
    public partial class PackerOptionsDialog : Form
    {
        internal PackerOptionsDialog(CharacterResourceManager res)
        {
            InitializeComponent();

            if (res.FileSystemProxy is AFSFileSystemProxy)
            {
                rbSaveAllWithState.Enabled = false;
            }

        }

        public String CharacterName
        {
            get { return textBox1.Text; }
            set { textBox1.Text = value; }
        }

        public Boolean OmitUnusedFiles
        {
            get
            {
                return rbSaveTrimLock.Checked || rbSaveTrimNoLock.Checked;
            }
        }

        public Boolean SaveState
        {
            get { return true; }
        }

        public Boolean LockFile
        {
            get
            {
                return rbSaveTrimLock.Checked;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }
        
    }
}
