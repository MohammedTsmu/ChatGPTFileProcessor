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



                // 3.1) prompt for Definitions in “GeneralLanguage” (e.g. user picks “French”)
                // 1) Read which “General Language” the user picked:
                string generalLangName = cmbGeneralLang.SelectedItem as string ?? "English";

                // 2) Read the “Medical Material” checkbox:
                bool isMedical = chkMedicalMaterial.Checked;
                // 3) Read the “Vocabulary Language” dropdown:
                string vocabLangName = cmbVocabLang.SelectedItem as string ?? "Arabic";

                //// 3) Build each prompt with a little conditional text:
                //string definitionsPrompt;
                //if (isMedical)
                //{
                //    // When “Medical Material” is checked, ask specifically for medical definitions
                //    definitionsPrompt =
                //        $"Provide concise medical definitions (in {generalLangName}) for each key medical term on this page. " +
                //        $"For each term, write:\n" +
                //        $"- The term itself as a heading\n" +
                //        $"- Then a one- or two-sentence definition in {generalLangName}\n\n" +
                //        $"Separate every entry by a blank line, without numbering.";
                //}
                //else
                //{
                //    // When unchecked, just ask for normal (non-medical) definitions
                //    definitionsPrompt =
                //        $"Provide concise definitions (in {generalLangName}) for each key term on this page. " +
                //        $"For each term, write:\n" +
                //        $"- The term itself as a heading\n" +
                //        $"- Then a one- or two-sentence definition in {generalLangName}\n\n" +
                //        $"Separate every entry by a blank line, without numbering.";
                //}


                //// 3.2) MCQs prompt (this is typically language-only; leave it as-is or you can adjust similarly)
                //string mcqsPrompt =
                //    $"Generate multiple-choice questions (only in {generalLangName}) based on the content of this page. Use EXACTLY this format (no deviations):\n\n" +
                //    $"Question: [Write the question in {generalLangName}]\n" +
                //    $"A) [Option A in {generalLangName}]\n" +
                //    $"B) [Option B in {generalLangName}]\n" +
                //    $"C) [Option C in {generalLangName}]\n" +
                //    $"D) [Option D in {generalLangName}]\n" +
                //    $"Answer: [Correct Letter]\n\n" +
                //    $"Separate each question block with a blank line.";


                //// 3.3) Flashcards prompt: toggle “medical” vocabulary vs. general vocabulary
                //string flashcardsPrompt;
                //if (isMedical)
                //{
                //    flashcardsPrompt =
                //        $"Create medical flashcards in {generalLangName} for each key medical or pharmaceutical term on this page. " +
                //        $"Use EXACTLY this format (no deviations):\n\n" +
                //        $"Front: [Term in {generalLangName}]\n" +
                //        $"Back:  [Definition in {generalLangName}]\n\n" +
                //        $"Leave exactly one blank line between each card.";
                //}
                //else
                //{
                //    flashcardsPrompt =
                //        $"Create flashcards in {generalLangName} for each key term on this page. " +
                //        $"Use EXACTLY this format (no deviations):\n\n" +
                //        $"Front: [Term in {generalLangName}]\n" +
                //        $"Back:  [Definition in {generalLangName}]\n\n" +
                //        $"Leave exactly one blank line between each card.";
                //}


                //// 3.4) Vocabulary: translate into whichever “Vocab Language” the user chose
                ////string vocabLangName = cmbVocabLang.SelectedItem as string ?? "Arabic";
                //string vocabularyPrompt =
                //    $"Extract important vocabulary terms from this page and translate them to {vocabLangName}. " +
                //    $"Use EXACTLY this format (no bullets, no numbering):\n\n" +
                //    $"EnglishTerm – {vocabLangName}Translation\n\n" +
                //    $"Leave exactly one blank line between each entry.";




                //// 5.1 Page Summary (2–3 sentences high-level overview)
                //string summaryPrompt =
                //    $"In {generalLangName}, write a concise 2–3 sentence SUMMARY of the text on these page(s). " +
                //    (isMedical
                //        ? "Focus on the key medical concepts and terminology."
                //        : "Focus on the core concepts.")
                //    + "\n\nProvide as plain prose, no bullet points.";

                //// 5.2 Key Takeaways (5 bullet points)
                //string takeawaysPrompt =
                //    $"List 5 “Key Takeaway” bullet points (in {generalLangName}) that capture the MOST IMPORTANT facts from these page(s). " +
                //    (isMedical
                //        ? "Include any critical medical terms."
                //        : "")
                //    + "\n\nFormat exactly as:\n- Takeaway 1\n- Takeaway 2\n…\n(no numbering, just a dash and a space).";

                //// 5.3 Fill-in-the-Blank (Cloze)
                //string clozePrompt =
                //    $"Generate 5 fill-in-the-blank sentences (in {generalLangName}) based on these page(s). " +
                //    $"Each line should be formatted exactly like:\n“___(blank)___ is a [brief clue or definition].”\n\n" +
                //    $"For instance: “___(Pilocarpine)___ is a miotic drug used to ____(clue)____.”";

                //// 5.4 True/False Questions
                //string trueFalsePrompt =
                //    $"Generate 5 True/False statements (in {generalLangName}) based on the content of these page(s). " +
                //    $"Each statement should be exactly “Statement. True or False?”\n\n" +
                //    $"For example:\n“Pilocarpine is an osmotic diuretic. True or False?”";

                //// 5.5 Generate Outline
                //string outlinePrompt =
                //    $"Produce a hierarchical OUTLINE (section headings and sub-headings) in {generalLangName} for the material on these page(s). " +
                //    $"Use numbered levels like “1. Main Topic – 1.1 Subtopic – 1.1.1 Details,” etc.";

                //// 5.6 Concept Relationships (Concept Map text)
                //string conceptMapPrompt =
                //    $"List the key CONCEPTS from these page(s) and show their relationships in text form. " +
                //    $"For each relationship, format as:\n“ConceptA → relates to → ConceptB” or “ConceptA —   contrasts with — ConceptC.”\n" +
                //    $"Write in {generalLangName}.";

                //// 5.7 Table Extraction
                //string tableExtractPrompt =
                //    $"If these page(s) contain any tabular data (drug doses, side effects, contraindications, etc.), " +
                //    $"extract that table into a Markdown-style table (columns | column names | …) and output just the table. " +
                //    $"If no table is present, respond “No table found.”";

                //// 5.8 Simplified Explanation
                //string simplifiedPrompt =
                //    $"Explain the content of these page(s) in simpler language as if teaching a first-year medical student. " +
                //    $"Use {generalLangName}, avoid jargon or define any technical terms.";

                //// 5.9 Case Study Scenario
                //string caseStudyPrompt =
                //    $"Write a short clinical vignette (1 paragraph) based on these page(s). " +
                //    $"Include patient age, presentation, key symptoms, and ask 1 multiple-choice question at the end. " +
                //    $"Use {generalLangName}.";

                //// 5.10 High-Yield Keywords
                //string keywordsPrompt =
                //    $"List the HIGH-YIELD KEYWORDS (in {generalLangName}) that appear on these page(s). " +
                //    $"Return a comma-separated list or a vertical list—just the keywords, no definitions.";



                //// ─── Build prompt templates:
                //string definitionsPrompt = isMedical
                //    ? $"Provide concise medical definitions (in {generalLangName}) for each key medical term on this page. " +
                //      //$"- Term as heading, then 1–2 sentence definition in {generalLangName}. Separate each entry by a blank line."
                //      $"- Term as heading, then 1–2 sentence definition in {generalLangName}."
                //    : $"Provide concise definitions (in {generalLangName}) for each key term on this page. " +
                //      $"- Term as heading, then 1–2 sentence definition in {generalLangName}. Separate each entry by a blank line.";

                //string mcqsPrompt =
                //    $"Generate multiple-choice questions (only in {generalLangName}) based on this page. " +
                //    $"Format exactly:\nQuestion: [in {generalLangName}]\nA) …\nB) …\nC) …\nD) …\nAnswer: [Letter]\n\n(blank line).";

                //string flashcardsPrompt = isMedical
                //    ? $"Create medical flashcards in {generalLangName} for each key medical term. " +
                //      $"Format exactly:\nFront: [Term in {generalLangName}]\nBack: [Definition in {generalLangName}]\n\n(blank line)."
                //    : $"Create flashcards in {generalLangName} for each key term. " +
                //      $"Format exactly:\nFront: [Term in {generalLangName}]\nBack: [Definition in {generalLangName}]\n\n(blank line).";

                //string vocabularyPrompt =
                //    $"Extract important vocabulary terms and translate them into {vocabLangName}. " +
                //    $"Format exactly:\nEnglishTerm – {vocabLangName}Translation\n\n(blank line).";

                //// ─── New feature prompts:
                //string summaryPrompt =
                //    $"In {generalLangName}, write a concise 2–3 sentence SUMMARY of the text on these page(s)." +
                //    (isMedical ? " Focus on medical concepts." : "");

                //string takeawaysPrompt =
                //    $"List 5 “Key Takeaway” bullet points (in {generalLangName}) capturing the MOST IMPORTANT facts from these page(s). " +
                //    $"Use a dash then a space for each bullet.";

                //string clozePrompt =
                //    $"Generate 5 fill-in-the-blank sentences (in {generalLangName}) based on these page(s). " +
                //    //$"Each line exactly: \"___(blank)___ is [clue].\"";
                //    $"Each line exactly: \"______ is [clue].followed by new line for (blank) answer\"";

                //string trueFalsePrompt =
                //    $"Generate 5 True/False statements (in {generalLangName}) based on these page(s). " +
                //    //$"Each statement ends with \"True or False?\"";
                //    $"Each statement ends with \"True or False?\". then new line have \"(True Answer)\"";

                //string outlinePrompt =
                //    $"Produce a hierarchical OUTLINE (in {generalLangName}) for the material on these page(s). " +
                //    $"Use levels like 1., 1.1, 1.1.1, etc.";

                //string conceptMapPrompt =
                //    $"List the key CONCEPTS (in {generalLangName}) from these page(s) and show their relationships. " +
                //    $"Format as “ConceptA → relates to → ConceptB” or “ConceptA — contrasts with — ConceptC.”";

                //string tableExtractPrompt =
                //    $"If these page(s) contain any tables (drug doses, side effects, etc.), extract that table into Markdown table format. " +
                //    $"If none, respond “No table found.”";

                //string simplifiedPrompt =
                //    $"Explain the content of these page(s) in simpler language (like you’re teaching a first-year student). " +
                //    $"Use {generalLangName} and define any technical terms.";

                //string caseStudyPrompt =
                //    $"Write a short clinical vignette (1 para) based on these page(s), including patient details and a 1 MCQ at the end. " +
                //    $"Use {generalLangName}.";

                //string keywordsPrompt =
                //    $"List the HIGH-YIELD KEYWORDS (comma-separated) from these page(s) in {generalLangName}.";


                // ─── Build each prompt with a little conditional text:
                // 3.1) Definitions prompt
                //Best Version
                string definitionsPrompt =
                    $"In {generalLangName}, provide concise DEFINITIONS for each key " +
                    $"{(isMedical ? "medical " : "")}term found on these page(s). " +
                    $"For each term, output exactly:\n\n" +
                    $"- Term: <the term as a heading>\n" +
                    $"- Definition: <a 1–2-3 sentence definition in {generalLangName}>\n\n" +
                    $"Separate entries with a blank line.  Do NOT number anything.";

                // 3.2) MCQs prompt
                //Best Version
                string mcqsPrompt =
                    //$"Generate 5 MULTIPLE‐CHOICE QUESTIONS in {generalLangName} " +
                    $"Generate MULTIPLE‐CHOICE QUESTIONS in {generalLangName} " +
                    $"based strictly on the content of these page(s).  Follow this pattern exactly (no deviations):\n\n" +
                    $"Question: <Write the question here in {generalLangName}>\n" +
                    $"A) <Option A in {generalLangName}>\n" +
                    $"B) <Option B in {generalLangName}>\n" +
                    $"C) <Option C in {generalLangName}>\n" +
                    $"D) <Option D in {generalLangName}>\n" +
                    $"Answer: <Exactly one letter: A, B, C, or D>\n\n" +
                    $"Separate each MCQ block with a single blank line.  Do NOT include any extra text.";

                // 3.3) Flashcards prompt
                //Best Version
                string flashcardsPrompt =
                    $"Create FLASHCARDS in {generalLangName} for each key " +
                    $"{(isMedical ? "medical " : "")}term on these page(s).  Use this exact format (no deviations):\n\n" +
                    //$"Front: <Term in {generalLangName}>\n" +
                    $"Front: <Term>\n" +
                    $"Back:  <One- or two- or three- sentence definition in {generalLangName}>\n\n" +
                    $"Leave exactly one blank line between each card.  Do NOT number or bullet anything.";


                // 3.4) Vocabulary: translate into whichever “Vocab Language” the user chose
                //Best Version
                string vocabularyPrompt =
                    $"Extract IMPORTANT VOCABULARY TERMS from these page(s) and translate them into {vocabLangName}.  Use exactly this format (no bullets or numbering):\n\n" +
                    //$"EnglishTerm – {vocabLangName}Translation\n\n" +
                    $"OriginalTerm – {vocabLangName}Translation\n\n" +
                    $"Leave exactly one blank line between each entry.  If a term doesn’t have a direct translation, write “– [Translation Needed]”.";

                // 3.5) Summary prompt
                //Best Version
                string summaryPrompt =
                    //$"In {generalLangName}, write a concise SUMMARY (2–3 sentences) of the content on these page(s). " +
                    $"In {generalLangName}, write a concise SUMMARY (2–3-4-5-6-7-8-9-10 sentences) of the content on these page(s). " +
                    $"{(isMedical ? "Highlight key medical concepts; keep technical terms accurate." : "")}" +
                    $"\n\nFormat your summary as plain prose (no bullets or numbering).";

                // 3.6) Key Takeaways prompt
                //Best Version
                string takeawaysPrompt =
                    //$"List 5 KEY TAKEAWAYS (in {generalLangName}) from these page(s), formatted as bullets.  " +
                    $"List KEY TAKEAWAYS (in {generalLangName}) from these page(s), formatted as bullets.  " +
                    $"Each line must begin with a dash and a space, like:\n" +
                    $"- Takeaway 1\n" +
                    $"- Takeaway 2\n" +
                    $"…\n\n" +
                    $"{(isMedical ? "Include any critical medical terms and their context." : "")}";

                // 3.7) Fill-in-the-Blank (Cloze) prompt
                //Best Version
                string clozePrompt =
                    //$"Generate 5 FILL‐IN‐THE‐BLANK sentences (in {generalLangName}) based on these page(s).  " +
                    $"Generate FILL‐IN‐THE‐BLANK sentences (in {generalLangName}) based on these page(s).  " +
                    $"Each entry should consist of two lines:\n\n" +
                    $"Sentence:“_______________ is <brief clue>.”\n" +
                    $"Answer: <the correct word or phrase> (in {generalLangName}).\n\n" +
                    //$"For example:\nSentence: “_____[Pilocarpine]_____ is a miotic drug.”\nAnswer: Pilocarpine\n\n" +
                    $"For example:\nSentence: “_______________ is a miotic drug.”\nAnswer: Pilocarpine\n\n" +
                    $"Leave a single blank line between each pair.  Do NOT embed the answer inside the blank.";

                // 3.8) True/False Questions prompt
                //Best Version
                string trueFalsePrompt =
                    //$"Generate 5 TRUE/FALSE statements (in {generalLangName}) based on these page(s).  " +
                    $"Generate TRUE/FALSE statements (in {generalLangName}) based on these page(s).  " +
                    $"Each block should be two lines:\n\n" +
                    $"Statement: <write a true‐or‐false sentence here>\n" +
                    $"Answer: <True or False>\n\n" +
                    $"Leave exactly one blank line between each pair.  Do NOT write any additional explanation.";

                // 3.9) Outline prompt
                //Best Version
                string outlinePrompt =
                    $"Produce a hierarchical OUTLINE in {generalLangName} for the material on these page(s).  " +
                    $"Use numbered levels (e.g., “1. Main Heading,” “1.1 Subheading,” “1.1.1 Detail”).  " +
                    $"Do NOT use bullet points—strictly use decimal numbering.  " +
                    $"{(isMedical ? "Include medical subheadings where appropriate." : "")}";

                string conceptMapPrompt =
                    $"List the key CONCEPTS from these page(s) and show how they relate, in {generalLangName}.  " +
                    $"For each pair, use one of these formats exactly:\n" +
                    $"“ConceptA → relates to → ConceptB”\n" +
                    $"or\n" +
                    $"“ConceptA — contrasts with — ConceptC”\n\n" +
                    $"Separate each relationship on its own line.  Provide at least 5 relationships.";

                string tableExtractPrompt =
                    $"If these page(s) contain any tables (e.g., drug doses, side effects, lab values), " +
                    $"extract each table into a Markdown‐style table in {generalLangName}.  Use this exact format:\n\n" +
                    $"| Column1 | Column2 | Column3 |\n" +
                    $"|---------|---------|---------|\n" +
                    $"| data11  | data12  | data13  |\n" +
                    $"| data21  | data22  | data23  |\n\n" +
                    $"If no table is present, respond with exactly: “No table found.”";

                string simplifiedPrompt =
                    $"Explain the content of these page(s) in simpler language, as if teaching a first-year medical student.  " +
                    $"Use {generalLangName}.  Define any technical or medical jargon in parentheses the first time it appears.  " +
                    $"Write one cohesive paragraph—no bullets or lists.";

                string caseStudyPrompt =
                    $"Write a short CLINICAL VIGNETTE (1 paragraph) based on these page(s), in {generalLangName}.  " +
                    $"Include:\n" +
                    $"- Patient age and gender\n" +
                    $"- Presenting complaint or symptom\n" +
                    $"- Key pertinent findings (e.g., vital signs, lab results)\n\n" +
                    $"Then immediately follow with a single multiple-choice question (in {generalLangName}) about the most likely diagnosis or next step.  " +
                    $"Format exactly:\n" +
                    $"\nMCQ: <The question text>\n" +
                    $"A) <Option A>\n" +
                    $"B) <Option B>\n" +
                    $"C) <Option C>\n" +
                    $"D) <Option D>\n" +
                    $"Answer: <A, B, C, or D>\n\n" +
                    $"No extra commentary—only the vignette paragraph, blank line, then the MCQ block.";

                string keywordsPrompt =
                    $"List the HIGH-YIELD KEYWORDS from these page(s) in {generalLangName}.  " +
                    $"Output as a comma-separated list (e.g., “keyword1, keyword2, keyword3”).  " +
                    $"Do NOT include definitions—only the keywords themselves.  " +
                    $"Provide at least 8–10 keywords.";





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
                     && allKeywords == null)
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

                // 7.2) ملف MCQs (يمكن تكييف تنسيق MCQs إذا أردتم تنسيقًا أضبط)
                if (chkMCQs.Checked)
                {
                    string mcqsText = allMCQs.ToString();
                    SaveContentToFile(mcqsText, mcqsFilePath, "MCQs");
                }

                // 7.3) ملف Flashcards
                if (chkFlashcards.Checked)
                {
                    string flashcardsText = allFlashcards.ToString();
                    SaveContentToFile(flashcardsText, flashcardsFilePath, "Flashcards");
                }

                // 7.4) ملف Vocabulary (بعد تطبيق FormatVocabulary على الناتج)
                if (chkVocabulary.Checked)
                {
                    string vocabularyText = FormatVocabulary(allVocabulary.ToString());
                    SaveContentToFile(vocabularyText, vocabularyFilePath, "Vocabulary");
                }

                // ── New features:
                if (chkSummary.Checked)
                    SaveContentToFile(allSummary.ToString(), summaryFilePath, "Page Summaries");

                if (chkTakeaways.Checked)
                    SaveContentToFile(allTakeaways.ToString(), takeawaysFilePath, "Key Takeaways");

                if (chkCloze.Checked)
                    SaveContentToFile(allCloze.ToString(), clozeFilePath, "Fill-in-the-Blank (Cloze)");

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
                titlePara.Range.Font.Bold = 1;
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

    }
}