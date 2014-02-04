namespace MoltenMercury
{
    partial class PackerOptionsDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PackerOptionsDialog));
            this.label1 = new System.Windows.Forms.Label();
            this.rbSaveAllWithState = new System.Windows.Forms.RadioButton();
            this.rbSaveTrimNoLock = new System.Windows.Forms.RadioButton();
            this.rbSaveTrimLock = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(225, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Please select one of the following modes:";
            // 
            // rbSaveAllWithState
            // 
            this.rbSaveAllWithState.AutoSize = true;
            this.rbSaveAllWithState.Font = new System.Drawing.Font("Microsoft YaHei", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rbSaveAllWithState.Location = new System.Drawing.Point(12, 61);
            this.rbSaveAllWithState.Name = "rbSaveAllWithState";
            this.rbSaveAllWithState.Size = new System.Drawing.Size(316, 20);
            this.rbSaveAllWithState.TabIndex = 1;
            this.rbSaveAllWithState.Text = "Pack ALL resources and character state into single file";
            this.rbSaveAllWithState.UseVisualStyleBackColor = true;
            // 
            // rbSaveTrimNoLock
            // 
            this.rbSaveTrimNoLock.AutoSize = true;
            this.rbSaveTrimNoLock.Checked = true;
            this.rbSaveTrimNoLock.Font = new System.Drawing.Font("Microsoft YaHei", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rbSaveTrimNoLock.Location = new System.Drawing.Point(12, 139);
            this.rbSaveTrimNoLock.Name = "rbSaveTrimNoLock";
            this.rbSaveTrimNoLock.Size = new System.Drawing.Size(362, 20);
            this.rbSaveTrimNoLock.TabIndex = 2;
            this.rbSaveTrimNoLock.TabStop = true;
            this.rbSaveTrimNoLock.Text = "Pack ONLY selected resources without locking character state";
            this.rbSaveTrimNoLock.UseVisualStyleBackColor = true;
            // 
            // rbSaveTrimLock
            // 
            this.rbSaveTrimLock.AutoSize = true;
            this.rbSaveTrimLock.Font = new System.Drawing.Font("Microsoft YaHei", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rbSaveTrimLock.Location = new System.Drawing.Point(12, 225);
            this.rbSaveTrimLock.Name = "rbSaveTrimLock";
            this.rbSaveTrimLock.Size = new System.Drawing.Size(329, 20);
            this.rbSaveTrimLock.TabIndex = 3;
            this.rbSaveTrimLock.Text = "Pack ONLY selected resources and LOCK character state";
            this.rbSaveTrimLock.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(29, 79);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(527, 48);
            this.label2.TabIndex = 4;
            this.label2.Text = resources.GetString("label2.Text");
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(29, 162);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(519, 48);
            this.label3.TabIndex = 5;
            this.label3.Text = resources.GetString("label3.Text");
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(29, 248);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(519, 48);
            this.label4.TabIndex = 6;
            this.label4.Text = resources.GetString("label4.Text");
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(524, 308);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 7;
            this.button1.Text = "Continue";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.Location = new System.Drawing.Point(443, 308);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 8;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 9);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(72, 16);
            this.label5.TabIndex = 9;
            this.label5.Text = "Chara Name";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(90, 6);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(509, 22);
            this.textBox1.TabIndex = 10;
            // 
            // PackerOptionsDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(611, 343);
            this.ControlBox = false;
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.rbSaveTrimLock);
            this.Controls.Add(this.rbSaveTrimNoLock);
            this.Controls.Add(this.rbSaveAllWithState);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Microsoft YaHei", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "PackerOptionsDialog";
            this.Text = "Pack Character Options";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton rbSaveAllWithState;
        private System.Windows.Forms.RadioButton rbSaveTrimNoLock;
        private System.Windows.Forms.RadioButton rbSaveTrimLock;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox1;


    }
}