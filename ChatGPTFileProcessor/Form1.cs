using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;  // Add this at the top of your file if not present
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Utils.MVVM;
using DevExpress.XtraEditors.Controls;
using Microsoft.Office.Interop.Word;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;
using Word = Microsoft.Office.Interop.Word;





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


        

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBoxEditModel.Properties.Items.Add("gpt-4o"); // Add gpt-4o model to the combo box

            InitializeOverlay();

            // Load API key and model selection
            LoadApiKeyAndModel();

            // Load saved user preferences for checkboxes
            loadCheckBoxesSettings();


            // ▼ Populate the “General Language” dropdown
            cmbGeneralLang.Properties.Items.Clear();
            foreach (var lang in _supportedLanguages)
            {
                cmbGeneralLang.Properties.Items.Add(lang.DisplayName);
            }
            // default to English if not set
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
            var savedVocab = Properties.Settings.Default.VocabLanguage;
            if (!string.IsNullOrWhiteSpace(savedVocab) &&
                _supportedLanguages.Any(x => x.DisplayName == savedVocab))
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
            {
                radioPageBatchSize.EditValue = savedMode;
            }
            else
            {
                radioPageBatchSize.EditValue = 1;
            }


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


        private void buttonBrowseFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedPdfPath = openFileDialog.FileName;

                    using (var pageForm = new PageSelectionForm())
                    {
                        pageForm.LoadPdfPreview(selectedPdfPath);
                        if (pageForm.ShowDialog() == DialogResult.OK)
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

                ShowOverlay("🔄 Processing, please wait...");
                UpdateOverlayLog("S T A R T   G E N E R A T I N G...");
                UpdateOverlayLog("🚀 Starting GPT-4o multimodal processing...");

                // اسم النموذج والـ timestamp لإنشاء مسارات الملفات
                string modelName = comboBoxEditModel.SelectedItem?.ToString() ?? "gpt-4o"; // Use the new combo box for model selection
                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string basePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                // Prepare file‐paths
                // مسارات ملفات التعاريف و MCQs و Flashcards و Vocabulary
                string definitionsFilePath = Path.Combine(basePath, $"Definitions_{modelName}_{timeStamp}.docx");
                string mcqsFilePath = Path.Combine(basePath, $"MCQs_{modelName}_{timeStamp}.docx");
                string flashcardsFilePath = Path.Combine(basePath, $"Flashcards_{modelName}_{timeStamp}.docx");
                string vocabularyFilePath = Path.Combine(basePath, $"Vocabulary_{modelName}_{timeStamp}.docx");

                // New features: Summary, Key Takeaways, Cloze, True/False, Outline, Concept Map, Table Extract, Simplified, Case Study, Keywords
                // New output files:
                string summaryFilePath = Path.Combine(basePath, $"Summary_{modelName}_{timeStamp}.docx");
                string takeawaysFilePath = Path.Combine(basePath, $"Takeaways_{modelName}_{timeStamp}.docx");
                string clozeFilePath = Path.Combine(basePath, $"Cloze_{modelName}_{timeStamp}.docx");
                string tfFilePath = Path.Combine(basePath, $"TrueFalse_{modelName}_{timeStamp}.docx");
                string outlineFilePath = Path.Combine(basePath, $"Outline_{modelName}_{timeStamp}.docx");
                string conceptMapFilePath = Path.Combine(basePath, $"ConceptMap_{modelName}_{timeStamp}.docx");
                string tableFilePath = Path.Combine(basePath, $"Tables_{modelName}_{timeStamp}.docx");
                string simplifiedFilePath = Path.Combine(basePath, $"Simplified_{modelName}_{timeStamp}.docx");
                string caseStudyFilePath = Path.Combine(basePath, $"CaseStudy_{modelName}_{timeStamp}.docx");
                string keywordsFilePath = Path.Combine(basePath, $"Keywords_{modelName}_{timeStamp}.docx");
                string translatedSectionsFilePath = Path.Combine(basePath, $"TranslatedSections_{modelName}_{timeStamp}.docx");



                // 3.1) prompt for Definitions in “GeneralLanguage” (e.g. user picks “French”)
                // 1) Read which “General Language” the user picked:
                string generalLangName = cmbGeneralLang.SelectedItem as string ?? "English";

                // 2) Read the “Medical Material” checkbox:
                bool isMedical = chkMedicalMaterial.Checked;
                // 3) Read the “Vocabulary Language” dropdown:
                string vocabLangName = cmbVocabLang.SelectedItem as string ?? "Arabic";









                //// ─── Build each prompt with a little conditional text:
                //// 3.1) Definitions prompt
                ////Best Version
                //string definitionsPrompt =
                //    $"In {generalLangName}, provide concise DEFINITIONS for each key " +
                //    $"{(isMedical ? "medical " : "")}term found on these page(s). " +
                //    $"For each term, output exactly:\n\n" +
                //    $"- Term: <the term as a heading>\n" +
                //    $"- Definition: <a 1–2-3 sentence definition in {generalLangName}>\n\n" +
                //    $"Separate entries with a blank line.  Do NOT number anything.";

                //// 3.2) MCQs prompt
                ////Best Version
                //string mcqsPrompt =
                //    //$"Generate 5 MULTIPLE‐CHOICE QUESTIONS in {generalLangName} " +
                //    $"Generate MULTIPLE‐CHOICE QUESTIONS in {generalLangName} " +
                //    $"based strictly on the content of these page(s).  Follow this pattern exactly (no deviations):\n\n" +
                //    $"Question: <Write the question here in {generalLangName}>\n" +
                //    $"A) <Option A in {generalLangName}>\n" +
                //    $"B) <Option B in {generalLangName}>\n" +
                //    $"C) <Option C in {generalLangName}>\n" +
                //    $"D) <Option D in {generalLangName}>\n" +
                //    $"Answer: <Exactly one letter: A, B, C, or D>\n\n" +
                //    $"Separate each MCQ block with a single blank line.  Do NOT include any extra text.";

                //// 3.3) Flashcards prompt
                ////Best Version
                //string flashcardsPrompt =
                //    $"Create FLASHCARDS in {generalLangName} for each key " +
                //    $"{(isMedical ? "medical " : "")}term on these page(s).  Use this exact format (no deviations):\n\n" +
                //    //$"Front: <Term in {generalLangName}>\n" +
                //    $"Front: <Term>\n" +
                //    $"Back:  <One- or two- or three- sentence definition in {generalLangName}>\n\n" +
                //    $"Leave exactly one blank line between each card.  Do NOT number or bullet anything.";


                //// 3.4) Vocabulary: translate into whichever “Vocab Language” the user chose
                ////Best Version
                //string vocabularyPrompt =
                //    $"Extract IMPORTANT VOCABULARY TERMS from these page(s) and translate them into {vocabLangName}.  Use exactly this format (no bullets or numbering):\n\n" +
                //    //$"EnglishTerm – {vocabLangName}Translation\n\n" +
                //    $"OriginalTerm – {vocabLangName}Translation\n\n" +
                //    $"Leave exactly one blank line between each entry.  If a term doesn’t have a direct translation, write “– [Translation Needed]”.";

                //// 3.5) Summary prompt
                ////Best Version
                //string summaryPrompt =
                //    //$"In {generalLangName}, write a concise SUMMARY (2–3 sentences) of the content on these page(s). " +
                //    $"In {generalLangName}, write a concise SUMMARY (2–3-4-5-6-7-8-9-10 sentences) of the content on these page(s). " +
                //    $"{(isMedical ? "Highlight key medical concepts; keep technical terms accurate." : "")}" +
                //    $"\n\nFormat your summary as plain prose (no bullets or numbering).";

                //// 3.6) Key Takeaways prompt
                ////Best Version
                //string takeawaysPrompt =
                //    //$"List 5 KEY TAKEAWAYS (in {generalLangName}) from these page(s), formatted as bullets.  " +
                //    $"List KEY TAKEAWAYS (in {generalLangName}) from these page(s), formatted as bullets.  " +
                //    $"Each line must begin with a dash and a space, like:\n" +
                //    $"- Takeaway 1\n" +
                //    $"- Takeaway 2\n" +
                //    $"…\n\n" +
                //    $"{(isMedical ? "Include any critical medical terms and their context." : "")}";

                //// 3.7) Fill-in-the-Blank (Cloze) prompt
                ////Best Version
                //string clozePrompt =
                //    //$"Generate 5 FILL‐IN‐THE‐BLANK sentences (in {generalLangName}) based on these page(s).  " +
                //    $"Generate FILL‐IN‐THE‐BLANK sentences (in {generalLangName}) based on these page(s).  " +
                //    $"Each entry should consist of two lines:\n\n" +
                //    $"Sentence:“_______________ is <brief clue>.”\n" +
                //    $"Answer: <the correct word or phrase> (in {generalLangName}).\n\n" +
                //    //$"For example:\nSentence: “_____[Pilocarpine]_____ is a miotic drug.”\nAnswer: Pilocarpine\n\n" +
                //    $"For example:\nSentence: “_______________ is a miotic drug.”\nAnswer: Pilocarpine\n\n" +
                //    $"Leave a single blank line between each pair.  Do NOT embed the answer inside the blank.";

                //// 3.8) True/False Questions prompt
                ////Best Version
                //string trueFalsePrompt =
                //    //$"Generate 5 TRUE/FALSE statements (in {generalLangName}) based on these page(s).  " +
                //    $"Generate TRUE/FALSE statements (in {generalLangName}) based on these page(s).  " +
                //    $"Each block should be two lines:\n\n" +
                //    $"Statement: <write a true‐or‐false sentence here>\n" +
                //    $"Answer: <True or False>\n\n" +
                //    $"Leave exactly one blank line between each pair.  Do NOT write any additional explanation.";

                //// 3.9) Outline prompt
                ////Best Version
                //string outlinePrompt =
                //    $"Produce a hierarchical OUTLINE in {generalLangName} for the material on these page(s).  " +
                //    $"Use numbered levels (e.g., “1. Main Heading,” “1.1 Subheading,” “1.1.1 Detail”).  " +
                //    $"Do NOT use bullet points—strictly use decimal numbering.  " +
                //    $"{(isMedical ? "Include medical subheadings where appropriate." : "")}";

                //// 3.10) Concept Map prompt
                ////Best Version
                //string conceptMapPrompt =
                //    $"List the key CONCEPTS from these page(s) and show how they relate, in {generalLangName}.  " +
                //    $"For each pair, use one of these formats exactly:\n" +
                //    $"“ConceptA → relates to → ConceptB”\n" +
                //    $"or\n" +
                //    $"“ConceptA — contrasts with — ConceptC”\n\n" +
                //    $"Separate each relationship on its own line.  Provide at least 5 relationships.";

                //// 3.11) Table Extraction prompt
                ////Not Ready / Not Best Version
                //string tableExtractPrompt =
                //    $"If these page(s) contain any tables (e.g., drug doses, side effects, lab values), " +
                //    $"extract each table into a Markdown‐style table in {generalLangName}.  Use this exact format:\n\n" +
                //    $"| Column1 | Column2 | Column3 |\n" +
                //    $"|---------|---------|---------|\n" +
                //    $"| data11  | data12  | data13  |\n" +
                //    $"| data21  | data22  | data23  |\n\n" +
                //    $"If no table is present, respond with exactly: “No table found.”";

                //// 3.12) Simplified Explanation prompt
                ////Best Version
                //string simplifiedPrompt =
                //    $"Explain the content of these page(s) in simpler language, as if teaching a first-year medical student.  " +
                //    $"Use {generalLangName}.  Define any technical or medical jargon in parentheses the first time it appears.  " +
                //    $"Write one cohesive paragraph—no bullets or lists.";

                //// 3.13) Case Study prompt
                ////Best Version
                //string caseStudyPrompt =
                //    $"Write a short CLINICAL VIGNETTE (1 paragraph) based on these page(s), in {generalLangName}.  " +
                //    $"Include:\n" +
                //    $"- Patient age and gender\n" +
                //    $"- Presenting complaint or symptom\n" +
                //    $"- Key pertinent findings (e.g., vital signs, lab results)\n\n" +
                //    $"Then immediately follow with a single multiple-choice question (in {generalLangName}) about the most likely diagnosis or next step.  " +
                //    $"Format exactly:\n" +
                //    $"\nMCQ: <The question text>\n" +
                //    $"A) <Option A>\n" +
                //    $"B) <Option B>\n" +
                //    $"C) <Option C>\n" +
                //    $"D) <Option D>\n" +
                //    $"Answer: <A, B, C, or D>\n\n" +
                //    //$"No extra commentary—only the vignette paragraph, blank line, then the MCQ block.";
                //    $"No extra commentary—only the vignette paragraph, blank line, then the MCQ block, at least two cases.";

                //// 3.14) Keywords prompt
                ////Best Version
                //string keywordsPrompt =
                //    $"List the HIGH-YIELD KEYWORDS from these page(s) in {generalLangName}.  " +
                //    $"Output as a comma-separated list (e.g., “keyword1, keyword2, keyword3”).  " +
                //    $"Do NOT include definitions—only the keywords themselves.  " +
                //    $"Provide at least 8–10 keywords.";

                // 3.1) Definitions prompt
                // Best Version (preserved for reference)
                //string definitionsPrompt =
                //    $"In {generalLangName}, provide concise DEFINITIONS for each key " +
                //    $"{(isMedical ? "medical " : "")}term found on these page(s). " +
                //    $"For each term, output exactly:\n\n" +
                //    $"- Term: <the term as a heading>\n" +
                //    $"- Definition: <a 1–2-3 sentence definition in {generalLangName}>\n\n" +
                //    $"Separate entries with a blank line.  Do NOT number anything.";

                string definitionsPrompt;
                if (isMedical)
                {
                    // When medical mode is on, enforce clinically accurate language,
                    // include brief usage/context if relevant, and keep medical terms correct.
                    definitionsPrompt =
                        $"In {generalLangName}, provide concise MEDICAL DEFINITIONS for each key medical term found on these page(s). " +
                        $"For each term, output exactly (no numbering):\n\n" +
                        $"- Term: <the term as a heading>\n" +
                        $"- Definition: <a 1–2 sentence clinical definition in {generalLangName}, including brief context or indication if applicable>\n\n" +
                        $"Use precise medical terminology and separate each entry with a blank line.";
                }
                else
                {
                    definitionsPrompt =
                        $"In {generalLangName}, provide concise DEFINITIONS for each key term found on these page(s). " +
                        $"For each term, output exactly (no numbering):\n\n" +
                        $"- Term: <the term as a heading>\n" +
                        $"- Definition: <a 1–2 sentence definition in {generalLangName}>\n\n" +
                        $"Separate entries with a blank line.";
                }


                // 3.2) MCQs prompt
                // Best Version (preserved for reference)
                //string mcqsPrompt =
                //    //$"Generate 5 MULTIPLE‐CHOICE QUESTIONS in {generalLangName} " +
                //    $"Generate MULTIPLE‐CHOICE QUESTIONS in {generalLangName} " +
                //    $"based strictly on the content of these page(s).  Follow this pattern exactly (no deviations):\n\n" +
                //    $"Question: <Write the question here in {generalLangName}>\n" +
                //    $"A) <Option A in {generalLangName}>\n" +
                //    $"B) <Option B in {generalLangName}>\n" +
                //    $"C) <Option C in {generalLangName}>\n" +
                //    $"D) <Option D in {generalLangName}>\n" +
                //    $"Answer: <Exactly one letter: A, B, C, or D>\n\n" +
                //    $"Separate each MCQ block with a single blank line.  Do NOT include any extra text.";

                string mcqsPrompt;
                if (isMedical)
                {
                    mcqsPrompt =
                        $"Generate MULTIPLE-CHOICE QUESTIONS (in {generalLangName}) focused on the MEDICAL content of these page(s).  " +
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
                    mcqsPrompt =
                        $"Generate MULTIPLE-CHOICE QUESTIONS (in {generalLangName}) based strictly on the content of these page(s).  " +
                        $"Write exactly (no deviations):\n\n" +
                        $"Question: <Write the question here in {generalLangName}>\n" +
                        $"A) <Option A in {generalLangName}>\n" +
                        $"B) <Option B in {generalLangName}>\n" +
                        $"C) <Option C in {generalLangName}>\n" +
                        $"D) <Option D in {generalLangName}>\n" +
                        $"Answer: <Exactly one letter: A, B, C, or D>\n\n" +
                        $"Separate each MCQ block with a blank line.  Do NOT include any extra text.";
                }


                // 3.3) Flashcards prompt
                // Best Version (preserved for reference)
                //string flashcardsPrompt =
                //    $"Create FLASHCARDS in {generalLangName} for each key " +
                //    $"{(isMedical ? "medical " : "")}term on these page(s).  Use this exact format (no deviations):\n\n" +
                //    //$"Front: <Term in {generalLangName}>\n" +
                //    $"Front: <Term>\n" +
                //    $"Back:  <One- or two- or three- sentence definition in {generalLangName}>\n\n" +
                //    $"Leave exactly one blank line between each card.  Do NOT number or bullet anything.";

                string flashcardsPrompt;
                if (isMedical)
                {
                    flashcardsPrompt =
                        $"Create MEDICAL FLASHCARDS in {generalLangName} for each key medical or pharmaceutical term on these page(s).  " +
                        $"Use this exact format (no deviations):\n\n" +
                        $"Front: <Term>\n" +
                        $"Back:  <A 1–2 sentence clinical definition/use in {generalLangName}, including indication if relevant>\n\n" +
                        $"Separate each card with a blank line; do NOT number or bullet anything.";
                }
                else
                {
                    flashcardsPrompt =
                        $"Create FLASHCARDS in {generalLangName} for each key term on these page(s).  " +
                        $"Use this exact format (no deviations):\n\n" +
                        $"Front: <Term>\n" +
                        $"Back:  <One- or two-sentence definition in {generalLangName}>\n\n" +
                        $"Separate each card with a blank line; do NOT number or bullet anything.";
                }


                // 3.4) Vocabulary prompt
                // Best Version (preserved for reference)
                //string vocabularyPrompt =
                //    $"Extract IMPORTANT VOCABULARY TERMS from these page(s) and translate them into {vocabLangName}.  Use exactly this format (no bullets or numbering):\n\n" +
                //    //$"EnglishTerm – {vocabLangName}Translation\n\n" +
                //    $"OriginalTerm – {vocabLangName}Translation\n\n" +
                //    $"Leave exactly one blank line between each entry.  If a term doesn’t have a direct translation, write “– [Translation Needed]”.";

                string vocabularyPrompt =
                    $"Extract IMPORTANT VOCABULARY TERMS from these page(s) and translate them into {vocabLangName}.  " +
                    $"Use exactly this format (no bullets or numbering):\n\n" +
                    $"OriginalTerm – {vocabLangName}Translation\n\n" +
                    $"Leave exactly one blank line between each entry.  If a term doesn’t have a direct translation, write “– [Translation Needed]”.";


                // 3.5) Summary prompt
                // Best Version (preserved for reference)
                //string summaryPrompt =
                //    //$"In {generalLangName}, write a concise SUMMARY (2–3 sentences) of the content on these page(s). " +
                //    $"In {generalLangName}, write a concise SUMMARY (2–3-4-5-6-7-8-9-10 sentences) of the content on these page(s). " +
                //    $"{(isMedical ? "Highlight key medical concepts; keep technical terms accurate." : "")}" +
                //    $"\n\nFormat your summary as plain prose (no bullets or numbering).";

                string summaryPrompt;
                if (isMedical)
                {
                    summaryPrompt =
                        $"In {generalLangName}, write a concise MEDICAL SUMMARY (3–5 sentences) of the content on these page(s).  " +
                        $"Highlight key medical concepts and maintain technical accuracy (e.g., pathophysiology, indications, contraindications).  " +
                        $"Format as plain prose (no bullets or numbering).";
                }
                else
                {
                    summaryPrompt =
                        $"In {generalLangName}, write a concise SUMMARY (3–5 sentences) of the content on these page(s).  " +
                        $"Format as plain prose (no bullets or numbering).";
                }


                // 3.6) Key Takeaways prompt
                // Best Version (preserved for reference)
                //string takeawaysPrompt =
                //    //$"List 5 KEY TAKEAWAYS (in {generalLangName}) from these page(s), formatted as bullets.  " +
                //    $"List KEY TAKEAWAYS (in {generalLangName}) from these page(s), formatted as bullets.  " +
                //    $"Each line must begin with a dash and a space, like:\n" +
                //    $"- Takeaway 1\n" +
                //    $"- Takeaway 2\n" +
                //    $"…\n\n" +
                //    $"{(isMedical ? "Include any critical medical terms and their context." : "")}";

                string takeawaysPrompt;
                if (isMedical)
                {
                    takeawaysPrompt =
                        $"List KEY TAKEAWAYS (in {generalLangName}) from these page(s), formatted as bullets.  " +
                        $"Each line must begin with a dash and a space, for example:\n" +
                        $"- Takeaway 1\n" +
                        $"- Takeaway 2\n\n" +
                        $"Include critical medical terms, their context, and implications for patient care.";
                }
                else
                {
                    takeawaysPrompt =
                        $"List KEY TAKEAWAYS (in {generalLangName}) from these page(s), formatted as bullets.  " +
                        $"Each line must begin with a dash and a space, for example:\n" +
                        $"- Takeaway 1\n" +
                        $"- Takeaway 2\n\n";
                }


                // 3.7) Fill-in-the-Blank (Cloze) prompt
                // Best Version (preserved for reference)
                //string clozePrompt =
                //    //$"Generate 5 FILL‐IN‐THE‐BLANK sentences (in {generalLangName}) based on these page(s).  " +
                //    $"Generate FILL‐IN‐THE‐BLANK sentences (in {generalLangName}) based on these page(s).  " +
                //    $"Each entry should consist of two lines:\n\n" +
                //    $"Sentence:“_______________ is <brief clue>.”\n" +
                //    $"Answer: <the correct word or phrase> (in {generalLangName}).\n\n" +
                //    //$"For example:\nSentence: “_____[Pilocarpine]_____ is a miotic drug.”\nAnswer: Pilocarpine\n\n" +
                //    $"For example:\nSentence: “_______________ is a miotic drug.”\nAnswer: Pilocarpine\n\n" +
                //    $"Leave a single blank line between each pair.  Do NOT embed the answer inside the blank.";

                string clozePrompt;
                if (isMedical)
                {
                    clozePrompt =
                        $"Generate FILL-IN-THE-BLANK sentences (in {generalLangName}) based on these page(s), focusing on medical terminology.  " +
                        $"Each entry should consist of two lines:\n\n" +
                        $"Sentence: \"_______________ is <brief medical clue>.\"\n" +
                        $"Answer: <the correct medical term or phrase> (in {generalLangName}).\n\n" +
                        $"For example:\n" +
                        $"Sentence: \"_______________ is a cholinergic agonist used to treat glaucoma.\"\n" +
                        $"Answer: Pilocarpine\n\n" +
                        $"Leave exactly one blank line between each pair; do NOT show the answer inside the blank.";
                }
                else
                {
                    clozePrompt =
                        $"Generate FILL-IN-THE-BLANK sentences (in {generalLangName}) based on these page(s).  " +
                        $"Each entry should consist of two lines:\n\n" +
                        $"Sentence: \"_______________ is <brief clue>.\"\n" +
                        $"Answer: <the correct word or phrase> (in {generalLangName}).\n\n" +
                        $"For example:\n" +
                        $"Sentence: \"_______________ is the capital of France.\"\n" +
                        $"Answer: Paris\n\n" +
                        $"Leave exactly one blank line between each pair; do NOT show the answer inside the blank.";
                }


                // 3.8) True/False Questions prompt
                // Best Version (preserved for reference)
                //string trueFalsePrompt =
                //    //$"Generate 5 TRUE/FALSE statements (in {generalLangName}) based on these page(s).  " +
                //    $"Generate TRUE/FALSE statements (in {generalLangName}) based on these page(s).  " +
                //    $"Each block should be two lines:\n\n" +
                //    $"Statement: <write a true‐or‐false sentence here>\n" +
                //    $"Answer: <True or False>\n\n" +
                //    $"Leave exactly one blank line between each pair.  Do NOT write any additional explanation.";

                string trueFalsePrompt;
                if (isMedical)
                {
                    trueFalsePrompt =
                        $"Generate TRUE/FALSE statements (in {generalLangName}) focused on the medical content of these page(s).  " +
                        $"Each block should be two lines:\n\n" +
                        $"Statement: <a clinically accurate true-or-false sentence>\n" +
                        $"Answer: <True or False>\n\n" +
                        $"Leave exactly one blank line between each pair; do NOT provide explanations.";
                }
                else
                {
                    trueFalsePrompt =
                        $"Generate TRUE/FALSE statements (in {generalLangName}) based on these page(s).  " +
                        $"Each block should be two lines:\n\n" +
                        $"Statement: <write a true-or-false sentence>\n" +
                        $"Answer: <True or False>\n\n" +
                        $"Leave exactly one blank line between each pair; do NOT provide explanations.";
                }


                // 3.9) Outline prompt
                // Best Version (preserved for reference)
                //string outlinePrompt =
                //    $"Produce a hierarchical OUTLINE in {generalLangName} for the material on these page(s).  " +
                //    $"Use numbered levels (e.g., “1. Main Heading,” “1.1 Subheading,” “1.1.1 Detail”).  " +
                //    $"Do NOT use bullet points—strictly use decimal numbering.  " +
                //    $"{(isMedical ? "Include medical subheadings where appropriate." : "")}";

                string outlinePrompt;
                if (isMedical)
                {
                    outlinePrompt =
                        $"Produce a hierarchical MEDICAL OUTLINE in {generalLangName} for the material on these page(s).  " +
                        $"Use decimal numbering (e.g., “1. Topic,” “1.1 Subtopic,” “1.1.1 Detail”).  " +
                        $"Include specific medical subheadings (e.g., pathophysiology, clinical presentation, management) where appropriate.";
                }
                else
                {
                    outlinePrompt =
                        $"Produce a hierarchical OUTLINE in {generalLangName} for the material on these page(s).  " +
                        $"Use decimal numbering (e.g., “1. Topic,” “1.1 Subtopic,” “1.1.1 Detail”).  " +
                        $"Do NOT use bullet points—strictly use decimal numbering.";
                }


                // 3.10) Concept Map prompt
                // Best Version (preserved for reference)
                //string conceptMapPrompt =
                //    $"List the key CONCEPTS from these page(s) and show how they relate, in {generalLangName}.  " +
                //    $"For each pair, use one of these formats exactly:\n" +
                //    $"“ConceptA → relates to → ConceptB”\n" +
                //    $"or\n" +
                //    $"“ConceptA — contrasts with — ConceptC”\n\n" +
                //    $"Separate each relationship on its own line.  Provide at least 5 relationships.";

                string conceptMapPrompt;
                if (isMedical)
                {
                    conceptMapPrompt =
                        $"List the key MEDICAL CONCEPTS from these page(s) and show how they relate, in {generalLangName}.  " +
                        $"For each pair, use exactly one of these formats:\n" +
                        $"“ConceptA → relates to → ConceptB”\n" +
                        $"or\n" +
                        $"“ConceptA — contrasts with — ConceptC”\n\n" +
                        $"Focus on clinical or pathophysiological relationships.  Provide at least 5 relationships.";
                }
                else
                {
                    conceptMapPrompt =
                        $"List the key CONCEPTS from these page(s) and show how they relate, in {generalLangName}.  " +
                        $"For each pair, use exactly one of these formats:\n" +
                        $"“ConceptA → relates to → ConceptB”\n" +
                        $"or\n" +
                        $"“ConceptA — contrasts with — ConceptC”\n\n" +
                        $"Separate each relationship on its own line.  Provide at least 5 relationships.";
                }


                // 3.11) Table Extraction prompt
                // Not Ready / Not Best Version (preserved for reference)
                //string tableExtractPrompt =
                //    $"If these page(s) contain any tables (e.g., drug doses, side effects, lab values), " +
                //    $"extract each table into a Markdown‐style table in {generalLangName}.  Use this exact format:\n\n" +
                //    $"| Column1 | Column2 | Column3 |\n" +
                //    $"|---------|---------|---------|\n" +
                //    $"| data11  | data12  | data13  |\n" +
                //    $"| data21  | data22  | data23  |\n\n" +
                //    $"If no table is present, respond with exactly: “No table found.”";

                string tableExtractPrompt;
                if (isMedical)
                {
                    tableExtractPrompt =
                        $"If these page(s) contain any MEDICAL TABLES (e.g., drug doses, indications, side effects, lab values), " +
                        $"extract each table into markdown format in {generalLangName}.  Follow this exact format:\n\n" +
                        $"| Column1         | Column2                   | Column3            |\n" +
                        $"|-----------------|---------------------------|--------------------|\n" +
                        $"| data11 (e.g., drug) | data12 (e.g., dose)   | data13 (e.g., side effect) |\n" +
                        $"| data21         | data22                     | data23             |\n\n" +
                        $"If no table is present, respond exactly: “No table found.”";
                }
                else
                {
                    tableExtractPrompt =
                        $"If these page(s) contain any tables (e.g., schedules, comparisons, statistics), " +
                        $"extract each table into markdown format in {generalLangName}.  Follow this exact format:\n\n" +
                        $"| Column1 | Column2 | Column3 |\n" +
                        $"|---------|---------|---------|\n" +
                        $"| data11  | data12  | data13  |\n" +
                        $"| data21  | data22  | data23  |\n\n" +
                        $"If no table is present, respond exactly: “No table found.”";
                }


                // 3.12) Simplified Explanation prompt
                // Best Version (preserved for reference)
                //string simplifiedPrompt =
                //    $"Explain the content of these page(s) in simpler language, as if teaching a first-year medical student.  " +
                //    $"Use {generalLangName}.  Define any technical or medical jargon in parentheses the first time it appears.  " +
                //    $"Write one cohesive paragraph—no bullets or lists.";

                string simplifiedPrompt;
                if (isMedical)
                {
                    simplifiedPrompt =
                        $"Explain the content of these page(s) in simpler language (in {generalLangName}), as if teaching a first-year medical student.  " +
                        $"Define any technical/medical jargon in parentheses upon first use.  " +
                        $"Write one cohesive paragraph—no bullets or lists.";
                }
                else
                {
                    simplifiedPrompt =
                        $"Explain the content of these page(s) in simpler language (in {generalLangName}).  " +
                        $"Write one cohesive paragraph—no bullets or lists.";
                }


                // 3.13) Case Study prompt
                // Best Version (preserved for reference)
                //string caseStudyPrompt =
                //    $"Write a short CLINICAL VIGNETTE (1 paragraph) based on these page(s), in {generalLangName}.  " +
                //    $"Include:\n" +
                //    $"- Patient age and gender\n" +
                //    $"- Presenting complaint or symptom\n" +
                //    $"- Key pertinent findings (e.g., vital signs, lab results)\n\n" +
                //    $"Then immediately follow with a single multiple-choice question (in {generalLangName}) about the most likely diagnosis or next step.  " +
                //    $"Format exactly:\n" +
                //    $"\nMCQ: <The question text>\n" +
                //    $"A) <Option A>\n" +
                //    $"B) <Option B>\n" +
                //    $"C) <Option C>\n" +
                //    $"D) <Option D>\n" +
                //    $"Answer: <A, B, C, or D>\n\n" +
                //    //$"No extra commentary—only the vignette paragraph, blank line, then the MCQ block.";
                //    $"No extra commentary—only the vignette paragraph, blank line, then the MCQ block, at least two cases.";

                string caseStudyPrompt;
                if (isMedical)
                {
                    caseStudyPrompt =
                        $"Write a short CLINICAL VIGNETTE (1 paragraph) based on these page(s), in {generalLangName}.  " +
                        $"Include:\n" +
                        $"- Patient age and gender\n" +
                        $"- Presenting complaint or symptom\n" +
                        $"- Key pertinent findings (e.g., vital signs, lab results)\n\n" +
                        $"Then immediately follow with a MULTIPLE-CHOICE QUESTION (in {generalLangName}) about the most likely diagnosis or next step.  " +
                        $"Format exactly:\n\n" +
                        $"MCQ: <The question text>\n" +
                        $"A) <Option A>\n" +
                        $"B) <Option B>\n" +
                        $"C) <Option C>\n" +
                        $"D) <Option D>\n" +
                        $"Answer: <A, B, C, or D>\n\n" +
                        $"No extra commentary—only the vignette paragraph, blank line, then the MCQ block.";
                }
                else
                {
                    caseStudyPrompt =
                        $"Write a short CASE SCENARIO (1 paragraph) based on these page(s), in {generalLangName}.  " +
                        $"Then follow with a MULTIPLE-CHOICE QUESTION (in {generalLangName}) about a key concept.  " +
                        $"Format exactly:\n\n" +
                        $"MCQ: <The question text>\n" +
                        $"A) <Option A>\n" +
                        $"B) <Option B>\n" +
                        $"C) <Option C>\n" +
                        $"D) <Option D>\n" +
                        $"Answer: <A, B, C, or D>\n\n" +
                        $"No extra commentary—only the scenario paragraph, blank line, then the MCQ block.";
                }


                // 3.14) Keywords prompt
                // Best Version (preserved for reference)
                //string keywordsPrompt =
                //    $"List the HIGH-YIELD KEYWORDS from these page(s) in {generalLangName}.  " +
                //    $"Output as a comma-separated list (e.g., “keyword1, keyword2, keyword3”).  " +
                //    $"Do NOT include definitions—only the keywords themselves.  " +
                //    $"Provide at least 8–10 keywords.";

                string keywordsPrompt;
                if (isMedical)
                {
                    keywordsPrompt =
                        $"List the HIGH-YIELD MEDICAL KEYWORDS from these page(s) in {generalLangName}.  " +
                        $"Output as a comma-separated list (e.g., “keyword1, keyword2, keyword3”).  " +
                        $"Do NOT include definitions—only the keywords themselves.  " +
                        $"Provide at least 8–10 medical terms.";
                }
                else
                {
                    keywordsPrompt =
                        $"List the HIGH-YIELD KEYWORDS from these page(s) in {generalLangName}.  " +
                        $"Output as a comma-separated list (e.g., “keyword1, keyword2, keyword3”).  " +
                        $"Do NOT include definitions—only the keywords themselves.  " +
                        $"Provide at least 8–10 keywords.";
                }

                //string translatedSectionsPrompt =
                //    $"Translate the following text into {vocabLangName}. " +
                //    $"Keep every sentence or paragraph exactly as it is in the original language. " +
                //    $"After each sentence or paragraph, provide the translation immediately below it. " +
                //    $"Do not remove or shorten any part of the original text. " +
                //    $"Use clear labels in the output: start the original with 'Original:' and the translation with 'Translation:'. " +
                //    $"Maintain the original meaning and style (e.g., scientific or medical if applicable). " +
                //    $"Continue this format until the end of the text.";

                string translatedSectionsPrompt =
                    $"Translate the following text from {generalLangName} into {vocabLangName}. " +
                    $"Keep every sentence or paragraph exactly as it is in the original language. " +
                    $"After each sentence or paragraph, provide the translation immediately below it. " +
                    $"Do not remove or shorten any part of the original text. " +
                    //$"Use clear labels in the output: start the original with 'Original:' and the translation with 'Translation:'. " +
                    $"Do not add any introductions, explanations, notes, or extra formatting. " +
                    $"Only output the text in the requested format.";

                //bool useSeparators = true; // لو ما تريده، غيّرها إلى false

                //string translatedSectionsPrompt =
                //    $"Translate the following text from {generalLangName} into {vocabLangName}. " +
                //    $"Keep every sentence or paragraph exactly as it is in the original language. " +
                //    $"After each sentence or paragraph, provide the translation immediately below it. " +
                //    $"Do not remove or shorten any part of the original text. " +
                //    //$"Use clear labels in the output: start the original with 'Original:' and the translation with 'Translation:'. " +
                //    (useSeparators
                //        ? $"After each translation, add a line with exactly this text on a separate line: '---------------------------------------'."
                //        : $"Do not add any extra lines, explanations, notes, or separators.") +
                //    $" Only output the text in the requested format.";







                // 4) استخراج صور كل الصفحات المحددة في الواجهة
                var allPages = ConvertPdfToImages(filePath);

                // 5) إنشاء StringBuilder لكل قسم من الأقسام الأربع
                // 5) Prepare StringBuilders for whichever sections are checked
                StringBuilder allDefinitions = chkDefinitions.Checked ? new StringBuilder() : null;
                StringBuilder allMCQs = chkMCQs.Checked ? new StringBuilder() : null;
                StringBuilder allFlashcards = chkFlashcards.Checked ? new StringBuilder() : null;
                StringBuilder allVocabulary = chkVocabulary.Checked ? new StringBuilder() : null;

                //New feateure
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

                // Check if at least one section is selected
                //if (allDefinitions == null && allMCQs == null && allFlashcards == null && allVocabulary == null)
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
                     && allTranslatedSections == null)
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
                                UpdateOverlayLog($"🖼️ Sending page {pageNumber} to GPT (Definitions)...");
                                string pageDef = await ProcessPdfPageMultimodal(image, apiKey, definitionsPrompt);
                                allDefinitions.AppendLine($"===== Page {pageNumber} =====");
                                allDefinitions.AppendLine(pageDef);
                                allDefinitions.AppendLine();
                            }

                            if (chkMCQs.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending page {pageNumber} to GPT (MCQs)...");
                                string pageMCQs = await ProcessPdfPageMultimodal(image, apiKey, mcqsPrompt);
                                allMCQs.AppendLine($"===== Page {pageNumber} =====");
                                allMCQs.AppendLine(pageMCQs);
                                allMCQs.AppendLine();
                            }

                            if (chkFlashcards.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending page {pageNumber} to GPT (Flashcards)...");
                                string pageFlash = await ProcessPdfPageMultimodal(image, apiKey, flashcardsPrompt);
                                allFlashcards.AppendLine($"===== Page {pageNumber} =====");
                                allFlashcards.AppendLine(pageFlash);
                                allFlashcards.AppendLine();
                            }

                            if (chkVocabulary.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending page {pageNumber} to GPT (Vocabulary)...");
                                string pageVocab = await ProcessPdfPageMultimodal(image, apiKey, vocabularyPrompt);
                                allVocabulary.AppendLine($"===== Page {pageNumber} =====");
                                allVocabulary.AppendLine(pageVocab);
                                allVocabulary.AppendLine();
                            }

                            // ── NEW: Summary
                            if (chkSummary.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending page {pageNumber} → Summary…");
                                string pageSum = await ProcessPdfPageMultimodal(image, apiKey, summaryPrompt);
                                allSummary.AppendLine($"===== Page {pageNumber} =====");
                                allSummary.AppendLine(pageSum);
                                allSummary.AppendLine();
                            }

                            // ── NEW: Key Takeaways
                            if (chkTakeaways.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending page {pageNumber} → Key Takeaways…");
                                string pageTA = await ProcessPdfPageMultimodal(image, apiKey, takeawaysPrompt);
                                allTakeaways.AppendLine($"===== Page {pageNumber} =====");
                                allTakeaways.AppendLine(pageTA);
                                allTakeaways.AppendLine();
                            }

                            // ── NEW: Cloze
                            if (chkCloze.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending page {pageNumber} → Cloze…");
                                string pageCL = await ProcessPdfPageMultimodal(image, apiKey, clozePrompt);
                                allCloze.AppendLine($"===== Page {pageNumber} =====");
                                allCloze.AppendLine(pageCL);
                                allCloze.AppendLine();
                            }

                            // ── NEW: True/False
                            if (chkTrueFalse.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending page {pageNumber} → True/False…");
                                string pageTF = await ProcessPdfPageMultimodal(image, apiKey, trueFalsePrompt);
                                allTrueFalse.AppendLine($"===== Page {pageNumber} =====");
                                allTrueFalse.AppendLine(pageTF);
                                allTrueFalse.AppendLine();
                            }

                            // ── NEW: Outline
                            if (chkOutline.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending page {pageNumber} → Outline…");
                                string pageOL = await ProcessPdfPageMultimodal(image, apiKey, outlinePrompt);
                                allOutline.AppendLine($"===== Page {pageNumber} =====");
                                allOutline.AppendLine(pageOL);
                                allOutline.AppendLine();
                            }

                            // ── NEW: Concept Map
                            if (chkConceptMap.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending page {pageNumber} → Concept Map…");
                                string pageCM = await ProcessPdfPageMultimodal(image, apiKey, conceptMapPrompt);
                                allConceptMap.AppendLine($"===== Page {pageNumber} =====");
                                allConceptMap.AppendLine(pageCM);
                                allConceptMap.AppendLine();
                            }

                            // ── NEW: Table Extraction
                            if (chkTableExtract.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending page {pageNumber} → Table Extract…");
                                string pageTE = await ProcessPdfPageMultimodal(image, apiKey, tableExtractPrompt);
                                allTableExtract.AppendLine($"===== Page {pageNumber} =====");
                                allTableExtract.AppendLine(pageTE);
                                allTableExtract.AppendLine();
                            }

                            // ── NEW: Simplified Explanation
                            if (chkSimplified.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending page {pageNumber} → Simplified Explanation…");
                                string pageSI = await ProcessPdfPageMultimodal(image, apiKey, simplifiedPrompt);
                                allSimplified.AppendLine($"===== Page {pageNumber} =====");
                                allSimplified.AppendLine(pageSI);
                                allSimplified.AppendLine();
                            }

                            // ── NEW: Case Study Scenario
                            if (chkCaseStudy.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending page {pageNumber} → Case Study…");
                                string pageCS = await ProcessPdfPageMultimodal(image, apiKey, caseStudyPrompt);
                                allCaseStudy.AppendLine($"===== Page {pageNumber} =====");
                                allCaseStudy.AppendLine(pageCS);
                                allCaseStudy.AppendLine();
                            }

                            // ── NEW: High-Yield Keywords
                            if (chkKeywords.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending page {pageNumber} → Keywords…");
                                string pageKW = await ProcessPdfPageMultimodal(image, apiKey, keywordsPrompt);
                                allKeywords.AppendLine($"===== Page {pageNumber} =====");
                                allKeywords.AppendLine(pageKW);
                                allKeywords.AppendLine();
                            }

                            // ── NEW: Translated Sections
                            if (chkTranslatedSections.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending page {pageNumber} → Translated Sections…");
                                string pageTS = await ProcessPdfPageMultimodal(image, apiKey, translatedSectionsPrompt);
                                allTranslatedSections.AppendLine($"===== Page {pageNumber} =====");
                                allTranslatedSections.AppendLine(pageTS);
                                allTranslatedSections.AppendLine();
                            }

                            UpdateOverlayLog($"✅ Page {pageNumber} done.");
                        }
                        break;


                    case 2:
                        // ——— Mode: 2 pages at a time ———
                        for (int i = 0; i < allPages.Count; i += 2)
                        {
                            // build a small group of up to 2 pages
                            var pageGroup = new List<(int pageNumber, Image image)>();
                            for (int j = i; j < i + 2 && j < allPages.Count; j++)
                                pageGroup.Add(allPages[j]);

                            int startPage = pageGroup.First().pageNumber;
                            int endPage = pageGroup.Last().pageNumber;
                            string header = (startPage == endPage)
                                ? $"===== Page {startPage} ====="
                                : $"===== Pages {startPage}–{endPage} =====";

                            if (chkDefinitions.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} to GPT (Definitions) …");
                                string pagesDef = await ProcessPdfPagesMultimodal(pageGroup, apiKey, definitionsPrompt);
                                allDefinitions.AppendLine(header);
                                allDefinitions.AppendLine(pagesDef);
                                allDefinitions.AppendLine();
                            }

                            if (chkMCQs.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} to GPT (MCQs) …");
                                string pagesMCQs = await ProcessPdfPagesMultimodal(pageGroup, apiKey, mcqsPrompt);
                                allMCQs.AppendLine(header);
                                allMCQs.AppendLine(pagesMCQs);
                                allMCQs.AppendLine();
                            }

                            if (chkFlashcards.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} to GPT (Flashcards) …");
                                string pagesFlash = await ProcessPdfPagesMultimodal(pageGroup, apiKey, flashcardsPrompt);
                                allFlashcards.AppendLine(header);
                                allFlashcards.AppendLine(pagesFlash);
                                allFlashcards.AppendLine();
                            }

                            if (chkVocabulary.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} to GPT (Vocabulary) …");
                                string pagesVocab = await ProcessPdfPagesMultimodal(pageGroup, apiKey, vocabularyPrompt);
                                allVocabulary.AppendLine(header);
                                allVocabulary.AppendLine(pagesVocab);
                                allVocabulary.AppendLine();
                            }

                            // ── NEW: Summary
                            if (chkSummary.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Summary…");
                                string pagesSum = await ProcessPdfPagesMultimodal(pageGroup, apiKey, summaryPrompt);
                                allSummary.AppendLine(header);
                                allSummary.AppendLine(pagesSum);
                                allSummary.AppendLine();
                            }

                            // ── NEW: Key Takeaways
                            if (chkTakeaways.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Key Takeaways…");
                                string pagesTA = await ProcessPdfPagesMultimodal(pageGroup, apiKey, takeawaysPrompt);
                                allTakeaways.AppendLine(header);
                                allTakeaways.AppendLine(pagesTA);
                                allTakeaways.AppendLine();
                            }

                            // ── NEW: Cloze
                            if (chkCloze.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Cloze…");
                                string pagesCL = await ProcessPdfPagesMultimodal(pageGroup, apiKey, clozePrompt);
                                allCloze.AppendLine(header);
                                allCloze.AppendLine(pagesCL);
                                allCloze.AppendLine();
                            }

                            // ── NEW: True/False
                            if (chkTrueFalse.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → True/False…");
                                string pagesTF = await ProcessPdfPagesMultimodal(pageGroup, apiKey, trueFalsePrompt);
                                allTrueFalse.AppendLine(header);
                                allTrueFalse.AppendLine(pagesTF);
                                allTrueFalse.AppendLine();
                            }

                            // ── NEW: Outline
                            if (chkOutline.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Outline…");
                                string pagesOL = await ProcessPdfPagesMultimodal(pageGroup, apiKey, outlinePrompt);
                                allOutline.AppendLine(header);
                                allOutline.AppendLine(pagesOL);
                                allOutline.AppendLine();
                            }

                            // ── NEW: Concept Map
                            if (chkConceptMap.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Concept Map…");
                                string pagesCM = await ProcessPdfPagesMultimodal(pageGroup, apiKey, conceptMapPrompt);
                                allConceptMap.AppendLine(header);
                                allConceptMap.AppendLine(pagesCM);
                                allConceptMap.AppendLine();
                            }

                            // ── NEW: Table Extraction
                            if (chkTableExtract.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Table Extract…");
                                string pagesTE = await ProcessPdfPagesMultimodal(pageGroup, apiKey, tableExtractPrompt);
                                allTableExtract.AppendLine(header);
                                allTableExtract.AppendLine(pagesTE);
                                allTableExtract.AppendLine();
                            }

                            // ── NEW: Simplified Explanation
                            if (chkTableExtract.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Simplified Explanation…");
                                string pagesSI = await ProcessPdfPagesMultimodal(pageGroup, apiKey, simplifiedPrompt);
                                allSimplified.AppendLine(header);
                                allSimplified.AppendLine(pagesSI);
                                allSimplified.AppendLine();
                            }

                            // ── NEW: Case Study Scenario
                            if (chkCaseStudy.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Case Study…");
                                string pagesCS = await ProcessPdfPagesMultimodal(pageGroup, apiKey, caseStudyPrompt);
                                allCaseStudy.AppendLine(header);
                                allCaseStudy.AppendLine(pagesCS);
                                allCaseStudy.AppendLine();
                            }

                            // ── NEW: High-Yield Keywords
                            if (chkKeywords.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Keywords…");
                                string pagesKW = await ProcessPdfPagesMultimodal(pageGroup, apiKey, keywordsPrompt);
                                allKeywords.AppendLine(header);
                                allKeywords.AppendLine(pagesKW);
                                allKeywords.AppendLine();
                            }

                            // ── NEW: Translated Sections
                            if (chkTranslatedSections.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Translated Sections…");
                                string pagesTS = await ProcessPdfPagesMultimodal(pageGroup, apiKey, translatedSectionsPrompt);
                                allTranslatedSections.AppendLine(header);
                                allTranslatedSections.AppendLine(pagesTS);
                                allTranslatedSections.AppendLine();
                            }


                            UpdateOverlayLog($"✅ Pages {startPage}–{endPage} done.");
                        }
                        break;


                    case 3:
                        // ─── Three‐page‐batch mode ───

                        // 6) Instead of one‐by‐one, we chunk into groups of three pages at a time:
                        for (int i = 0; i < allPages.Count; i += 3)
                        {
                            // Build up to a 3‐page slice
                            var pageGroup = new List<(int pageNumber, Image image)>();
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
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} to GPT (Definitions)...");
                                string pagesDef = await ProcessPdfPagesMultimodal(pageGroup, apiKey, definitionsPrompt);
                                allDefinitions.AppendLine(header);
                                allDefinitions.AppendLine(pagesDef);
                                allDefinitions.AppendLine();
                            }

                            // 6b) MCQs
                            if (chkMCQs.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} to GPT (MCQs)...");
                                string pagesMCQs = await ProcessPdfPagesMultimodal(pageGroup, apiKey, mcqsPrompt);
                                allMCQs.AppendLine(header);
                                allMCQs.AppendLine(pagesMCQs);
                                allMCQs.AppendLine();
                            }

                            // 6c) Flashcards
                            if (chkFlashcards.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} to GPT (Flashcards)...");
                                string pagesFlash = await ProcessPdfPagesMultimodal(pageGroup, apiKey, flashcardsPrompt);
                                allFlashcards.AppendLine(header);
                                allFlashcards.AppendLine(pagesFlash);
                                allFlashcards.AppendLine();
                            }

                            // 6d) Vocabulary
                            if (chkVocabulary.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} to GPT (Vocabulary)...");
                                string pagesVocab = await ProcessPdfPagesMultimodal(pageGroup, apiKey, vocabularyPrompt);
                                allVocabulary.AppendLine(header);
                                allVocabulary.AppendLine(pagesVocab);
                                allVocabulary.AppendLine();
                            }

                            // ── NEW: Summary
                            if (chkSummary.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Summary…");
                                string pagesSum = await ProcessPdfPagesMultimodal(pageGroup, apiKey, summaryPrompt);
                                allSummary.AppendLine(header);
                                allSummary.AppendLine(pagesSum);
                                allSummary.AppendLine();
                            }

                            // ── NEW: Key Takeaways
                            if (chkTakeaways.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Key Takeaways…");
                                string pagesTA = await ProcessPdfPagesMultimodal(pageGroup, apiKey, takeawaysPrompt);
                                allTakeaways.AppendLine(header);
                                allTakeaways.AppendLine(pagesTA);
                                allTakeaways.AppendLine();
                            }

                            // ── NEW: Cloze
                            if (chkCloze.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Cloze…");
                                string pagesCL = await ProcessPdfPagesMultimodal(pageGroup, apiKey, clozePrompt);
                                allCloze.AppendLine(header);
                                allCloze.AppendLine(pagesCL);
                                allCloze.AppendLine();
                            }

                            // ── NEW: True/False
                            if (chkTrueFalse.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → True/False…");
                                string pagesTF = await ProcessPdfPagesMultimodal(pageGroup, apiKey, trueFalsePrompt);
                                allTrueFalse.AppendLine(header);
                                allTrueFalse.AppendLine(pagesTF);
                                allTrueFalse.AppendLine();
                            }

                            // ── NEW: Outline
                            if (chkOutline.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Outline…");
                                string pagesOL = await ProcessPdfPagesMultimodal(pageGroup, apiKey, outlinePrompt);
                                allOutline.AppendLine(header);
                                allOutline.AppendLine(pagesOL);
                                allOutline.AppendLine();
                            }

                            // ── NEW: Concept Map
                            if (chkConceptMap.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Concept Map…");
                                string pagesCM = await ProcessPdfPagesMultimodal(pageGroup, apiKey, conceptMapPrompt);
                                allConceptMap.AppendLine(header);
                                allConceptMap.AppendLine(pagesCM);
                                allConceptMap.AppendLine();
                            }

                            // ── NEW: Table Extraction
                            if (chkTableExtract.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Table Extract…");
                                string pagesTE = await ProcessPdfPagesMultimodal(pageGroup, apiKey, tableExtractPrompt);
                                allTableExtract.AppendLine(header);
                                allTableExtract.AppendLine(pagesTE);
                                allTableExtract.AppendLine();
                            }

                            // ── NEW: Simplified Explanation
                            if (chkTableExtract.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Simplified Explanation…");
                                string pagesSI = await ProcessPdfPagesMultimodal(pageGroup, apiKey, simplifiedPrompt);
                                allSimplified.AppendLine(header);
                                allSimplified.AppendLine(pagesSI);
                                allSimplified.AppendLine();
                            }

                            // ── NEW: Case Study Scenario
                            if (chkCaseStudy.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Case Study…");
                                string pagesCS = await ProcessPdfPagesMultimodal(pageGroup, apiKey, caseStudyPrompt);
                                allCaseStudy.AppendLine(header);
                                allCaseStudy.AppendLine(pagesCS);
                                allCaseStudy.AppendLine();
                            }

                            // ── NEW: High-Yield Keywords
                            if (chkKeywords.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Keywords…");
                                string pagesKW = await ProcessPdfPagesMultimodal(pageGroup, apiKey, keywordsPrompt);
                                allKeywords.AppendLine(header);
                                allKeywords.AppendLine(pagesKW);
                                allKeywords.AppendLine();
                            }

                            // ── NEW: Translated Sections
                            if (chkTranslatedSections.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Translated Sections…");
                                string pagesTS = await ProcessPdfPagesMultimodal(pageGroup, apiKey, translatedSectionsPrompt);
                                allTranslatedSections.AppendLine(header);
                                allTranslatedSections.AppendLine(pagesTS);
                                allTranslatedSections.AppendLine();
                            }

                            UpdateOverlayLog($"✅ Pages {startPage}–{endPage} done.");
                        }
                        break;

                    case 4:
                        // ─── Four‐page‐batch mode ───

                        // 6) Instead of one‐by‐one, we chunk into groups of three pages at a time:
                        for (int i = 0; i < allPages.Count; i += 4)
                        {
                            // Build up to a 4‐page slice
                            var pageGroup = new List<(int pageNumber, Image image)>();
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
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} to GPT (Definitions)...");
                                string pagesDef = await ProcessPdfPagesMultimodal(pageGroup, apiKey, definitionsPrompt);
                                allDefinitions.AppendLine(header);
                                allDefinitions.AppendLine(pagesDef);
                                allDefinitions.AppendLine();
                            }

                            // 6b) MCQs
                            if (chkMCQs.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} to GPT (MCQs)...");
                                string pagesMCQs = await ProcessPdfPagesMultimodal(pageGroup, apiKey, mcqsPrompt);
                                allMCQs.AppendLine(header);
                                allMCQs.AppendLine(pagesMCQs);
                                allMCQs.AppendLine();
                            }

                            // 6c) Flashcards
                            if (chkFlashcards.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} to GPT (Flashcards)...");
                                string pagesFlash = await ProcessPdfPagesMultimodal(pageGroup, apiKey, flashcardsPrompt);
                                allFlashcards.AppendLine(header);
                                allFlashcards.AppendLine(pagesFlash);
                                allFlashcards.AppendLine();
                            }

                            // 6d) Vocabulary
                            if (chkVocabulary.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} to GPT (Vocabulary)...");
                                string pagesVocab = await ProcessPdfPagesMultimodal(pageGroup, apiKey, vocabularyPrompt);
                                allVocabulary.AppendLine(header);
                                allVocabulary.AppendLine(pagesVocab);
                                allVocabulary.AppendLine();
                            }

                            // ── NEW: Summary
                            if (chkSummary.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Summary…");
                                string pagesSum = await ProcessPdfPagesMultimodal(pageGroup, apiKey, summaryPrompt);
                                allSummary.AppendLine(header);
                                allSummary.AppendLine(pagesSum);
                                allSummary.AppendLine();
                            }

                            // ── NEW: Key Takeaways
                            if (chkTakeaways.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Key Takeaways…");
                                string pagesTA = await ProcessPdfPagesMultimodal(pageGroup, apiKey, takeawaysPrompt);
                                allTakeaways.AppendLine(header);
                                allTakeaways.AppendLine(pagesTA);
                                allTakeaways.AppendLine();
                            }

                            // ── NEW: Cloze
                            if (chkCloze.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Cloze…");
                                string pagesCL = await ProcessPdfPagesMultimodal(pageGroup, apiKey, clozePrompt);
                                allCloze.AppendLine(header);
                                allCloze.AppendLine(pagesCL);
                                allCloze.AppendLine();
                            }

                            // ── NEW: True/False
                            if (chkTrueFalse.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → True/False…");
                                string pagesTF = await ProcessPdfPagesMultimodal(pageGroup, apiKey, trueFalsePrompt);
                                allTrueFalse.AppendLine(header);
                                allTrueFalse.AppendLine(pagesTF);
                                allTrueFalse.AppendLine();
                            }

                            // ── NEW: Outline
                            if (chkOutline.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Outline…");
                                string pagesOL = await ProcessPdfPagesMultimodal(pageGroup, apiKey, outlinePrompt);
                                allOutline.AppendLine(header);
                                allOutline.AppendLine(pagesOL);
                                allOutline.AppendLine();
                            }

                            // ── NEW: Concept Map
                            if (chkConceptMap.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Concept Map…");
                                string pagesCM = await ProcessPdfPagesMultimodal(pageGroup, apiKey, conceptMapPrompt);
                                allConceptMap.AppendLine(header);
                                allConceptMap.AppendLine(pagesCM);
                                allConceptMap.AppendLine();
                            }

                            // ── NEW: Table Extraction
                            if (chkTableExtract.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Table Extract…");
                                string pagesTE = await ProcessPdfPagesMultimodal(pageGroup, apiKey, tableExtractPrompt);
                                allTableExtract.AppendLine(header);
                                allTableExtract.AppendLine(pagesTE);
                                allTableExtract.AppendLine();
                            }

                            // ── NEW: Simplified Explanation
                            if (chkTableExtract.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Simplified Explanation…");
                                string pagesSI = await ProcessPdfPagesMultimodal(pageGroup, apiKey, simplifiedPrompt);
                                allSimplified.AppendLine(header);
                                allSimplified.AppendLine(pagesSI);
                                allSimplified.AppendLine();
                            }

                            // ── NEW: Case Study Scenario
                            if (chkCaseStudy.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Case Study…");
                                string pagesCS = await ProcessPdfPagesMultimodal(pageGroup, apiKey, caseStudyPrompt);
                                allCaseStudy.AppendLine(header);
                                allCaseStudy.AppendLine(pagesCS);
                                allCaseStudy.AppendLine();
                            }

                            // ── NEW: High-Yield Keywords
                            if (chkKeywords.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Keywords…");
                                string pagesKW = await ProcessPdfPagesMultimodal(pageGroup, apiKey, keywordsPrompt);
                                allKeywords.AppendLine(header);
                                allKeywords.AppendLine(pagesKW);
                                allKeywords.AppendLine();
                            }

                            // ── NEW: Translated Sections
                            if (chkTranslatedSections.Checked)
                            {
                                UpdateOverlayLog($"🖼️ Sending pages {startPage}–{endPage} → Translated Sections…");
                                string pagesTS = await ProcessPdfPagesMultimodal(pageGroup, apiKey, translatedSectionsPrompt);
                                allTranslatedSections.AppendLine(header);
                                allTranslatedSections.AppendLine(pagesTS);
                                allTranslatedSections.AppendLine();
                            }

                            UpdateOverlayLog($"✅ Pages {startPage}–{endPage} done.");
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected batchSize: {batchSize}");
                } // end of batch size switch


                // 7) تحويل StringBuilder إلى نصٍّ نهائي وحفظه في ملفات Word منسّقة
                // 7.1) ملف التعاريف
                // 7) Save out only those StringBuilders that were created (i.e. their CheckEdit was checked)
                if (chkDefinitions.Checked)
                {
                    string definitionsText = allDefinitions.ToString();
                    SaveContentToFile(FormatDefinitions(definitionsText), definitionsFilePath, "Definitions");
                }

                //// 7.2) ملف MCQs (يمكن تكييف تنسيق MCQs إذا أردتم تنسيقًا أضبط)
                //if (chkMCQs.Checked)
                //{
                //    string mcqsText = allMCQs.ToString();
                //    SaveContentToFile(mcqsText, mcqsFilePath, "MCQs");
                //}

                //if (chkMCQs.Checked)
                //{
                //    // 1) export to Word as before
                //    string mcqsRaw = allMCQs.ToString();
                //    SaveContentToFile(mcqsRaw, mcqsFilePath, "MCQs");

                //    // 2) parse into MCQ objects
                //    var parsed = ParseMcqs(mcqsRaw);

                //    // 3) decide CSV vs TSV
                //    // Option A: hard-coded
                //    // bool useCommaDelimiter = false;

                //    // Option B: from a new UI checkbox
                //    bool useCommaDelimiter = chkUseCommaDelimiter.Checked;

                //    // 4) build same base name but .csv or .tsv
                //    string delimitedPath = Path.ChangeExtension(mcqsFilePath,
                //        useCommaDelimiter ? ".csv" : ".tsv");

                //    // 5) write out
                //    SaveMcqsToDelimitedFile(parsed, delimitedPath, useCommaDelimiter);
                //}
                if (chkMCQs.Checked)
                {
                    string mcqsRaw = allMCQs.ToString();
                    // 1) still save the Word version:
                    SaveContentToFile(mcqsRaw, mcqsFilePath, "MCQs");

                    // 2) now parse & save out a .csv/.tsv
                    var parsed = ParseMcqs(mcqsRaw);
                    bool useComma = chkUseCommaDelimiter.Checked;    // or read from your combo
                    var delPath = Path.ChangeExtension(mcqsFilePath,
                                        useComma ? ".csv" : ".tsv");
                    SaveMcqsToDelimitedFile(parsed, delPath, useComma);
                }





                //// 7.3) ملف Flashcards
                //if (chkFlashcards.Checked)
                //{
                //    string flashcardsText = allFlashcards.ToString();
                //    SaveContentToFile(flashcardsText, flashcardsFilePath, "Flashcards");
                //}
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

                //if (chkVocabulary.Checked)
                //{
                //    string vocabularyText = FormatVocabulary(allVocabulary.ToString());
                //    SaveContentToFile(vocabularyText, vocabularyFilePath, "Vocabulary");

                //    // ** New: export CSV for Anki **
                //    var vocabCsvPath = Path.ChangeExtension(vocabularyFilePath, ".txt");

                //    var records = vocabularyText
                //        .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                //        .Select(line =>
                //        {
                //            var parts = line.Split(new[] { " - " }, 2, StringSplitOptions.None);
                //            var term = parts[0].Trim();
                //            var translation = parts.Length > 1
                //                ? parts[1].Trim()
                //                : "[Translation Needed]";
                //            return Tuple.Create(term, translation);
                //        })
                //        .ToList();  // this is List<Tuple<string,string>>


                //    //// write out a .txt next to your .docx
                //    SaveVocabularyForAnki(records, vocabCsvPath, cmbDelimiter.SelectedIndex);
                //    UpdateStatus($"Vocabulary CSV/TSV saved: {vocabCsvPath}");
                //}

                // ── New features:
                if (chkSummary.Checked)
                    SaveContentToFile(allSummary.ToString(), summaryFilePath, "Page Summaries");

                if (chkTakeaways.Checked)
                    SaveContentToFile(allTakeaways.ToString(), takeawaysFilePath, "Key Takeaways");

                //if (chkCloze.Checked)
                //    SaveContentToFile(allCloze.ToString(), clozeFilePath, "Fill-in-the-Blank (Cloze)");
                // 7.5) Fill-in-the-Blank (Cloze) export


                //if (chkCloze.Checked)
                //{
                //    // 1) Word export (exactly as you had before)
                //    string clozeRaw = allCloze.ToString();
                //    SaveContentToFile(clozeRaw, clozeFilePath, "Fill-in-the-Blank (Cloze)");

                //    // 2) Anki export
                //    var parsed = ParseCloze(clozeRaw);
                //    bool useComma = chkUseCommaDelimiter.Checked;
                //    string ext = useComma ? ".csv" : ".tsv";
                //    string outPath = Path.ChangeExtension(clozeFilePath, ext);

                //    using (var sw = new StreamWriter(outPath, false, Encoding.UTF8))
                //    {
                //        sw.WriteLine("Text");
                //        foreach (var (sentence, answer) in parsed)
                //        {
                //            var markup = $"{{{{c1::{answer}}}}}";
                //            var line = sentence.Replace("_______________", markup);
                //            if (useComma && line.Contains(","))
                //                line = $"\"{line.Replace("\"", "\"\"")}\"";
                //            sw.WriteLine(line);
                //        }
                //    }
                //    UpdateStatus($"Cloze exports saved: {Path.GetFileName(clozeFilePath)} and {Path.GetFileName(outPath)}");
                //}

                //if (chkCloze.Checked)
                //{
                //    // 1) Word export, unchanged
                //    string clozeRaw = allCloze.ToString();
                //    SaveContentToFile(clozeRaw, clozeFilePath, "Fill-in-the-Blank (Cloze)");

                //    // 2) Parse into (sentence, answer) pairs
                //    var parsed = ParseCloze(clozeRaw);
                //    bool useComma = chkUseCommaDelimiter.Checked;
                //    string ext = useComma ? ".csv" : ".tsv";

                //    //// 3) Emit a single-column CSV with the markup in place of the blank
                //    //string csvPath = Path.ChangeExtension(clozeFilePath, ".csv");
                //    string csvPath = Path.ChangeExtension(clozeFilePath, ext);
                //    using (var sw = new StreamWriter(csvPath, false, Encoding.UTF8))
                //    {
                //        // no header needed
                //        foreach (var (sentence, answer) in parsed)
                //        {
                //            // inject the cloze back into the sentence
                //            var markup = $"{{{{c1::{answer}}}}}";
                //            var line = sentence.Replace("_______________", markup);
                //            // wrap in quotes if it contains commas/newlines
                //            if (line.Contains(",") || line.Contains("\n"))
                //                line = $"\"{line.Replace("\"", "\"\"")}\"";
                //            sw.WriteLine(line);
                //        }
                //    }

                //    UpdateStatus($"✅ Cloze export saved: {Path.GetFileName(csvPath)}");
                //}

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
                    SaveContentToFile(allTableExtract.ToString(), tableFilePath, "Table Extractions");

                if (chkSimplified.Checked)
                    SaveContentToFile(allSimplified.ToString(), simplifiedFilePath, "Simplified Explanation");

                if (chkCaseStudy.Checked)
                    SaveContentToFile(allCaseStudy.ToString(), caseStudyFilePath, "Case Study Scenario");

                if (chkKeywords.Checked)
                    SaveContentToFile(allKeywords.ToString(), keywordsFilePath, "High-Yield Keywords");

                if (chkTranslatedSections.Checked)
                    SaveContentToFile(allTranslatedSections.ToString(), translatedSectionsFilePath, "Translated Sections");


                // 8) إظهار رسالة انتهاء المعالجة
                UpdateStatus("✅ Processing complete. Files saved to Desktop.");
                UpdateOverlayLog("✅ Processing complete. Files saved to Desktop as selected outputs.");
                UpdateOverlayLog("E N D   G E N E R A T I N G...");
                UpdateOverlayLog("-----------------------------------------------------");
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Error: " + ex.Message, "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("❌ An error occurred during processing.");
                UpdateOverlayLog("❌ An error occurred during processing: " + ex.Message);
            }
            finally
            {
                buttonProcessFile.Enabled = true;
                buttonBrowseFile.Enabled = true;
                // Disable the maximize and minimize of the processing form
                this.MaximizeBox = true; // Disable maximize button
                this.MinimizeBox = true; // Disable minimize button
                this.Text = "ChatGPT File Processor"; // Reset form title

                UpdateStatus("🔚 Processing finished.");
                UpdateOverlayLog("🔚 Processing finished.");
                HideOverlay();
            }
        }



        // Method to save content to specific file
        private void SaveContentToFile(string content, string filePath, string sectionTitle)
        {
            Word.Application wordApp = new Word.Application();
            Word.Document doc = wordApp.Documents.Add();

            try
            {
                Word.Paragraph titlePara = doc.Content.Paragraphs.Add();
                titlePara.Range.Text = sectionTitle;
                //titlePara.Range.Font.Bold = 1;
                titlePara.Range.Font.Size = 14;
                titlePara.Format.SpaceAfter = 10;
                titlePara.Range.InsertParagraphAfter();

                Word.Paragraph contentPara = doc.Content.Paragraphs.Add();
                contentPara.Range.Text = content;
                contentPara.Range.Font.Bold = 0;
                contentPara.Format.SpaceAfter = 10;
                contentPara.Range.InsertParagraphAfter();

                doc.SaveAs2(filePath);
            }
            finally
            {
                doc.Close();
                wordApp.Quit();
            }
            UpdateStatus($"Results saved successfully to {filePath}");
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
            UpdateStatus("Model changed, saving selection...");
            SaveApiKeyAndModel();
        }
        private void comboBoxEditModel_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateStatus("Model changed, saving selection...");
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


        private List<(int pageNumber, Image image)> ConvertPdfToImages(string filePath, int dpi = 300)
        {
            var pages = new List<(int, Image)>();
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



        public async System.Threading.Tasks.Task<string> SendImageToGPTAsync(Image image, string apiKey)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var base64 = Convert.ToBase64String(ms.ToArray());

                var jsonBody = new
                {
                    model = "gpt-4o",
                    messages = new[]
                    {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "image_url", image_url = new { url = $"data:image/png;base64,{base64}" } },
                        new { type = "text", text = "Please extract all readable content from this page including equations, tables, and diagrams if present." }
                    }
                }
            }
                };

                const int maxRetries = 3;
                const int delayMs = 1500;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        using (var http = new HttpClient())
                        {
                            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                            var content = new StringContent(JsonConvert.SerializeObject(jsonBody), Encoding.UTF8, "application/json");
                            var response = await http.PostAsync("https://api.openai.com/v1/chat/completions", content);
                            if (!response.IsSuccessStatusCode)
                            {
                                throw new Exception($"API Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                            }


                            response.EnsureSuccessStatusCode(); // Throws if not 2xx

                            var result = await response.Content.ReadAsStringAsync();
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (attempt == maxRetries)
                            throw new Exception($"❌ Failed after {maxRetries} attempts. Last error: {ex.Message}");

                        await Task.Delay(delayMs);
                    }
                }

                return null; // Should not reach here
            }
        }



        /// يعالج صفحةً واحدةً (كـ صورة) بطريقة Multimodal: يرسل الصورة + التعليمات النصّية دفعةً واحدة إلى GPT-4o.
        /// يرجع النصّ الناتج (مثل التعاريف أو الأسئلة) مباشرة.
        private async Task<string> ProcessPdfPageMultimodal(Image image, string apiKey, string taskPrompt)
        {
            // 1. تحويل الصورة إلى Base64
            using (var ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                string base64 = Convert.ToBase64String(ms.ToArray());

                // 2. بناء JSON payload لإرسال الصورة مع النص دفعةً واحدة
                var requestBody = new
                {
                    //In the future you can change this model gpt-40 with user selection model name dynamiclly, right now it is static due to the app designed for one model.
                    model = "gpt-4o",
                    messages = new object[]
                    {
            new
            {
                role = "user",
                content = new object[]
                {
                    new
                    {
                        type = "image_url",
                        image_url = new { url = $"data:image/png;base64,{base64}" }
                    },
                    new
                    {
                        type = "text",
                        text = taskPrompt
                    }
                }
            }
                    }
                };

                string jsonContent = System.Text.Json.JsonSerializer.Serialize(
                    requestBody,
                    new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }
                );



                // 3. إرسال الطلب للـ Chat Completion endpoint
                using (var client = new HttpClient())
                {

                    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);

                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API Error: {response.StatusCode} - {error}");
                    }

                    // 4. قراءة النتيجة (النص الناتج) وإرجاعه
                    string resultJson = await response.Content.ReadAsStringAsync();
                    var jsonNode = JsonNode.Parse(resultJson);
                    return jsonNode?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "";
                }
            }
        }


        ///// <summary>
        ///// Sends up to three page‐images in one shot (multimodal) to GPT-4o, along with a single text prompt.
        ///// </summary>
        //private async Task<string> ProcessPdfPagesMultimodal(
        //    List<(int pageNumber, Image image)> pageGroup,
        //    string apiKey,
        //    string taskPrompt
        //)
        //{
        //    // Build a single “messages” list that contains each image_url entry first, then the text prompt
        //    var multimodalContent = new List<object>();

        //    foreach (var (pageNumber, image) in pageGroup)
        //    {
        //        using (var ms = new MemoryStream())
        //        {
        //            image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        //            string base64 = Convert.ToBase64String(ms.ToArray());
        //            multimodalContent.Add(new
        //            {
        //                type = "image_url",
        //                image_url = new { url = $"data:image/png;base64,{base64}" }
        //            });
        //        }
        //    }

        //    // Finally, add the single text prompt (task instructions) as the last content element
        //    multimodalContent.Add(new
        //    {
        //        type = "text",
        //        text = taskPrompt
        //    });

        //    var requestBody = new
        //    {
        //        model = "gpt-4o",
        //        messages = new object[]
        //        {
        //        new
        //        {
        //            role = "user",
        //            content = multimodalContent.ToArray()
        //        }
        //        }
        //    };

        //    string jsonContent = System.Text.Json.JsonSerializer.Serialize(
        //        requestBody,
        //        new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }
        //    );

        //    using (var client = new HttpClient())
        //    {
        //        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
        //        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        //        HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            string error = await response.Content.ReadAsStringAsync();
        //            throw new Exception($"API Error: {response.StatusCode} - {error}");
        //        }

        //        string resultJson = await response.Content.ReadAsStringAsync();
        //        var jsonNode = JsonNode.Parse(resultJson);
        //        return jsonNode?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "";
        //    }
        //}

        /// <summary>
        /// Sends up to N images (in pageGroup) plus the text prompt in one chat call.
        /// This works for batchSize = 2 or 3.
        /// </summary>
        private async Task<string> ProcessPdfPagesMultimodal(
            List<(int pageNumber, Image image)> pageGroup,
            string apiKey,
            string taskPrompt
        )
        {
            // 1. Convert each image to a Base64 segment
            var imageContents = new List<object>();
            foreach (var (pageNumber, image) in pageGroup)
            {
                using (var ms = new MemoryStream())
                {

                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    var base64 = Convert.ToBase64String(ms.ToArray());
                    imageContents.Add(new
                    {
                        type = "image_url",
                        image_url = new { url = $"data:image/png;base64,{base64}" }
                    });
                }
            }

            // 2. Build a single “content” array: [ image1, image2, (maybe image3), { type="text", text=taskPrompt } ]
            var fullContent = new List<object>();
            fullContent.AddRange(imageContents);
            fullContent.Add(new { type = "text", text = taskPrompt });

            // 3. Assemble top‐level request
            var requestBody = new
            {
                model = "gpt-4o",
                messages = new object[]
                {
            new
            {
                role = "user",
                content = fullContent.ToArray()
            }
                }
            };

            string jsonContent = System.Text.Json.JsonSerializer.Serialize(
                requestBody,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }
            );

            using (var client = new HttpClient())
            {

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API Error: {response.StatusCode} – {error}");
                }

                string resultJson = await response.Content.ReadAsStringAsync();
                var jsonNode = JsonNode.Parse(resultJson);
                return jsonNode?["choices"]?[0]?["message"]?["content"]?.ToString()
                       ?? "No content returned.";
            }
        }





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
                Size = new Size(120, 120),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = Properties.Resources.loading_gif,
                Location = new System.Drawing.Point(centerX - 60, overlayPanel.Height / 2 - 150)
            };

            statusLabel = new Label
            {
                AutoSize = false,
                Size = new Size(400, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new System.Drawing.Font("Segoe UI", 12, FontStyle.Bold),
                Location = new System.Drawing.Point(centerX - 200, loadingIcon.Bottom + 10),
                Text = "⏳ Processing, please wait..."
            };


            logTextBox = new TextBox
            {
                Size = new Size(500, 100),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = new System.Drawing.Font("Consolas", 10),
                Location = new System.Drawing.Point(centerX - 250, statusLabel.Bottom + 10)
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


        private void loadCheckBoxesSettings()
        {
            // Load the settings for checkboxes from a file or application settings

            // Load saved user preferences into each CheckEdit:
            chkDefinitions.Checked = Properties.Settings.Default.GenerateDefinitions;
            chkMCQs.Checked = Properties.Settings.Default.GenerateMCQs;
            chkFlashcards.Checked = Properties.Settings.Default.GenerateFlashcards;
            chkVocabulary.Checked = Properties.Settings.Default.GenerateVocabulary;
            chkMedicalMaterial.Checked = Properties.Settings.Default.MedicalMaterial;
            // ── New feature checkboxes:
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

            textEditAPIKey.ReadOnly = Properties.Settings.Default.ApiKeyLock;
        }

        private void chkDefinitions_CheckedChanged(object sender, EventArgs e)
        {
            if (chkDefinitions.Checked)
            {
                UpdateStatus("Definitions...Activated");
            }
            else
            {
                UpdateStatus("Definitions...Deactivated");
            }
            // Save the state of the checkbox
            Properties.Settings.Default.GenerateDefinitions = chkDefinitions.Checked;
            Properties.Settings.Default.Save();
        }

        private void chkMCQs_CheckedChanged(object sender, EventArgs e)
        {
            if (chkMCQs.Checked)
            {
                UpdateStatus("MCQs...Activated");
            }
            else
            {
                UpdateStatus("MCQs...Deactivated");
            }
            // Save the state of the checkbox
            Properties.Settings.Default.GenerateMCQs = chkMCQs.Checked;
            Properties.Settings.Default.Save();
        }

        private void chkFlashcards_CheckedChanged(object sender, EventArgs e)
        {
            if (chkFlashcards.Checked)
            {
                UpdateStatus("Flashcards...Activated");
            }
            else
            {
                UpdateStatus("Flashcards...Deactivated");
            }
            // Save the state of the checkbox
            Properties.Settings.Default.GenerateFlashcards = chkFlashcards.Checked;
            Properties.Settings.Default.Save();
        }

        private void chkVocabulary_CheckedChanged(object sender, EventArgs e)
        {
            if (chkVocabulary.Checked)
            {
                UpdateStatus("Vocabulary...Activated");
            }
            else
            {
                UpdateStatus("Vocabulary...Deactivated");
            }
            // Save the state of the checkbox
            Properties.Settings.Default.GenerateVocabulary = chkVocabulary.Checked;
            Properties.Settings.Default.Save();
        }

        //private void chkSummary_CheckedChanged(object sender, EventArgs e)
        //{

        //}

        //private void chkTakeaways_CheckedChanged(object sender, EventArgs e)
        //{

        //}
        private void chkSummary_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateSummary = chkSummary.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Page Summary…{(chkSummary.Checked ? "Activated" : "Deactivated")}");
        }

        private void chkTakeaways_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateTakeaways = chkTakeaways.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Key Takeaways…{(chkTakeaways.Checked ? "Activated" : "Deactivated")}");
        }

        private void chkCloze_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateCloze = chkCloze.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Cloze Deletions…{(chkCloze.Checked ? "Activated" : "Deactivated")}");
        }

        private void chkTrueFalse_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateTrueFalse = chkTrueFalse.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"True/False Questions…{(chkTrueFalse.Checked ? "Activated" : "Deactivated")}");
        }

        private void chkOutline_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateOutline = chkOutline.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Page Outline…{(chkOutline.Checked ? "Activated" : "Deactivated")}");
        }

        private void chkConceptMap_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateConceptMap = chkConceptMap.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Concept Map…{(chkConceptMap.Checked ? "Activated" : "Deactivated")}");
        }

        private void chkTableExtract_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateTableExtract = chkTableExtract.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Table Extraction…{(chkTableExtract.Checked ? "Activated" : "Deactivated")}");
        }

        private void chkSimplified_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateSimplified = chkSimplified.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Simplified Content…{(chkSimplified.Checked ? "Activated" : "Deactivated")}");
        }

        private void chkCaseStudy_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateCaseStudy = chkCaseStudy.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Case Study…{(chkCaseStudy.Checked ? "Activated" : "Deactivated")}");
        }

        private void chkKeywords_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateKeywords = chkKeywords.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Keywords Extraction…{(chkKeywords.Checked ? "Activated" : "Deactivated")}");
        }

        private void chkTranslatedSections_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.GenerateTranslatedSections = chkTranslatedSections.Checked;
            Properties.Settings.Default.Save();
            UpdateStatus($"Translated Sections…{(chkTranslatedSections.Checked ? "Activated" : "Deactivated")}");
        }

        private void chkUseCommaDelimiter_CheckedChanged(object sender, EventArgs e)
        {
            if (chkUseCommaDelimiter.Checked)
            {
                UpdateStatus("Using Comma Delimiter for CSV files");
            }
            else
            {
                UpdateStatus("Using Tab Delimiter for TSV files");
            }
            // store the UseCommaDelimiter setting
            Properties.Settings.Default.useCommaDelimiter = chkUseCommaDelimiter.Checked;
            Properties.Settings.Default.Save();
        }

        private void chkMedicalMaterial_CheckedChanged(object sender, EventArgs e)
        {
            if (chkMedicalMaterial.Checked)
            {
                UpdateStatus("Medical Material...Activated");
            }
            else
            {
                UpdateStatus("Medical Material...Deactivated");
            }
            // store the MedicalMaterial setting
            Properties.Settings.Default.MedicalMaterial = chkMedicalMaterial.Checked;
            Properties.Settings.Default.Save();
        }

        private void cmbGeneralLang_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateStatus("General Language...Changed");
            
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
            UpdateStatus("Vocabulary Language...Changed");
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
            UpdateStatus($"Page batch mode set to: {chosen} page(s) at a time");
        }



        /// <summary>
        /// Turns the raw flashcard text into a list of (Front,Back) tuples.
        /// Expects blocks like:
        ///    Front: X
        ///    Back:  Y
        /// separated by blank lines.
        /// </summary>
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

        /// <summary>
        /// Writes out a Front/Back list to a comma‐ or tab‐delimited file.
        /// </summary>
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


        //        void SaveVocabularyForAnki(
        //    List<Tuple<string, string>> records,
        //    string path,
        //    int delimiterChoiceIndex  // 0 = TSV, 1 = CSV
        //)
        //        {
        //            string sep = (delimiterChoiceIndex == 0) ? "\t" : ",";

        //            // old-school using block, not declaration
        //            using (var sw = new System.IO.StreamWriter(path, false, System.Text.Encoding.UTF8))
        //            {
        //                foreach (var rec in records)
        //                {
        //                    string t = EscapeField(rec.Item1, sep);
        //                    string tr = EscapeField(rec.Item2, sep);
        //                    sw.WriteLine(t + sep + tr);
        //                }
        //            }
        //        }

        //        string EscapeField(string text, string sep)
        //        {
        //            bool mustQuote = text.Contains(sep) || text.Contains("\"") || text.Contains("\n");
        //            if (mustQuote)
        //            {
        //                // double up any quotes, wrap in quotes
        //                return "\"" + text.Replace("\"", "\"\"") + "\"";
        //            }
        //            return text;
        //        }


        /// <summary>
        /// Represents one MCQ with 4 choices and a correct answer letter.
        /// </summary>
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

        //public class MCQ
        //{
        //    public string Question { get; set; }
        //    public string A { get; set; }
        //    public string B { get; set; }
        //    public string C { get; set; }
        //    public string D { get; set; }
        //    public string Answer { get; set; }
        //}


        /// <summary>
        /// Parse your raw MCQs (blocks separated by blank lines) into a List&lt;MCQ&gt;.
        /// </summary>
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

        //private List<MCQ> ParseMcqs(string raw)
        //{
        //    var list = new List<MCQ>();
        //    var blocks = raw
        //        .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
        //        .Select(b => b.Trim());

        //    foreach (var block in blocks)
        //    {
        //        var lines = block
        //            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        //            .Select(l => l.Trim())
        //            .ToArray();

        //        var mcq = new MCQ();
        //        foreach (var line in lines)
        //        {
        //            if (line.StartsWith("Question:", StringComparison.OrdinalIgnoreCase))
        //                mcq.Question = line.Substring("Question:".Length).Trim();
        //            else if (line.StartsWith("A)", StringComparison.OrdinalIgnoreCase))
        //                mcq.A = line.Substring(2).Trim();
        //            else if (line.StartsWith("B)", StringComparison.OrdinalIgnoreCase))
        //                mcq.B = line.Substring(2).Trim();
        //            else if (line.StartsWith("C)", StringComparison.OrdinalIgnoreCase))
        //                mcq.C = line.Substring(2).Trim();
        //            else if (line.StartsWith("D)", StringComparison.OrdinalIgnoreCase))
        //                mcq.D = line.Substring(2).Trim();
        //            else if (line.StartsWith("Answer:", StringComparison.OrdinalIgnoreCase))
        //                mcq.Answer = line.Substring("Answer:".Length).Trim();
        //        }
        //        // only add if we got at least a question and an answer
        //        if (!string.IsNullOrWhiteSpace(mcq.Question) && !string.IsNullOrWhiteSpace(mcq.Answer))
        //            list.Add(mcq);
        //    }

        //    return list;
        //}

        /// <summary>
        /// Write a list of MCQs out to CSV or TSV.
        /// </summary>
        /// <param name="mcqs">Parsed questions</param>
        /// <param name="filePath">Full path ending in “.csv” or “.tsv”</param>
        /// <param name="useComma">True =&gt; comma-delimited (CSV), false =&gt; tab-delimited (TSV)</param>
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



        //private void SaveMcqsToDelimitedFile(List<MCQ> mcqs, string filePath, bool useComma = false)
        //{
        //    char delim = useComma ? ',' : '\t';
        //    using (var sw = new StreamWriter(filePath, false, Encoding.UTF8))
        //    {
        //        // header
        //        sw.WriteLine($"Question{delim}A{delim}B{delim}C{delim}D{delim}Answer");

        //        foreach (var q in mcqs)
        //        {
        //            // for CSV only: wrap fields in quotes if they contain commas or quotes
        //            Func<string, string> esc = field =>
        //            {
        //                if (!useComma) // TSV never needs escaping
        //                    return field.Replace("\t", " ");
        //                // CSV escape:
        //                var f = field.Replace("\"", "\"\"");
        //                if (f.Contains(",") || f.Contains("\"") || f.Contains("\n"))
        //                    return $"\"{f}\"";
        //                return f;
        //            };

        //            sw.WriteLine(
        //                esc(q.Question) + delim +
        //                esc(q.A) + delim +
        //                esc(q.B) + delim +
        //                esc(q.C) + delim +
        //                esc(q.D) + delim +
        //                esc(q.Answer)
        //            );
        //        }
        //    }

        //    UpdateStatus($"MCQs exported to {Path.GetFileName(filePath)}");
        //}




        ///// <summary>
        ///// Split your raw cloze text into individual blocks
        ///// </summary>
        //private List<string> ParseCloze(string raw)
        //{
        //    return raw
        //      .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
        //      .Select(b => b.Trim().Replace("\r\n", " ")) // collapse any internal new-lines
        //      .ToList();
        //}

        ///// <summary>
        ///// Write a two-column (Text,Extra) CSV/TSV for Anki’s Cloze note type
        ///// </summary>
        //private void SaveClozeToDelimitedFile(List<string> entries, string path, bool useComma)
        //{
        //    char sep = useComma ? ',' : '\t';
        //    // UTF8 without BOM helps avoid field-name mismatches
        //    using (var sw = new StreamWriter(path, false, new UTF8Encoding(false)))
        //    {

        //        // header—map Column1→Text, Column2→Extra in Anki
        //        if (useComma)
        //            sw.WriteLine("\"Text\",\"Extra\"");
        //        else
        //            sw.WriteLine("Text\tExtra");

        //        foreach (var entry in entries)
        //        {
        //            // escape for CSV
        //            if (useComma)
        //            {
        //                var e = entry.Replace("\"", "\"\"");
        //                sw.WriteLine($"\"{e}\",\"\"");  // blank Extra
        //            }
        //            else
        //            {
        //                sw.WriteLine(entry + "\t");     // trailing tab = empty Extra
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// Parse raw cloze blocks into (Sentence,Answer) pairs.
        /// Expects blocks like:
        ///   Sentence: "_______________ is a miotic drug."
        ///   Answer: Pilocarpine
        /// separated by blank lines.
        /// </summary>
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

        //private List<(string Sentence, string Answer)> ParseCloze(string raw)
        //{
        //    var list = new List<(string, string)>();
        //    // split on blank-line (2+ newlines)
        //    var blocks = Regex.Split(raw.Trim(), @"\r?\n\s*\r?\n");
        //    foreach (var block in blocks)
        //    {
        //        string sent = null, ans = null;
        //        foreach (var line in block.Split('\n'))
        //        {
        //            var t = line.Trim();
        //            if (t.StartsWith("Sentence:", StringComparison.OrdinalIgnoreCase))
        //                sent = t.Substring("Sentence:".Length).Trim().Trim('"');
        //            else if (t.StartsWith("Answer:", StringComparison.OrdinalIgnoreCase))
        //                ans = t.Substring("Answer:".Length).Trim();
        //        }
        //        if (!string.IsNullOrEmpty(sent) && !string.IsNullOrEmpty(ans))
        //            list.Add((sent, ans));
        //    }
        //    return list;
        //}




        /// <summary>
        /// Write out cloze pairs to CSV or TSV:
        /// columns: Sentence [with blank], Answer
        /// </summary>
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

        

        //        private void SaveClozeToDelimitedFile(
        //    List<(string Sentence, string Answer)> items,
        //    string path,
        //    bool useCommaDelimiter
        //)
        //        {
        //            char sep = useCommaDelimiter ? ',' : '\t';
        //            using (var w = new StreamWriter(path, false, Encoding.UTF8))
        //            {
        //                // **no header** is actually fine for a single-column import,
        //                // but if you want one you could do:
        //                // w.WriteLine("Text");

        //                foreach (var (sent, ans) in items)
        //                {
        //                    // inject your cloze markup back into the sentence
        //                    var markup = $"{{{{c1::{ans}}}}}";
        //                    var line = sent.Replace("_______________", markup);

        //                    // if CSV, wrap any commas/newlines in quotes
        //                    if (useCommaDelimiter && (line.Contains(',') || line.Contains('\n')))
        //                    {
        //                        line = "\"" + line.Replace("\"", "\"\"") + "\"";
        //                    }

        //                    w.WriteLine(line);
        //                }
        //            }
        //        }



    }
}