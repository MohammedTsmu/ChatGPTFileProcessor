using DevExpress.Export.Xl;
using DevExpress.Utils.CommonDialogs;
using DevExpress.Utils.MVVM;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraExport.Implementation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;
using SDImage = System.Drawing.Image;
using Xceed.Words.NET;
using Xceed.Document.NET;



namespace ChatGPTFileProcessor
{

    public partial class Form1 : Form
    {
        private readonly string apiKeyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatGPTFileProcessor", "api_key.txt");
        private readonly string modelPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatGPTFileProcessor", "model.txt");
        private string selectedPdfPath;
        private int selectedFromPage = 1;
        private int selectedToPage = 1;

        private Panel overlayPanel;
        private Label statusLabel;
        private PictureBox loadingIcon;
        private TextBox logTextBox;

        // يُسجّل آخر مجلد إخراج فعلي تم استخدامه أثناء آخر تشغيل
        private string _lastOutputRoot = null;

        // يُسجّل آخر ملف PDF اختاره المستخدم من واجهة الاختيار
        private string _lastSelectedPdfPath = null;





        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // أفرغ العناصر أولاً
            comboBoxEditModel.Properties.Items.Clear();

            // أضف الموديلات المتوفرة
            comboBoxEditModel.Properties.Items.Add("gpt-4o"); // Add gpt-4o model to the combo box
            comboBoxEditModel.Properties.Items.Add("gpt-5");


            InitializeOverlay();

            // Load API key and model selection
            LoadApiKeyAndModel();

            // Load saved user preferences for checkboxes
            loadCheckBoxesSettings();

            // Load output folder path
            textEditOutputFolder.Text = GetOutputFolder();


            // ▼ Populate the “General Language” dropdown
            cmbGeneralLang.Properties.Items.Clear();
            foreach (var lang in _supportedLanguages)
            {
                cmbGeneralLang.Properties.Items.Add(lang.DisplayName);
            }

            //load saved general language, default to English if not set
            var savedGen = Properties.Settings.Default.GeneralLanguage;
            if (!string.IsNullOrWhiteSpace(savedGen) &&
                _supportedLanguages.Any(x => x.DisplayName == savedGen))
            {
                cmbGeneralLang.SelectedIndex = Array.FindIndex(_supportedLanguages, x => x.DisplayName == savedGen);
            }
            else
            {
                cmbGeneralLang.SelectedIndex = 0; // “English”
            }

            // ▼ Populate the “Vocabulary Target Language” dropdown
            cmbVocabLang.Properties.Items.Clear();
            foreach (var lang in _supportedLanguages)
            {
                cmbVocabLang.Properties.Items.Add(lang.DisplayName);
            }

            //load saved vocab language, default to Arabic if not set
            var savedVocab = Properties.Settings.Default.VocabLanguage;
            if (!string.IsNullOrWhiteSpace(savedVocab) && _supportedLanguages.Any(x => x.DisplayName == savedVocab))
            {
                cmbVocabLang.SelectedIndex = Array.FindIndex(_supportedLanguages, x => x.DisplayName == savedVocab);
            }
            else
            {
                // default “Arabic”
                int idxAr = Array.FindIndex(_supportedLanguages, x => x.Code == "ar");
                cmbVocabLang.SelectedIndex = idxAr >= 0 ? idxAr : 0;
            }


            // ▼ Load saved page batch mode last setting
            var savedMode = Properties.Settings.Default.PageBatchMode; // 1, 2, 3 or 4
            if (savedMode >= 1 && savedMode <= 4)
                radioPageBatchSize.EditValue = savedMode;
            else
                radioPageBatchSize.EditValue = 1;


            //// ▼ Populate the “Delimiter” dropdown of the csv export feature
            //cmbDelimiter.Properties.Items.AddRange(new[] { "Tab (TSV)", "Comma (CSV)" });
            //cmbDelimiter.SelectedIndex = 0; // default to TSV
        }


        private void buttonSaveAPIKey_Click(object sender, EventArgs e)
        {
            string apiKey = textEditAPIKey.Text.Trim();
            if (!string.IsNullOrEmpty(apiKey))
            {
                File.WriteAllText(apiKeyPath, apiKey);
                textEditAPIKey.ReadOnly = true;  // Make it read-only after saving
                UpdateStatus("API Key saved successfully.");


                UpdateStatus("API Key Saved...");
                UpdateStatus("API Key Locked Succesfully...");

                // Save the state of the TextEdit control to settings
                Properties.Settings.Default.ApiKeyLock = true;
                Properties.Settings.Default.Save();
            }
            else
            {
                UpdateStatus("API Key cannot be empty.");
            }
        }

        private void buttonEditAPIKey_Click(object sender, EventArgs e)
        {
            textEditAPIKey.ReadOnly = false;  // Allow editing
            UpdateStatus("Editing API Key. Don't forget to save after changes.");
        }

        private void buttonClearAPIKey_Click(object sender, EventArgs e)
        {
            if (File.Exists(apiKeyPath))
            {
                File.Delete(apiKeyPath);
                textEditAPIKey.Clear(); // Clear the text edit control
                UpdateStatus("API Key cleared successfully.");
            }
            else
            {
                UpdateStatus("No API Key found to clear.");
            }
        }

        private void UpdateStatus(string message)
        {
            textBoxStatus.AppendText(message + Environment.NewLine);
        }

        private string GetOutputFolder()
        {
            var saved = Properties.Settings.Default.OutputFolder;
            // fallback إلى Desktop إن لم يكن محفوظًا أو غير موجود
            if (string.IsNullOrWhiteSpace(saved) || !Directory.Exists(saved))
                return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            return saved;
        }

        private void SetOutputFolder(string folder)
        {
            if (!string.IsNullOrWhiteSpace(folder))
            {
                Properties.Settings.Default.OutputFolder = folder;
                Properties.Settings.Default.Save();
                textEditOutputFolder.Text = folder;
            }
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        // تُعيد المجلد النهائي للجلسة بحسب الخيارات
        private string ResolveBaseOutputFolder(string pdfPath, string timeStamp, string modelName)
        {
            // 1) أساس الاختيار: بجانب PDF أو المجلد المخصص
            string baseFolder = Properties.Settings.Default.SaveBesidePdf && File.Exists(pdfPath)
                ? Path.GetDirectoryName(pdfPath)
                : GetOutputFolder();

            EnsureDir(baseFolder);

            // 2) مجلد جلسة لكل تشغيل (اختياري)
            if (Properties.Settings.Default.UseSessionFolder)
            {
                string sessionFolder = Path.Combine(baseFolder, $"{timeStamp}_{modelName}");
                EnsureDir(sessionFolder);
                return sessionFolder;
            }
            return baseFolder;
        }

        // إن كان خيار "OrganizeByType" مفعلًا، يحفظ داخل مجلد فرعي حسب النوع
        private string PathInTypeFolder(string baseFolder, string typeFolderName, string fileName)
        {
            if (Properties.Settings.Default.OrganizeByType)
            {
                string typeDir = Path.Combine(baseFolder, typeFolderName);
                EnsureDir(typeDir);
                return Path.Combine(typeDir, fileName);
            }
            return Path.Combine(baseFolder, fileName);
        }


        private void buttonBrowseFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedPdfPath = openFileDialog.FileName;
                    _lastSelectedPdfPath = openFileDialog.FileName; // أو المتغير الذي تحمل به المسار

                    using (var pageForm = new PageSelectionForm())
                    {
                        // لا تستدعِ LoadPdfPreview هنا
                        pageForm.PendingPdfPath = selectedPdfPath;     // مرّر المسار فقط

                        if (pageForm.ShowDialog(this) == DialogResult.OK)
                        {
                            selectedFromPage = pageForm.FromPage;
                            selectedToPage = pageForm.ToPage;
                            labelFileName.Text = selectedPdfPath;
                        }
                    }

                }
            }
        }


        private async void buttonProcessFile_Click(object sender, EventArgs e)
        {
            string filePath = labelFileName.Text;
            string apiKey = textEditAPIKey.Text.Trim(); // Use the new text edit control

            // 1) التحقق من مفتاح الـAPI
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                MessageBox.Show("Please enter your API key.", "API Key Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2) التحقق من مسار الملف
            if (filePath == "No file selected" || !File.Exists(filePath))
            {
                MessageBox.Show("Please select a valid PDF file.", "File Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            // سيُستخدم في finally، لذا لازم يكون معرّف هنا
            string outputFolder = GetOutputFolder();
            System.IO.Directory.CreateDirectory(outputFolder);

            // سنبني منه قائمة المقاطع التي ستُكتب في ملف Word الموحد
            var allExtractedTexts = new List<string>(); // using System.Collections.Generic;

            // 4) استخراج صور كل الصفحات المحددة في الواجهة
            var allPages = ConvertPdfToImages(filePath);

            // 5) إنشاء StringBuilder لكل قسم من الأقسام الأربع
            // 5) Prepare StringBuilders for whichever sections are checked
            StringBuilder allDefinitions = chkDefinitions.Checked ? new StringBuilder() : null;
            StringBuilder allMCQs = chkMCQs.Checked ? new StringBuilder() : null;
            StringBuilder allFlashcards = chkFlashcards.Checked ? new StringBuilder() : null;
            StringBuilder allVocabulary = chkVocabulary.Checked ? new StringBuilder() : null;
            StringBuilder allSummary = chkSummary.Checked ? new StringBuilder() : null;
            StringBuilder allTakeaways = chkTakeaways.Checked ? new StringBuilder() : null;
            StringBuilder allCloze = chkCloze.Checked ? new StringBuilder() : null;
            StringBuilder allTrueFalse = chkTrueFalse.Checked ? new StringBuilder() : null;
            StringBuilder allOutline = chkOutline.Checked ? new StringBuilder() : null;
            StringBuilder allConceptMap = chkConceptMap.Checked ? new StringBuilder() : null;
            StringBuilder allTableExtract = chkTableExtract.Checked ? new StringBuilder() : null;
            StringBuilder allSimplified = chkSimplified.Checked ? new StringBuilder() : null;
            StringBuilder allCaseStudy = chkCaseStudy.Checked ? new StringBuilder() : null;
            StringBuilder allKeywords = chkKeywords.Checked ? new StringBuilder() : null;
            StringBuilder allTranslatedSections = chkTranslatedSections.Checked ? new StringBuilder() : null;
            StringBuilder allExplainTerms = chkExplainTerms.Checked ? new StringBuilder() : null;


            // Check if at least one section is selected
            if (allDefinitions == null
                 && allMCQs == null
                 && allFlashcards == null
                 && allVocabulary == null
                 && allSummary == null
                 && allTakeaways == null
                 && allCloze == null
                 && allTrueFalse == null
                 && allOutline == null
                 && allConceptMap == null
                 && allTableExtract == null
                 && allSimplified == null
                 && allCaseStudy == null
                 && allKeywords == null
                 && allTranslatedSections == null
                 && allExplainTerms == null)
            {
                MessageBox.Show("Please select at least one section to process.", "No Sections Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                buttonProcessFile.Enabled = true;
                buttonBrowseFile.Enabled = true;

                // Enable the maximize and minimize buttons again
                this.MaximizeBox = true; // Disable maximize button
                this.MinimizeBox = true; // Disable minimize button
                this.Text = "ChatGPT File Processor"; // Reset form title

                UpdateStatus("❌ No sections selected for processing.");

                HideOverlay();
                return;
            }

            try
            {
                // منع النقرات المتكررة أثناء المعالجة
                buttonProcessFile.Enabled = false;
                buttonBrowseFile.Enabled = false;
                buttonBrowseFile.Enabled = false;
                // Disable the maximize and minimize of the processing form
                this.MaximizeBox = false; // Disable maximize button
                this.MinimizeBox = false; // Disable minimize button
                this.Text = "Processing PDF..."; // Update form title to indicate processing

                // اسم النموذج والـ timestamp لإنشاء مسارات الملفات
                string modelName = comboBoxEditModel.SelectedItem?.ToString() ?? "gpt-4o"; // Use the new combo box for model selection
                //string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string timeStamp = DateTime.Now.ToString("yyyy_MM_dd___HH_mmss");

                UpdateOverlayLog("                                   ");
                UpdateOverlayLog("                                   ");
                UpdateOverlayLog("▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽");
                ShowOverlay("▶▶▶ 🔄 Processing, please wait...");
                UpdateOverlayLog("▰▰▰▰▰ S T A R T   G E N E R A T I N G ▰▰▰▰▰");
                UpdateOverlayLog($"▶▶▶ Starting {modelName} multimodal processing...");

                Directory.CreateDirectory(outputFolder);


                // احصل على المجلد النهائي حسب الخيارات
                string outputRoot = ResolveBaseOutputFolder(filePath, timeStamp, modelName);
                _lastOutputRoot = outputRoot; // سجّل آخر مجلد فعلي استخدمته

                // 💾 أعلن أين سنحفظ
                UpdateOverlayLog($"▶▶▶ 💾 Saving outputs to: {outputRoot}");
                UpdateOverlayLog($"▰▰▰ Options → SaveBesidePdf={Properties.Settings.Default.SaveBesidePdf}, " +
                                 $"▰▰▰ SessionFolder={Properties.Settings.Default.UseSessionFolder}, " +
                                 $"▰▰▰ OrganizeByType ={Properties.Settings.Default.OrganizeByType}");

                // بناء أسماء الملفات
                string defName = $"Definitions_{modelName}_{timeStamp}.docx";
                string mcqName = $"MCQs_{modelName}_{timeStamp}.docx";
                string flashName = $"Flashcards_{modelName}_{timeStamp}.docx";
                string vocabName = $"Vocabulary_{modelName}_{timeStamp}.docx";
                string sumName = $"Summary_{modelName}_{timeStamp}.docx";
                string takeName = $"Takeaways_{modelName}_{timeStamp}.docx";
                string clozeName = $"Cloze_{modelName}_{timeStamp}.docx";
                string tfName = $"TrueFalse_{modelName}_{timeStamp}.docx";
                string outlName = $"Outline_{modelName}_{timeStamp}.docx";
                string cmapName = $"ConceptMap_{modelName}_{timeStamp}.docx";
                string tblName = $"Tables_{modelName}_{timeStamp}.docx";
                string simpName = $"Simplified_{modelName}_{timeStamp}.docx";
                string caseName = $"CaseStudy_{modelName}_{timeStamp}.docx";
                string keywName = $"Keywords_{modelName}_{timeStamp}.docx";
                string transName = $"TranslatedSections_{modelName}_{timeStamp}.docx";
                string explName = $"ExplainTerms_{modelName}_{timeStamp}.docx"; // للميزة الجديدة

                // الآن اختر مجلد النوع عندما تطلبه
                string definitionsFilePath = PathInTypeFolder(outputRoot, "Definitions", defName);
                string mcqsFilePath = PathInTypeFolder(outputRoot, "MCQs", mcqName);
                string flashcardsFilePath = PathInTypeFolder(outputRoot, "Flashcards", flashName);
                string vocabularyFilePath = PathInTypeFolder(outputRoot, "Vocabulary", vocabName);
                string summaryFilePath = PathInTypeFolder(outputRoot, "Summary", sumName);
                string takeawaysFilePath = PathInTypeFolder(outputRoot, "Takeaways", takeName);
                string clozeFilePath = PathInTypeFolder(outputRoot, "Cloze", clozeName);
                string tfFilePath = PathInTypeFolder(outputRoot, "TrueFalse", tfName);
                string outlineFilePath = PathInTypeFolder(outputRoot, "Outline", outlName);
                string conceptMapFilePath = PathInTypeFolder(outputRoot, "ConceptMap", cmapName);
                string tableFilePath = PathInTypeFolder(outputRoot, "Tables", tblName);
                string simplifiedFilePath = PathInTypeFolder(outputRoot, "Simplified", simpName);
                string caseStudyFilePath = PathInTypeFolder(outputRoot, "CaseStudy", caseName);
                string keywordsFilePath = PathInTypeFolder(outputRoot, "Keywords", keywName);
                string translatedSectionsFilePath = PathInTypeFolder(outputRoot, "TranslatedSections", transName);
                string explainTermsFilePath = PathInTypeFolder(outputRoot, "ExplainTerms", explName);



                //bool includeArabicExplain = chkArabicExplainTerms.Checked;

                //// 3.1) prompt for Definitions in “GeneralLanguage” (e.g. user picks “French”)
                //// 1) Read which “General Language” the user picked:
                //string generalLangName = cmbGeneralLang.SelectedItem as string ?? "English";

                //// 2) Read the “Medical Material” checkbox:
                //bool isMedical = chkMedicalMaterial.Checked;
                //// 3) Read the “Vocabulary Language” dropdown:
                //string vocabLangName = cmbVocabLang.SelectedItem as string ?? "Arabic";

                ////// 3) Build each prompt with a little conditional text:
                //// 3.1) Definitions prompt\
                //string definitionsPrompt;
                //if (isMedical)
                //{
                //    // When medical mode is on, enforce clinically accurate language,
                //    // include brief usage/context if relevant, and keep medical terms correct.
                //    definitionsPrompt =
                //        $"In {generalLangName}, provide concise MEDICAL DEFINITIONS for each key medical term found on these page(s). " +
                //        $"For each term, output exactly (no numbering):\n\n" +
                //        $"- Term: <the term as a heading>\n" +
                //        $"- Definition: <a 1–2 sentence clinical definition in {generalLangName}, including brief context or indication if applicable>\n\n" +
                //        $"Use precise medical terminology and separate each entry with a blank line.";
                //}
                //else
                //{
                //    definitionsPrompt =
                //        $"In {generalLangName}, provide concise DEFINITIONS for each key term found on these page(s). " +
                //        $"For each term, output exactly (no numbering):\n\n" +
                //        $"- Term: <the term as a heading>\n" +
                //        $"- Definition: <a 1–2 sentence definition in {generalLangName}>\n\n" +
                //        $"Separate entries with a blank line.";
                //}


                //// 3.2) MCQs prompt
                //// Best Version (preserved for reference)
                ////string mcqsPrompt =
                ////    //$"Generate 5 MULTIPLE‐CHOICE QUESTIONS in {generalLangName} " +
                ////    $"Generate MULTIPLE‐CHOICE QUESTIONS in {generalLangName} " +
                ////    $"based strictly on the content of these page(s).  Follow this pattern exactly (no deviations):\n\n" +
                ////    $"Question: <Write the question here in {generalLangName}>\n" +
                ////    $"A) <Option A in {generalLangName}>\n" +
                ////    $"B) <Option B in {generalLangName}>\n" +
                ////    $"C) <Option C in {generalLangName}>\n" +
                ////    $"D) <Option D in {generalLangName}>\n" +
                ////    $"Answer: <Exactly one letter: A, B, C, or D>\n\n" +
                ////    $"Separate each MCQ block with a single blank line.  Do NOT include any extra text.";

                //string mcqsPrompt;
                //if (isMedical)
                //{
                //    mcqsPrompt =
                //        $"Generate MULTIPLE-CHOICE QUESTIONS (in {generalLangName}) focused on the MEDICAL content of these page(s).  " +
                //        $"Write exactly (no deviations):\n\n" +
                //        $"Question: <Compose a clinically relevant question in {generalLangName}, using proper medical terminology>\n" +
                //        $"A) <Option A in {generalLangName}>\n" +
                //        $"B) <Option B in {generalLangName}>\n" +
                //        $"C) <Option C in {generalLangName}>\n" +
                //        $"D) <Option D in {generalLangName}>\n" +
                //        $"Answer: <Exactly one letter: A, B, C, or D>\n\n" +
                //        $"Separate each MCQ block with a blank line.  Do NOT include any explanations after the answer.";
                //}
                //else
                //{
                //    mcqsPrompt =
                //        $"Generate MULTIPLE-CHOICE QUESTIONS (in {generalLangName}) based strictly on the content of these page(s).  " +
                //        $"Write exactly (no deviations):\n\n" +
                //        $"Question: <Write the question here in {generalLangName}>\n" +
                //        $"A) <Option A in {generalLangName}>\n" +
                //        $"B) <Option B in {generalLangName}>\n" +
                //        $"C) <Option C in {generalLangName}>\n" +
                //        $"D) <Option D in {generalLangName}>\n" +
                //        $"Answer: <Exactly one letter: A, B, C, or D>\n\n" +
                //        $"Separate each MCQ block with a blank line.  Do NOT include any extra text.";
                //}


                //// 3.3) Flashcards prompt
                //// Best Version (preserved for reference)
                ////string flashcardsPrompt =
                ////    $"Create FLASHCARDS in {generalLangName} for each key " +
                ////    $"{(isMedical ? "medical " : "")}term on these page(s).  Use this exact format (no deviations):\n\n" +
                ////    //$"Front: <Term in {generalLangName}>\n" +
                ////    $"Front: <Term>\n" +
                ////    $"Back:  <One- or two- or three- sentence definition in {generalLangName}>\n\n" +
                ////    $"Leave exactly one blank line between each card.  Do NOT number or bullet anything.";

                //string flashcardsPrompt;
                //if (isMedical)
                //{
                //    flashcardsPrompt =
                //        $"Create MEDICAL FLASHCARDS in {generalLangName} for each key medical or pharmaceutical term on these page(s).  " +
                //        $"Use this exact format (no deviations):\n\n" +
                //        $"Front: <Term>\n" +
                //        $"Back:  <A 1–2 sentence clinical definition/use in {generalLangName}, including indication if relevant>\n\n" +
                //        $"Separate each card with a blank line; do NOT number or bullet anything.";
                //}
                //else
                //{
                //    flashcardsPrompt =
                //        $"Create FLASHCARDS in {generalLangName} for each key term on these page(s).  " +
                //        $"Use this exact format (no deviations):\n\n" +
                //        $"Front: <Term>\n" +
                //        $"Back:  <One- or two-sentence definition in {generalLangName}>\n\n" +
                //        $"Separate each card with a blank line; do NOT number or bullet anything.";
                //}


                //// 3.4) Vocabulary prompt
                //// Best Version (preserved for reference)
                ////string vocabularyPrompt =
                ////    $"Extract IMPORTANT VOCABULARY TERMS from these page(s) and translate them into {vocabLangName}.  Use exactly this format (no bullets or numbering):\n\n" +
                ////    //$"EnglishTerm – {vocabLangName}Translation\n\n" +
                ////    $"OriginalTerm – {vocabLangName}Translation\n\n" +
                ////    $"Leave exactly one blank line between each entry.  If a term doesn’t have a direct translation, write “– [Translation Needed]”.";

                //string vocabularyPrompt =
                //    $"Extract IMPORTANT VOCABULARY TERMS from these page(s) and translate them into {vocabLangName}.  " +
                //    $"Use exactly this format (no bullets or numbering):\n\n" +
                //    $"OriginalTerm – {vocabLangName}Translation\n\n" +
                //    $"Leave exactly one blank line between each entry.  If a term doesn’t have a direct translation, write “– [Translation Needed]”.";


                //// 3.5) Summary prompt
                //// Best Version (preserved for reference)
                ////string summaryPrompt =
                ////    //$"In {generalLangName}, write a concise SUMMARY (2–3 sentences) of the content on these page(s). " +
                ////    $"In {generalLangName}, write a concise SUMMARY (2–3-4-5-6-7-8-9-10 sentences) of the content on these page(s). " +
                ////    $"{(isMedical ? "Highlight key medical concepts; keep technical terms accurate." : "")}" +
                ////    $"\n\nFormat your summary as plain prose (no bullets or numbering).";

                //string summaryPrompt;
                //if (isMedical)
                //{
                //    summaryPrompt =
                //        $"In {generalLangName}, write a concise MEDICAL SUMMARY (3–5 sentences) of the content on these page(s).  " +
                //        $"Highlight key medical concepts and maintain technical accuracy (e.g., pathophysiology, indications, contraindications).  " +
                //        $"Format as plain prose (no bullets or numbering).";
                //}
                //else
                //{
                //    summaryPrompt =
                //        $"In {generalLangName}, write a concise SUMMARY (3–5 sentences) of the content on these page(s).  " +
                //        $"Format as plain prose (no bullets or numbering).";
                //}


                //// 3.6) Key Takeaways prompt
                //// Best Version (preserved for reference)
                ////string takeawaysPrompt =
                ////    //$"List 5 KEY TAKEAWAYS (in {generalLangName}) from these page(s), formatted as bullets.  " +
                ////    $"List KEY TAKEAWAYS (in {generalLangName}) from these page(s), formatted as bullets.  " +
                ////    $"Each line must begin with a dash and a space, like:\n" +
                ////    $"- Takeaway 1\n" +
                ////    $"- Takeaway 2\n" +
                ////    $"…\n\n" +
                ////    $"{(isMedical ? "Include any critical medical terms and their context." : "")}";

                //string takeawaysPrompt;
                //if (isMedical)
                //{
                //    takeawaysPrompt =
                //        $"List KEY TAKEAWAYS (in {generalLangName}) from these page(s), formatted as bullets.  " +
                //        $"Each line must begin with a dash and a space, for example:\n" +
                //        $"- Takeaway 1\n" +
                //        $"- Takeaway 2\n\n" +
                //        $"Include critical medical terms, their context, and implications for patient care.";
                //}
                //else
                //{
                //    takeawaysPrompt =
                //        $"List KEY TAKEAWAYS (in {generalLangName}) from these page(s), formatted as bullets.  " +
                //        $"Each line must begin with a dash and a space, for example:\n" +
                //        $"- Takeaway 1\n" +
                //        $"- Takeaway 2\n\n";
                //}


                //// 3.7) Fill-in-the-Blank (Cloze) prompt
                //// Best Version (preserved for reference)
                ////string clozePrompt =
                ////    //$"Generate 5 FILL‐IN‐THE‐BLANK sentences (in {generalLangName}) based on these page(s).  " +
                ////    $"Generate FILL‐IN‐THE‐BLANK sentences (in {generalLangName}) based on these page(s).  " +
                ////    $"Each entry should consist of two lines:\n\n" +
                ////    $"Sentence:“_______________ is <brief clue>.”\n" +
                ////    $"Answer: <the correct word or phrase> (in {generalLangName}).\n\n" +
                ////    //$"For example:\nSentence: “_____[Pilocarpine]_____ is a miotic drug.”\nAnswer: Pilocarpine\n\n" +
                ////    $"For example:\nSentence: “_______________ is a miotic drug.”\nAnswer: Pilocarpine\n\n" +
                ////    $"Leave a single blank line between each pair.  Do NOT embed the answer inside the blank.";

                //string clozePrompt;
                //if (isMedical)
                //{
                //    clozePrompt =
                //        $"Generate FILL-IN-THE-BLANK sentences (in {generalLangName}) based on these page(s), focusing on medical terminology.  " +
                //        $"Each entry should consist of two lines:\n\n" +
                //        $"Sentence: \"_______________ is <brief medical clue>.\"\n" +
                //        $"Answer: <the correct medical term or phrase> (in {generalLangName}).\n\n" +
                //        $"For example:\n" +
                //        $"Sentence: \"_______________ is a cholinergic agonist used to treat glaucoma.\"\n" +
                //        $"Answer: Pilocarpine\n\n" +
                //        $"Leave exactly one blank line between each pair; do NOT show the answer inside the blank.";
                //}
                //else
                //{
                //    clozePrompt =
                //        $"Generate FILL-IN-THE-BLANK sentences (in {generalLangName}) based on these page(s).  " +
                //        $"Each entry should consist of two lines:\n\n" +
                //        $"Sentence: \"_______________ is <brief clue>.\"\n" +
                //        $"Answer: <the correct word or phrase> (in {generalLangName}).\n\n" +
                //        $"For example:\n" +
                //        $"Sentence: \"_______________ is the capital of France.\"\n" +
                //        $"Answer: Paris\n\n" +
                //        $"Leave exactly one blank line between each pair; do NOT show the answer inside the blank.";
                //}


                //// 3.8) True/False Questions prompt
                //// Best Version (preserved for reference)
                ////string trueFalsePrompt =
                ////    //$"Generate 5 TRUE/FALSE statements (in {generalLangName}) based on these page(s).  " +
                ////    $"Generate TRUE/FALSE statements (in {generalLangName}) based on these page(s).  " +
                ////    $"Each block should be two lines:\n\n" +
                ////    $"Statement: <write a true‐or‐false sentence here>\n" +
                ////    $"Answer: <True or False>\n\n" +
                ////    $"Leave exactly one blank line between each pair.  Do NOT write any additional explanation.";

                //string trueFalsePrompt;
                //if (isMedical)
                //{
                //    trueFalsePrompt =
                //        $"Generate TRUE/FALSE statements (in {generalLangName}) focused on the medical content of these page(s).  " +
                //        $"Each block should be two lines:\n\n" +
                //        $"Statement: <a clinically accurate true-or-false sentence>\n" +
                //        $"Answer: <True or False>\n\n" +
                //        $"Leave exactly one blank line between each pair; do NOT provide explanations.";
                //}
                //else
                //{
                //    trueFalsePrompt =
                //        $"Generate TRUE/FALSE statements (in {generalLangName}) based on these page(s).  " +
                //        $"Each block should be two lines:\n\n" +
                //        $"Statement: <write a true-or-false sentence>\n" +
                //        $"Answer: <True or False>\n\n" +
                //        $"Leave exactly one blank line between each pair; do NOT provide explanations.";
                //}


                //// 3.9) Outline prompt
                //// Best Version (preserved for reference)
                ////string outlinePrompt =
                ////    $"Produce a hierarchical OUTLINE in {generalLangName} for the material on these page(s).  " +
                ////    $"Use numbered levels (e.g., “1. Main Heading,” “1.1 Subheading,” “1.1.1 Detail”).  " +
                ////    $"Do NOT use bullet points—strictly use decimal numbering.  " +
                ////    $"{(isMedical ? "Include medical subheadings where appropriate." : "")}";

                //string outlinePrompt;
                //if (isMedical)
                //{
                //    outlinePrompt =
                //        $"Produce a hierarchical MEDICAL OUTLINE in {generalLangName} for the material on these page(s).  " +
                //        $"Use decimal numbering (e.g., “1. Topic,” “1.1 Subtopic,” “1.1.1 Detail”).  " +
                //        $"Include specific medical subheadings (e.g., pathophysiology, clinical presentation, management) where appropriate.";
                //}
                //else
                //{
                //    outlinePrompt =
                //        $"Produce a hierarchical OUTLINE in {generalLangName} for the material on these page(s).  " +
                //        $"Use decimal numbering (e.g., “1. Topic,” “1.1 Subtopic,” “1.1.1 Detail”).  " +
                //        $"Do NOT use bullet points—strictly use decimal numbering.";
                //}


                //// 3.10) Concept Map prompt
                //// Best Version (preserved for reference)
                ////string conceptMapPrompt =
                ////    $"List the key CONCEPTS from these page(s) and show how they relate, in {generalLangName}.  " +
                ////    $"For each pair, use one of these formats exactly:\n" +
                ////    $"“ConceptA → relates to → ConceptB”\n" +
                ////    $"or\n" +
                ////    $"“ConceptA — contrasts with — ConceptC”\n\n" +
                ////    $"Separate each relationship on its own line.  Provide at least 5 relationships.";

                //string conceptMapPrompt;
                //if (isMedical)
                //{
                //    conceptMapPrompt =
                //        $"List the key MEDICAL CONCEPTS from these page(s) and show how they relate, in {generalLangName}.  " +
                //        $"For each pair, use exactly one of these formats:\n" +
                //        $"“ConceptA → relates to → ConceptB”\n" +
                //        $"or\n" +
                //        $"“ConceptA — contrasts with — ConceptC”\n\n" +
                //        $"Focus on clinical or pathophysiological relationships.  Provide at least 5 relationships.";
                //}
                //else
                //{
                //    conceptMapPrompt =
                //        $"List the key CONCEPTS from these page(s) and show how they relate, in {generalLangName}.  " +
                //        $"For each pair, use exactly one of these formats:\n" +
                //        $"“ConceptA → relates to → ConceptB”\n" +
                //        $"or\n" +
                //        $"“ConceptA — contrasts with — ConceptC”\n\n" +
                //        $"Separate each relationship on its own line.  Provide at least 5 relationships.";
                //}


                ////// 3.11) Table Extraction prompt
                ////string tableExtractPrompt;
                ////if (isMedical)
                ////{
                ////    tableExtractPrompt =
                ////        $"If these page(s) contain any MEDICAL TABLES (e.g., drug doses, indications, side effects, lab values), " +
                ////        $"extract each table into markdown format in {generalLangName}.  Follow this exact format:\n\n" +
                ////        $"| Column1         | Column2                   | Column3            |\n" +
                ////        $"|-----------------|---------------------------|--------------------|\n" +
                ////        $"| data11 (e.g., drug) | data12 (e.g., dose)   | data13 (e.g., side effect) |\n" +
                ////        $"| data21         | data22                     | data23             |\n\n" +
                ////        $"If no table is present, respond exactly: “No table found.”";
                ////}
                ////else
                ////{
                ////    tableExtractPrompt =
                ////        $"If these page(s) contain any tables (e.g., schedules, comparisons, statistics), " +
                ////        $"extract each table into markdown format in {generalLangName}.  Follow this exact format:\n\n" +
                ////        $"| Column1 | Column2 | Column3 |\n" +
                ////        $"|---------|---------|---------|\n" +
                ////        $"| data11  | data12  | data13  |\n" +
                ////        $"| data21  | data22  | data23  |\n\n" +
                ////        $"If no table is present, respond exactly: “No table found.”";
                ////}
                //string tableExtractPrompt =
                //    "From the following text, extract every table you can logically infer. " +
                //    "For EACH table:\n" +
                //    "1) Print a title line exactly as: TABLE: <table title>\n" +
                //    "2) Then output a valid Markdown pipe table:\n" +
                //    "| Column1 | Column2 | ... |\n" +
                //    "| --- | --- | ... |\n" +
                //    "| row1col1 | row1col2 | ... |\n" +
                //    "(No extra commentary; keep one blank line between tables.)\n" +
                //    "Escape pipes inside cells as &#124; if needed.\n";



                //// 3.12) Simplified Explanation prompt
                //// Best Version (preserved for reference)
                ////string simplifiedPrompt =
                ////    $"Explain the content of these page(s) in simpler language, as if teaching a first-year medical student.  " +
                ////    $"Use {generalLangName}.  Define any technical or medical jargon in parentheses the first time it appears.  " +
                ////    $"Write one cohesive paragraph—no bullets or lists.";

                //string simplifiedPrompt;
                //if (isMedical)
                //{
                //    simplifiedPrompt =
                //        $"Explain the content of these page(s) in simpler language (in {generalLangName}), as if teaching a first-year medical student.  " +
                //        $"Define any technical/medical jargon in parentheses upon first use.  " +
                //        $"Write one cohesive paragraph—no bullets or lists.";
                //}
                //else
                //{
                //    simplifiedPrompt =
                //        $"Explain the content of these page(s) in simpler language (in {generalLangName}).  " +
                //        $"Write one cohesive paragraph—no bullets or lists.";
                //}


                //// 3.13) Case Study prompt
                //// Best Version (preserved for reference)
                ////string caseStudyPrompt =
                ////    $"Write a short CLINICAL VIGNETTE (1 paragraph) based on these page(s), in {generalLangName}.  " +
                ////    $"Include:\n" +
                ////    $"- Patient age and gender\n" +
                ////    $"- Presenting complaint or symptom\n" +
                ////    $"- Key pertinent findings (e.g., vital signs, lab results)\n\n" +
                ////    $"Then immediately follow with a single multiple-choice question (in {generalLangName}) about the most likely diagnosis or next step.  " +
                ////    $"Format exactly:\n" +
                ////    $"\nMCQ: <The question text>\n" +
                ////    $"A) <Option A>\n" +
                ////    $"B) <Option B>\n" +
                ////    $"C) <Option C>\n" +
                ////    $"D) <Option D>\n" +
                ////    $"Answer: <A, B, C, or D>\n\n" +
                ////    //$"No extra commentary—only the vignette paragraph, blank line, then the MCQ block.";
                ////    $"No extra commentary—only the vignette paragraph, blank line, then the MCQ block, at least two cases.";

                //string caseStudyPrompt;
                //if (isMedical)
                //{
                //    caseStudyPrompt =
                //        $"Write a short CLINICAL VIGNETTE (1 paragraph) based on these page(s), in {generalLangName}.  " +
                //        $"Include:\n" +
                //        $"- Patient age and gender\n" +
                //        $"- Presenting complaint or symptom\n" +
                //        $"- Key pertinent findings (e.g., vital signs, lab results)\n\n" +
                //        $"Then immediately follow with a MULTIPLE-CHOICE QUESTION (in {generalLangName}) about the most likely diagnosis or next step.  " +
                //        $"Format exactly:\n\n" +
                //        $"MCQ: <The question text>\n" +
                //        $"A) <Option A>\n" +
                //        $"B) <Option B>\n" +
                //        $"C) <Option C>\n" +
                //        $"D) <Option D>\n" +
                //        $"Answer: <A, B, C, or D>\n\n" +
                //        $"No extra commentary—only the vignette paragraph, blank line, then the MCQ block.";
                //}
                //else
                //{
                //    caseStudyPrompt =
                //        $"Write a short CASE SCENARIO (1 paragraph) based on these page(s), in {generalLangName}.  " +
                //        $"Then follow with a MULTIPLE-CHOICE QUESTION (in {generalLangName}) about a key concept.  " +
                //        $"Format exactly:\n\n" +
                //        $"MCQ: <The question text>\n" +
                //        $"A) <Option A>\n" +
                //        $"B) <Option B>\n" +
                //        $"C) <Option C>\n" +
                //        $"D) <Option D>\n" +
                //        $"Answer: <A, B, C, or D>\n\n" +
                //        $"No extra commentary—only the scenario paragraph, blank line, then the MCQ block.";
                //}


                //// 3.14) Keywords prompt
                //// Best Version (preserved for reference)
                ////string keywordsPrompt =
                ////    $"List the HIGH-YIELD KEYWORDS from these page(s) in {generalLangName}.  " +
                ////    $"Output as a comma-separated list (e.g., “keyword1, keyword2, keyword3”).  " +
                ////    $"Do NOT include definitions—only the keywords themselves.  " +
                ////    $"Provide at least 8–10 keywords.";

                //string keywordsPrompt;
                //if (isMedical)
                //{
                //    keywordsPrompt =
                //        $"List the HIGH-YIELD MEDICAL KEYWORDS from these page(s) in {generalLangName}.  " +
                //        $"Output as a comma-separated list (e.g., “keyword1, keyword2, keyword3”).  " +
                //        $"Do NOT include definitions—only the keywords themselves.  " +
                //        $"Provide at least 8–10 medical terms.";
                //}
                //else
                //{
                //    keywordsPrompt =
                //        $"List the HIGH-YIELD KEYWORDS from these page(s) in {generalLangName}.  " +
                //        $"Output as a comma-separated list (e.g., “keyword1, keyword2, keyword3”).  " +
                //        $"Do NOT include definitions—only the keywords themselves.  " +
                //        $"Provide at least 8–10 keywords.";
                //}

                ////Translated Section Prompt
                //string translatedSectionsPrompt =
                //    $"Translate the following text from {generalLangName} into {vocabLangName}. " +
                //    $"Keep every sentence or paragraph exactly as it is in the original language. " +
                //    $"After each sentence or paragraph, provide the translation immediately below it. " +
                //    $"Do not remove or shorten any part of the original text. " +
                //    //$"Use clear labels in the output: start the original with 'Original:' and the translation with 'Translation:'. " +
                //    $"Do not add any introductions, explanations, notes, or extra formatting. " +
                //    $"Only output the text in the requested format.";

                ////// 3.16) Explain Terms prompt
                ////string explainTermsPrompt;
                ////if (isMedical)
                ////{
                ////    // وضع طبي: ركّز على المصطلحات الطبية غير الشائعة
                ////    explainTermsPrompt =
                ////        $"Identify KEY MEDICAL TERMS on these page(s) that a non-specialist may not understand. " +
                ////        $"For EACH term, output EXACTLY:\n\n" +
                ////        $"Term: <the term as written>\n" +
                ////        $"Pronunciation: </IPA or syllable breakdown/>\n" +
                ////        $"Explanation ({generalLangName}): <2–3 sentences in clear plain language>\n" +
                ////        $"Analogy ({generalLangName}): <a simple analogy or everyday example>\n" +
                ////        $"If the term is an abbreviation, first expand it.\n\n" +
                ////        $"Separate each term block with ONE blank line. Do NOT add extra commentary.";
                ////}
                ////else
                ////{
                ////    // عام: مصطلحات تقنية/علمية عامة
                ////    explainTermsPrompt =
                ////        $"Identify KEY TECHNICAL TERMS on these page(s) that a non-specialist may not understand. " +
                ////        $"For EACH term, output EXACTLY:\n\n" +
                ////        $"Term: <the term as written>\n" +
                ////        $"Pronunciation: </IPA or syllable breakdown/>\n" +
                ////        $"Explanation ({generalLangName}): <2–3 sentences in clear plain language>\n" +
                ////        $"Analogy ({generalLangName}): <a simple analogy or everyday example>\n" +
                ////        $"If the term is an abbreviation, first expand it.\n\n" +
                ////        $"Separate each term block with ONE blank line. Do NOT add extra commentary.";
                ////}


                ////// 3.16) Explain Terms prompt (with optional Arabic beside general language)
                ////string explainTermsPrompt;

                ////string arabicBlock =
                ////    includeArabicExplain
                ////        ? "ArabicExplanation (Arabic): <2–3 sentences in clear Arabic>\n" +
                ////          "ArabicAnalogy (Arabic): <a simple analogy/example in Arabic>\n"
                ////        : ""; // empty if not selected

                ////if (isMedical)
                ////{
                ////    explainTermsPrompt =
                ////        $"Identify KEY MEDICAL TERMS on these page(s) that a non-specialist may not understand. " +
                ////        $"For EACH term, output EXACTLY:\n\n" +
                ////        $"Term: <the term as written>\n" +
                ////        $"Pronunciation: </IPA or syllable breakdown/>\n" +
                ////        $"Explanation ({generalLangName}): <2–3 sentences in clear plain language>\n" +
                ////        $"Analogy ({generalLangName}): <a simple analogy or everyday example>\n" +
                ////        arabicBlock +
                ////        $"If the term is an abbreviation, first expand it.\n\n" +
                ////        $"Separate each term block with ONE blank line. Do NOT add extra commentary.";
                ////}
                ////else
                ////{
                ////    explainTermsPrompt =
                ////        $"Identify KEY TECHNICAL TERMS on these page(s) that a non-specialist may not understand. " +
                ////        $"For EACH term, output EXACTLY:\n\n" +
                ////        $"Term: <the term as written>\n" +
                ////        $"Pronunciation: </IPA or syllable breakdown/>\n" +
                ////        $"Explanation ({generalLangName}): <2–3 sentences in clear plain language>\n" +
                ////        $"Analogy ({generalLangName}): <a simple analogy or everyday example>\n" +
                ////        arabicBlock +
                ////        $"If the term is an abbreviation, first expand it.\n\n" +
                ////        $"Separate each term block with ONE blank line. Do NOT add extra commentary.";
                ////}


                ////// 3.16) Explain Terms prompt (IPA + syllables in one line + optional Arabic)
                ////string explainTermsPrompt;

                ////string arabicBlock =
                ////    includeArabicExplain
                ////        ? "ArabicExplanation (Arabic): <2–3 sentences in clear Arabic>\n" +
                ////          "ArabicAnalogy (Arabic): <a simple analogy/example in Arabic>\n"
                ////        : ""; // empty if not selected

                ////if (isMedical)
                ////{
                ////    explainTermsPrompt =
                ////        $"Identify KEY MEDICAL TERMS on these page(s) that a non-specialist may not understand. " +
                ////        $"For EACH term, output EXACTLY:\n\n" +
                ////        $"Term: <the term as written>\n" +
                ////        $"Pronunciation: IPA = </International Phonetic Alphabet/>, Syllables = <break into simple syllables>\n" +
                ////        $"Explanation ({generalLangName}): <2–3 sentences in clear plain language>\n" +
                ////        $"Analogy ({generalLangName}): <a simple analogy or everyday example>\n" +
                ////        arabicBlock +
                ////        $"If the term is an abbreviation, first expand it.\n\n" +
                ////        $"Separate each term block with ONE blank line. Do NOT add extra commentary.";
                ////}
                ////else
                ////{
                ////    explainTermsPrompt =
                ////        $"Identify KEY TECHNICAL TERMS on these page(s) that a non-specialist may not understand. " +
                ////        $"For EACH term, output EXACTLY:\n\n" +
                ////        $"Term: <the term as written>\n" +
                ////        $"Pronunciation: IPA = </International Phonetic Alphabet/>, Syllables = <break into simple syllables>\n" +
                ////        $"Explanation ({generalLangName}): <2–3 sentences in clear plain language>\n" +
                ////        $"Analogy ({generalLangName}): <a simple analogy or everyday example>\n" +
                ////        arabicBlock +
                ////        $"If the term is an abbreviation, first expand it.\n\n" +
                ////        $"Separate each term block with ONE blank line. Do NOT add extra commentary.";
                ////}

                //// 3.16) Explain Terms prompt (numbered terms + IPA + syllables + optional Arabic)
                //string explainTermsPrompt;

                //string arabicBlock =
                //    includeArabicExplain
                //        ? "ArabicExplanation (Arabic): <2–3 sentences in clear Arabic>\n" +
                //          "ArabicAnalogy (Arabic): <a simple analogy/example in Arabic>\n"
                //        : ""; // empty if not selected

                //if (isMedical)
                //{
                //    explainTermsPrompt =
                //        $"Identify KEY MEDICAL TERMS on these page(s) that a non-specialist may not understand. " +
                //        $"Number each term block sequentially (1, 2, 3, ...). " +
                //        $"For EACH term, output EXACTLY:\n\n" +
                //        $"<Number>. Term: <the term as written>\n" +
                //        $"Pronunciation: IPA = </International Phonetic Alphabet/>, Syllables = <break into simple syllables>\n" +
                //        $"Explanation ({generalLangName}): <2–3 sentences in clear plain language>\n" +
                //        //$"Analogy ({generalLangName}): <a simple analogy or everyday example>\n" +
                //        //arabicBlock +
                //        $"If the term is an abbreviation, first expand it.\n\n" +
                //        $"Separate each term block with ONE blank line. Do NOT add extra commentary.";
                //}
                //else
                //{
                //    explainTermsPrompt =
                //        $"Identify KEY TECHNICAL TERMS on these page(s) that a non-specialist may not understand. " +
                //        $"Number each term block sequentially (1, 2, 3, ...). " +
                //        $"For EACH term, output EXACTLY:\n\n" +
                //        $"<Number>. Term: <the term as written>\n" +
                //        $"Pronunciation: IPA = </International Phonetic Alphabet/>, Syllables = <break into simple syllables>\n" +
                //        $"Explanation ({generalLangName}): <2–3 sentences in clear plain language>\n" +
                //        //$"Analogy ({generalLangName}): <a simple analogy or everyday example>\n" +
                //        //arabicBlock +
                //        $"If the term is an abbreviation, first expand it.\n\n" +
                //        $"Separate each term block with ONE blank line. Do NOT add extra commentary.";
                //}








                // جمع كل المطالبات في قائمة
                // 3) إعدادات المعالجة
                bool includeArabicExplain = chkArabicExplainTerms.Checked;
                string generalLangName = cmbGeneralLang.SelectedItem as string ?? "English";
                bool isMedical = chkMedicalMaterial.Checked;
                string vocabLangName = cmbVocabLang.SelectedItem as string ?? "Arabic";

                // Helpers (لا تغيّر أسماء المتغيّرات عندك)
                Func<string, bool> IsArabic = lang =>
                {
                    if (string.IsNullOrEmpty(lang)) return false;
                    var s = lang.Trim().ToLowerInvariant();
                    return s == "ar" || s == "arabic" || s.Contains("arab") || s.Contains("العرب") || s.Contains("العربية");
                };

                bool targetArabic = IsArabic(generalLangName);
                bool vocabArabic = IsArabic(vocabLangName);

                // 3.1) Definitions
                string definitionsPrompt;
                if (isMedical)
                {
                    definitionsPrompt = targetArabic
                        ? "اكتب تعريفات طبية موجزة بالعربية لكل مصطلح طبي مذكور في هذه الصفحات. لكل مصطلح اكتب بالضبط (بدون ترقيم):\n\n" +
                          "- المصطلح: <العنوان>\n" +
                          "- التعريف: <تعريف سريري من 1–2 جملة بالعربية، مع سياق/دلالة إن لزم>\n\n" +
                          "استخدم مصطلحات دقيقة وافصل بين الإدخالات بسطر فارغ."
                        : $"In {generalLangName}, provide concise MEDICAL DEFINITIONS for each key medical term found on these page(s). " +
                          $"For each term, output exactly (no numbering):\n\n" +
                          $"- Term: <the term as a heading>\n" +
                          $"- Definition: <a 1–2 sentence clinical definition in {generalLangName}, including brief context or indication if applicable>\n\n" +
                          $"Use precise medical terminology and separate each entry with a blank line.";
                }
                else
                {
                    definitionsPrompt = targetArabic
                        ? "اكتب تعريفات موجزة بالعربية لكل مصطلح مهم في هذه الصفحات. لكل مصطلح اكتب بالضبط (بدون ترقيم):\n\n" +
                          "- المصطلح: <العنوان>\n" +
                          "- التعريف: <تعريف من 1–2 جملة بالعربية>\n\n" +
                          "افصل بين كل إدخال بسطر فارغ."
                        : $"In {generalLangName}, provide concise DEFINITIONS for each key term found on these page(s). " +
                          $"For each term, output exactly (no numbering):\n\n" +
                          $"- Term: <the term as a heading>\n" +
                          $"- Definition: <a 1–2 sentence definition in {generalLangName}>\n\n" +
                          $"Separate entries with a blank line.";
                }

                // 3.2) MCQs
                string mcqsPrompt;
                if (isMedical)
                {
                    mcqsPrompt = targetArabic
                        ? "أنشئ أسئلة اختيار من متعدد بالعربية اعتمادًا على المحتوى الطبي لهذه الصفحات. التزم تمامًا بالشكل التالي:\n\n" +
                          "السؤال: <صيِغ سؤالًا سريريًا بالعربية>\n" +
                          "أ) <الخيار أ>\n" +
                          "ب) <الخيار ب>\n" +
                          "ج) <الخيار ج>\n" +
                          "د) <الخيار د>\n" +
                          "الإجابة: <حرف واحد فقط: أ أو ب أو ج أو د>\n\n" +
                          "افصل بين كل سؤال بسطر فارغ، ولا تضف أي شروح."
                        : $"Generate MULTIPLE-CHOICE QUESTIONS (in {generalLangName}) focused on the MEDICAL content of these page(s).  " +
                          $"Write exactly (no deviations):\n\n" +
                          $"Question: <Compose a clinically relevant question in {generalLangName}, using proper medical terminology>\n" +
                          $"A) <Option A in {generalLangName}>\n" +
                          $"B) <Option B in {generalLangName}>\n" +
                          $"C) <Option C in {generalLangName}>\n" +
                          $"D) <Option D in {generalLangName}>\n" +
                          $"Answer: <Exactly one letter: A, B, C, or D>\n\n" +
                          $"Separate each MCQ block with a blank line.  Do NOT include any explanations after the answer.";
                }
                else
                {
                    mcqsPrompt = targetArabic
                        ? "أنشئ أسئلة اختيار من متعدد بالعربية اعتمادًا على محتوى هذه الصفحات. التزم تمامًا بالشكل التالي:\n\n" +
                          "السؤال: <اكتب السؤال بالعربية>\n" +
                          "أ) <الخيار أ>\n" +
                          "ب) <الخيار ب>\n" +
                          "ج) <الخيار ج>\n" +
                          "د) <الخيار د>\n" +
                          "الإجابة: <حرف واحد فقط: أ أو ب أو ج أو د>\n\n" +
                          "افصل بين كل سؤال بسطر فارغ، ولا تضف أي نص إضافي."
                        : $"Generate MULTIPLE-CHOICE QUESTIONS (in {generalLangName}) based strictly on the content of these page(s).  " +
                          $"Write exactly (no deviations):\n\n" +
                          $"Question: <Write the question here in {generalLangName}>\n" +
                          $"A) <Option A in {generalLangName}>\n" +
                          $"B) <Option B in {generalLangName}>\n" +
                          $"C) <Option C in {generalLangName}>\n" +
                          $"D) <Option D in {generalLangName}>\n" +
                          $"Answer: <Exactly one letter: A, B, C, or D>\n\n" +
                          $"Separate each MCQ block with a blank line.  Do NOT include any extra text.";
                }

                // 3.3) Flashcards
                string flashcardsPrompt;
                if (isMedical)
                {
                    flashcardsPrompt = targetArabic
                        ? "أنشئ بطاقات تعليمية طبية بالعربية لكل مصطلح طبي/دوائي مهم في هذه الصفحات. استخدم الشكل التالي دون أي تغيير:\n\n" +
                          "الوجه: <المصطلح>\n" +
                          "الظهر: <تعريف/استخدام سريري من 1–2 جملة بالعربية>\n\n" +
                          "افصل بين كل بطاقة بسطر فارغ ومن دون تعداد."
                        : $"Create MEDICAL FLASHCARDS in {generalLangName} for each key medical or pharmaceutical term on these page(s).  " +
                          $"Use this exact format (no deviations):\n\n" +
                          $"Front: <Term>\n" +
                          $"Back:  <A 1–2 sentence clinical definition/use in {generalLangName}, including indication if relevant>\n\n" +
                          $"Separate each card with a blank line; do NOT number or bullet anything.";
                }
                else
                {
                    flashcardsPrompt = targetArabic
                        ? "أنشئ بطاقات تعليمية بالعربية لكل مصطلح مهم في هذه الصفحات. استخدم الشكل التالي دون أي تغيير:\n\n" +
                          "الوجه: <المصطلح>\n" +
                          "الظهر: <تعريف من جملة أو جملتين بالعربية>\n\n" +
                          "افصل بين كل بطاقة بسطر فارغ ومن دون تعداد."
                        : $"Create FLASHCARDS in {generalLangName} for each key term on these page(s).  " +
                          $"Use this exact format (no deviations):\n\n" +
                          $"Front: <Term>\n" +
                          $"Back:  <One- or two-sentence definition in {generalLangName}>\n\n" +
                          $"Separate each card with a blank line; do NOT number or bullet anything.";
                }

                // 3.4) Vocabulary
                string vocabularyPrompt = vocabArabic
                    ? "استخرج المصطلحات المهمة من هذه الصفحات وترجمها إلى العربية. استخدم هذا الشكل بالضبط (من دون تعداد):\n\n" +
                      "المصطلح الأصلي – الترجمة العربية\n\n" +
                      "اترك سطرًا فارغًا بين كل إدخال. إن لم توجد ترجمة دقيقة، اكتب: – [بحاجة إلى ترجمة]."
                    : $"Extract IMPORTANT VOCABULARY TERMS from these page(s) and translate them into {vocabLangName}.  " +
                      $"Use exactly this format (no bullets or numbering):\n\n" +
                      $"OriginalTerm – {vocabLangName}Translation\n\n" +
                      $"Leave exactly one blank line between each entry.  If a term doesn’t have a direct translation, write “– [Translation Needed]”.";

                // 3.5) Summary
                string summaryPrompt;
                if (isMedical)
                {
                    summaryPrompt = targetArabic
                        ? "اكتب خلاصة طبية موجزة بالعربية (3–5 جمل) لمحتوى هذه الصفحات، مع إبراز المفاهيم الطبية الأساسية والدقة العلمية. اكتب نصًا متماسكًا بدون تعداد."
                        : $"In {generalLangName}, write a concise MEDICAL SUMMARY (3–5 sentences) of the content on these page(s).  " +
                          $"Highlight key medical concepts and maintain technical accuracy (e.g., pathophysiology, indications, contraindications).  " +
                          $"Format as plain prose (no bullets or numbering).";
                }
                else
                {
                    summaryPrompt = targetArabic
                        ? "اكتب خلاصة موجزة بالعربية (3–5 جمل) لمحتوى هذه الصفحات بصيغة نثرية بدون تعداد."
                        : $"In {generalLangName}, write a concise SUMMARY (3–5 sentences) of the content on these page(s).  " +
                          $"Format as plain prose (no bullets or numbering).";
                }

                // 3.6) Key Takeaways
                string takeawaysPrompt = targetArabic
                    ? "اكتب نقاطًا أساسية بالعربية من هذه الصفحات بصيغة تعداد نقطي. يجب أن يبدأ كل سطر بشرطة ومسافة، مثل:\n- نقطة 1\n- نقطة 2\n"
                    : $"List KEY TAKEAWAYS (in {generalLangName}) from these page(s), formatted as bullets.  " +
                      $"Each line must begin with a dash and a space, for example:\n- Takeaway 1\n- Takeaway 2\n";

                // 3.7) Cloze
                string clozePrompt;
                if (isMedical)
                {
                    clozePrompt = targetArabic
                        ? "أنشئ جُملاً ملء الفراغ بالعربية مبنية على المحتوى الطبي. يتكوّن كل إدخال من سطرين:\n\n" +
                          "الجملة: \"_______________ هو <تلميح طبي قصير>.\"\n" +
                          "الإجابة: <المصطلح/العبارة الصحيحة بالعربية>.\n\n" +
                          "اترك سطرًا فارغًا بين كل زوج؛ لا تُظهر الإجابة داخل الفراغ."
                        : $"Generate FILL-IN-THE-BLANK sentences (in {generalLangName}) based on these page(s), focusing on medical terminology.  " +
                          $"Each entry should consist of two lines:\n\n" +
                          $"Sentence: \"_______________ is <brief medical clue>.\"\n" +
                          $"Answer: <the correct medical term or phrase> (in {generalLangName}).\n\n" +
                          $"Leave exactly one blank line between each pair; do NOT show the answer inside the blank.";
                }
                else
                {
                    clozePrompt = targetArabic
                        ? "أنشئ جُملاً ملء الفراغ بالعربية مبنية على هذه الصفحات. يتكوّن كل إدخال من سطرين:\n\n" +
                          "الجملة: \"_______________ هو <تلميح قصير>.\"\n" +
                          "الإجابة: <الكلمة/العبارة الصحيحة بالعربية>.\n\n" +
                          "اترك سطرًا فارغًا بين كل زوج؛ لا تُظهر الإجابة داخل الفراغ."
                        : $"Generate FILL-IN-THE-BLANK sentences (in {generalLangName}) based on these page(s).  " +
                          $"Each entry should consist of two lines:\n\n" +
                          $"Sentence: \"_______________ is <brief clue>.\"\n" +
                          $"Answer: <the correct word or phrase> (in {generalLangName}).\n\n" +
                          $"Leave exactly one blank line between each pair; do NOT show the answer inside the blank.";
                }

                // 3.8) True/False
                string trueFalsePrompt = targetArabic
                    ? "أنشئ عبارات صح/خطأ بالعربية مبنية على هذه الصفحات. يتكوّن كل إدخال من سطرين:\n\n" +
                      "العبارة: <جملة يمكن الحكم عليها بالصواب أو الخطأ>\n" +
                      "الإجابة: <صحيح أو خطأ>\n\n" +
                      "اترك سطرًا فارغًا بين كل زوج، ولا تكتب شروحًا."
                    : $"Generate TRUE/FALSE statements (in {generalLangName}) based on these page(s).  " +
                      $"Each block should be two lines:\n\n" +
                      $"Statement: <write a true-or-false sentence>\n" +
                      $"Answer: <True or False>\n\n" +
                      $"Leave exactly one blank line between each pair; do NOT provide explanations.";

                // 3.9) Outline
                string outlinePrompt;
                if (isMedical)
                {
                    outlinePrompt = targetArabic
                        ? "أنشئ مخططًا هرميًا طبيًا بالعربية لمحتوى هذه الصفحات باستخدام ترقيم عشري (مثل: 1، 1.1، 1.1.1). أدرج عناوين فرعية مثل: الفيزيولوجيا المرضية، العرض السريري، المعالجة."
                        : $"Produce a hierarchical MEDICAL OUTLINE in {generalLangName} for the material on these page(s).  " +
                          $"Use decimal numbering (e.g., “1. Topic,” “1.1 Subtopic,” “1.1.1 Detail”).  " +
                          $"Include specific medical subheadings (e.g., pathophysiology, clinical presentation, management) where appropriate.";
                }
                else
                {
                    outlinePrompt = targetArabic
                        ? "أنشئ مخططًا هرميًا بالعربية لمحتوى هذه الصفحات باستخدام الترقيم العشري (مثل: 1، 1.1، 1.1.1). لا تستخدم التعداد النقطي."
                        : $"Produce a hierarchical OUTLINE in {generalLangName} for the material on these page(s).  " +
                          $"Use decimal numbering (e.g., “1. Topic,” “1.1 Subtopic,” “1.1.1 Detail”).  " +
                          $"Do NOT use bullet points—strictly use decimal numbering.";
                }

                // 3.10) Concept Map
                string conceptMapPrompt = targetArabic
                    ? "اذكر المفاهيم الرئيسة في هذه الصفحات وبيّن العلاقات بينها بالعربية. لكل علاقة استخدم أحد الشكلين:\n" +
                      "«المفهوم أ → يرتبط بـ → المفهوم ب»\n" +
                      "أو\n" +
                      "«المفهوم أ — يتعارض مع — المفهوم ج»\n\n" +
                      "اكتب كل علاقة في سطر مستقل وقدّم على الأقل 5 علاقات."
                    : $"List the key CONCEPTS from these page(s) and show how they relate, in {generalLangName}.  " +
                      $"For each pair, use exactly one of these formats:\n" +
                      $"“ConceptA → relates to → ConceptB”\n" +
                      $"or\n" +
                      $"“ConceptA — contrasts with — ConceptC”\n\n" +
                      $"Separate each relationship on its own line.  Provide at least 5 relationships.";

                // 3.11) Table Extraction
                string tableExtractPrompt = targetArabic
                    ? "من النص التالي، استخرج كل جدول يمكن استنتاجه منطقيًا. لكل جدول:\n" +
                      "1) اطبع سطر العنوان كالتالي تمامًا: جدول: <عنوان الجدول>\n" +
                      "2) ثم اكتب جدول ماركداون صالحًا باستخدام الأنابيب:\n" +
                      "| العمود1 | العمود2 | ... |\n" +
                      "| --- | --- | ... |\n" +
                      "| صف1عم1 | صف1عم2 | ... |\n" +
                      "(بدون تعليق إضافي؛ اترك سطرًا فارغًا بين الجداول.)\n" +
                      "اهرب علامة | داخل الخلايا باستخدام &#124; عند الحاجة.\n"
                    : "From the following text, extract every table you can logically infer. For EACH table:\n" +
                      "1) Print a title line exactly as: TABLE: <table title>\n" +
                      "2) Then output a valid Markdown pipe table:\n" +
                      "| Column1 | Column2 | ... |\n" +
                      "| --- | --- | ... |\n" +
                      "| row1col1 | row1col2 | ... |\n" +
                      "(No extra commentary; keep one blank line between tables.)\n" +
                      "Escape pipes inside cells as &#124; if needed.\n";

                // 3.12) Simplified Explanation
                string simplifiedPrompt;
                if (isMedical)
                {
                    simplifiedPrompt = targetArabic
                        ? "اشرح محتوى هذه الصفحات بالعربية بلغة مبسطة كما لو كنت تدرّس لطالب طب سنة أولى. عرّف أي مصطلح طبي عند أول ظهور له بين قوسين. اكتب فقرة واحدة متماسكة دون تعداد."
                        : $"Explain the content of these page(s) in simpler language (in {generalLangName}), as if teaching a first-year medical student.  " +
                          $"Define any technical/medical jargon in parentheses upon first use.  " +
                          $"Write one cohesive paragraph—no bullets or lists.";
                }
                else
                {
                    simplifiedPrompt = targetArabic
                        ? "اشرح محتوى هذه الصفحات بالعربية بلغة مبسطة في فقرة واحدة دون تعداد أو قوائم."
                        : $"Explain the content of these page(s) in simpler language (in {generalLangName}).  " +
                          $"Write one cohesive paragraph—no bullets or lists.";
                }

                // 3.13) Case Study
                string caseStudyPrompt;
                if (isMedical)
                {
                    caseStudyPrompt = targetArabic
                        ? "اكتب قصة سريرية قصيرة (فقرة واحدة) بالعربية مبنية على هذه الصفحات، تتضمن العمر والجنس والشكوى الأساسية وأبرز الموجودات. بعدها مباشرة ضع سؤال اختيار من متعدد بالعربية حول التشخيص الأرجح أو الخطوة التالية، بالصيغة:\n\n" +
                          "MCQ: <نص السؤال>\n" +
                          "أ) <الخيار أ>\n" +
                          "ب) <الخيار ب>\n" +
                          "ج) <الخيار ج>\n" +
                          "د) <الخيار د>\n" +
                          "الإجابة: <أ، ب، ج، أو د>\n\n" +
                          "من دون أي تعليق إضافي."
                        : $"Write a short CLINICAL VIGNETTE (1 paragraph) based on these page(s), in {generalLangName}.  " +
                          $"Include: patient age & gender, presenting complaint, key findings. Then follow with a MULTIPLE-CHOICE QUESTION (in {generalLangName}) about the most likely diagnosis/next step.\n\n" +
                          $"MCQ: <The question text>\nA) <Option A>\nB) <Option B>\nC) <Option C>\nD) <Option D>\nAnswer: <A, B, C, or D>\n\n" +
                          $"No extra commentary—only the vignette paragraph, blank line, then the MCQ block.";
                }
                else
                {
                    caseStudyPrompt = targetArabic
                        ? "اكتب سيناريو قصيرًا (فقرة واحدة) بالعربية مبنيًا على هذه الصفحات، ثم اتبعه بسؤال اختيار من متعدد بالعربية حول مفهوم أساسي، بصيغة:\n\n" +
                          "MCQ: <نص السؤال>\n" +
                          "أ) <الخيار أ>\n" +
                          "ب) <الخيار ب>\n" +
                          "ج) <الخيار ج>\n" +
                          "د) <الخيار د>\n" +
                          "الإجابة: <أ، ب، ج، أو د>\n\n" +
                          "بدون تعليق إضافي."
                        : $"Write a short CASE SCENARIO (1 paragraph) based on these page(s), in {generalLangName}.  " +
                          $"Then follow with a MULTIPLE-CHOICE QUESTION (in {generalLangName}) about a key concept.\n\n" +
                          $"MCQ: <The question text>\nA) <Option A>\nB) <Option B>\nC) <Option C>\nD) <Option D>\nAnswer: <A, B, C, or D>\n\n" +
                          $"No extra commentary—only the scenario paragraph, blank line, then the MCQ block.";
                }

                // 3.14) Keywords
                string keywordsPrompt;
                if (isMedical)
                {
                    keywordsPrompt = targetArabic
                        ? "اذكر الكلمات المفتاحية الطبية عالية الأهمية بالعربية من هذه الصفحات، وافصل بينها بفواصل (،). بدون تعريفات—مجرد الكلمات نفسها. قدّم 8–10 مصطلحات على الأقل."
                        : $"List the HIGH-YIELD MEDICAL KEYWORDS from these page(s) in {generalLangName}.  " +
                          $"Output as a comma-separated list (e.g., “keyword1, keyword2, keyword3”).  " +
                          $"Do NOT include definitions—only the keywords themselves.  " +
                          $"Provide at least 8–10 medical terms.";
                }
                else
                {
                    keywordsPrompt = targetArabic
                        ? "اذكر الكلمات المفتاحية عالية الأهمية بالعربية من هذه الصفحات، وافصل بينها بفواصل (،). بدون تعريفات—مجرد الكلمات نفسها. قدّم 8–10 كلمات على الأقل."
                        : $"List the HIGH-YIELD KEYWORDS from these page(s) in {generalLangName}.  " +
                          $"Output as a comma-separated list (e.g., “keyword1, keyword2, keyword3”).  " +
                          $"Do NOT include definitions—only the keywords themselves.  " +
                          $"Provide at least 8–10 keywords.";
                }

                // Translated Sections (نبقيها عامة؛ التنسيق RTL/LTR سيتم ضبطه عند حفظ Word)
                string translatedSectionsPrompt =
                    $"Translate the following text from {generalLangName} into {vocabLangName}. " +
                    $"Keep every sentence or paragraph exactly as it is in the original language. " +
                    $"After each sentence or paragraph, provide the translation immediately below it. " +
                    $"Do not remove or shorten any part of the original text. " +
                    $"Do not add any introductions, explanations, notes, or extra formatting. " +
                    $"Only output the text in the requested format.";

                // Explain Terms (رقم + IPA + مقاطع + بلوك عربي اختياري)
                string explainTermsPrompt;
                string arabicBlock =
                    includeArabicExplain
                        ? "ArabicExplanation (Arabic): <2–3 sentences in clear Arabic>\n" +
                          "ArabicAnalogy (Arabic): <a simple analogy/example in Arabic>\n"
                        : "";

                if (isMedical)
                {
                    explainTermsPrompt = targetArabic
                        ? "استخرج المصطلحات الطبية الأساسية التي قد لا يفهمها غير المتخصص. رقّم كل مصطلح تسلسليًا (1، 2، 3، ...). لكل مصطلح اكتب بالضبط:\n\n" +
                          "<الرقم>. المصطلح: <كما هو مكتوب>\n" +
                          "النطق: IPA = </International Phonetic Alphabet/>, المقاطع = <تقسيم مبسّط>\n" +
                          "الشرح (العربية العامة): <2–3 جمل بلغة واضحة>\n" +
                          (includeArabicExplain ? "ArabicExplanation (Arabic): <2–3 sentences in clear Arabic>\nArabicAnalogy (Arabic): <a simple analogy/example in Arabic>\n" : "") +
                          "إذا كان المصطلح اختصارًا فافتحه أولًا.\n\n" +
                          "افصل بين كل كتلة بمسطرة واحدة."
                        : $"Identify KEY MEDICAL TERMS on these page(s) that a non-specialist may not understand. " +
                          $"Number each term block sequentially (1, 2, 3, ...). For EACH term, output EXACTLY:\n\n" +
                          $"<Number>. Term: <the term as written>\n" +
                          $"Pronunciation: IPA = </International Phonetic Alphabet/>, Syllables = <break into simple syllables>\n" +
                          $"Explanation ({generalLangName}): <2–3 sentences in clear plain language>\n" +
                          arabicBlock +
                          $"If the term is an abbreviation, first expand it.\n\n" +
                          $"Separate each term block with ONE blank line. Do NOT add extra commentary.";
                }
                else
                {
                    explainTermsPrompt = targetArabic
                        ? "استخرج المصطلحات التقنية الأساسية التي قد لا يفهمها غير المتخصص. رقّم كل مصطلح تسلسليًا (1، 2، 3، ...). لكل مصطلح اكتب بالضبط:\n\n" +
                          "<الرقم>. المصطلح: <كما هو مكتوب>\n" +
                          "النطق: IPA = </International Phonetic Alphabet/>, المقاطع = <تقسيم مبسّط>\n" +
                          "الشرح (العربية العامة): <2–3 جمل بلغة واضحة>\n" +
                          (includeArabicExplain ? "ArabicExplanation (Arabic): <2–3 sentences in clear Arabic>\nArabicAnalogy (Arabic): <a simple analogy/example in Arabic>\n" : "") +
                          "إذا كان المصطلح اختصارًا فافتحه أولًا.\n\n" +
                          "افصل بين كل كتلة بمسطرة واحدة."
                        : $"Identify KEY TECHNICAL TERMS on these page(s) that a non-specialist may not understand. " +
                          $"Number each term block sequentially (1, 2, 3, ...). For EACH term, output EXACTLY:\n\n" +
                          $"<Number>. Term: <the term as written>\n" +
                          $"Pronunciation: IPA = </International Phonetic Alphabet/>, Syllables = <break into simple syllables>\n" +
                          $"Explanation ({generalLangName}): <2–3 sentences in clear plain language>\n" +
                          arabicBlock +
                          $"If the term is an abbreviation, first expand it.\n\n" +
                          $"Separate each term block with ONE blank line. Do NOT add extra commentary.";
                }




                // 6) تحديد حجم الدفعة (batch size) من الواجهة
                int batchSize = (int)radioPageBatchSize.EditValue; // reads 1, 2 or 3


                switch (batchSize)
                {
                    case 1:

                        // ─── One‐page‐at‐a‐time mode ───

                        // 6) حلقة لمعالجة كل صفحة عبر Multimodal (صورة + نص)
                        // 6) Loop through each page, only calling ProcessPdfPageMultimodal if that section is enabled:
                        foreach (var (pageNumber, image) in allPages)
                        {
                            if (chkDefinitions.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} to GPT (Definitions)...");
                                string pageDef = await ProcessPdfPageMultimodal(image, apiKey, definitionsPrompt, modelName);
                                allDefinitions.AppendLine($"===== Page {pageNumber} =====");
                                allDefinitions.AppendLine(pageDef);
                                allDefinitions.AppendLine();
                            }

                            if (chkMCQs.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} to GPT (MCQs)...");
                                string pageMCQs = await ProcessPdfPageMultimodal(image, apiKey, mcqsPrompt, modelName);
                                allMCQs.AppendLine($"===== Page {pageNumber} =====");
                                allMCQs.AppendLine(pageMCQs);
                                allMCQs.AppendLine();
                            }

                            if (chkFlashcards.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} to GPT (Flashcards)...");
                                string pageFlash = await ProcessPdfPageMultimodal(image, apiKey, flashcardsPrompt, modelName);
                                allFlashcards.AppendLine($"===== Page {pageNumber} =====");
                                allFlashcards.AppendLine(pageFlash);
                                allFlashcards.AppendLine();
                            }

                            if (chkVocabulary.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} to GPT (Vocabulary)...");
                                string pageVocab = await ProcessPdfPageMultimodal(image, apiKey, vocabularyPrompt, modelName);
                                allVocabulary.AppendLine($"===== Page {pageNumber} =====");
                                allVocabulary.AppendLine(pageVocab);
                                allVocabulary.AppendLine();
                            }

                            // ── NEW: Summary
                            if (chkSummary.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} → Summary…");
                                string pageSum = await ProcessPdfPageMultimodal(image, apiKey, summaryPrompt, modelName);
                                allSummary.AppendLine($"===== Page {pageNumber} =====");
                                allSummary.AppendLine(pageSum);
                                allSummary.AppendLine();
                            }

                            // ── NEW: Key Takeaways
                            if (chkTakeaways.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} → Key Takeaways…");
                                string pageTA = await ProcessPdfPageMultimodal(image, apiKey, takeawaysPrompt,modelName);
                                allTakeaways.AppendLine($"===== Page {pageNumber} =====");
                                allTakeaways.AppendLine(pageTA);
                                allTakeaways.AppendLine();
                            }

                            // ── NEW: Cloze
                            if (chkCloze.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} → Cloze…");
                                string pageCL = await ProcessPdfPageMultimodal(image, apiKey, clozePrompt,modelName);
                                allCloze.AppendLine($"===== Page {pageNumber} =====");
                                allCloze.AppendLine(pageCL);
                                allCloze.AppendLine();
                            }

                            // ── NEW: True/False
                            if (chkTrueFalse.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} → True/False…");
                                string pageTF = await ProcessPdfPageMultimodal(image, apiKey, trueFalsePrompt,modelName);
                                allTrueFalse.AppendLine($"===== Page {pageNumber} =====");
                                allTrueFalse.AppendLine(pageTF);
                                allTrueFalse.AppendLine();
                            }

                            // ── NEW: Outline
                            if (chkOutline.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} → Outline…");
                                string pageOL = await ProcessPdfPageMultimodal(image, apiKey, outlinePrompt,modelName);
                                allOutline.AppendLine($"===== Page {pageNumber} =====");
                                allOutline.AppendLine(pageOL);
                                allOutline.AppendLine();
                            }

                            // ── NEW: Concept Map
                            if (chkConceptMap.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} → Concept Map…");
                                string pageCM = await ProcessPdfPageMultimodal(image, apiKey, conceptMapPrompt,modelName);
                                allConceptMap.AppendLine($"===== Page {pageNumber} =====");
                                allConceptMap.AppendLine(pageCM);
                                allConceptMap.AppendLine();
                            }

                            // ── NEW: Table Extraction
                            if (chkTableExtract.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} → Table Extract…");
                                string pageTE = await ProcessPdfPageMultimodal(image, apiKey, tableExtractPrompt,modelName);
                                allTableExtract.AppendLine($"===== Page {pageNumber} =====");
                                allTableExtract.AppendLine(pageTE);
                                allTableExtract.AppendLine();
                            }

                            // ── NEW: Simplified Explanation
                            if (chkSimplified.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} → Simplified Explanation…");
                                string pageSI = await ProcessPdfPageMultimodal(image, apiKey, simplifiedPrompt,modelName);
                                allSimplified.AppendLine($"===== Page {pageNumber} =====");
                                allSimplified.AppendLine(pageSI);
                                allSimplified.AppendLine();
                            }

                            // ── NEW: Case Study Scenario
                            if (chkCaseStudy.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} → Case Study…");
                                string pageCS = await ProcessPdfPageMultimodal(image, apiKey, caseStudyPrompt,modelName);
                                allCaseStudy.AppendLine($"===== Page {pageNumber} =====");
                                allCaseStudy.AppendLine(pageCS);
                                allCaseStudy.AppendLine();
                            }

                            // ── NEW: High-Yield Keywords
                            if (chkKeywords.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} → Keywords…");
                                string pageKW = await ProcessPdfPageMultimodal(image, apiKey, keywordsPrompt,modelName);
                                allKeywords.AppendLine($"===== Page {pageNumber} =====");
                                allKeywords.AppendLine(pageKW);
                                allKeywords.AppendLine();
                            }

                            // ── NEW: Translated Sections
                            if (chkTranslatedSections.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} → Translated Sections…");
                                string pageTS = await ProcessPdfPageMultimodal(image, apiKey, translatedSectionsPrompt,modelName);
                                allTranslatedSections.AppendLine($"===== Page {pageNumber} =====");
                                allTranslatedSections.AppendLine(pageTS);
                                allTranslatedSections.AppendLine();
                            }

                            //-- NEW: ExplainTerms
                            if (chkExplainTerms.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending page {pageNumber} → Explain Terms…");
                                string pageET = await ProcessPdfPageMultimodal(image, apiKey, explainTermsPrompt,modelName);
                                allExplainTerms.AppendLine($"===== Page {pageNumber} =====");
                                allExplainTerms.AppendLine(pageET);
                                allExplainTerms.AppendLine();
                            }


                            UpdateOverlayLog($"▶▶▶ ✅ Page {pageNumber} done.");
                        }
                        break;


                    case 2:
                        // ——— Mode: 2 pages at a time ———
                        for (int i = 0; i < allPages.Count; i += 2)
                        {
                            // build a small group of up to 2 pages
                            var pageGroup = new List<(int pageNumber, SDImage image)>();
                            for (int j = i; j < i + 2 && j < allPages.Count; j++)
                                pageGroup.Add(allPages[j]);

                            int startPage = pageGroup.First().pageNumber;
                            int endPage = pageGroup.Last().pageNumber;
                            string header = (startPage == endPage)
                                ? $"===== Page {startPage} ====="
                                : $"===== Pages {startPage}–{endPage} =====";

                            if (chkDefinitions.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} to GPT (Definitions) …");
                                string pagesDef = await ProcessPdfPagesMultimodal(pageGroup, apiKey, definitionsPrompt, modelName);
                                allDefinitions.AppendLine(header);
                                allDefinitions.AppendLine(pagesDef);
                                allDefinitions.AppendLine();
                            }

                            if (chkMCQs.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} to GPT (MCQs) …");
                                string pagesMCQs = await ProcessPdfPagesMultimodal(pageGroup, apiKey, mcqsPrompt, modelName);
                                allMCQs.AppendLine(header);
                                allMCQs.AppendLine(pagesMCQs);
                                allMCQs.AppendLine();
                            }

                            if (chkFlashcards.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} to GPT (Flashcards) …");
                                string pagesFlash = await ProcessPdfPagesMultimodal(pageGroup, apiKey, flashcardsPrompt, modelName);
                                allFlashcards.AppendLine(header);
                                allFlashcards.AppendLine(pagesFlash);
                                allFlashcards.AppendLine();
                            }

                            if (chkVocabulary.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} to GPT (Vocabulary) …");
                                string pagesVocab = await ProcessPdfPagesMultimodal(pageGroup, apiKey, vocabularyPrompt, modelName);
                                allVocabulary.AppendLine(header);
                                allVocabulary.AppendLine(pagesVocab);
                                allVocabulary.AppendLine();
                            }

                            // ── NEW: Summary
                            if (chkSummary.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Summary…");
                                string pagesSum = await ProcessPdfPagesMultimodal(pageGroup, apiKey, summaryPrompt, modelName);
                                allSummary.AppendLine(header);
                                allSummary.AppendLine(pagesSum);
                                allSummary.AppendLine();
                            }

                            // ── NEW: Key Takeaways
                            if (chkTakeaways.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Key Takeaways…");
                                string pagesTA = await ProcessPdfPagesMultimodal(pageGroup, apiKey, takeawaysPrompt, modelName);
                                allTakeaways.AppendLine(header);
                                allTakeaways.AppendLine(pagesTA);
                                allTakeaways.AppendLine();
                            }

                            // ── NEW: Cloze
                            if (chkCloze.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Cloze…");
                                string pagesCL = await ProcessPdfPagesMultimodal(pageGroup, apiKey, clozePrompt, modelName);
                                allCloze.AppendLine(header);
                                allCloze.AppendLine(pagesCL);
                                allCloze.AppendLine();
                            }

                            // ── NEW: True/False
                            if (chkTrueFalse.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → True/False…");
                                string pagesTF = await ProcessPdfPagesMultimodal(pageGroup, apiKey, trueFalsePrompt, modelName);
                                allTrueFalse.AppendLine(header);
                                allTrueFalse.AppendLine(pagesTF);
                                allTrueFalse.AppendLine();
                            }

                            // ── NEW: Outline
                            if (chkOutline.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Outline…");
                                string pagesOL = await ProcessPdfPagesMultimodal(pageGroup, apiKey, outlinePrompt, modelName);
                                allOutline.AppendLine(header);
                                allOutline.AppendLine(pagesOL);
                                allOutline.AppendLine();
                            }

                            // ── NEW: Concept Map
                            if (chkConceptMap.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Concept Map…");
                                string pagesCM = await ProcessPdfPagesMultimodal(pageGroup, apiKey, conceptMapPrompt, modelName);
                                allConceptMap.AppendLine(header);
                                allConceptMap.AppendLine(pagesCM);
                                allConceptMap.AppendLine();
                            }

                            // ── NEW: Table Extraction
                            if (chkTableExtract.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Table Extract…");
                                string pagesTE = await ProcessPdfPagesMultimodal(pageGroup, apiKey, tableExtractPrompt, modelName);
                                allTableExtract.AppendLine(header);
                                allTableExtract.AppendLine(pagesTE);
                                allTableExtract.AppendLine();
                            }

                            // ── NEW: Simplified Explanation
                            if (chkSimplified.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Simplified Explanation…");
                                string pagesSI = await ProcessPdfPagesMultimodal(pageGroup, apiKey, simplifiedPrompt, modelName);
                                allSimplified.AppendLine(header);
                                allSimplified.AppendLine(pagesSI);
                                allSimplified.AppendLine();
                            }

                            // ── NEW: Case Study Scenario
                            if (chkCaseStudy.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Case Study…");
                                string pagesCS = await ProcessPdfPagesMultimodal(pageGroup, apiKey, caseStudyPrompt, modelName);
                                allCaseStudy.AppendLine(header);
                                allCaseStudy.AppendLine(pagesCS);
                                allCaseStudy.AppendLine();
                            }

                            // ── NEW: High-Yield Keywords
                            if (chkKeywords.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Keywords…");
                                string pagesKW = await ProcessPdfPagesMultimodal(pageGroup, apiKey, keywordsPrompt, modelName);
                                allKeywords.AppendLine(header);
                                allKeywords.AppendLine(pagesKW);
                                allKeywords.AppendLine();
                            }

                            // ── NEW: Translated Sections
                            if (chkTranslatedSections.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Translated Sections…");
                                string pagesTS = await ProcessPdfPagesMultimodal(pageGroup, apiKey, translatedSectionsPrompt, modelName);
                                allTranslatedSections.AppendLine(header);
                                allTranslatedSections.AppendLine(pagesTS);
                                allTranslatedSections.AppendLine();
                            }

                            //-- NEW: ExplainTerms
                            if (chkExplainTerms.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Explain Terms…");
                                string pagesET = await ProcessPdfPagesMultimodal(pageGroup, apiKey, explainTermsPrompt, modelName);
                                allExplainTerms.AppendLine(header);
                                allExplainTerms.AppendLine(pagesET);
                                allExplainTerms.AppendLine();
                            }

                            UpdateOverlayLog($"▶▶▶ ✅ Pages {startPage}–{endPage} done.");
                        }
                        break;


                    case 3:
                        // ─── Three‐page‐batch mode ───

                        // 6) Instead of one‐by‐one, we chunk into groups of three pages at a time:
                        for (int i = 0; i < allPages.Count; i += 3)
                        {
                            // Build up to a 3‐page slice
                            var pageGroup = new List<(int pageNumber, SDImage image)>();
                            for (int j = i; j < i + 3 && j < allPages.Count; j++)
                            {
                                pageGroup.Add(allPages[j]);
                            }

                            // We’ll label them by “Pages X–Y” or “Page X” if only one in the group
                            int startPage = pageGroup.First().pageNumber;
                            int endPage = pageGroup.Last().pageNumber;
                            string header = (startPage == endPage)
                                ? $"===== Page {startPage} ====="
                                : $"===== Pages {startPage}–{endPage} =====";

                            // 6a) Definitions
                            if (chkDefinitions.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} to GPT (Definitions)...");
                                string pagesDef = await ProcessPdfPagesMultimodal(pageGroup, apiKey, definitionsPrompt, modelName);
                                allDefinitions.AppendLine(header);
                                allDefinitions.AppendLine(pagesDef);
                                allDefinitions.AppendLine();
                            }

                            // 6b) MCQs
                            if (chkMCQs.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} to GPT (MCQs)...");
                                string pagesMCQs = await ProcessPdfPagesMultimodal(pageGroup, apiKey, mcqsPrompt, modelName);
                                allMCQs.AppendLine(header);
                                allMCQs.AppendLine(pagesMCQs);
                                allMCQs.AppendLine();
                            }

                            // 6c) Flashcards
                            if (chkFlashcards.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} to GPT (Flashcards)...");
                                string pagesFlash = await ProcessPdfPagesMultimodal(pageGroup, apiKey, flashcardsPrompt, modelName);
                                allFlashcards.AppendLine(header);
                                allFlashcards.AppendLine(pagesFlash);
                                allFlashcards.AppendLine();
                            }

                            // 6d) Vocabulary
                            if (chkVocabulary.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} to GPT (Vocabulary)...");
                                string pagesVocab = await ProcessPdfPagesMultimodal(pageGroup, apiKey, vocabularyPrompt, modelName);
                                allVocabulary.AppendLine(header);
                                allVocabulary.AppendLine(pagesVocab);
                                allVocabulary.AppendLine();
                            }

                            // ── NEW: Summary
                            if (chkSummary.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Summary…");
                                string pagesSum = await ProcessPdfPagesMultimodal(pageGroup, apiKey, summaryPrompt, modelName);
                                allSummary.AppendLine(header);
                                allSummary.AppendLine(pagesSum);
                                allSummary.AppendLine();
                            }

                            // ── NEW: Key Takeaways
                            if (chkTakeaways.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Key Takeaways…");
                                string pagesTA = await ProcessPdfPagesMultimodal(pageGroup, apiKey, takeawaysPrompt, modelName);
                                allTakeaways.AppendLine(header);
                                allTakeaways.AppendLine(pagesTA);
                                allTakeaways.AppendLine();
                            }

                            // ── NEW: Cloze
                            if (chkCloze.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Cloze…");
                                string pagesCL = await ProcessPdfPagesMultimodal(pageGroup, apiKey, clozePrompt, modelName);
                                allCloze.AppendLine(header);
                                allCloze.AppendLine(pagesCL);
                                allCloze.AppendLine();
                            }

                            // ── NEW: True/False
                            if (chkTrueFalse.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → True/False…");
                                string pagesTF = await ProcessPdfPagesMultimodal(pageGroup, apiKey, trueFalsePrompt, modelName);
                                allTrueFalse.AppendLine(header);
                                allTrueFalse.AppendLine(pagesTF);
                                allTrueFalse.AppendLine();
                            }

                            // ── NEW: Outline
                            if (chkOutline.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Outline…");
                                string pagesOL = await ProcessPdfPagesMultimodal(pageGroup, apiKey, outlinePrompt, modelName);
                                allOutline.AppendLine(header);
                                allOutline.AppendLine(pagesOL);
                                allOutline.AppendLine();
                            }

                            // ── NEW: Concept Map
                            if (chkConceptMap.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Concept Map…");
                                string pagesCM = await ProcessPdfPagesMultimodal(pageGroup, apiKey, conceptMapPrompt, modelName);
                                allConceptMap.AppendLine(header);
                                allConceptMap.AppendLine(pagesCM);
                                allConceptMap.AppendLine();
                            }

                            // ── NEW: Table Extraction
                            if (chkTableExtract.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Table Extract…");
                                string pagesTE = await ProcessPdfPagesMultimodal(pageGroup, apiKey, tableExtractPrompt, modelName);
                                allTableExtract.AppendLine(header);
                                allTableExtract.AppendLine(pagesTE);
                                allTableExtract.AppendLine();
                            }

                            // ── NEW: Simplified Explanation
                            if (chkSimplified.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Simplified Explanation…");
                                string pagesSI = await ProcessPdfPagesMultimodal(pageGroup, apiKey, simplifiedPrompt, modelName);
                                allSimplified.AppendLine(header);
                                allSimplified.AppendLine(pagesSI);
                                allSimplified.AppendLine();
                            }

                            // ── NEW: Case Study Scenario
                            if (chkCaseStudy.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Case Study…");
                                string pagesCS = await ProcessPdfPagesMultimodal(pageGroup, apiKey, caseStudyPrompt, modelName);
                                allCaseStudy.AppendLine(header);
                                allCaseStudy.AppendLine(pagesCS);
                                allCaseStudy.AppendLine();
                            }

                            // ── NEW: High-Yield Keywords
                            if (chkKeywords.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Keywords…");
                                string pagesKW = await ProcessPdfPagesMultimodal(pageGroup, apiKey, keywordsPrompt, modelName);
                                allKeywords.AppendLine(header);
                                allKeywords.AppendLine(pagesKW);
                                allKeywords.AppendLine();
                            }

                            // ── NEW: Translated Sections
                            if (chkTranslatedSections.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Translated Sections…");
                                string pagesTS = await ProcessPdfPagesMultimodal(pageGroup, apiKey, translatedSectionsPrompt, modelName);
                                allTranslatedSections.AppendLine(header);
                                allTranslatedSections.AppendLine(pagesTS);
                                allTranslatedSections.AppendLine();
                            }

                            //-- NEW: ExplainTerms
                            if (chkExplainTerms.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Explain Terms…");
                                string pagesET = await ProcessPdfPagesMultimodal(pageGroup, apiKey, explainTermsPrompt, modelName);
                                allExplainTerms.AppendLine(header);
                                allExplainTerms.AppendLine(pagesET);
                                allExplainTerms.AppendLine();
                            }


                            UpdateOverlayLog($"▶▶▶ ✅ Pages {startPage}–{endPage} done.");
                        }
                        break;

                    case 4:
                        // ─── Four‐page‐batch mode ───

                        // 6) Instead of one‐by‐one, we chunk into groups of three pages at a time:
                        for (int i = 0; i < allPages.Count; i += 4)
                        {
                            // Build up to a 4‐page slice
                            var pageGroup = new List<(int pageNumber, SDImage image)>();
                            for (int j = i; j < i + 4 && j < allPages.Count; j++)
                            {
                                pageGroup.Add(allPages[j]);
                            }

                            // We’ll label them by “Pages X–Y” or “Page X” if only one in the group
                            int startPage = pageGroup.First().pageNumber;
                            int endPage = pageGroup.Last().pageNumber;
                            string header = (startPage == endPage)
                                ? $"===== Page {startPage} ====="
                                : $"===== Pages {startPage}–{endPage} =====";

                            // 6a) Definitions
                            if (chkDefinitions.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} to GPT (Definitions)...");
                                string pagesDef = await ProcessPdfPagesMultimodal(pageGroup, apiKey, definitionsPrompt, modelName);
                                allDefinitions.AppendLine(header);
                                allDefinitions.AppendLine(pagesDef);
                                allDefinitions.AppendLine();
                            }

                            // 6b) MCQs
                            if (chkMCQs.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} to GPT (MCQs)...");
                                string pagesMCQs = await ProcessPdfPagesMultimodal(pageGroup, apiKey, mcqsPrompt, modelName);
                                allMCQs.AppendLine(header);
                                allMCQs.AppendLine(pagesMCQs);
                                allMCQs.AppendLine();
                            }

                            // 6c) Flashcards
                            if (chkFlashcards.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} to GPT (Flashcards)...");
                                string pagesFlash = await ProcessPdfPagesMultimodal(pageGroup, apiKey, flashcardsPrompt, modelName);
                                allFlashcards.AppendLine(header);
                                allFlashcards.AppendLine(pagesFlash);
                                allFlashcards.AppendLine();
                            }

                            // 6d) Vocabulary
                            if (chkVocabulary.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} to GPT (Vocabulary)...");
                                string pagesVocab = await ProcessPdfPagesMultimodal(pageGroup, apiKey, vocabularyPrompt, modelName);
                                allVocabulary.AppendLine(header);
                                allVocabulary.AppendLine(pagesVocab);
                                allVocabulary.AppendLine();
                            }

                            // ── NEW: Summary
                            if (chkSummary.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Summary…");
                                string pagesSum = await ProcessPdfPagesMultimodal(pageGroup, apiKey, summaryPrompt, modelName);
                                allSummary.AppendLine(header);
                                allSummary.AppendLine(pagesSum);
                                allSummary.AppendLine();
                            }

                            // ── NEW: Key Takeaways
                            if (chkTakeaways.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Key Takeaways…");
                                string pagesTA = await ProcessPdfPagesMultimodal(pageGroup, apiKey, takeawaysPrompt, modelName);
                                allTakeaways.AppendLine(header);
                                allTakeaways.AppendLine(pagesTA);
                                allTakeaways.AppendLine();
                            }

                            // ── NEW: Cloze
                            if (chkCloze.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Cloze…");
                                string pagesCL = await ProcessPdfPagesMultimodal(pageGroup, apiKey, clozePrompt, modelName);
                                allCloze.AppendLine(header);
                                allCloze.AppendLine(pagesCL);
                                allCloze.AppendLine();
                            }

                            // ── NEW: True/False
                            if (chkTrueFalse.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → True/False…");
                                string pagesTF = await ProcessPdfPagesMultimodal(pageGroup, apiKey, trueFalsePrompt, modelName);
                                allTrueFalse.AppendLine(header);
                                allTrueFalse.AppendLine(pagesTF);
                                allTrueFalse.AppendLine();
                            }

                            // ── NEW: Outline
                            if (chkOutline.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Outline…");
                                string pagesOL = await ProcessPdfPagesMultimodal(pageGroup, apiKey, outlinePrompt, modelName);
                                allOutline.AppendLine(header);
                                allOutline.AppendLine(pagesOL);
                                allOutline.AppendLine();
                            }

                            // ── NEW: Concept Map
                            if (chkConceptMap.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Concept Map…");
                                string pagesCM = await ProcessPdfPagesMultimodal(pageGroup, apiKey, conceptMapPrompt, modelName);
                                allConceptMap.AppendLine(header);
                                allConceptMap.AppendLine(pagesCM);
                                allConceptMap.AppendLine();
                            }

                            // ── NEW: Table Extraction
                            if (chkTableExtract.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Table Extract…");
                                string pagesTE = await ProcessPdfPagesMultimodal(pageGroup, apiKey, tableExtractPrompt, modelName);
                                allTableExtract.AppendLine(header);
                                allTableExtract.AppendLine(pagesTE);
                                allTableExtract.AppendLine();
                            }

                            // ── NEW: Simplified Explanation
                            if (chkSimplified.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Simplified Explanation…");
                                string pagesSI = await ProcessPdfPagesMultimodal(pageGroup, apiKey, simplifiedPrompt, modelName);
                                allSimplified.AppendLine(header);
                                allSimplified.AppendLine(pagesSI);
                                allSimplified.AppendLine();
                            }

                            // ── NEW: Case Study Scenario
                            if (chkCaseStudy.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Case Study…");
                                string pagesCS = await ProcessPdfPagesMultimodal(pageGroup, apiKey, caseStudyPrompt, modelName);
                                allCaseStudy.AppendLine(header);
                                allCaseStudy.AppendLine(pagesCS);
                                allCaseStudy.AppendLine();
                            }

                            // ── NEW: High-Yield Keywords
                            if (chkKeywords.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Keywords…");
                                string pagesKW = await ProcessPdfPagesMultimodal(pageGroup, apiKey, keywordsPrompt, modelName);
                                allKeywords.AppendLine(header);
                                allKeywords.AppendLine(pagesKW);
                                allKeywords.AppendLine();
                            }

                            // ── NEW: Translated Sections
                            if (chkTranslatedSections.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Translated Sections…");
                                string pagesTS = await ProcessPdfPagesMultimodal(pageGroup, apiKey, translatedSectionsPrompt, modelName);
                                allTranslatedSections.AppendLine(header);
                                allTranslatedSections.AppendLine(pagesTS);
                                allTranslatedSections.AppendLine();
                            }

                            //-- NEW: ExplainTerms
                            if (chkExplainTerms.Checked)
                            {
                                UpdateOverlayLog($"▶▶▶ Sending pages {startPage}–{endPage} → Explain Terms…");
                                string pagesET = await ProcessPdfPagesMultimodal(pageGroup, apiKey, explainTermsPrompt, modelName);
                                allExplainTerms.AppendLine(header);
                                allExplainTerms.AppendLine(pagesET);
                                allExplainTerms.AppendLine();
                            }


                            UpdateOverlayLog($"▶▶▶✅ Pages {startPage}–{endPage} done.");
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected batchSize: {batchSize}");
                } // end of batch size switch



                UpdateOverlayLog("▶▶▶🗂️ Selected exports (paths):");
                LogIfSelected("▶Definitions", chkDefinitions.Checked, definitionsFilePath);
                LogIfSelected("▶MCQs (.docx)", chkMCQs.Checked, mcqsFilePath);
                LogIfSelected("▶Flashcards (.docx)", chkFlashcards.Checked, flashcardsFilePath);
                LogIfSelected("▶Vocabulary (.docx)", chkVocabulary.Checked, vocabularyFilePath);
                LogIfSelected("▶Summary", chkSummary.Checked, summaryFilePath);
                LogIfSelected("▶Takeaways", chkTakeaways.Checked, takeawaysFilePath);
                LogIfSelected("▶Cloze (.docx)", chkCloze.Checked, clozeFilePath);
                LogIfSelected("▶True/False", chkTrueFalse.Checked, tfFilePath);
                LogIfSelected("▶Outline", chkOutline.Checked, outlineFilePath);
                LogIfSelected("▶Concept Map", chkConceptMap.Checked, conceptMapFilePath);
                LogIfSelected("▶Tables", chkTableExtract.Checked, tableFilePath);
                LogIfSelected("▶Simplified", chkSimplified.Checked, simplifiedFilePath);
                LogIfSelected("▶Case Study", chkCaseStudy.Checked, caseStudyFilePath);
                LogIfSelected("▶Keywords", chkKeywords.Checked, keywordsFilePath);
                LogIfSelected("▶Translated Sections", chkTranslatedSections.Checked, translatedSectionsFilePath);
                LogIfSelected("▶Explain Terms", chkExplainTerms.Checked, explainTermsFilePath);



                // 7) تحويل StringBuilder إلى نصٍّ نهائي وحفظه في ملفات Word منسّقة
                // 7.1) ملف التعاريف
                // 7) Save out only those StringBuilders that were created (i.e. their CheckEdit was checked)
                if (chkDefinitions.Checked)
                {
                    string definitionsText = allDefinitions.ToString();
                    SaveContentToFile(FormatDefinitions(definitionsText), definitionsFilePath, "Definitions");
                }

                //// 7.2) ملف MCQs (يمكن تكييف تنسيق MCQs إذا أردتم تنسيقًا أضبط)
                if (chkMCQs.Checked)
                {
                    string mcqsRaw = allMCQs.ToString();
                    // 1) still save the Word version:
                    SaveContentToFile(mcqsRaw, mcqsFilePath, "MCQs");

                    // 2) now parse & save out a .csv/.tsv
                    var parsed = ParseMcqs(mcqsRaw);
                    bool useComma = chkUseCommaDelimiter.Checked;    // or read from your combo
                    var delPath = Path.ChangeExtension(mcqsFilePath, useComma ? ".csv" : ".tsv");

                    SaveMcqsToDelimitedFile(parsed, delPath, useComma);
                }


                if (chkFlashcards.Checked)
                {
                    // 1) Word export stays as-is
                    string flashcardsRaw = allFlashcards.ToString();
                    SaveContentToFile(flashcardsRaw, flashcardsFilePath, "Flashcards");

                    // 2) Parse into (Front,Back) pairs
                    var parsed = ParseFlashcards(flashcardsRaw);

                    // 3) Build the CSV/TSV path
                    var flashCsvPath = Path.ChangeExtension(flashcardsFilePath,
                        chkUseCommaDelimiter.Checked ? ".csv" : ".tsv");

                    // 4) Write it out
                    SaveFlashcardsToDelimitedFile(parsed, flashCsvPath, chkUseCommaDelimiter.Checked);
                }


                // 7.4) ملف Vocabulary (بعد تطبيق FormatVocabulary على الناتج)
                if (chkVocabulary.Checked)
                {
                    // 1) Word export stays the same
                    string vocabularyText = FormatVocabulary(allVocabulary.ToString());
                    SaveContentToFile(vocabularyText, vocabularyFilePath, "Vocabulary");

                    // 2) Build .csv or .tsv path from the .docx
                    bool useComma = chkUseCommaDelimiter.Checked;
                    string ext = useComma ? ".csv" : ".tsv";
                    string vocabDelimitedPath = Path.ChangeExtension(vocabularyFilePath, ext);

                    // 3) Parse each line "Term - Translation" into a record
                    var records = vocabularyText
                        .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(line =>
                        {
                            var parts = line.Split(new[] { " - " }, 2, StringSplitOptions.None);
                            var term = parts[0].Trim();
                            var translation = parts.Length > 1
                                ? parts[1].Trim()
                                : "[Translation Needed]";
                            return Tuple.Create(term, translation);
                        })
                        .ToList();  // List<Tuple<string,string>>

                    // 4) Write out the delimited file for Anki
                    using (var sw = new StreamWriter(vocabDelimitedPath, false, Encoding.UTF8))
                    {
                        // Optional header (Anki doesn't strictly need it, but can help)
                        sw.WriteLine(useComma
                            ? "\"Term\",\"Translation\""
                            : "Term\tTranslation");

                        char sep = useComma ? ',' : '\t';
                        foreach (var rec in records)
                        {
                            string t = rec.Item1.Replace("\"", "\"\"");   // escape quotes
                            string tr = rec.Item2.Replace("\"", "\"\""); // escape quotes

                            if (useComma)
                                sw.WriteLine($"\"{t}\"{sep}\"{tr}\"");
                            else
                                sw.WriteLine($"{t}{sep}{tr}");
                        }
                    }

                    UpdateStatus($"Vocabulary export saved: {Path.GetFileName(vocabDelimitedPath)}");
                }


                if (chkSummary.Checked)
                    SaveContentToFile(allSummary.ToString(), summaryFilePath, "Page Summaries");

                if (chkTakeaways.Checked)
                    SaveContentToFile(allTakeaways.ToString(), takeawaysFilePath, "Key Takeaways");

                if (chkCloze.Checked)
                {
                    // 1) Word export (unchanged)
                    string clozeRaw = allCloze.ToString();
                    SaveContentToFile(clozeRaw, clozeFilePath, "Fill-in-the-Blank (Cloze)");

                    // 2) Delimited export for Anki
                    var parsed = ParseCloze(clozeRaw);                     // your (sentence,answer) pairs
                    bool useComma = chkUseCommaDelimiter.Checked;             // true ⇒ CSV, false ⇒ TSV
                    string ext = useComma ? ".csv" : ".tsv";
                    string outPath = Path.ChangeExtension(clozeFilePath, ext);

                    using (var sw = new StreamWriter(outPath, false, Encoding.UTF8))
                    {
                        // _no header_ → Anki will import every line into the Text field
                        foreach (var (sentence, answer) in parsed)
                        {
                            // inject the {{c1::answer}} into the blank
                            var markup = $"{{{{c1::{answer}}}}}";
                            var line = sentence.Replace("_______________", markup);

                            // if CSV and the line itself has commas or newlines, wrap in quotes
                            if (useComma && (line.Contains(',') || line.Contains('\n')))
                                line = $"\"{line.Replace("\"", "\"\"")}\"";

                            sw.WriteLine(line);
                        }
                    }

                    UpdateStatus($"✅ Cloze exports saved: {Path.GetFileName(clozeFilePath)} and {Path.GetFileName(outPath)}");
                }

                if (chkTrueFalse.Checked)
                    SaveContentToFile(allTrueFalse.ToString(), tfFilePath, "True/False Questions");

                if (chkOutline.Checked)
                    SaveContentToFile(allOutline.ToString(), outlineFilePath, "Outline");

                if (chkConceptMap.Checked)
                    SaveContentToFile(allConceptMap.ToString(), conceptMapFilePath, "Concept Relationships");

                if (chkTableExtract.Checked)
                    SaveMarkdownTablesToWord(allTableExtract.ToString(), tableFilePath, "Table Extractions");

                if (chkSimplified.Checked)
                    SaveContentToFile(allSimplified.ToString(), simplifiedFilePath, "Simplified Explanation");

                if (chkCaseStudy.Checked)
                    SaveContentToFile(allCaseStudy.ToString(), caseStudyFilePath, "Case Study Scenario");

                if (chkKeywords.Checked)
                    SaveContentToFile(allKeywords.ToString(), keywordsFilePath, "High-Yield Keywords");

                if (chkTranslatedSections.Checked)
                    SaveContentToFile(allTranslatedSections.ToString(), translatedSectionsFilePath, "Translated Sections");

                if (chkExplainTerms.Checked)
                    SaveContentToFile(allExplainTerms.ToString(), explainTermsFilePath, "Explain Terms");

                UpdateOverlayLog("✅ All selected exports finished successfully.");


                //// 8) إظهار رسالة انتهاء المعالجة
                UpdateOverlayLog("✅ Processing complete. Files saved to Desktop as selected outputs.");
                UpdateOverlayLog("▰▰▰▰▰ E N D   G E N E R A T I N G ▰▰▰▰▰");
                UpdateOverlayLog("▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△");
                UpdateOverlayLog("------------------------------------------------------");
                UpdateOverlayLog("                                                     ");
                UpdateOverlayLog("                                                     ");
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Error: " + ex.Message, "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("❌ An error occurred during processing.");
                UpdateOverlayLog("❌ An error occurred during processing: " + ex.Message);
            }
            finally
            {
                // نبني المستند الموحد من المخرجات المختارة
                allExtractedTexts.Clear();

                if (chkDefinitions.Checked)
                    allExtractedTexts.Add("▰▰▰ Definitions ▰▰▰\r\n" + allDefinitions.ToString());

                if (chkMCQs.Checked)
                    allExtractedTexts.Add("=== MCQs ===\r\n" + allMCQs.ToString());

                if (chkFlashcards.Checked)
                    allExtractedTexts.Add("=== Flashcards ===\r\n" + allFlashcards.ToString());

                if (chkVocabulary.Checked)
                    allExtractedTexts.Add("=== Vocabulary ===\r\n" + FormatVocabulary(allVocabulary.ToString()));

                if (chkSummary.Checked)
                    allExtractedTexts.Add("=== Page Summaries ===\r\n" + allSummary.ToString());

                if (chkTakeaways.Checked)
                    allExtractedTexts.Add("=== Key Takeaways ===\r\n" + allTakeaways.ToString());

                if (chkCloze.Checked)
                    allExtractedTexts.Add("=== Cloze ===\r\n" + allCloze.ToString());

                if (chkTrueFalse.Checked)
                    allExtractedTexts.Add("=== True/False ===\r\n" + allTrueFalse.ToString());

                if (chkOutline.Checked)
                    allExtractedTexts.Add("=== Outline ===\r\n" + allOutline.ToString());

                if (chkConceptMap.Checked)
                    allExtractedTexts.Add("=== Concept Map ===\r\n" + allConceptMap.ToString());

                if (chkTableExtract.Checked)
                    allExtractedTexts.Add("=== Table Extractions ===\r\n" + allTableExtract.ToString());

                if (chkSimplified.Checked)
                    allExtractedTexts.Add("=== Simplified Explanation ===\r\n" + allSimplified.ToString());

                if (chkCaseStudy.Checked)
                    allExtractedTexts.Add("=== Case Study ===\r\n" + allCaseStudy.ToString());

                if (chkKeywords.Checked)
                    allExtractedTexts.Add("=== High-Yield Keywords ===\r\n" + allKeywords.ToString());

                if (chkTranslatedSections.Checked)
                    allExtractedTexts.Add("=== Translated Sections ===\r\n" + allTranslatedSections.ToString());

                if (chkExplainTerms.Checked)
                    allExtractedTexts.Add("=== Explain Terms ===\r\n" + allExplainTerms.ToString());

                // (ب) حدّد مسار ملف الـ Word الموحّد
                string docxPath = Path.Combine(
                    outputFolder, "Result_" + DateTime.Now.ToString("yyyy_MM_dd___HH_mmss") + ".docx");

                // (ج) تحديث اللوج على UI thread
                if (this.InvokeRequired)
                    this.BeginInvoke(new Action(() => UpdateOverlayLog("▰▰▰ 📝 Generating Word file...")));
                else
                    UpdateOverlayLog("▰▰▰📝 Generating Word file...");

                ExportToWord_DocX(docxPath, allExtractedTexts);



                // (ج) تحديث اللوج
                if (this.InvokeRequired)
                    this.BeginInvoke(new Action(() => UpdateOverlayLog("▰▰▰ 📝 Generating Word file...")));
                else
                    UpdateOverlayLog("▰▰▰📝 Generating Word file...");

                // 🔧 Do the heavy work off the UI thread
                await Task.Run(() => ExportToWord_DocX(docxPath, allExtractedTexts));

                // (هـ) نجاح
                if (this.InvokeRequired)
                    this.BeginInvoke(new Action(() => UpdateOverlayLog("▰▰▰ ✅ Word file generated: " + docxPath)));
                else
                    UpdateOverlayLog("▰▰▰✅ Word file generated: " + docxPath);



                // (و) إعادة تفعيل الواجهة والتنظيف
                buttonProcessFile.Enabled = true;
                buttonBrowseFile.Enabled = true;
                this.MaximizeBox = true;
                this.MinimizeBox = true;
                this.Text = "ChatGPT File Processor";

                UpdateStatus("▰▰▰ Processing finished ▰▰▰");
                UpdateOverlayLog("▰▰▰ Processing finished ▰▰▰");
                HideOverlay();
            }
        }



        //// Method to save content to specific file (بصيغة فقرات تحترم RTL/LTR لكل سطر)
        //private void SaveContentToFile(string content, string filePath, string sectionTitle)
        //{
        //    Word.Application wordApp = new Word.Application();
        //    Word.Document doc = wordApp.Documents.Add();

        //    try
        //    {
        //        // عنوان القسم (يتنسّق تلقائيًا حسب اللغة)
        //        Word.Paragraph titlePara = doc.Content.Paragraphs.Add();
        //        ApplyBiDiToRange(titlePara.Range, sectionTitle);
        //        titlePara.Range.Font.Size = 14;
        //        titlePara.Format.SpaceAfter = 10;
        //        titlePara.Range.InsertParagraphAfter();

        //        // المحتوى: فقرة لكل سطر مع BiDi تلقائي
        //        string safe = content ?? string.Empty;
        //        string[] lines = safe.Replace("\r\n", "\n").Split('\n');
        //        foreach (var line in lines)
        //        {
        //            Word.Paragraph p = doc.Content.Paragraphs.Add();
        //            ApplyBiDiToRange(p.Range, line);
        //            p.Range.Font.Bold = 0;
        //            p.Format.SpaceAfter = 10;
        //            p.Range.InsertParagraphAfter();
        //        }

        //        doc.SaveAs2(filePath);
        //    }
        //    finally
        //    {
        //        doc.Close();
        //        wordApp.Quit();
        //    }

        //    UpdateStatus($"Results saved successfully to {filePath}");
        //}

        //// Method to save content to specific file (بصيغة فقرات تحترم RTL/LTR + Alignment)
        //private void SaveContentToFile(string content, string filePath, string sectionTitle)
        //{
        //    Word.Application wordApp = new Word.Application();
        //    Word.Document doc = wordApp.Documents.Add();

        //    try
        //    {
        //        // عنوان القسم
        //        Word.Paragraph titlePara = doc.Content.Paragraphs.Add();
        //        ApplyBiDiToRange(titlePara.Range, sectionTitle);
        //        // فرض المحاذاة صراحةً حسب اللغة
        //        titlePara.Alignment = LooksArabic(sectionTitle)
        //            ? Word.WdParagraphAlignment.wdAlignParagraphRight
        //            : Word.WdParagraphAlignment.wdAlignParagraphLeft;

        //        titlePara.Range.Font.Size = 14;
        //        titlePara.Format.SpaceAfter = 10;
        //        titlePara.Range.InsertParagraphAfter();

        //        // المحتوى: فقرة لكل سطر مع BiDi + Alignment صريح
        //        string safe = content ?? string.Empty;
        //        string[] lines = safe.Replace("\r\n", "\n").Split('\n');
        //        foreach (var line in lines)
        //        {
        //            Word.Paragraph p = doc.Content.Paragraphs.Add();
        //            ApplyBiDiToRange(p.Range, line);

        //            // مهم: المحاذاة تُضبط بعد وضع النص
        //            p.Alignment = LooksArabic(line)
        //                ? Word.WdParagraphAlignment.wdAlignParagraphRight
        //                : Word.WdParagraphAlignment.wdAlignParagraphLeft;

        //            p.Range.Font.Bold = 0;
        //            p.Format.SpaceAfter = 10;
        //            p.Range.InsertParagraphAfter();
        //        }

        //        doc.SaveAs2(filePath);
        //    }
        //    finally
        //    {
        //        doc.Close();
        //        wordApp.Quit();
        //    }

        //    UpdateStatus($"Results saved successfully to {filePath}");
        //}

        private void SaveContentToFile(string content, string filePath, string sectionTitle)
        {
            using (var doc = DocX.Create(filePath))
            {
                // العنوان
                var title = doc.InsertParagraph();
                AppendWithBiDi(title, sectionTitle);
                title.FontSize(14).SpacingAfter(10);

                // المحتوى: فقرة لكل سطر
                var text = (content ?? string.Empty).Replace("\r\n", "\n");
                foreach (var line in text.Split('\n'))
                {
                    var p = doc.InsertParagraph();
                    AppendWithBiDi(p, line);
                    p.FontSize(12).SpacingAfter(10);
                }

                doc.Save();
            }
        }




        //// NEW: Save markdown-style tables to a real Word table using Interop (مع BiDi + Alignment لكل خلية)
        //private void SaveMarkdownTablesToWord(string markdown, string filePath, string sectionTitle)
        //{
        //    Word.Application wordApp = new Word.Application();
        //    Word.Document doc = wordApp.Documents.Add();

        //    try
        //    {
        //        // عنوان
        //        Word.Paragraph titlePara = doc.Content.Paragraphs.Add();
        //        ApplyBiDiToRange(titlePara.Range, sectionTitle);
        //        titlePara.Alignment = LooksArabic(sectionTitle)
        //            ? Word.WdParagraphAlignment.wdAlignParagraphRight
        //            : Word.WdParagraphAlignment.wdAlignParagraphLeft;
        //        titlePara.Range.Font.Size = 14;
        //        titlePara.Format.SpaceAfter = 10;
        //        titlePara.Range.InsertParagraphAfter();

        //        if (string.IsNullOrWhiteSpace(markdown) ||
        //            markdown.Trim().Equals("No table found.", StringComparison.OrdinalIgnoreCase))
        //        {
        //            Word.Paragraph p = doc.Content.Paragraphs.Add();
        //            ApplyBiDiToRange(p.Range, "No table found.");
        //            p.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft; // نص إنجليزي
        //            p.Range.InsertParagraphAfter();
        //            doc.SaveAs2(filePath);
        //            return;
        //        }

        //        string text = markdown.Replace("\r\n", "\n");
        //        string[] lines = text.Split('\n');

        //        var alignRow = new System.Text.RegularExpressions.Regex(@"^\|\s*:?-+\s*(\|\s*:?-+\s*)+\|$");

        //        int i = 0;
        //        while (i < lines.Length)
        //        {
        //            string line = lines[i].Trim();

        //            // أسطر ليست جداول (عناوين/فواصل)
        //            if (string.IsNullOrWhiteSpace(line)) { i++; continue; }
        //            if (!line.StartsWith("|"))
        //            {
        //                Word.Paragraph p = doc.Content.Paragraphs.Add();
        //                ApplyBiDiToRange(p.Range, line);
        //                p.Alignment = LooksArabic(line)
        //                    ? Word.WdParagraphAlignment.wdAlignParagraphRight
        //                    : Word.WdParagraphAlignment.wdAlignParagraphLeft;
        //                p.Range.InsertParagraphAfter();
        //                i++;
        //                continue;
        //            }

        //            // تجميع أسطر الجدول المتتالية
        //            var tableLines = new System.Collections.Generic.List<string>();
        //            while (i < lines.Length && lines[i].Trim().StartsWith("|"))
        //            {
        //                tableLines.Add(lines[i].Trim());
        //                i++;
        //            }

        //            // تحويلها لصفوف/أعمدة (تجاهل سطر المحاذاة)
        //            var rows = new System.Collections.Generic.List<string[]>();
        //            for (int k = 0; k < tableLines.Count; k++)
        //            {
        //                string t = tableLines[k];
        //                if (alignRow.IsMatch(t)) continue;

        //                string inner = t;
        //                if (inner.StartsWith("|")) inner = inner.Substring(1);
        //                if (inner.EndsWith("|")) inner = inner.Substring(0, inner.Length - 1);

        //                string[] cells = inner.Split(new[] { '|' }, StringSplitOptions.None);
        //                for (int c = 0; c < cells.Length; c++) cells[c] = cells[c].Trim();
        //                rows.Add(cells);
        //            }

        //            if (rows.Count == 0) continue;

        //            // أعمدة
        //            int cols = 0;
        //            for (int r = 0; r < rows.Count; r++)
        //                if (rows[r].Length > cols) cols = rows[r].Length;

        //            // إنشاء الجدول
        //            Word.Paragraph tblPara = doc.Content.Paragraphs.Add();
        //            Word.Range rng = tblPara.Range;
        //            Word.Table tbl = doc.Tables.Add(rng, rows.Count, cols);
        //            tbl.Borders.Enable = 1;
        //            if (rows.Count > 0) tbl.Rows[1].Range.Bold = 1;

        //            // تعبئة الخلايا + BiDi + Alignment صريح
        //            for (int r = 0; r < rows.Count; r++)
        //            {
        //                for (int c = 0; c < cols; c++)
        //                {
        //                    string cellText = (c < rows[r].Length) ? rows[r][c] : string.Empty;
        //                    Word.Range cellRange = tbl.Cell(r + 1, c + 1).Range;
        //                    string clean = (cellText ?? string.Empty).TrimEnd('\r', '\a');

        //                    ApplyBiDiToRange(cellRange, clean);
        //                    cellRange.ParagraphFormat.Alignment = LooksArabic(clean)
        //                        ? Word.WdParagraphAlignment.wdAlignParagraphRight
        //                        : Word.WdParagraphAlignment.wdAlignParagraphLeft;
        //                }
        //            }

        //            // ملاءمة ذاتية + سطر فارغ بعد كل جدول
        //            tbl.AutoFitBehavior(Word.WdAutoFitBehavior.wdAutoFitContent);
        //            Word.Paragraph after = doc.Content.Paragraphs.Add();
        //            after.Range.InsertParagraphAfter();
        //        }

        //        doc.SaveAs2(filePath);
        //    }
        //    finally
        //    {
        //        doc.Close();
        //        wordApp.Quit();
        //    }

        //    UpdateStatus($"Results saved successfully to {filePath}");
        //}

        //private void SaveMarkdownTablesToWord(string markdown, string filePath, string sectionTitle)
        //{
        //    using (var doc = DocX.Create(filePath))
        //    {
        //        var title = doc.InsertParagraph(sectionTitle).FontSize(14).SpacingAfter(10);
        //        title.Alignment = LooksArabic(sectionTitle) ? Alignment.right : Alignment.left;

        //        if (string.IsNullOrWhiteSpace(markdown) ||
        //            markdown.Trim().Equals("No table found.", StringComparison.OrdinalIgnoreCase))
        //        {
        //            doc.InsertParagraph("No table found.").Alignment = Alignment.left;
        //            doc.Save();
        //            UpdateStatus($"Results saved successfully to {filePath}");
        //            return;
        //        }

        //        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        //        var alignRow = new System.Text.RegularExpressions.Regex(@"^\|\s*:?-+\s*(\|\s*:?-+\s*)+\|$");

        //        int i = 0;
        //        while (i < lines.Length)
        //        {
        //            var line = lines[i].Trim();

        //            // نص عادي
        //            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("|"))
        //            {
        //                if (!string.IsNullOrWhiteSpace(line))
        //                {
        //                    var p = doc.InsertParagraph(line).SpacingAfter(6);
        //                    p.Alignment = LooksArabic(line) ? Alignment.right : Alignment.left;
        //                }
        //                i++;
        //                continue;
        //            }

        //            // تجميع أسطر الجدول
        //            var tableLines = new List<string>();
        //            while (i < lines.Length && lines[i].Trim().StartsWith("|"))
        //            {
        //                tableLines.Add(lines[i].Trim());
        //                i++;
        //            }

        //            // تحويل إلى صفوف/أعمدة (تجاهل سطر المحاذاة)
        //            var rows = new List<string[]>();
        //            foreach (var tl in tableLines)
        //            {
        //                if (alignRow.IsMatch(tl)) continue;
        //                var inner = tl.Trim('|');
        //                var cells = inner.Split('|').Select(c => c.Trim()).ToArray();
        //                rows.Add(cells);
        //            }
        //            if (rows.Count == 0) continue;

        //            int cols = rows.Max(r => r.Length);
        //            var tbl = doc.AddTable(rows.Count, cols);
        //            tbl.Design = TableDesign.TableGrid;

        //            //for (int r = 0; r < rows.Count; r++)
        //            //{
        //            //    for (int c = 0; c < cols; c++)
        //            //    {
        //            //        var cellText = c < rows[r].Length ? rows[r][c] : string.Empty;
        //            //        var para = tbl.Rows[r].Cells[c].Paragraphs[0];

        //            //        // 🔧 لا تستخدم ReplaceText القديم (المتقادم)
        //            //        para.Text = "";                 // امسح أي نص افتراضي
        //            //        para.Append(cellText ?? "");    // اكتب النص
        //            //        para.Alignment = LooksArabic(cellText) ? Alignment.right : Alignment.left;
        //            //    }
        //            //}
        //            for (int r = 0; r < rows.Count; r++)
        //            {
        //                for (int c = 0; c < cols; c++)
        //                {
        //                    var cellText = c < rows[r].Length ? rows[r][c] : string.Empty;

        //                    // خذ الفقرة الأولى في الخلية
        //                    var para = tbl.Rows[r].Cells[c].Paragraphs[0];

        //                    // امسح أي نص افتراضي (Text خاصية قراءة فقط؛ استعمل RemoveText/ReplaceText)
        //                    if (!string.IsNullOrEmpty(para.Text))
        //                        para.RemoveText(0);              // يحذف كل النص الموجود

        //                    // اكتب النص الجديد
        //                    para.Append(cellText ?? string.Empty);

        //                    // المحاذاة حسب اللغة
        //                    para.Alignment = LooksArabic(cellText) ? Alignment.right : Alignment.left;
        //                }
        //            }


        //            // اجعل الصف الأول عناوين إن وجد أكثر من صف
        //            if (rows.Count > 1)
        //                foreach (var p in tbl.Rows[0].Cells.SelectMany(x => x.Paragraphs)) p.Bold();

        //            doc.InsertTable(tbl);
        //            doc.InsertParagraph().SpacingAfter(8);
        //        }

        //        doc.Save();
        //    }
        //    UpdateStatus($"Results saved successfully to {filePath}");
        //}


        private void SaveMarkdownTablesToWord(string markdown, string filePath, string sectionTitle)
        {
            // إنشاء المستند
            using (var doc = DocX.Create(filePath))
            {
                // عنوان القسم
                var title = doc.InsertParagraph();
                AppendWithBiDi(title, sectionTitle);
                title.FontSize(14).Bold().SpacingAfter(10);

                // لا يوجد جدول
                if (string.IsNullOrWhiteSpace(markdown) ||
                    markdown.Trim().Equals("No table found.", System.StringComparison.OrdinalIgnoreCase))
                {
                    var p = doc.InsertParagraph();
                    AppendWithBiDi(p, "No table found.");
                    p.FontSize(12).SpacingAfter(10);
                    doc.Save();
                    UpdateStatus($"Results saved successfully to {filePath}");
                    return;
                }

                // تجهيز السطور
                var lines = (markdown ?? string.Empty).Replace("\r\n", "\n").Split('\n');
                var alignRow = new System.Text.RegularExpressions.Regex(@"^\|\s*:?-+\s*(\|\s*:?-+\s*)+\|$");

                int i = 0;
                while (i < lines.Length)
                {
                    string line = lines[i].Trim();

                    // أسطر نصّية (ليست جداول)
                    if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("|"))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var p = doc.InsertParagraph();
                            AppendWithBiDi(p, line);
                            p.FontSize(12).SpacingAfter(6);
                        }
                        i++;
                        continue;
                    }

                    // تجميع أسطر الجدول المتتالية
                    var tableLines = new List<string>();
                    while (i < lines.Length && lines[i].Trim().StartsWith("|"))
                    {
                        tableLines.Add(lines[i].Trim());
                        i++;
                    }

                    // تحويل إلى صفوف/أعمدة، مع تجاهل سطر المحاذاة
                    var rows = new List<string[]>();
                    foreach (var tl in tableLines)
                    {
                        if (alignRow.IsMatch(tl)) continue;       // سطر --- | :---: | ---:
                        var inner = tl.Trim('|');                 // أزل | الأولى والأخيرة
                        var cells = inner.Split('|').Select(c => c.Trim()).ToArray();
                        rows.Add(cells);
                    }
                    if (rows.Count == 0) continue;

                    // عدد الأعمدة الأقصى
                    int cols = rows.Max(r => r.Length);
                    var tbl = doc.AddTable(rows.Count, cols);
                    tbl.Design = TableDesign.TableGrid;

                    // تعبئة الخلايا مع BiDi
                    for (int r = 0; r < rows.Count; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            var cellText = (c < rows[r].Length) ? (rows[r][c] ?? string.Empty) : string.Empty;

                            // DocX: الفقرة الافتراضية لكل خلية
                            var para = tbl.Rows[r].Cells[c].Paragraphs[0];

                            // أزل أي نص سابق (Paragraph.Text قراءة فقط، استخدم RemoveText)
                            if (!string.IsNullOrEmpty(para.Text))
                                para.RemoveText(0);

                            AppendWithBiDi(para, cellText);
                            para.FontSize(11);
                        }
                    }

                    // اجعل الصف الأول عناوين إن وُجد أكثر من صف
                    if (rows.Count > 1)
                    {
                        foreach (var p in tbl.Rows[0].Cells.SelectMany(x => x.Paragraphs))
                            p.Bold();
                    }

                    // إدراج الجدول وسطر فارغ بعده
                    doc.InsertTable(tbl);
                    doc.InsertParagraph().SpacingAfter(8);
                }

                // حفظ المستند
                doc.Save();
            }

            UpdateStatus($"Results saved successfully to {filePath}");
        }



        // يحدّد إن كان النص “يبدو عربيًا”: العربية الأساسية 0600–06FF +
        // Arabic Supplement 0750–077F + Arabic Extended-A 08A0–08FF +
        // Arabic Presentation Forms-A/B: FB50–FDFF و FE70–FEFF + الأرقام العربية 0660–0669
        private static bool LooksArabic(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < s.Length; i++)
            {
                int u = s[i];
                if ((u >= 0x0600 && u <= 0x06FF) || // Arabic
                    (u >= 0x0750 && u <= 0x077F) || // Arabic Supplement
                    (u >= 0x08A0 && u <= 0x08FF) || // Arabic Extended-A
                    (u >= 0xFB50 && u <= 0xFDFF) || // Arabic Presentation Forms-A
                    (u >= 0xFE70 && u <= 0xFEFF) || // Arabic Presentation Forms-B
                    (u >= 0x0660 && u <= 0x0669))   // Arabic-Indic digits
                {
                    return true;
                }
            }
            return false;
        }

        //// طبّق اتجاه/محاذاة وخط مناسبين حسب اللغة على Word.Range
        //private static void ApplyBiDiToRange(Word.Range rng, string text)
        //{
        //    bool isAr = LooksArabic(text);
        //    string safe = text ?? string.Empty;

        //    // اكتب النص أولاً
        //    rng.Text = safe;

        //    var ro = isAr ? Word.WdReadingOrder.wdReadingOrderRtl
        //                  : Word.WdReadingOrder.wdReadingOrderLtr;
        //    var al = isAr ? Word.WdParagraphAlignment.wdAlignParagraphRight
        //                  : Word.WdParagraphAlignment.wdAlignParagraphLeft;

        //    // طبّق على تنسيق الفقرة في الـRange
        //    rng.ParagraphFormat.ReadingOrder = ro;
        //    rng.ParagraphFormat.Alignment = al;

        //    // طبّق أيضاً على كل فقرة ضمن الـRange (بعض البيئات تحتاج هذا)
        //    foreach (Word.Paragraph para in rng.Paragraphs)
        //    {
        //        try
        //        {
        //            para.Format.ReadingOrder = ro;
        //            para.Alignment = al;  // فرض المحاذاة يمين/يسار
        //        }
        //        finally
        //        {
        //            try { System.Runtime.InteropServices.Marshal.ReleaseComObject(para); } catch { }
        //        }
        //    }

        //    // خطوط BiDi
        //    try
        //    {
        //        if (isAr) { rng.Font.NameBi = "Segoe UI"; rng.Font.SizeBi = rng.Font.Size; }
        //        else { rng.Font.Name = "Segoe UI"; }
        //    }
        //    catch { }
        //}

        //// يحسم RTL/LTR + Alignment يقينًا باستخدام Selection.RtlPara / LtrPara
        //private static void ApplyBiDiToRange(Word.Range rng, string text)
        //{
        //    bool isAr = LooksArabic(text);
        //    string safe = text ?? string.Empty;

        //    // اكتب النص أولاً
        //    rng.Text = safe;

        //    // اختر المدى وطبّق الأمر المناسب (يضبط الاتجاه + المحاذاة معًا)
        //    rng.Select();
        //    Word.Selection sel = rng.Application.Selection;

        //    if (isAr)
        //    {
        //        sel.RtlPara(); // يجعل الفقرة RTL ويضبط المحاذاة يمينًا
        //        try { sel.Range.Font.NameBi = "Segoe UI"; } catch { }
        //        sel.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;
        //    }
        //    else
        //    {
        //        sel.LtrPara(); // يجعل الفقرة LTR ويضبط المحاذاة يسارًا
        //        try { sel.Range.Font.Name = "Segoe UI"; } catch { }
        //        sel.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
        //    }
        //}


        private static void AppendWithBiDi(Paragraph p, string text)
        {
            var safe = text ?? string.Empty;
            bool isAr = LooksArabic(safe);

            // اضبط اتجاه ومحاذاة الفقرة
            p.Direction = isAr ? Direction.RightToLeft : Direction.LeftToRight;
            p.Alignment = isAr ? Alignment.right : Alignment.left;

            // اكتب النص
            p.Append(safe).Font("Segoe UI"); // اختياري: غيّر الخط إذا رغبت
        }


        private void LoadApiKeyAndModel()
        {
            EnsureConfigDirectoryExists();

            // Load API key
            if (File.Exists(apiKeyPath))
            {
                textEditAPIKey.Text = File.ReadAllText(apiKeyPath).Trim(); // Use the new text edit control
            }

            // Load selected model
            if (File.Exists(modelPath))
            {
                string savedModel = File.ReadAllText(modelPath).Trim();
                if (comboBoxEditModel.Properties.Items.Contains(savedModel))
                {
                    comboBoxEditModel.SelectedItem = savedModel; // Use the new combo box for model selection
                }
                else
                {
                    comboBoxEditModel.SelectedIndex = 0;  // Default to first item if model is not in options
                }
            }
            else
            {
                comboBoxEditModel.SelectedIndex = 0;  // Default if model file is missing
            }
        }

        private void SaveApiKeyAndModel()
        {
            EnsureConfigDirectoryExists();

            // Save API key
            string apiKey = textEditAPIKey.Text.Trim(); // Use the new text edit control
            File.WriteAllText(apiKeyPath, apiKey);

            // Save selected model
            string selectedModel = comboBoxEditModel.SelectedItem?.ToString() ?? "gpt-4o"; // Use the new combo box for model selection
            File.WriteAllText(modelPath, selectedModel);
        }

        // Create the directory in AppData if it doesn’t exist
        private void EnsureConfigDirectoryExists()
        {
            var configDirectory = Path.GetDirectoryName(apiKeyPath);
            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }
        }


        private void comboBoxModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateStatus("▶ Model changed, saving selection...");
            SaveApiKeyAndModel();
        }
        private void comboBoxEditModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateStatus("▶ Model changed, saving selection...");
            SaveApiKeyAndModel();
        }



        // Function to format definitions
        private string FormatDefinitions(string text)
        {
            //var formattedDefinitions = new List<string>();
            //var lines = text.Split('\n');

            //foreach (var line in lines)
            //{
            //    string cleanedLine = line.TrimStart('-', ' ');
            //    if (!string.IsNullOrWhiteSpace(cleanedLine))
            //        formattedDefinitions.Add(cleanedLine);
            //}
            //return string.Join("\n\n", formattedDefinitions);
            return text;
        }



        private string FormatVocabulary(string text)
        {
            var formattedVocabulary = new List<string>();
            var terms = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in terms)
            {
                // نمط يلتقط dash أو en-dash أو colon، ويتجاهل المسافات الزائدة
                var match = Regex.Match(line, @"^(?<english>.+?)\s*[-–:]\s*(?<arabic>.+)$");
                if (match.Success)
                {
                    string english = match.Groups["english"].Value.Trim();
                    string arabic = match.Groups["arabic"].Value.Trim();
                    formattedVocabulary.Add($"{english} - {arabic}");
                }
                else
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        formattedVocabulary.Add($"{trimmed} - [Translation Needed]");
                    }
                }
            }

            return string.Join("\n", formattedVocabulary);
        }


        private List<(int pageNumber, SDImage image)> ConvertPdfToImages(string filePath, int dpi = 300)
        {
            var pages = new List<(int, SDImage)>();
            using (var document = PdfiumViewer.PdfDocument.Load(filePath))
            {
                //for (int i = 0; i < document.PageCount; i++)
                int from = Math.Max(0, selectedFromPage - 1);
                int to = Math.Min(document.PageCount - 1, selectedToPage - 1);

                for (int i = from; i <= to; i++)
                {
                    // high DPI (300+) for better image quality
                    var img = document.Render(i, dpi, dpi, true);
                    pages.Add((i + 1, img));
                }
            }
            return pages;
        }


        // يرسل صورةً إلى GPT-4o مع تعليمات لاستخراج كل المحتوى القابل للقراءة.
        public async System.Threading.Tasks.Task<string> SendImageToGPTAsync(SDImage image, string apiKey, string modelName)
        {
            // 1) تصغير + ضغط
            string base64;
            using (var scaled = ResizeForApi(image, 1280))
            {
                base64 = ToBase64Jpeg(scaled, 85L);
            }

            var jsonBody = new
            {
                model = modelName,
                messages = new[]
                {
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "image_url", image_url = new { url = "data:image/jpeg;base64," + base64 } },
                    new { type = "text", text = "Please extract all readable content from this page including equations, tables, and diagrams if present." }
                }
            }
        }
            };

            // إعداد الترويسات
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            const int maxRetries = 3;
            int delayMs = 1200;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(7));
                try
                {
                    var content = new StringContent(
                        Newtonsoft.Json.JsonConvert.SerializeObject(jsonBody), Encoding.UTF8, "application/json");

                    var response = await _http.PostAsync("https://api.openai.com/v1/chat/completions", content, cts.Token);
                    string body = await response.Content.ReadAsStringAsync(); // بدون Token في .NET Framework

                    if (!response.IsSuccessStatusCode)
                        throw new Exception("API Error: " + (int)response.StatusCode + " - " + body);

                    return body; // JSON خام
                }
                catch (TaskCanceledException)
                {
                    if (attempt == maxRetries) throw;
                    await Task.Delay(delayMs);
                    delayMs *= 2;
                }
                catch
                {
                    if (attempt == maxRetries) throw;
                    await Task.Delay(delayMs);
                    delayMs *= 2;
                }
                finally
                {
                    cts.Dispose();
                }
            }

            return null;
        }


        ///// يعالج صفحةً واحدةً (كـ صورة) بطريقة Multimodal: يرسل الصورة + التعليمات النصّية دفعةً واحدة إلى GPT-4o.
        ///// يرجع النصّ الناتج (مثل التعاريف أو الأسئلة) مباشرة.
        private async Task<string> ProcessPdfPageMultimodal(
    SDImage image, string apiKey, string taskPrompt, string modelName)
        {
            // تصغير + ضغط لتقليل زمن الرفع/المعالجة
            string base64;
            using (var scaled = ResizeForApi(image, 1024))  // ارجعها 1280 إذا تحب جودة أعلى
            {
                base64 = ToBase64Jpeg(scaled, 80L);         // 80 = حجم أقل وسرعة أعلى
            }

            var requestBody = new
            {
                model = modelName,
                messages = new object[]
                {
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "image_url", image_url = new { url = "data:image/jpeg;base64," + base64 } },
                    new { type = "text", text = taskPrompt }
                }
            }
                }
            };

            string jsonContent = System.Text.Json.JsonSerializer.Serialize(
                requestBody,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }
            );

            const int maxRetries = 4;
            int delayMs = 1200;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(7)); // أطول من _http.Timeout
                try
                {
                    using (var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions"))
                    {
                        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                        req.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        using (var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token))
                        {
                            string resultJson = await resp.Content.ReadAsStringAsync();
                            int status = (int)resp.StatusCode;
                            bool transient = (status == 429) || (status >= 500);

                            if (!resp.IsSuccessStatusCode)
                            {
                                if (transient && attempt < maxRetries)
                                    throw new Exception("Transient: " + status + " - " + resultJson);

                                throw new Exception("API Error: " + status + " - " + resultJson);
                            }

                            var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(resultJson);
                            var text = jsonNode?["choices"]?[0]?["message"]?["content"]?.ToString();
                            return string.IsNullOrEmpty(text) ? "No content returned." : text;
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    if (attempt == maxRetries) throw;
                    await Task.Delay(delayMs); delayMs *= 2;
                }
                catch (Exception ex)
                {
                    if (!ex.Message.StartsWith("Transient") || attempt == maxRetries) throw;
                    await Task.Delay(delayMs); delayMs *= 2;
                }
                finally
                {
                    cts.Dispose();
                }
            }

            return "No content returned.";
        }


        //Sends up to N images (in pageGroup) plus the text prompt in one chat call.
        //This works for batchSize = 2 or 3 or 4.
        private async Task<string> ProcessPdfPagesMultimodal(
    List<(int pageNumber, SDImage image)> pageGroup,
    string apiKey,
    string taskPrompt,
    string modelName)
        {
            var imageContents = new List<object>();
            foreach (var tuple in pageGroup)
            {
                var img = tuple.image;

                string base64;
                using (var scaled = ResizeForApi(img, 1024))
                {
                    base64 = ToBase64Jpeg(scaled, 80L);
                }

                imageContents.Add(new
                {
                    type = "image_url",
                    image_url = new { url = "data:image/jpeg;base64," + base64 }
                });
            }

            var fullContent = new List<object>();
            fullContent.AddRange(imageContents);
            fullContent.Add(new { type = "text", text = taskPrompt });

            var requestBody = new
            {
                model = modelName,
                messages = new object[]
                {
            new { role = "user", content = fullContent.ToArray() }
                }
            };

            string jsonContent = System.Text.Json.JsonSerializer.Serialize(
                requestBody,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }
            );

            const int maxRetries = 4;
            int delayMs = 1200;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(7));
                try
                {
                    using (var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions"))
                    {
                        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                        req.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        using (var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token))
                        {
                            string resultJson = await resp.Content.ReadAsStringAsync();
                            int status = (int)resp.StatusCode;
                            bool transient = (status == 429) || (status >= 500);

                            if (!resp.IsSuccessStatusCode)
                            {
                                if (transient && attempt < maxRetries)
                                    throw new Exception("Transient: " + status + " - " + resultJson);

                                throw new Exception("API Error: " + status + " – " + resultJson);
                            }

                            var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(resultJson);
                            var text = jsonNode?["choices"]?[0]?["message"]?["content"]?.ToString();
                            return string.IsNullOrEmpty(text) ? "No content returned." : text;
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    if (attempt == maxRetries) throw;
                    await Task.Delay(delayMs); delayMs *= 2;
                }
                catch (Exception ex)
                {
                    if (!ex.Message.StartsWith("Transient") || attempt == maxRetries) throw;
                    await Task.Delay(delayMs); delayMs *= 2;
                }
                finally
                {
                    cts.Dispose();
                }
            }

            return "No content returned.";
        }


        // HttpClient مُشترك للتطبيق كله (أفضل ممارسة + نتفادى مشاكل المنافذ/المهلات)
        private static readonly HttpClient _http = new HttpClient(
            new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            })
        {
            Timeout = TimeSpan.FromMinutes(6) // زدها إذا تحتاج
        };

        // (اختياري) تأكيد TLS 1.2
        static Form1()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        }

        // تصغير الصورة قبل الإرسال
        private static SDImage ResizeForApi(SDImage src, int maxWidth = 1280)
        {
            if (src.Width <= maxWidth) return (SDImage)src.Clone();
            int newHeight = (int)Math.Round(src.Height * (maxWidth / (double)src.Width));
            var bmp = new Bitmap(maxWidth, newHeight);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, 0, 0, maxWidth, newHeight);
            }
            return bmp;
        }

        // حفظ JPEG بجودة مضبوطة ثم تحويله إلى Base64
        private static string ToBase64Jpeg(SDImage img, long jpegQuality = 85L)
        {
            using (var ms = new MemoryStream())
            {
                var enc = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
                    .First(e => e.MimeType == "image/jpeg");
                var ep = new System.Drawing.Imaging.EncoderParameters(1);
                ep.Param[0] = new System.Drawing.Imaging.EncoderParameter(
                    System.Drawing.Imaging.Encoder.Quality, jpegQuality);
                img.Save(ms, enc, ep);
                return Convert.ToBase64String(ms.ToArray());
            }
        }


        // C# 7.3 compatible – لا COM ولا STA
        private void ExportToWord_DocX(string filePath, IList<string> sections)
        {
            // يتأكد من المجلد
            var dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using (var doc = DocX.Create(filePath))
            {
                for (int i = 0; i < sections.Count; i++)
                {
                    //// عنوان اختياري لكل مقطع
                    //// تقدر تشيله لو أنت مُسبقاً ضايف عناوين داخل النص
                    //doc.InsertParagraph($"Section {i + 1}")
                    //    .Bold()
                    //    .FontSize(14)
                    //    .SpacingAfter(6);

                    // النص
                    doc.InsertParagraph(sections[i])
                       .FontSize(12)
                       .SpacingAfter(12);

                    // فاصل صفحة بين المقاطع (عدا آخر واحد)
                    if (i < sections.Count - 1)
                    {
                        var p = doc.InsertParagraph(string.Empty, false);
                        p.InsertPageBreakAfterSelf();  // Page Break
                    }
                }

                doc.Save();
            }
        }


        // Overlay panel and its controls
        private void InitializeOverlay()
        {
            overlayPanel = new Panel
            {
                Size = this.ClientSize,
                BackColor = Color.FromArgb(150, Color.Black),
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };

            int centerX = overlayPanel.Width / 2;

            loadingIcon = new PictureBox
            {
                //Size = new Size(120, 120),
                Size = new Size(600, 320),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = Properties.Resources.loading_gif,
                //Location = new System.Drawing.Point(centerX - 60, overlayPanel.Height / 2 - 150)
                Location = new System.Drawing.Point(centerX - 300, overlayPanel.Height / 2 - 300)
            };

            statusLabel = new Label
            {
                AutoSize = false,
                //Size = new Size(400, 40),

                Size = new Size(600, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new System.Drawing.Font("Segoe UI", 12, FontStyle.Bold),
                //Location = new System.Drawing.Point(centerX - 200, loadingIcon.Bottom + 10),
                Location = new System.Drawing.Point(centerX - 300, loadingIcon.Bottom + 10),
                Text = "⏳ Processing, please wait...",
                Anchor = AnchorStyles.None
            };

            logTextBox = new TextBox
            {
                Size = new Size(600, 200),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.DarkSlateBlue,
                ForeColor = Color.White,
                Font = new System.Drawing.Font("Consolas", 10),
                Location = new System.Drawing.Point(centerX - 300, statusLabel.Bottom + 10)
            };

            overlayPanel.Controls.Add(loadingIcon);
            overlayPanel.Controls.Add(statusLabel);
            overlayPanel.Controls.Add(logTextBox);
            this.Controls.Add(overlayPanel);
        }

        private void UpdateOverlayLog(string message)
        {
            if (logTextBox == null) return; // prevent error if not initialized

            if (logTextBox.InvokeRequired)
            {
                logTextBox.Invoke(new System.Action(() => logTextBox.AppendText(message + Environment.NewLine)));
            }
            else
            {
                logTextBox.AppendText(message + Environment.NewLine);
            }
        }


        private void ShowOverlay(string message)
        {
            statusLabel.Text = message;
            overlayPanel.BringToFront();
            overlayPanel.Visible = true;
            overlayPanel.Refresh();
        }

        private void HideOverlay()
        {
            overlayPanel.Visible = false;
        }

        private void LogIfSelected(string label, bool enabled, string path)
        {
            if (enabled && !string.IsNullOrWhiteSpace(path))
                UpdateOverlayLog($"{label} → {Path.GetFileName(path)}");
        }

        private void loadCheckBoxesSettings()
        {
            // Load the settings for checkboxes from a file or application settings
            // Load saved user preferences into each CheckEdit:
            chkDefinitions.Checked = Properties.Settings.Default.GenerateDefinitions;
            chkMCQs.Checked = Properties.Settings.Default.GenerateMCQs;
            chkFlashcards.Checked = Properties.Settings.Default.GenerateFlashcards;
            chkVocabulary.Checked = Properties.Settings.Default.GenerateVocabulary;
            chkMedicalMaterial.Checked = Properties.Settings.Default.MedicalMaterial;
            chkSummary.Checked = Properties.Settings.Default.GenerateSummary;
            chkTakeaways.Checked = Properties.Settings.Default.GenerateTakeaways;
            chkCloze.Checked = Properties.Settings.Default.GenerateCloze;
            chkTrueFalse.Checked = Properties.Settings.Default.GenerateTrueFalse;
            chkOutline.Checked = Properties.Settings.Default.GenerateOutline;
            chkConceptMap.Checked = Properties.Settings.Default.GenerateConceptMap;
            chkTableExtract.Checked = Properties.Settings.Default.GenerateTableExtract;
            chkSimplified.Checked = Properties.Settings.Default.GenerateSimplified;
            chkCaseStudy.Checked = Properties.Settings.Default.GenerateCaseStudy;
            chkKeywords.Checked = Properties.Settings.Default.GenerateKeywords;
            chkUseCommaDelimiter.Checked = Properties.Settings.Default.useCommaDelimiter;
            chkTranslatedSections.Checked = Properties.Settings.Default.GenerateTranslatedSections;
            chkExplainTerms.Checked = Properties.Settings.Default.GenerateExplainTerms;
            chkArabicExplainTerms.Checked = Properties.Settings.Default.ArabicExplainTerms;

            // Other settings:
            chkUseSessionFolder.Checked = Properties.Settings.Default.UseSessionFolder;
            chkSaveBesidePdf.Checked = Properties.Settings.Default.SaveBesidePdf;
            chkOrganizeByType.Checked = Properties.Settings.Default.OrganizeByType;
            textEditOutputFolder.Text = GetOutputFolder();
            
            // API Key and model:
            textEditAPIKey.ReadOnly = Properties.Settings.Default.ApiKeyLock;
        }

        private void chkDefinitions_CheckedChanged(object sender, EventArgs e)
        {
            if (chkDefinitions.Checked)
            {
                UpdateStatus("▶ Definitions...Activated");
            }
            else
            {
                UpdateStatus("▶ Definitions...Deactivated");
            }
            // Save the state of the checkbox
            Properties.Settings.Default.GenerateDefinitions = chkDefinitions.Checked;
            Properties.Settings.Default.Save();
        }

        private void chkMCQs_CheckedChanged(object sender, EventArgs e)
        {
            if (chkMCQs.Checked)
            {
                UpdateStatus("▶ MCQs...Activated");
            }
            else
            {
                UpdateStatus("▶ MCQs...Deactivated");
            }
            // Save the state of the checkbox
            Properties.Settings.Default.GenerateMCQs = chkMCQs.Checked;
            Properties.Settings.Default.Save();
        }

        private void chkFlashcards_CheckedChanged(object sender, EventArgs e)
        {
            if (chkFlashcards.Checked)
            {
                UpdateStatus("▶ Flashcards...Activated");
            }
            else
            {
                UpdateStatus("▶ Flashcards...Deactivated");
            }
            // Save the state of the checkbox
            Properties.Settings.Default.GenerateFlashcards = chkFlashcards.Checked;
            Properties.Settings.Default.Save();
        }

        private void chkVocabulary_CheckedChanged(object sender, EventArgs e)
        {
            if (chkVocabulary.Checked)
            {
                UpdateStatus("▶ Vocabulary...Activated");
            }
            else
            {
                UpdateStatus("▶ Vocabulary...Deactivated");
            }
            // Save the state of the checkbox
            Properties.Settings.Default.GenerateVocabulary = chkVocabulary.Checked;
            Properties.Settings.Default.Save();
        }

        private void chkSummary_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateSummary = chkSummary.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Page Summary…{(chkSummary.Checked ? "▶ Activated" : "▶ Deactivated")}");
        }

        private void chkTakeaways_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateTakeaways = chkTakeaways.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Key Takeaways…{(chkTakeaways.Checked ? "▶ Activated" : "▶ Deactivated")}");
        }

        private void chkCloze_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateCloze = chkCloze.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Cloze Deletions…{(chkCloze.Checked ? "▶ Activated" : "▶ Deactivated")}");
        }

        private void chkTrueFalse_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateTrueFalse = chkTrueFalse.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"True/False Questions…{(chkTrueFalse.Checked ? "▶ Activated" : "▶ Deactivated")}");
        }

        private void chkOutline_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateOutline = chkOutline.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Page Outline…{(chkOutline.Checked ? "▶ Activated" : "▶ Deactivated")}");
        }

        private void chkConceptMap_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateConceptMap = chkConceptMap.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Concept Map…{(chkConceptMap.Checked ? "▶ Activated" : "▶ Deactivated")}");
        }

        private void chkTableExtract_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateTableExtract = chkTableExtract.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Table Extraction…{(chkTableExtract.Checked ? "▶ Activated" : "▶ Deactivated")}");
        }

        private void chkSimplified_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateSimplified = chkSimplified.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Simplified Content…{(chkSimplified.Checked ? "▶ Activated" : "▶ Deactivated")}");
        }

        private void chkCaseStudy_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateCaseStudy = chkCaseStudy.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Case Study…{(chkCaseStudy.Checked ? "▶ Activated" : "▶ Deactivated")}");
        }

        private void chkKeywords_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateKeywords = chkKeywords.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Keywords Extraction…{(chkKeywords.Checked ? "▶ Activated" : "▶ Deactivated")}");
        }

        private void chkTranslatedSections_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateTranslatedSections = chkTranslatedSections.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Translated Sections…{(chkTranslatedSections.Checked ? "▶ Activated" : "▶ Deactivated")}");
        }

        private void chkExplainTerms_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateExplainTerms = chkExplainTerms.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Explain Terms…{(chkExplainTerms.Checked ? "▶ Activated" : "▶ Deactivated")}");
        }

        private void chkArabicExplainTerms_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.ArabicExplainTerms = chkArabicExplainTerms.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Explain Terms in Arabic…{(chkArabicExplainTerms.Checked ? "▶ Activated" : "▶ Deactivated")}");
        }

        private void chkUseSessionFolder_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.UseSessionFolder = chkUseSessionFolder.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Use Session Folder…{(chkUseSessionFolder.Checked ? "▶ Activated" : "▶ Deactivated")}");
        }

        private void chkSaveBesidePdf_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.SaveBesidePdf = chkSaveBesidePdf.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Save Beside PDF…{(chkSaveBesidePdf.Checked ? "▶ Activated" : "▶ Deactivated")}");

        }

        private void chkOrganizeByType_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.OrganizeByType = chkOrganizeByType.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Organize By Type…{(chkOrganizeByType.Checked ? "▶ Activated" : "▶ Deactivated")}");
        }

        private void chkUseCommaDelimiter_CheckedChanged(object sender, EventArgs e)
        {
            if (chkUseCommaDelimiter.Checked)
            {
                UpdateStatus("▶ Using Comma Delimiter for CSV files");
            }
            else
            {
                UpdateStatus("▶ Using Tab Delimiter for TSV files");
            }
            // store the UseCommaDelimiter setting
            Properties.Settings.Default.useCommaDelimiter = chkUseCommaDelimiter.Checked;
            Properties.Settings.Default.Save();
        }

        private void chkMedicalMaterial_CheckedChanged(object sender, EventArgs e)
        {
            if (chkMedicalMaterial.Checked)
            {
                UpdateStatus("▶ Medical Material...Activated");
            }
            else
            {
                UpdateStatus("▶ Medical Material...Deactivated");
            }
            // store the MedicalMaterial setting
            Properties.Settings.Default.MedicalMaterial = chkMedicalMaterial.Checked;
            Properties.Settings.Default.Save();
        }

        private void cmbGeneralLang_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateStatus("▶ General Language...Changed");

            // store the DisplayName of the selected language
            var selectedDisplay = cmbGeneralLang.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(selectedDisplay))
            {
                Properties.Settings.Default.GeneralLanguage = selectedDisplay;
                Properties.Settings.Default.Save();
            }
        }

        private void cmbVocabLang_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateStatus("▶ Vocabulary Language...Changed");
            var selectedDisplay = cmbVocabLang.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(selectedDisplay))
            {
                Properties.Settings.Default.VocabLanguage = selectedDisplay;
                Properties.Settings.Default.Save();
            }
        }

        private readonly (string Code, string DisplayName)[] _supportedLanguages = new[]
        {
            // Favorites (always appear at the top)
            ("en", "English — English"),
            ("ar", "العربية — Arabic"),

            //---------------------------------------------------------------------
            // ChatGPT-supported languages (sorted alphabetically by English name)
            //---------------------------------------------------------------------

            ("sq", "Shqip — Albanian"),
            ("am", "አማርኛ — Amharic"),
            ("hy", "Հայերեն — Armenian"),
            ("bn", "বাংলা — Bengali"),
            ("bs", "bosanski — Bosnian"),
            ("bg", "български — Bulgarian"),
            ("my", "မြန်မာ — Burmese"),
            ("ca", "Català — Catalan"),
            ("zh", "中文 — Chinese"),
            ("hr", "Hrvatski — Croatian"),
            ("cs", "čeština — Czech"),
            ("da", "Dansk — Danish"),
            ("nl", "Nederlands — Dutch"),
            ("et", "eesti — Estonian"),
            ("fi", "suomi — Finnish"),
            ("fr", "Français — French"),
            ("ka", "ქართული — Georgian"),
            ("de", "Deutsch — German"),
            ("el", "Ελληνικά — Greek"),
            ("gu", "ગુજરાતી — Gujarati"),
            ("hi", "हिन्दी — Hindi"),
            ("hu", "Magyar — Hungarian"),
            ("is", "Íslenska — Icelandic"),
            ("id", "Bahasa Indonesia — Indonesian"),
            ("it", "Italiano — Italian"),
            ("ja", "日本語 — Japanese"),
            ("kn", "ಕನ್ನಡ — Kannada"),
            ("kk", "қазақ тілі — Kazakh"),
            ("ko", "한국어 — Korean"),
            ("lv", "latviešu — Latvian"),
            ("lt", "lietuvių — Lithuanian"),
            ("mk", "македонски — Macedonian"),
            ("ms", "Bahasa Melayu — Malay"),
            ("ml", "മലയാളം — Malayalam"),
            ("mr", "मराठी — Marathi"),
            ("mn", "монгол — Mongolian"),
            ("no", "Norsk — Norwegian"),
            ("fa", "فارسی — Persian"),
            ("pl", "Polski — Polish"),
            ("pt", "Português — Portuguese"),
            ("pa", "ਪੰਜਾਬੀ — Punjabi"),
            ("ro", "Română — Romanian"),
            ("ru", "Русский — Russian"),
            ("sr", "српски — Serbian"),
            ("sk", "slovenčina — Slovak"),
            ("sl", "slovenščina — Slovenian"),
            ("so", "Soomaaliga — Somali"),
            ("es", "Español — Spanish"),
            ("sw", "Kiswahili — Swahili"),
            ("sv", "Svenska — Swedish"),
            ("tl", "Wikang Tagalog — Tagalog"),
            ("ta", "தமிழ் — Tamil"),
            ("te", "తెలుగు — Telugu"),
            ("th", "ไทย — Thai"),
            ("tr", "Türkçe — Turkish"),
            ("uk", "Українська — Ukrainian"),
            ("ur", "اردو — Urdu"),
            ("vi", "Tiếng Việt — Vietnamese"),
        };

        private void svgImageBoxAbout_Click(object sender, EventArgs e)
        {
            var aboutForm = new About();
            aboutForm.ShowDialog(this);
        }

        /// Event handler for the Page Batch Size radio group
        /// saves the selected value to settings and updates the status label.
        private void radioPageBatchSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            int chosen = (int)radioPageBatchSize.EditValue;
            Properties.Settings.Default.PageBatchMode = chosen;
            Properties.Settings.Default.Save();
            UpdateStatus($"▶ Page batch mode set to: {chosen} page(s) at a time");
        }



        /// Turns the raw flashcard text into a list of (Front,Back) tuples.
        /// Expects blocks like:
        ///    Front: X
        ///    Back:  Y
        /// separated by blank lines.
        private List<(string Front, string Back)> ParseFlashcards(string raw)
        {
            var cards = new List<(string, string)>();
            // split on *exactly* two newlines (blank‐line separator)
            var entries = raw.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                string front = null, back = null;
                foreach (var line in entry.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var t = line.Trim();
                    if (t.StartsWith("Front:", StringComparison.OrdinalIgnoreCase))
                        front = t.Substring("Front:".Length).Trim();
                    else if (t.StartsWith("Back:", StringComparison.OrdinalIgnoreCase))
                        back = t.Substring("Back:".Length).Trim();
                }
                if (!string.IsNullOrEmpty(front) && !string.IsNullOrEmpty(back))
                    cards.Add((front, back));
            }
            return cards;
        }


        /// Writes out a Front/Back list to a comma‐ or tab‐delimited file.
        private void SaveFlashcardsToDelimitedFile(List<(string Front, string Back)> cards,
                                                   string path,
                                                   bool commaDelimiter)
        {
            using (var w = new StreamWriter(path, false, Encoding.UTF8))
            {
                char sep = commaDelimiter ? ',' : '\t';
                // header (optional)
                w.WriteLine($"Front{sep}Back");
                foreach (var (front, back) in cards)
                {
                    // escape quotes if using CSV
                    if (commaDelimiter)
                    {
                        var f = front.Replace("\"", "\"\"");
                        var b = back.Replace("\"", "\"\"");
                        w.WriteLine($"\"{f}\"{sep}\"{b}\"");
                    }
                    else
                    {
                        // TSV: less chance of needing escapes
                        w.WriteLine($"{front}{sep}{back}");
                    }
                }
            }
        }


        /// Represents one MCQ with 4 choices and a correct answer letter.
        public class McqItem
        {
            public string Question { get; set; }
            public string OptionA { get; set; }
            public string OptionB { get; set; }
            public string OptionC { get; set; }
            public string OptionD { get; set; }
            public string Answer { get; set; }

            /// combine the four options into a single “Options” cell,
            /// with line-breaks between them
            public string OptionsCell =>
                $"A) {OptionA}\nB) {OptionB}\nC) {OptionC}\nD) {OptionD}";
        }



        /// Parse your raw MCQs (blocks separated by blank lines) into a List&lt;MCQ&gt;.
        private List<McqItem> ParseMcqs(string raw)
        {
            var items = new List<McqItem>();
            // split on blank‐line blocks
            var blocks = Regex.Split(raw.Trim(), @"\r?\n\s*\r?\n");

            foreach (var block in blocks)
            {
                var mcq = new McqItem();
                foreach (var line in block.Split('\n'))
                {
                    var t = line.Trim();
                    if (t.StartsWith("Question:", StringComparison.OrdinalIgnoreCase))
                        mcq.Question = t.Substring(9).Trim();
                    else if (t.StartsWith("A)", StringComparison.OrdinalIgnoreCase))
                        mcq.OptionA = t.Substring(2).Trim();
                    else if (t.StartsWith("B)", StringComparison.OrdinalIgnoreCase))
                        mcq.OptionB = t.Substring(2).Trim();
                    else if (t.StartsWith("C)", StringComparison.OrdinalIgnoreCase))
                        mcq.OptionC = t.Substring(2).Trim();
                    else if (t.StartsWith("D)", StringComparison.OrdinalIgnoreCase))
                        mcq.OptionD = t.Substring(2).Trim();
                    else if (t.StartsWith("Answer:", StringComparison.OrdinalIgnoreCase))
                        mcq.Answer = t.Substring(7).Trim();
                }
                // only add if we got at least a question and answer
                if (!string.IsNullOrEmpty(mcq.Question) && !string.IsNullOrEmpty(mcq.Answer))
                    items.Add(mcq);
            }

            return items;
        }


        /// Write a list of MCQs out to CSV or TSV.
        private void SaveMcqsToDelimitedFile(
                List<McqItem> items,
                string path,
                bool useCommaDelimiter
            )
        {
            var delim = useCommaDelimiter ? "," : "\t";

            using (var sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                // header row must match your Anki field names
                sw.WriteLine($"Question{delim}Options{delim}Correct Answer");

                string Escape(string field)
                {
                    // wrap in quotes if it contains delim or newline
                    if (field.Contains(delim) || field.Contains("\n"))
                    {
                        // double up any existing quotes
                        var escaped = field.Replace("\"", "\"\"");
                        return $"\"{escaped}\"";
                    }
                    return field;
                }

                foreach (var mcq in items)
                {
                    var q = Escape(mcq.Question);
                    var opt = Escape(mcq.OptionsCell);
                    var a = Escape(mcq.Answer);
                    sw.WriteLine($"{q}{delim}{opt}{delim}{a}");
                }
            }
        }


        /// Parse raw cloze blocks into (Sentence,Answer) pairs.
        /// Expects blocks like:
        ///   Sentence: "_______________ is a miotic drug."
        ///   Answer: Pilocarpine
        /// separated by blank lines.
        private List<(string Sentence, string Answer)> ParseCloze(string raw)
        {
            var list = new List<(string, string)>();
            var blocks = Regex.Split(raw.Trim(), @"\r?\n\s*\r?\n");
            foreach (var block in blocks)
            {
                string sent = null, ans = null;
                foreach (var line in block.Split('\n'))
                {
                    var t = line.Trim();
                    if (t.StartsWith("Sentence:", StringComparison.OrdinalIgnoreCase))
                        sent = t.Substring("Sentence:".Length).Trim().Trim('"');
                    else if (t.StartsWith("Answer:", StringComparison.OrdinalIgnoreCase))
                        ans = t.Substring("Answer:".Length).Trim();
                }
                if (!string.IsNullOrEmpty(sent) && !string.IsNullOrEmpty(ans))
                    list.Add((sent, ans));
            }
            return list;
        }



        /// Write out cloze pairs to CSV or TSV:
        /// columns: Sentence [with blank], Answer
        private void SaveClozeToDelimitedFile(List<(string Sentence, string Answer)> items,
                                               string path,
                                               bool useCommaDelimiter)
        {
            char sep = useCommaDelimiter ? ',' : '\t';
            using (var w = new StreamWriter(path, false, Encoding.UTF8))
            {
                // optional header
                w.WriteLine(useCommaDelimiter
                    ? "\"Sentence\",\"Answer\""
                    : "Sentence\tAnswer");
                foreach (var (sent, ans) in items)
                {
                    string Escape(string f)
                    {
                        if (f.Contains(sep) || f.Contains("\"") || f.Contains("\n"))
                        {
                            var esc = f.Replace("\"", "\"\"");
                            return $"\"{esc}\"";
                        }
                        return f;
                    }
                    w.WriteLine($"{Escape(sent)}{sep}{Escape(ans)}");
                }
            }
        }

        private void buttonShowApi_Click(object sender, EventArgs e)
        {
            if (textEditAPIKey.Properties.PasswordChar == '*')
            {
                textEditAPIKey.Properties.PasswordChar = '\0';
            }
            else
            {
                textEditAPIKey.Properties.PasswordChar = '*';
            }
        }

        private void buttonLockApiKey_Click(object sender, EventArgs e)
        {
            if (textEditAPIKey.Properties.ReadOnly == false)
            {
                /*Demo*/
                textEditAPIKey.Properties.ReadOnly = true;
            }
            else
            {
                textEditAPIKey.Properties.ReadOnly = false;
            }
        }


        private void btnBrowseOutputFolder_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "اختر مجلد حفظ الملفات المولّدة";
                fbd.SelectedPath = GetOutputFolder();
                if (fbd.ShowDialog() == DialogResult.OK && Directory.Exists(fbd.SelectedPath))
                {
                    SetOutputFolder(fbd.SelectedPath);
                    UpdateStatus($"▶ ✅ Output folder set to: {fbd.SelectedPath}");
                }
            }
        }


        private void btnOpenOutputFolder_Click(object sender, EventArgs e)
        {
            try
            {
                var path = GetEffectiveOutputFolderForUi();
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                // افتح المجلد في Windows Explorer
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = path,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot open folder: " + ex.Message);
            }
        }


        private string GetEffectiveOutputFolderForUi()
        {
            // أولوية 1: آخر مجلد إخراج فعلي (قد يكون مجلد جلسة)
            if (!string.IsNullOrWhiteSpace(_lastOutputRoot) && Directory.Exists(_lastOutputRoot))
                return _lastOutputRoot;

            // أولوية 2: إذا مفعل حفظ بجانب الـ PDF وكان عندنا PDF مختار
            if (Properties.Settings.Default.SaveBesidePdf &&
                !string.IsNullOrWhiteSpace(_lastSelectedPdfPath) &&
                File.Exists(_lastSelectedPdfPath))
            {
                var pdfDir = Path.GetDirectoryName(_lastSelectedPdfPath); // يُرجع مجلد المسار
                if (!string.IsNullOrWhiteSpace(pdfDir) && Directory.Exists(pdfDir))
                    return pdfDir;
            }

            // أولوية 3: المجلد المخصص (الإعداد)
            return GetOutputFolder();
        }
    }
}