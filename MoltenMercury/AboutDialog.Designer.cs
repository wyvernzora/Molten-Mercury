//#define EDIT_MODE

namespace MoltenMercury
{
    partial class AboutDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblAboutMC = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tcAbout = new System.Windows.Forms.TabControl();
            this.tbAboutMC = new System.Windows.Forms.TabPage();
            this.tpAboutMCU = new System.Windows.Forms.TabPage();
            this.lblAboutMCU = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.tcAbout.SuspendLayout();
            this.tbAboutMC.SuspendLayout();
            this.tpAboutMCU.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox1.Image = global::MoltenMercury.Properties.Resources.chara_00001;
            this.pictureBox1.Location = new System.Drawing.Point(312, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(300, 400);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.AboutDialog_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(6, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(150, 29);
            this.label1.TabIndex = 1;
            this.label1.Text = "MoltenChara";
            this.label1.Click += new System.EventHandler(this.AboutDialog_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label2.ForeColor = System.Drawing.Color.DimGray;
            this.label2.Location = new System.Drawing.Point(9, 31);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(256, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "2012 (C) Jieni Luchijinzhou a.k.a Aragorn Wyvernzora";
            this.label2.Click += new System.EventHandler(this.AboutDialog_Click);
            // 
            // lblAboutMC
            // 
            this.lblAboutMC.Location = new System.Drawing.Point(6, 59);
            this.lblAboutMC.Name = "lblAboutMC";
            this.lblAboutMC.Size = new System.Drawing.Size(384, 305);
            this.lblAboutMC.TabIndex = 3;
            this.lblAboutMC.Click += new System.EventHandler(this.AboutDialog_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.Location = new System.Drawing.Point(3, 377);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(211, 20);
            this.label4.TabIndex = 4;
            this.label4.Text = "Click anywhere to close...";
            this.label4.Click += new System.EventHandler(this.AboutDialog_Click);
            // 
            // tcAbout
            // 
            this.tcAbout.Controls.Add(this.tbAboutMC);
            //this.tcAbout.Controls.Add(this.tpAboutMCU);
            this.tcAbout.Location = new System.Drawing.Point(12, 12);
            this.tcAbout.Name = "tcAbout";
            this.tcAbout.SelectedIndex = 0;
            this.tcAbout.Size = new System.Drawing.Size(620, 426);
            this.tcAbout.TabIndex = 5;
            // 
            // tbAboutMC
            // 
            this.tbAboutMC.Controls.Add(this.lblAboutMC);
            this.tbAboutMC.Controls.Add(this.label1);
            this.tbAboutMC.Controls.Add(this.pictureBox1);
            this.tbAboutMC.Controls.Add(this.label2);
            this.tbAboutMC.Controls.Add(this.label4);
            this.tbAboutMC.Location = new System.Drawing.Point(4, 22);
            this.tbAboutMC.Name = "tbAboutMC";
            this.tbAboutMC.Padding = new System.Windows.Forms.Padding(3);
            this.tbAboutMC.Size = new System.Drawing.Size(612, 400);
            this.tbAboutMC.TabIndex = 0;
            this.tbAboutMC.Text = "About MoltenChara...";
            this.tbAboutMC.UseVisualStyleBackColor = true;
            this.tbAboutMC.Click += new System.EventHandler(this.AboutDialog_Click);
            // 
            // tpAboutMCU
            // 
            this.tpAboutMCU.Controls.Add(this.lblAboutMCU);
            this.tpAboutMCU.Controls.Add(this.label5);
            this.tpAboutMCU.Controls.Add(this.pictureBox2);
            this.tpAboutMCU.Controls.Add(this.label7);
            this.tpAboutMCU.Controls.Add(this.label8);
            this.tpAboutMCU.Location = new System.Drawing.Point(4, 22);
            this.tpAboutMCU.Name = "tpAboutMCU";
            this.tpAboutMCU.Padding = new System.Windows.Forms.Padding(3);
            this.tpAboutMCU.Size = new System.Drawing.Size(612, 400);
            this.tpAboutMCU.TabIndex = 1;
            this.tpAboutMCU.Text = "About MoltenChara Utilities...";
            this.tpAboutMCU.UseVisualStyleBackColor = true;
            this.tpAboutMCU.Click += new System.EventHandler(this.AboutDialog_Click);
            // 
            // lblAboutMCU
            // 
            this.lblAboutMCU.Location = new System.Drawing.Point(6, 59);
            this.lblAboutMCU.Name = "lblAboutMCU";
            this.lblAboutMCU.Size = new System.Drawing.Size(384, 305);
            this.lblAboutMCU.TabIndex = 10;
            this.lblAboutMCU.Click += new System.EventHandler(this.AboutDialog_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label5.Location = new System.Drawing.Point(6, 3);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(235, 29);
            this.label5.TabIndex = 6;
            this.label5.Text = "MoltenChara Utilities";
            this.label5.Click += new System.EventHandler(this.AboutDialog_Click);
            // 
            // pictureBox2
            // 
            this.pictureBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBox2.Image = global::MoltenMercury.Properties.Resources.chara_00002;
            this.pictureBox2.Location = new System.Drawing.Point(312, 0);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(300, 400);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox2.TabIndex = 5;
            this.pictureBox2.TabStop = false;
            this.pictureBox2.Click += new System.EventHandler(this.AboutDialog_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label7.ForeColor = System.Drawing.Color.DimGray;
            this.label7.Location = new System.Drawing.Point(9, 31);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(256, 13);
            this.label7.TabIndex = 7;
            this.label7.Text = "2012 (C) Jieni Luchijinzhou a.k.a Aragorn Wyvernzora";
            this.label7.Click += new System.EventHandler(this.AboutDialog_Click);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label8.Location = new System.Drawing.Point(3, 377);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(211, 20);
            this.label8.TabIndex = 9;
            this.label8.Text = "Click anywhere to close...";
            this.label8.Click += new System.EventHandler(this.AboutDialog_Click);
            // 
            // AboutDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(646, 452);
            this.ControlBox = false;
            this.Controls.Add(this.tcAbout);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "AboutDialog";
            this.ShowInTaskbar = false;
            this.Text = "About MoltenChara";
            this.Click += new System.EventHandler(this.AboutDialog_Click);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.tcAbout.ResumeLayout(false);
            this.tbAboutMC.ResumeLayout(false);
            this.tbAboutMC.PerformLayout();
            this.tpAboutMCU.ResumeLayout(false);
            this.tpAboutMCU.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblAboutMC;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TabControl tcAbout;
        private System.Windows.Forms.TabPage tbAboutMC;
        private System.Windows.Forms.TabPage tpAboutMCU;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label lblAboutMCU;
    }
}