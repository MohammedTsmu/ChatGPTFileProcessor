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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
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
            this.textBoxStatus = new System.Windows.Forms.TextBox();
            this.buttonProcessFile = new System.Windows.Forms.Button();
            this.statusLabe = new DevExpress.XtraEditors.LabelControl();
            this.developerProfileLinkLabel = new System.Windows.Forms.LinkLabel();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.comboBoxModel = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.buttonsToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.labelsToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 18);
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
            this.textBoxAPIKey.Size = new System.Drawing.Size(666, 22);
            this.textBoxAPIKey.TabIndex = 1;
            this.labelsToolTip.SetToolTip(this.textBoxAPIKey, "Enter You Api Key Generated From ChatGPT Website");
            // 
            // buttonSaveAPIKey
            // 
            this.buttonSaveAPIKey.Location = new System.Drawing.Point(6, 65);
            this.buttonSaveAPIKey.Name = "buttonSaveAPIKey";
            this.buttonSaveAPIKey.Size = new System.Drawing.Size(152, 44);
            this.buttonSaveAPIKey.TabIndex = 2;
            this.buttonSaveAPIKey.Text = "Save API Key";
            this.buttonsToolTip.SetToolTip(this.buttonSaveAPIKey, "Save The Entered Api Key In Application Settings Database");
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
            this.buttonsToolTip.SetToolTip(this.buttonEditAPIKey, "Edit The Key Then Click Edit > Then Save");
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
            this.buttonsToolTip.SetToolTip(this.buttonClearAPIKey, "Remove The Api Key From Application Database (in case to protect your personal ap" +
        "i key from being stolen)");
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
            this.buttonsToolTip.SetToolTip(this.buttonBrowseFile, "Select File To Generate Study Material From");
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
            this.labelsToolTip.SetToolTip(this.labelFileName, "Selected File Path");
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
            this.groupBox1.Size = new System.Drawing.Size(678, 143);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "API KEY";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.textBoxStatus);
            this.groupBox2.Controls.Add(this.buttonProcessFile);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.buttonBrowseFile);
            this.groupBox2.Controls.Add(this.labelFileName);
            this.groupBox2.Location = new System.Drawing.Point(12, 161);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(1004, 397);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "FILE AND PROCESS";
            // 
            // textBoxStatus
            // 
            this.textBoxStatus.BackColor = System.Drawing.Color.DarkMagenta;
            this.textBoxStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxStatus.ForeColor = System.Drawing.Color.AliceBlue;
            this.textBoxStatus.Location = new System.Drawing.Point(6, 116);
            this.textBoxStatus.Multiline = true;
            this.textBoxStatus.Name = "textBoxStatus";
            this.textBoxStatus.ReadOnly = true;
            this.textBoxStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxStatus.Size = new System.Drawing.Size(992, 263);
            this.textBoxStatus.TabIndex = 9;
            this.labelsToolTip.SetToolTip(this.textBoxStatus, "Application Log Area Were any Action Or Changes Will Be Written Here To Inform Th" +
        "e User");
            // 
            // buttonProcessFile
            // 
            this.buttonProcessFile.Location = new System.Drawing.Point(164, 37);
            this.buttonProcessFile.Name = "buttonProcessFile";
            this.buttonProcessFile.Size = new System.Drawing.Size(152, 44);
            this.buttonProcessFile.TabIndex = 8;
            this.buttonProcessFile.Text = "Process File";
            this.buttonsToolTip.SetToolTip(this.buttonProcessFile, "Start Generating Process");
            this.buttonProcessFile.UseVisualStyleBackColor = true;
            this.buttonProcessFile.Click += new System.EventHandler(this.buttonProcessFile_Click);
            // 
            // statusLabe
            // 
            this.statusLabe.Location = new System.Drawing.Point(307, 601);
            this.statusLabe.Name = "statusLabe";
            this.statusLabe.Size = new System.Drawing.Size(75, 16);
            this.statusLabe.TabIndex = 10;
            this.statusLabe.Text = "labelControl1";
            // 
            // developerProfileLinkLabel
            // 
            this.developerProfileLinkLabel.AutoSize = true;
            this.developerProfileLinkLabel.Location = new System.Drawing.Point(12, 598);
            this.developerProfileLinkLabel.Name = "developerProfileLinkLabel";
            this.developerProfileLinkLabel.Size = new System.Drawing.Size(185, 16);
            this.developerProfileLinkLabel.TabIndex = 9;
            this.developerProfileLinkLabel.TabStop = true;
            this.developerProfileLinkLabel.Text = "Dr. Mohammed Studio [v1.0.0]";
            this.developerProfileLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.developerProfileLinkLabel_LinkClicked);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.comboBoxModel);
            this.groupBox4.Controls.Add(this.label3);
            this.groupBox4.Location = new System.Drawing.Point(696, 12);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(320, 143);
            this.groupBox4.TabIndex = 11;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "MODELS";
            // 
            // comboBoxModel
            // 
            this.comboBoxModel.FormattingEnabled = true;
            this.comboBoxModel.Location = new System.Drawing.Point(6, 37);
            this.comboBoxModel.MaxDropDownItems = 15;
            this.comboBoxModel.Name = "comboBoxModel";
            this.comboBoxModel.Size = new System.Drawing.Size(308, 24);
            this.comboBoxModel.TabIndex = 1;
            this.labelsToolTip.SetToolTip(this.comboBoxModel, "Select One Of ChatGPT AI Models To Be Used In Generating Process");
            this.comboBoxModel.SelectedIndexChanged += new System.EventHandler(this.comboBoxModel_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 18);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(98, 16);
            this.label3.TabIndex = 0;
            this.label3.Text = "Choose Model:";
            // 
            // groupBox5
            // 
            this.groupBox5.Location = new System.Drawing.Point(12, 564);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(1004, 31);
            this.groupBox5.TabIndex = 13;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "STATUS";
            // 
            // buttonsToolTip
            // 
            this.buttonsToolTip.AutoPopDelay = 5000;
            this.buttonsToolTip.InitialDelay = 500;
            this.buttonsToolTip.ReshowDelay = 100;
            this.buttonsToolTip.ToolTipTitle = "Click To";
            // 
            // labelsToolTip
            // 
            this.labelsToolTip.AutoPopDelay = 5000;
            this.labelsToolTip.InitialDelay = 500;
            this.labelsToolTip.ReshowDelay = 100;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1028, 625);
            this.Controls.Add(this.developerProfileLinkLabel);
            this.Controls.Add(this.statusLabe);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ChatGPT File Processor";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private System.Windows.Forms.TextBox textBoxStatus;
        private System.Windows.Forms.Button buttonProcessFile;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.ComboBox comboBoxModel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.ToolTip buttonsToolTip;
        private System.Windows.Forms.ToolTip labelsToolTip;
        private System.Windows.Forms.LinkLabel developerProfileLinkLabel;
        private DevExpress.XtraEditors.LabelControl statusLabe;
    }
}

