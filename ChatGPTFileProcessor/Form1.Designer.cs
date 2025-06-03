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
            DevExpress.Utils.SuperToolTip superToolTip4 = new DevExpress.Utils.SuperToolTip();
            DevExpress.Utils.ToolTipItem toolTipItem4 = new DevExpress.Utils.ToolTipItem();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.labelControl5 = new DevExpress.XtraEditors.LabelControl();
            this.textEditAPIKey = new DevExpress.XtraEditors.TextEdit();
            this.buttonClearAPIKey = new DevExpress.XtraEditors.SimpleButton();
            this.buttonEditAPIKey = new DevExpress.XtraEditors.SimpleButton();
            this.buttonSaveAPIKey = new DevExpress.XtraEditors.SimpleButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.labelFileName = new DevExpress.XtraEditors.LabelControl();
            this.chkMedicalMaterial = new DevExpress.XtraEditors.CheckEdit();
            this.buttonProcessFile = new DevExpress.XtraEditors.SimpleButton();
            this.buttonBrowseFile = new DevExpress.XtraEditors.SimpleButton();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.cmbVocabLang = new DevExpress.XtraEditors.ComboBoxEdit();
            this.cmbGeneralLang = new DevExpress.XtraEditors.ComboBoxEdit();
            this.chkVocabulary = new DevExpress.XtraEditors.CheckEdit();
            this.chkFlashcards = new DevExpress.XtraEditors.CheckEdit();
            this.chkMCQs = new DevExpress.XtraEditors.CheckEdit();
            this.chkDefinitions = new DevExpress.XtraEditors.CheckEdit();
            this.textBoxStatus = new System.Windows.Forms.TextBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.labelControl4 = new DevExpress.XtraEditors.LabelControl();
            this.comboBoxModel = new System.Windows.Forms.ComboBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.buttonsToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.labelsToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.panelTopLeft = new System.Windows.Forms.Panel();
            this.panelTopRight = new System.Windows.Forms.Panel();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.panelTop = new System.Windows.Forms.Panel();
            this.panelFill = new System.Windows.Forms.Panel();
            this.comboBoxEditModel = new DevExpress.XtraEditors.ComboBoxEdit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.textEditAPIKey.Properties)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chkMedicalMaterial.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cmbVocabLang.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cmbGeneralLang.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkVocabulary.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkFlashcards.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkMCQs.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkDefinitions.Properties)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.panelTopLeft.SuspendLayout();
            this.panelTopRight.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.panelTop.SuspendLayout();
            this.panelFill.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.comboBoxEditModel.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.groupBox1.Controls.Add(this.labelControl5);
            this.groupBox1.Controls.Add(this.textEditAPIKey);
            this.groupBox1.Controls.Add(this.buttonClearAPIKey);
            this.groupBox1.Controls.Add(this.buttonEditAPIKey);
            this.groupBox1.Controls.Add(this.buttonSaveAPIKey);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(5, 5);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1052, 166);
            this.groupBox1.TabIndex = 10;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "API KEY";
            // 
            // labelControl5
            // 
            this.labelControl5.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl5.Appearance.Options.UseFont = true;
            this.labelControl5.Location = new System.Drawing.Point(7, 28);
            this.labelControl5.Name = "labelControl5";
            this.labelControl5.Size = new System.Drawing.Size(134, 22);
            this.labelControl5.TabIndex = 24;
            this.labelControl5.Text = "ChatGPT API Key";
            // 
            // textEditAPIKey
            // 
            this.textEditAPIKey.Location = new System.Drawing.Point(6, 56);
            this.textEditAPIKey.Name = "textEditAPIKey";
            this.textEditAPIKey.Properties.Appearance.Font = new System.Drawing.Font("LBC", 10.2F);
            this.textEditAPIKey.Properties.Appearance.Options.UseFont = true;
            this.textEditAPIKey.Properties.AppearanceDisabled.Font = new System.Drawing.Font("LBC", 10.2F);
            this.textEditAPIKey.Properties.AppearanceDisabled.Options.UseFont = true;
            this.textEditAPIKey.Properties.AppearanceFocused.Font = new System.Drawing.Font("LBC", 10.2F);
            this.textEditAPIKey.Properties.AppearanceFocused.Options.UseFont = true;
            this.textEditAPIKey.Properties.AppearanceReadOnly.Font = new System.Drawing.Font("LBC", 10.2F);
            this.textEditAPIKey.Properties.AppearanceReadOnly.Options.UseFont = true;
            this.textEditAPIKey.Properties.NullText = "ChatGPT API Key ... Enter Here";
            this.textEditAPIKey.Properties.PasswordChar = '*';
            this.textEditAPIKey.Size = new System.Drawing.Size(564, 28);
            this.textEditAPIKey.TabIndex = 23;
            // 
            // buttonClearAPIKey
            // 
            this.buttonClearAPIKey.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonClearAPIKey.Appearance.Options.UseFont = true;
            this.buttonClearAPIKey.ImageOptions.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("buttonClearAPIKey.ImageOptions.SvgImage")));
            this.buttonClearAPIKey.ImageOptions.SvgImageSize = new System.Drawing.Size(25, 25);
            this.buttonClearAPIKey.Location = new System.Drawing.Point(386, 90);
            this.buttonClearAPIKey.Name = "buttonClearAPIKey";
            this.buttonClearAPIKey.Size = new System.Drawing.Size(184, 44);
            this.buttonClearAPIKey.TabIndex = 22;
            this.buttonClearAPIKey.Text = "Clear API Key";
            this.buttonClearAPIKey.Click += new System.EventHandler(this.buttonClearAPIKey_Click);
            // 
            // buttonEditAPIKey
            // 
            this.buttonEditAPIKey.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonEditAPIKey.Appearance.Options.UseFont = true;
            this.buttonEditAPIKey.ImageOptions.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("buttonEditAPIKey.ImageOptions.SvgImage")));
            this.buttonEditAPIKey.ImageOptions.SvgImageSize = new System.Drawing.Size(25, 25);
            this.buttonEditAPIKey.Location = new System.Drawing.Point(196, 90);
            this.buttonEditAPIKey.Name = "buttonEditAPIKey";
            this.buttonEditAPIKey.Size = new System.Drawing.Size(184, 44);
            this.buttonEditAPIKey.TabIndex = 21;
            this.buttonEditAPIKey.Text = "Edit API Key";
            this.buttonEditAPIKey.Click += new System.EventHandler(this.buttonEditAPIKey_Click);
            // 
            // buttonSaveAPIKey
            // 
            this.buttonSaveAPIKey.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSaveAPIKey.Appearance.Options.UseFont = true;
            this.buttonSaveAPIKey.ImageOptions.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("buttonSaveAPIKey.ImageOptions.SvgImage")));
            this.buttonSaveAPIKey.ImageOptions.SvgImageSize = new System.Drawing.Size(25, 25);
            this.buttonSaveAPIKey.Location = new System.Drawing.Point(6, 90);
            this.buttonSaveAPIKey.Name = "buttonSaveAPIKey";
            this.buttonSaveAPIKey.Size = new System.Drawing.Size(184, 44);
            this.buttonSaveAPIKey.TabIndex = 20;
            this.buttonSaveAPIKey.Text = "Save API Key";
            this.buttonSaveAPIKey.Click += new System.EventHandler(this.buttonSaveAPIKey_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.BackColor = System.Drawing.SystemColors.ControlLight;
            this.groupBox2.Controls.Add(this.labelControl3);
            this.groupBox2.Controls.Add(this.labelFileName);
            this.groupBox2.Controls.Add(this.chkMedicalMaterial);
            this.groupBox2.Controls.Add(this.buttonProcessFile);
            this.groupBox2.Controls.Add(this.buttonBrowseFile);
            this.groupBox2.Controls.Add(this.labelControl2);
            this.groupBox2.Controls.Add(this.labelControl1);
            this.groupBox2.Controls.Add(this.cmbVocabLang);
            this.groupBox2.Controls.Add(this.cmbGeneralLang);
            this.groupBox2.Controls.Add(this.chkVocabulary);
            this.groupBox2.Controls.Add(this.chkFlashcards);
            this.groupBox2.Controls.Add(this.chkMCQs);
            this.groupBox2.Controls.Add(this.chkDefinitions);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox2.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox2.Location = new System.Drawing.Point(5, 5);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(1052, 274);
            this.groupBox2.TabIndex = 11;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "FILE AND PROCESS";
            // 
            // labelControl3
            // 
            this.labelControl3.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl3.Appearance.Options.UseFont = true;
            this.labelControl3.Location = new System.Drawing.Point(7, 154);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(86, 22);
            this.labelControl3.TabIndex = 22;
            this.labelControl3.Text = "Select File";
            // 
            // labelFileName
            // 
            this.labelFileName.Appearance.BackColor = System.Drawing.Color.LightGray;
            this.labelFileName.Appearance.Font = new System.Drawing.Font("LBC", 10.2F);
            this.labelFileName.Appearance.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.labelFileName.Appearance.Options.UseBackColor = true;
            this.labelFileName.Appearance.Options.UseFont = true;
            this.labelFileName.Appearance.Options.UseForeColor = true;
            this.labelFileName.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            this.labelFileName.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.labelFileName.Location = new System.Drawing.Point(3, 239);
            this.labelFileName.Name = "labelFileName";
            this.labelFileName.Padding = new System.Windows.Forms.Padding(5);
            this.labelFileName.Size = new System.Drawing.Size(1046, 32);
            toolTipItem4.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            toolTipItem4.Appearance.Options.UseFont = true;
            toolTipItem4.Text = "Selected File Path";
            superToolTip4.Items.Add(toolTipItem4);
            this.labelFileName.SuperTip = superToolTip4;
            this.labelFileName.TabIndex = 21;
            this.labelFileName.Text = "No file selected";
            // 
            // chkMedicalMaterial
            // 
            this.chkMedicalMaterial.Location = new System.Drawing.Point(7, 60);
            this.chkMedicalMaterial.Name = "chkMedicalMaterial";
            this.chkMedicalMaterial.Properties.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkMedicalMaterial.Properties.Appearance.Options.UseFont = true;
            this.chkMedicalMaterial.Properties.Caption = "Medical Material Only";
            this.chkMedicalMaterial.Size = new System.Drawing.Size(233, 26);
            this.chkMedicalMaterial.TabIndex = 20;
            this.chkMedicalMaterial.CheckedChanged += new System.EventHandler(this.chkMedicalMaterial_CheckedChanged);
            // 
            // buttonProcessFile
            // 
            this.buttonProcessFile.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonProcessFile.Appearance.Options.UseFont = true;
            this.buttonProcessFile.ImageOptions.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("buttonProcessFile.ImageOptions.SvgImage")));
            this.buttonProcessFile.ImageOptions.SvgImageSize = new System.Drawing.Size(25, 25);
            this.buttonProcessFile.Location = new System.Drawing.Point(197, 182);
            this.buttonProcessFile.Name = "buttonProcessFile";
            this.buttonProcessFile.Size = new System.Drawing.Size(184, 44);
            this.buttonProcessFile.TabIndex = 19;
            this.buttonProcessFile.Text = "Process File";
            this.buttonProcessFile.Click += new System.EventHandler(this.buttonProcessFile_Click);
            // 
            // buttonBrowseFile
            // 
            this.buttonBrowseFile.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonBrowseFile.Appearance.Options.UseFont = true;
            this.buttonBrowseFile.ImageOptions.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("buttonBrowseFile.ImageOptions.SvgImage")));
            this.buttonBrowseFile.ImageOptions.SvgImageSize = new System.Drawing.Size(25, 25);
            this.buttonBrowseFile.Location = new System.Drawing.Point(7, 182);
            this.buttonBrowseFile.Name = "buttonBrowseFile";
            this.buttonBrowseFile.Size = new System.Drawing.Size(184, 44);
            this.buttonBrowseFile.TabIndex = 18;
            this.buttonBrowseFile.Text = "Browse";
            this.buttonBrowseFile.Click += new System.EventHandler(this.buttonBrowseFile_Click);
            // 
            // labelControl2
            // 
            this.labelControl2.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Location = new System.Drawing.Point(7, 126);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(269, 22);
            this.labelControl2.TabIndex = 17;
            this.labelControl2.Text = "Translation Language (Vocabulary)";
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(7, 92);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(390, 22);
            this.labelControl1.TabIndex = 16;
            this.labelControl1.Text = "General Language (Definition - Mcqs - Flashcards)";
            // 
            // cmbVocabLang
            // 
            this.cmbVocabLang.Cursor = System.Windows.Forms.Cursors.Hand;
            this.cmbVocabLang.Location = new System.Drawing.Point(437, 123);
            this.cmbVocabLang.Name = "cmbVocabLang";
            this.cmbVocabLang.Properties.Appearance.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbVocabLang.Properties.Appearance.Options.UseFont = true;
            this.cmbVocabLang.Properties.AppearanceDisabled.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbVocabLang.Properties.AppearanceDisabled.Options.UseFont = true;
            this.cmbVocabLang.Properties.AppearanceDropDown.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbVocabLang.Properties.AppearanceDropDown.Options.UseFont = true;
            this.cmbVocabLang.Properties.AppearanceFocused.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbVocabLang.Properties.AppearanceFocused.Options.UseFont = true;
            this.cmbVocabLang.Properties.AppearanceItemDisabled.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbVocabLang.Properties.AppearanceItemDisabled.Options.UseFont = true;
            this.cmbVocabLang.Properties.AppearanceItemHighlight.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbVocabLang.Properties.AppearanceItemHighlight.Options.UseFont = true;
            this.cmbVocabLang.Properties.AppearanceItemSelected.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbVocabLang.Properties.AppearanceItemSelected.Options.UseFont = true;
            this.cmbVocabLang.Properties.AppearanceReadOnly.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbVocabLang.Properties.AppearanceReadOnly.Options.UseFont = true;
            this.cmbVocabLang.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cmbVocabLang.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.cmbVocabLang.Size = new System.Drawing.Size(324, 28);
            this.cmbVocabLang.TabIndex = 15;
            this.cmbVocabLang.SelectedIndexChanged += new System.EventHandler(this.cmbVocabLang_SelectedIndexChanged);
            // 
            // cmbGeneralLang
            // 
            this.cmbGeneralLang.Cursor = System.Windows.Forms.Cursors.Hand;
            this.cmbGeneralLang.Location = new System.Drawing.Point(437, 89);
            this.cmbGeneralLang.Name = "cmbGeneralLang";
            this.cmbGeneralLang.Properties.Appearance.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbGeneralLang.Properties.Appearance.Options.UseFont = true;
            this.cmbGeneralLang.Properties.AppearanceDisabled.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbGeneralLang.Properties.AppearanceDisabled.Options.UseFont = true;
            this.cmbGeneralLang.Properties.AppearanceDropDown.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbGeneralLang.Properties.AppearanceDropDown.Options.UseFont = true;
            this.cmbGeneralLang.Properties.AppearanceFocused.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbGeneralLang.Properties.AppearanceFocused.Options.UseFont = true;
            this.cmbGeneralLang.Properties.AppearanceItemDisabled.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbGeneralLang.Properties.AppearanceItemDisabled.Options.UseFont = true;
            this.cmbGeneralLang.Properties.AppearanceItemHighlight.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbGeneralLang.Properties.AppearanceItemHighlight.Options.UseFont = true;
            this.cmbGeneralLang.Properties.AppearanceItemSelected.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbGeneralLang.Properties.AppearanceItemSelected.Options.UseFont = true;
            this.cmbGeneralLang.Properties.AppearanceReadOnly.Font = new System.Drawing.Font("LBC", 10.2F);
            this.cmbGeneralLang.Properties.AppearanceReadOnly.Options.UseFont = true;
            this.cmbGeneralLang.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.cmbGeneralLang.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.cmbGeneralLang.Size = new System.Drawing.Size(324, 28);
            this.cmbGeneralLang.TabIndex = 14;
            this.cmbGeneralLang.SelectedIndexChanged += new System.EventHandler(this.cmbGeneralLang_SelectedIndexChanged);
            // 
            // chkVocabulary
            // 
            this.chkVocabulary.Location = new System.Drawing.Point(724, 28);
            this.chkVocabulary.Name = "chkVocabulary";
            this.chkVocabulary.Properties.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkVocabulary.Properties.Appearance.Options.UseFont = true;
            this.chkVocabulary.Properties.Caption = "Generate Vocabulary";
            this.chkVocabulary.Size = new System.Drawing.Size(233, 26);
            this.chkVocabulary.TabIndex = 13;
            this.chkVocabulary.CheckedChanged += new System.EventHandler(this.chkVocabulary_CheckedChanged);
            // 
            // chkFlashcards
            // 
            this.chkFlashcards.Location = new System.Drawing.Point(485, 28);
            this.chkFlashcards.Name = "chkFlashcards";
            this.chkFlashcards.Properties.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkFlashcards.Properties.Appearance.Options.UseFont = true;
            this.chkFlashcards.Properties.Caption = "Generate Flashcards";
            this.chkFlashcards.Size = new System.Drawing.Size(233, 26);
            this.chkFlashcards.TabIndex = 12;
            this.chkFlashcards.CheckedChanged += new System.EventHandler(this.chkFlashcards_CheckedChanged);
            // 
            // chkMCQs
            // 
            this.chkMCQs.Location = new System.Drawing.Point(246, 28);
            this.chkMCQs.Name = "chkMCQs";
            this.chkMCQs.Properties.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkMCQs.Properties.Appearance.Options.UseFont = true;
            this.chkMCQs.Properties.Caption = "Generate MCQs";
            this.chkMCQs.Size = new System.Drawing.Size(233, 26);
            this.chkMCQs.TabIndex = 11;
            this.chkMCQs.CheckedChanged += new System.EventHandler(this.chkMCQs_CheckedChanged);
            // 
            // chkDefinitions
            // 
            this.chkDefinitions.Location = new System.Drawing.Point(7, 28);
            this.chkDefinitions.Name = "chkDefinitions";
            this.chkDefinitions.Properties.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkDefinitions.Properties.Appearance.Options.UseFont = true;
            this.chkDefinitions.Properties.Caption = "Generate Definitions";
            this.chkDefinitions.Size = new System.Drawing.Size(233, 26);
            this.chkDefinitions.TabIndex = 10;
            this.chkDefinitions.CheckedChanged += new System.EventHandler(this.chkDefinitions_CheckedChanged);
            // 
            // textBoxStatus
            // 
            this.textBoxStatus.BackColor = System.Drawing.Color.Black;
            this.textBoxStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxStatus.ForeColor = System.Drawing.Color.White;
            this.textBoxStatus.Location = new System.Drawing.Point(3, 25);
            this.textBoxStatus.Multiline = true;
            this.textBoxStatus.Name = "textBoxStatus";
            this.textBoxStatus.ReadOnly = true;
            this.textBoxStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxStatus.Size = new System.Drawing.Size(1046, 175);
            this.textBoxStatus.TabIndex = 9;
            this.labelsToolTip.SetToolTip(this.textBoxStatus, "Application Log Area Were any Action Or Changes Will Be Written Here To Inform Th" +
        "e User");
            // 
            // groupBox4
            // 
            this.groupBox4.BackColor = System.Drawing.SystemColors.ControlLight;
            this.groupBox4.Controls.Add(this.comboBoxEditModel);
            this.groupBox4.Controls.Add(this.labelControl4);
            this.groupBox4.Controls.Add(this.comboBoxModel);
            this.groupBox4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox4.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox4.Location = new System.Drawing.Point(5, 5);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(439, 166);
            this.groupBox4.TabIndex = 11;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "MODELS";
            // 
            // labelControl4
            // 
            this.labelControl4.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl4.Appearance.Options.UseFont = true;
            this.labelControl4.Location = new System.Drawing.Point(6, 28);
            this.labelControl4.Name = "labelControl4";
            this.labelControl4.Size = new System.Drawing.Size(113, 22);
            this.labelControl4.TabIndex = 23;
            this.labelControl4.Text = "Choose Model";
            // 
            // comboBoxModel
            // 
            this.comboBoxModel.FormattingEnabled = true;
            this.comboBoxModel.Location = new System.Drawing.Point(6, 56);
            this.comboBoxModel.MaxDropDownItems = 15;
            this.comboBoxModel.Name = "comboBoxModel";
            this.comboBoxModel.Size = new System.Drawing.Size(426, 30);
            this.comboBoxModel.TabIndex = 1;
            this.labelsToolTip.SetToolTip(this.comboBoxModel, "Select One Of ChatGPT AI Models To Be Used In Generating Process");
            this.comboBoxModel.SelectedIndexChanged += new System.EventHandler(this.comboBoxModel_SelectedIndexChanged);
            // 
            // groupBox5
            // 
            this.groupBox5.BackColor = System.Drawing.SystemColors.ControlLight;
            this.groupBox5.Controls.Add(this.textBoxStatus);
            this.groupBox5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox5.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox5.Location = new System.Drawing.Point(5, 5);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(1052, 203);
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
            // panelTopLeft
            // 
            this.panelTopLeft.Controls.Add(this.groupBox1);
            this.panelTopLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelTopLeft.Location = new System.Drawing.Point(0, 0);
            this.panelTopLeft.Name = "panelTopLeft";
            this.panelTopLeft.Padding = new System.Windows.Forms.Padding(5);
            this.panelTopLeft.Size = new System.Drawing.Size(1062, 176);
            this.panelTopLeft.TabIndex = 22;
            // 
            // panelTopRight
            // 
            this.panelTopRight.Controls.Add(this.groupBox4);
            this.panelTopRight.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelTopRight.Location = new System.Drawing.Point(613, 0);
            this.panelTopRight.Name = "panelTopRight";
            this.panelTopRight.Padding = new System.Windows.Forms.Padding(5);
            this.panelTopRight.Size = new System.Drawing.Size(449, 176);
            this.panelTopRight.TabIndex = 23;
            // 
            // panelBottom
            // 
            this.panelBottom.Controls.Add(this.groupBox5);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 460);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Padding = new System.Windows.Forms.Padding(5);
            this.panelBottom.Size = new System.Drawing.Size(1062, 213);
            this.panelBottom.TabIndex = 24;
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.panelTopRight);
            this.panelTop.Controls.Add(this.panelTopLeft);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1062, 176);
            this.panelTop.TabIndex = 22;
            // 
            // panelFill
            // 
            this.panelFill.Controls.Add(this.groupBox2);
            this.panelFill.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelFill.Location = new System.Drawing.Point(0, 176);
            this.panelFill.Name = "panelFill";
            this.panelFill.Padding = new System.Windows.Forms.Padding(5);
            this.panelFill.Size = new System.Drawing.Size(1062, 284);
            this.panelFill.TabIndex = 25;
            // 
            // comboBoxEditModel
            // 
            this.comboBoxEditModel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.comboBoxEditModel.Location = new System.Drawing.Point(7, 93);
            this.comboBoxEditModel.Name = "comboBoxEditModel";
            this.comboBoxEditModel.Properties.Appearance.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Bold);
            this.comboBoxEditModel.Properties.Appearance.Options.UseFont = true;
            this.comboBoxEditModel.Properties.AppearanceDisabled.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Bold);
            this.comboBoxEditModel.Properties.AppearanceDisabled.Options.UseFont = true;
            this.comboBoxEditModel.Properties.AppearanceDropDown.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Bold);
            this.comboBoxEditModel.Properties.AppearanceDropDown.Options.UseFont = true;
            this.comboBoxEditModel.Properties.AppearanceFocused.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Bold);
            this.comboBoxEditModel.Properties.AppearanceFocused.Options.UseFont = true;
            this.comboBoxEditModel.Properties.AppearanceItemDisabled.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Bold);
            this.comboBoxEditModel.Properties.AppearanceItemDisabled.Options.UseFont = true;
            this.comboBoxEditModel.Properties.AppearanceItemHighlight.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Bold);
            this.comboBoxEditModel.Properties.AppearanceItemHighlight.Options.UseFont = true;
            this.comboBoxEditModel.Properties.AppearanceItemSelected.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Bold);
            this.comboBoxEditModel.Properties.AppearanceItemSelected.Options.UseFont = true;
            this.comboBoxEditModel.Properties.AppearanceReadOnly.Font = new System.Drawing.Font("LBC", 10.2F, System.Drawing.FontStyle.Bold);
            this.comboBoxEditModel.Properties.AppearanceReadOnly.Options.UseFont = true;
            this.comboBoxEditModel.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.comboBoxEditModel.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.comboBoxEditModel.Size = new System.Drawing.Size(425, 28);
            this.comboBoxEditModel.TabIndex = 24;
            this.comboBoxEditModel.SelectedIndexChanged += new System.EventHandler(this.comboBoxEditModel_SelectedIndexChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1062, 673);
            this.Controls.Add(this.panelFill);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.panelBottom);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ChatGPT File Processor";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.textEditAPIKey.Properties)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chkMedicalMaterial.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cmbVocabLang.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cmbGeneralLang.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkVocabulary.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkFlashcards.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkMCQs.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chkDefinitions.Properties)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.panelTopLeft.ResumeLayout(false);
            this.panelTopRight.ResumeLayout(false);
            this.panelBottom.ResumeLayout(false);
            this.panelTop.ResumeLayout(false);
            this.panelFill.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.comboBoxEditModel.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.TextBox textBoxStatus;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.ComboBox comboBoxModel;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.ToolTip buttonsToolTip;
        private System.Windows.Forms.ToolTip labelsToolTip;
        private DevExpress.XtraEditors.CheckEdit chkVocabulary;
        private DevExpress.XtraEditors.CheckEdit chkFlashcards;
        private DevExpress.XtraEditors.CheckEdit chkMCQs;
        private DevExpress.XtraEditors.CheckEdit chkDefinitions;
        private DevExpress.XtraEditors.ComboBoxEdit cmbVocabLang;
        private DevExpress.XtraEditors.ComboBoxEdit cmbGeneralLang;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.SimpleButton buttonProcessFile;
        private DevExpress.XtraEditors.SimpleButton buttonBrowseFile;
        private DevExpress.XtraEditors.SimpleButton buttonClearAPIKey;
        private DevExpress.XtraEditors.SimpleButton buttonEditAPIKey;
        private DevExpress.XtraEditors.SimpleButton buttonSaveAPIKey;
        private DevExpress.XtraEditors.CheckEdit chkMedicalMaterial;
        private DevExpress.XtraEditors.LabelControl labelFileName;
        private System.Windows.Forms.Panel panelTopLeft;
        private System.Windows.Forms.Panel panelTopRight;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Panel panelFill;
        private DevExpress.XtraEditors.TextEdit textEditAPIKey;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraEditors.LabelControl labelControl5;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.ComboBoxEdit comboBoxEditModel;
    }
}

