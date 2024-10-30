namespace ChatGPTFileProcessor
{
    partial class Form1
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
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxAPIKey = new System.Windows.Forms.TextBox();
            this.buttonSaveAPIKey = new System.Windows.Forms.Button();
            this.buttonEditAPIKey = new System.Windows.Forms.Button();
            this.buttonClearAPIKey = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonBrowseFile = new System.Windows.Forms.Button();
            this.labelFileName = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.textBoxStatus = new System.Windows.Forms.TextBox();
            this.buttonProcessFile = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.comboBoxModel = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(115, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "ChatGPT API Key:";
            // 
            // textBoxAPIKey
            // 
            this.textBoxAPIKey.Location = new System.Drawing.Point(6, 37);
            this.textBoxAPIKey.Name = "textBoxAPIKey";
            this.textBoxAPIKey.PasswordChar = '*';
            this.textBoxAPIKey.Size = new System.Drawing.Size(645, 22);
            this.textBoxAPIKey.TabIndex = 1;
            // 
            // buttonSaveAPIKey
            // 
            this.buttonSaveAPIKey.Location = new System.Drawing.Point(6, 65);
            this.buttonSaveAPIKey.Name = "buttonSaveAPIKey";
            this.buttonSaveAPIKey.Size = new System.Drawing.Size(152, 44);
            this.buttonSaveAPIKey.TabIndex = 2;
            this.buttonSaveAPIKey.Text = "Save API Key";
            this.buttonSaveAPIKey.UseVisualStyleBackColor = true;
            this.buttonSaveAPIKey.Click += new System.EventHandler(this.buttonSaveAPIKey_Click);
            // 
            // buttonEditAPIKey
            // 
            this.buttonEditAPIKey.Location = new System.Drawing.Point(164, 65);
            this.buttonEditAPIKey.Name = "buttonEditAPIKey";
            this.buttonEditAPIKey.Size = new System.Drawing.Size(152, 44);
            this.buttonEditAPIKey.TabIndex = 3;
            this.buttonEditAPIKey.Text = "Edit API Key";
            this.buttonEditAPIKey.UseVisualStyleBackColor = true;
            this.buttonEditAPIKey.Click += new System.EventHandler(this.buttonEditAPIKey_Click);
            // 
            // buttonClearAPIKey
            // 
            this.buttonClearAPIKey.Location = new System.Drawing.Point(321, 65);
            this.buttonClearAPIKey.Name = "buttonClearAPIKey";
            this.buttonClearAPIKey.Size = new System.Drawing.Size(152, 44);
            this.buttonClearAPIKey.TabIndex = 4;
            this.buttonClearAPIKey.Text = "Clear API Key";
            this.buttonClearAPIKey.UseVisualStyleBackColor = true;
            this.buttonClearAPIKey.Click += new System.EventHandler(this.buttonClearAPIKey_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 18);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 16);
            this.label2.TabIndex = 5;
            this.label2.Text = "Select File:";
            // 
            // buttonBrowseFile
            // 
            this.buttonBrowseFile.Location = new System.Drawing.Point(6, 37);
            this.buttonBrowseFile.Name = "buttonBrowseFile";
            this.buttonBrowseFile.Size = new System.Drawing.Size(152, 44);
            this.buttonBrowseFile.TabIndex = 6;
            this.buttonBrowseFile.Text = "Browse";
            this.buttonBrowseFile.UseVisualStyleBackColor = true;
            this.buttonBrowseFile.Click += new System.EventHandler(this.buttonBrowseFile_Click);
            // 
            // labelFileName
            // 
            this.labelFileName.AutoSize = true;
            this.labelFileName.BackColor = System.Drawing.Color.DarkMagenta;
            this.labelFileName.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelFileName.ForeColor = System.Drawing.SystemColors.ButtonFace;
            this.labelFileName.Location = new System.Drawing.Point(6, 84);
            this.labelFileName.Name = "labelFileName";
            this.labelFileName.Size = new System.Drawing.Size(125, 20);
            this.labelFileName.TabIndex = 7;
            this.labelFileName.Text = "No file selected";
            this.labelFileName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.buttonClearAPIKey);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.textBoxAPIKey);
            this.groupBox1.Controls.Add(this.buttonSaveAPIKey);
            this.groupBox1.Controls.Add(this.buttonEditAPIKey);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(657, 119);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "API Key Section";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.buttonBrowseFile);
            this.groupBox2.Controls.Add(this.labelFileName);
            this.groupBox2.Location = new System.Drawing.Point(12, 137);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(657, 123);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "File Selection";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.buttonProcessFile);
            this.groupBox3.Location = new System.Drawing.Point(675, 137);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(341, 123);
            this.groupBox3.TabIndex = 12;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Process";
            // 
            // textBoxStatus
            // 
            this.textBoxStatus.BackColor = System.Drawing.Color.DarkMagenta;
            this.textBoxStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxStatus.ForeColor = System.Drawing.Color.AliceBlue;
            this.textBoxStatus.Location = new System.Drawing.Point(6, 21);
            this.textBoxStatus.Multiline = true;
            this.textBoxStatus.Name = "textBoxStatus";
            this.textBoxStatus.ReadOnly = true;
            this.textBoxStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxStatus.Size = new System.Drawing.Size(992, 237);
            this.textBoxStatus.TabIndex = 9;
            // 
            // buttonProcessFile
            // 
            this.buttonProcessFile.Location = new System.Drawing.Point(6, 21);
            this.buttonProcessFile.Name = "buttonProcessFile";
            this.buttonProcessFile.Size = new System.Drawing.Size(152, 44);
            this.buttonProcessFile.TabIndex = 8;
            this.buttonProcessFile.Text = "Process File";
            this.buttonProcessFile.UseVisualStyleBackColor = true;
            this.buttonProcessFile.Click += new System.EventHandler(this.buttonProcessFile_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.comboBoxModel);
            this.groupBox4.Controls.Add(this.label3);
            this.groupBox4.Location = new System.Drawing.Point(675, 12);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(341, 119);
            this.groupBox4.TabIndex = 11;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Models Section";
            // 
            // comboBoxModel
            // 
            this.comboBoxModel.FormattingEnabled = true;
            this.comboBoxModel.Location = new System.Drawing.Point(12, 42);
            this.comboBoxModel.Name = "comboBoxModel";
            this.comboBoxModel.Size = new System.Drawing.Size(323, 24);
            this.comboBoxModel.TabIndex = 1;
            this.comboBoxModel.SelectedIndexChanged += new System.EventHandler(this.comboBoxModel_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 22);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(98, 16);
            this.label3.TabIndex = 0;
            this.label3.Text = "Choose Model:";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.textBoxStatus);
            this.groupBox5.Location = new System.Drawing.Point(12, 266);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(1004, 264);
            this.groupBox5.TabIndex = 13;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Status Section";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1028, 542);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "Form1";
            this.Text = "ChatGPT File Processor";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxAPIKey;
        private System.Windows.Forms.Button buttonSaveAPIKey;
        private System.Windows.Forms.Button buttonEditAPIKey;
        private System.Windows.Forms.Button buttonClearAPIKey;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonBrowseFile;
        private System.Windows.Forms.Label labelFileName;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.TextBox textBoxStatus;
        private System.Windows.Forms.Button buttonProcessFile;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.ComboBox comboBoxModel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox5;
    }
}

