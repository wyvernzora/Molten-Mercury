using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

/* ============================================================================
 * AboutDialog.cs
 * ----------------------------------------------------------------------------
 * 2012 (C) Jieni Luchijinzhou a.k.a Aragorn Wyvernzora
 *   
 * Name speaks for itself, nothing fancy here.
 */

namespace MoltenMercury
{
    public partial class AboutDialog : Form
    {
        public AboutDialog()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;
            if (File.Exists(Path.Combine(Application.StartupPath, "mcu.exe")))
                tcAbout.TabPages.Add(tpAboutMCU);

            lblAboutMC.Text = Localization.LocalizationDictionary.Instance["ABOUT_MC"];
            lblAboutMCU.Text = Localization.LocalizationDictionary.Instance["ABOUT_MCU"];
        }

        private void AboutDialog_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
