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
            var savedMode = Properties.Settings.Default.PageBatchMode; // 1, 2 or 3
            if (savedMode >= 1 && savedMode <= 3)
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


                // 3.1) prompt for Definitions in “GeneralLanguage” (e.g. user picks “French”)
                // 1) Read which “General Language” the user picked:
                string generalLangName = cmbGeneralLang.SelectedItem as string ?? "English";

                // 2) Read the “Medical Material” checkbox:
                bool isMedical = chkMedicalMaterial.Checked;

                // 3) Build each prompt with a little conditional text:
                string definitionsPrompt;
                if (isMedical)
                {
                    // When “Medical Material” is checked, ask specifically for medical definitions
                    definitionsPrompt =
                        $"Provide concise medical definitions (in {generalLangName}) for each key medical term on this page. " +
                        $"For each term, write:\n" +
                        $"- The term itself as a heading\n" +
                        $"- Then a one- or two-sentence definition in {generalLangName}\n\n" +
                        $"Separate every entry by a blank line, without numbering.";
                }
                else
                {
                    // When unchecked, just ask for normal (non-medical) definitions
                    definitionsPrompt =
                        $"Provide concise definitions (in {generalLangName}) for each key term on this page. " +
                        $"For each term, write:\n" +
                        $"- The term itself as a heading\n" +
                        $"- Then a one- or two-sentence definition in {generalLangName}\n\n" +
                        $"Separate every entry by a blank line, without numbering.";
                }


                // 3.2) MCQs prompt (this is typically language-only; leave it as-is or you can adjust similarly)
                string mcqsPrompt =
                    $"Generate multiple-choice questions (only in {generalLangName}) based on the content of this page. Use EXACTLY this format (no deviations):\n\n" +
                    $"Question: [Write the question in {generalLangName}]\n" +
                    $"A) [Option A in {generalLangName}]\n" +
                    $"B) [Option B in {generalLangName}]\n" +
                    $"C) [Option C in {generalLangName}]\n" +
                    $"D) [Option D in {generalLangName}]\n" +
                    $"Answer: [Correct Letter]\n\n" +
                    $"Separate each question block with a blank line.";


                // 3.3) Flashcards prompt: toggle “medical” vocabulary vs. general vocabulary
                string flashcardsPrompt;
                if (isMedical)
                {
                    flashcardsPrompt =
                        $"Create medical flashcards in {generalLangName} for each key medical or pharmaceutical term on this page. " +
                        $"Use EXACTLY this format (no deviations):\n\n" +
                        $"Front: [Term in {generalLangName}]\n" +
                        $"Back:  [Definition in {generalLangName}]\n\n" +
                        $"Leave exactly one blank line between each card.";
                }
                else
                {
                    flashcardsPrompt =
                        $"Create flashcards in {generalLangName} for each key term on this page. " +
                        $"Use EXACTLY this format (no deviations):\n\n" +
                        $"Front: [Term in {generalLangName}]\n" +
                        $"Back:  [Definition in {generalLangName}]\n\n" +
                        $"Leave exactly one blank line between each card.";
                }


                // 3.4) Vocabulary: translate into whichever “Vocab Language” the user chose
                string vocabLangName = cmbVocabLang.SelectedItem as string ?? "Arabic";
                string vocabularyPrompt =
                    $"Extract important vocabulary terms from this page and translate them to {vocabLangName}. " +
                    $"Use EXACTLY this format (no bullets, no numbering):\n\n" +
                    $"EnglishTerm – {vocabLangName}Translation\n\n" +
                    $"Leave exactly one blank line between each entry.";



                // 4) استخراج صور كل الصفحات المحددة في الواجهة
                var allPages = ConvertPdfToImages(filePath);

                // 5) إنشاء StringBuilder لكل قسم من الأقسام الأربع
                // 5) Prepare StringBuilders for whichever sections are checked
                StringBuilder allDefinitions = chkDefinitions.Checked ? new StringBuilder() : null;
                StringBuilder allMCQs = chkMCQs.Checked ? new StringBuilder() : null;
                StringBuilder allFlashcards = chkFlashcards.Checked ? new StringBuilder() : null;
                StringBuilder allVocabulary = chkVocabulary.Checked ? new StringBuilder() : null;
                if (allDefinitions == null && allMCQs == null && allFlashcards == null && allVocabulary == null)
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
                

                //bool useThreePageMode = chkThreePageMode.Checked;
                int batchSize = (int)radioPageBatchSize.EditValue; // reads 1, 2 or 3


                //if (!useThreePageMode)
                //{
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

                            UpdateOverlayLog($"✅ Pages {startPage}–{endPage} done.");
                        }
                        break;


                    case 3:
                        //}
                        //else
                        //{

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

                            UpdateOverlayLog($"✅ Pages {startPage}–{endPage} done.");
                        }
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected batchSize: {batchSize}");
                } // end of batch size switch

                  //}

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
            var formattedDefinitions = new List<string>();
            var lines = text.Split('\n');

            foreach (var line in lines)
            {
                string cleanedLine = line.TrimStart('-', ' ');
                if (!string.IsNullOrWhiteSpace(cleanedLine))
                    formattedDefinitions.Add(cleanedLine);
            }
            return string.Join("\n\n", formattedDefinitions);
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