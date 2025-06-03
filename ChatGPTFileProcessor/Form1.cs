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
            comboBoxModel.Items.Add("gpt-4o"); // Add gpt-4o model

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
        }


        private void buttonSaveAPIKey_Click(object sender, EventArgs e)
        {
            string apiKey = textBoxAPIKey.Text.Trim();
            if (!string.IsNullOrEmpty(apiKey))
            {
                File.WriteAllText(apiKeyPath, apiKey);
                UpdateStatus("API Key saved successfully.");
            }
            else
            {
                UpdateStatus("API Key cannot be empty.");
            }
        }

        private void buttonEditAPIKey_Click(object sender, EventArgs e)
        {
            textBoxAPIKey.ReadOnly = false;  // Allow editing
            UpdateStatus("Editing API Key. Don't forget to save after changes.");
        }

        private void buttonClearAPIKey_Click(object sender, EventArgs e)
        {
            if (File.Exists(apiKeyPath))
            {
                File.Delete(apiKeyPath);
                textBoxAPIKey.Clear();
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
            string apiKey = textBoxAPIKey.Text;

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

                ShowOverlay("🔄 Processing, please wait...");
                UpdateOverlayLog("🚀 Starting GPT-4o multimodal processing...");

                // اسم النموذج والـ timestamp لإنشاء مسارات الملفات
                string modelName = comboBoxModel.SelectedItem?.ToString() ?? "gpt-4o";
                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string basePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                // Prepare file‐paths
                // مسارات ملفات التعاريف و MCQs و Flashcards و Vocabulary
                string definitionsFilePath = Path.Combine(basePath, $"Definitions_{modelName}_{timeStamp}.docx");
                string mcqsFilePath = Path.Combine(basePath, $"MCQs_{modelName}_{timeStamp}.docx");
                string flashcardsFilePath = Path.Combine(basePath, $"Flashcards_{modelName}_{timeStamp}.docx");
                string vocabularyFilePath = Path.Combine(basePath, $"Vocabulary_{modelName}_{timeStamp}.docx");

                //// 3) إعداد الـ prompts لكل قسم
                //string definitionsPrompt =
                //    "Provide concise definitions (in English only) for each key medical term on this page. " +
                //    "For each term, write:\n" +
                //    "- The term itself as a heading\n" +
                //    "- Then a one- or two-sentence definition in English\n\n" +
                //    "Separate every entry by a blank line, without numbering.";

                //// 3.2) prompt الأسئلة (MCQs) بالكامل بالإنجليزية:
                //string mcqsPrompt =
                //    "Generate multiple-choice questions (only in English) based on the content of this page. Use EXACTLY this format (no deviations):\n\n" +
                //    "Question: [Write the question in English]\n" +
                //    "A) [Option A]\n" +
                //    "B) [Option B]\n" +
                //    "C) [Option C]\n" +
                //    "D) [Option D]\n" +
                //    "Answer: [Correct Letter]\n\n" +
                //    "Separate each question block with a blank line.";

                //// 3.3) prompt البطاقات (Flashcards) بالكامل بالإنجليزية:
                //string flashcardsPrompt =
                //    "Create flashcards in English for each key medical or pharmaceutical term on this page. " +
                //    "Use EXACTLY this format (no deviations):\n\n" +
                //    "Front: [Term]\n" +
                //    "Back:  [Definition in English]\n\n" +
                //    "Leave exactly one blank line between each card.";

                //// 3.4) prompt المفردات (Vocabulary) ثنائي اللغة (إنجليزي–عربي):
                //string vocabularyPrompt =
                //    "Extract important vocabulary terms from this page and translate them to Arabic. " +
                //    "Use EXACTLY this format (no bullets, no numbering):\n\n" +
                //    "EnglishTerm – ArabicTranslation\n\n" +
                //    "Leave exactly one blank line between each entry.";

                // 3.1) prompt for Definitions in “GeneralLanguage” (e.g. user picks “French”)
                string generalLangName = cmbGeneralLang.SelectedItem as string; // e.g. “French”
                string definitionsPrompt =
                    $"Provide concise definitions (in {generalLangName}) " +
                    $"for each key medical term on this page. " +
                    $"For each term, write:\n" +
                    $"- The term itself as a heading\n" +
                    $"- Then a one- or two-sentence definition in {generalLangName}\n\n" +
                    $"Separate every entry by a blank line, without numbering.";

                // 3.2) prompt MCQs in “GeneralLanguage”
                string mcqsPrompt =
                    $"Generate multiple‐choice questions (only in {generalLangName}) based on the content of this page. " +
                    $"Use EXACTLY this format (no deviations):\n\n" +
                    $"Question: [Write the question in {generalLangName}]\n" +
                    $"A) [Option A in {generalLangName}]\n" +
                    $"B) [Option B in {generalLangName}]\n" +
                    $"C) [Option C in {generalLangName}]\n" +
                    $"D) [Option D in {generalLangName}]\n" +
                    $"Answer: [Correct Letter]\n\n" +
                    $"Separate each question block with a blank line.";

                // 3.3) prompt Flashcards in “GeneralLanguage”
                string flashcardsPrompt =
                    $"Create flashcards in {generalLangName} for each key medical or pharmaceutical term on this page. " +
                    $"Use EXACTLY this format (no deviations):\n\n" +
                    $"Front: [Term in {generalLangName}]\n" +
                    $"Back:  [Definition in {generalLangName}]\n\n" +
                    $"Leave exactly one blank line between each card.";

                // 3.4) prompt Vocabulary in “VocabLanguage” (target language)
                string vocabLangName = cmbVocabLang.SelectedItem as string; // e.g. “French”
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
                    HideOverlay();
                    return;
                }


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
                textBoxAPIKey.Text = File.ReadAllText(apiKeyPath).Trim();
            }

            // Load selected model
            if (File.Exists(modelPath))
            {
                string savedModel = File.ReadAllText(modelPath).Trim();
                if (comboBoxModel.Items.Contains(savedModel))
                {
                    comboBoxModel.SelectedItem = savedModel;
                }
                else
                {
                    comboBoxModel.SelectedIndex = 0;  // Default to first item if model is not in options
                }
            }
            else
            {
                comboBoxModel.SelectedIndex = 0;  // Default if model file is missing
            }
        }

        private void SaveApiKeyAndModel()
        {
            EnsureConfigDirectoryExists();

            // Save API key
            string apiKey = textBoxAPIKey.Text.Trim();
            File.WriteAllText(apiKeyPath, apiKey);

            // Save selected model
            string selectedModel = comboBoxModel.SelectedItem?.ToString() ?? "gpt-3.5-turbo";
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





        private void developerProfileLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // Mark the link as visited
            developerProfileLinkLabel.LinkVisited = true;

            // Open the link in the default browser
            System.Diagnostics.Process.Start("https://github.com/MohammedTsmu/ChatGPTFileProcessor");
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



        //public async Task<string> SendImageToGPTAsync(Image image, string apiKey)
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
                            //var response = await http.PostAsync("https://api.openai.com/v1/chat/completions", content);
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
            //using var ms = new MemoryStream();
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



        private void InitializeOverlay()
        {
            overlayPanel = new Panel
            {
                Size = this.ClientSize,
                BackColor = Color.FromArgb(150, Color.Black),
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
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
        }

        private void chkDefinitions_CheckedChanged(object sender, EventArgs e)
        {
            // Save the state of the checkbox
            Properties.Settings.Default.GenerateDefinitions = chkDefinitions.Checked;
            Properties.Settings.Default.Save();
        }

        private void chkMCQs_CheckedChanged(object sender, EventArgs e)
        {
            // Save the state of the checkbox
            Properties.Settings.Default.GenerateMCQs = chkMCQs.Checked;
            Properties.Settings.Default.Save();
        }

        private void chkFlashcards_CheckedChanged(object sender, EventArgs e)
        {
            // Save the state of the checkbox
            Properties.Settings.Default.GenerateFlashcards = chkFlashcards.Checked;
            Properties.Settings.Default.Save();
        }

        private void chkVocabulary_CheckedChanged(object sender, EventArgs e)
        {
            // Save the state of the checkbox
            Properties.Settings.Default.GenerateVocabulary = chkVocabulary.Checked;
            Properties.Settings.Default.Save();
        }

        //private readonly (string Code, string DisplayName)[] _supportedLanguages = new[]
        //{
        //    // Favorites first
        //    ("en", "English"),
        //    ("ar", "Arabic"),

        //    // All other ISO 639-1 codes (excluding "he")
        //    ("aa", "Afar"),
        //    ("ab", "Abkhazian"),
        //    ("ae", "Avestan"),
        //    ("af", "Afrikaans"),
        //    ("ak", "Akan"),
        //    ("am", "Amharic"),
        //    ("an", "Aragonese"),
        //    ("ar", "Arabic"),            // already listed among favorites
        //    ("as", "Assamese"),
        //    ("av", "Avaric"),
        //    ("ay", "Aymara"),
        //    ("az", "Azerbaijani"),
        //    ("ba", "Bashkir"),
        //    ("be", "Belarusian"),
        //    ("bg", "Bulgarian"),
        //    ("bi", "Bislama"),
        //    ("bm", "Bambara"),
        //    ("bn", "Bengali"),
        //    ("bo", "Tibetan"),
        //    ("br", "Breton"),
        //    ("bs", "Bosnian"),
        //    ("ca", "Catalan"),
        //    ("ce", "Chechen"),
        //    ("ch", "Chamorro"),
        //    ("co", "Corsican"),
        //    ("cr", "Cree"),
        //    ("cs", "Czech"),
        //    ("cu", "Church Slavic"),
        //    ("cv", "Chuvash"),
        //    ("cy", "Welsh"),
        //    ("da", "Danish"),
        //    ("de", "German"),
        //    ("dv", "Divehi"),
        //    ("dz", "Dzongkha"),
        //    ("ee", "Ewe"),
        //    ("el", "Greek"),
        //    ("en", "English"),           // duplicate of favorite; you may omit if desired
        //    ("eo", "Esperanto"),
        //    ("es", "Spanish"),
        //    ("et", "Estonian"),
        //    ("eu", "Basque"),
        //    ("fa", "Persian"),
        //    ("ff", "Fulah"),
        //    ("fi", "Finnish"),
        //    ("fj", "Fijian"),
        //    ("fo", "Faroese"),
        //    ("fr", "French"),
        //    ("fy", "Western Frisian"),
        //    ("ga", "Irish"),
        //    ("gd", "Scottish Gaelic"),
        //    ("gl", "Galician"),
        //    ("gn", "Guarani"),
        //    ("gu", "Gujarati"),
        //    ("gv", "Manx"),
        //    ("ha", "Hausa"),
        //    // ("he", "Hebrew"),         // deliberately omitted per request
        //    ("hi", "Hindi"),
        //    ("ho", "Hiri Motu"),
        //    ("hr", "Croatian"),
        //    ("ht", "Haitian Creole"),
        //    ("hu", "Hungarian"),
        //    ("hy", "Armenian"),
        //    ("hz", "Herero"),
        //    ("ia", "Interlingua"),
        //    ("id", "Indonesian"),
        //    ("ie", "Interlingue"),
        //    ("ig", "Igbo"),
        //    ("ii", "Sichuan Yi"),
        //    ("ik", "Inupiaq"),
        //    // ("in", "Indonesian (deprecated)"),  // omitted because 'id' is now preferred
        //    ("io", "Ido"),
        //    ("is", "Icelandic"),
        //    ("it", "Italian"),
        //    ("iu", "Inuktitut"),
        //    ("ja", "Japanese"),
        //    ("jv", "Javanese"),
        //    ("ka", "Georgian"),
        //    ("kg", "Kongo"),
        //    ("ki", "Kikuyu"),
        //    ("kj", "Kuanyama"),
        //    ("kk", "Kazakh"),
        //    ("kl", "Kalaallisut"),
        //    ("km", "Khmer"),
        //    ("kn", "Kannada"),
        //    ("ko", "Korean"),
        //    ("kr", "Kanuri"),
        //    ("ks", "Kashmiri"),
        //    ("ku", "Kurdish"),
        //    ("kv", "Komi"),
        //    ("kw", "Cornish"),
        //    ("ky", "Kirghiz"),
        //    ("la", "Latin"),
        //    ("lb", "Luxembourgish"),
        //    ("lg", "Ganda"),
        //    ("li", "Limburgish"),
        //    ("ln", "Lingala"),
        //    ("lo", "Lao"),
        //    ("lt", "Lithuanian"),
        //    ("lu", "Luba-Katanga"),
        //    ("lv", "Latvian"),
        //    ("mg", "Malagasy"),
        //    ("mh", "Marshallese"),
        //    ("mi", "Māori"),
        //    ("mk", "Macedonian"),
        //    ("ml", "Malayalam"),
        //    ("mn", "Mongolian"),
        //    ("mr", "Marathi"),
        //    ("ms", "Malay"),
        //    ("mt", "Maltese"),
        //    ("my", "Burmese"),
        //    ("na", "Nauru"),
        //    ("nb", "Norwegian Bokmål"),
        //    ("nd", "Ndebele, North"),
        //    ("ne", "Nepali"),
        //    ("ng", "Ndonga"),
        //    ("nl", "Dutch"),
        //    ("nn", "Norwegian Nynorsk"),
        //    ("no", "Norwegian"),
        //    ("nr", "Ndebele, South"),
        //    ("nv", "Navajo"),
        //    ("ny", "Chichewa"),
        //    ("oc", "Occitan"),
        //    ("oj", "Ojibwe"),
        //    ("om", "Oromo"),
        //    ("or", "Odia"),
        //    ("os", "Ossetian"),
        //    ("pa", "Punjabi"),
        //    ("pi", "Pāli"),
        //    ("pl", "Polish"),
        //    ("ps", "Pashto"),
        //    ("pt", "Portuguese"),
        //    ("qu", "Quechua"),
        //    ("rm", "Romansh"),
        //    ("rn", "Kirundi"),
        //    ("ro", "Romanian"),
        //    ("ru", "Russian"),
        //    ("rw", "Kinyarwanda"),
        //    ("sa", "Sanskrit"),
        //    ("sc", "Sardinian"),
        //    ("sd", "Sindhi"),
        //    ("se", "Northern Sami"),
        //    ("sg", "Sango"),
        //    ("si", "Sinhala"),
        //    ("sk", "Slovak"),
        //    ("sl", "Slovenian"),
        //    ("sm", "Samoan"),
        //    ("sn", "Shona"),
        //    ("so", "Somali"),
        //    ("sq", "Albanian"),
        //    ("sr", "Serbian"),
        //    ("ss", "Swati"),
        //    ("st", "Sotho, Southern"),
        //    ("su", "Sundanese"),
        //    ("sv", "Swedish"),
        //    ("sw", "Swahili"),
        //    ("ta", "Tamil"),
        //    ("te", "Telugu"),
        //    ("tg", "Tajik"),
        //    ("th", "Thai"),
        //    ("ti", "Tigrinya"),
        //    ("tk", "Turkmen"),
        //    ("tl", "Tagalog"),
        //    ("tn", "Tswana"),
        //    ("to", "Tonga (Tonga Islands)"),
        //    ("tr", "Turkish"),
        //    ("ts", "Tsonga"),
        //    ("tt", "Tatar"),
        //    ("tw", "Twi"),
        //    ("ty", "Tahitian"),
        //    ("ug", "Uyghur"),
        //    ("uk", "Ukrainian"),
        //    ("ur", "Urdu"),
        //    ("uz", "Uzbek"),
        //    ("ve", "Venda"),
        //    ("vi", "Vietnamese"),
        //    ("vo", "Volapük"),
        //    ("wa", "Walloon"),
        //    ("wo", "Wolof"),
        //    ("xh", "Xhosa"),
        //    ("yi", "Yiddish"),
        //    ("yo", "Yoruba"),
        //    ("za", "Zhuang"),
        //    ("zh", "Chinese"),
        //    ("zu", "Zulu")
        //};

        //private readonly (string Code, string DisplayName)[] _supportedLanguages = new[]
        //{
        //    // Favorites (native name first, then English)
        //    ("en",    "English — English"),
        //    ("ar",    "العربية — Arabic"),

        //    // All other ISO 639-1 codes (excluding "he")
        //    ("aa",    "Afaraf — Afar"),
        //    ("ab",    "Аҧсны бызшәа — Abkhazian"),
        //    ("ae",    "avesta — Avestan"),
        //    ("af",    "Afrikaans — Afrikaans"),
        //    ("ak",    "Akan — Akan"),
        //    ("am",    "አማርኛ — Amharic"),
        //    ("an",    "aragonés — Aragonese"),
        //    ("as",    "অসমীয়া — Assamese"),
        //    ("av",    "авар мац — Avaric"),
        //    ("ay",    "aymar aru — Aymara"),
        //    ("az",    "azərbaycan — Azerbaijani"),
        //    ("ba",    "башҡорт — Bashkir"),
        //    ("be",    "беларуская — Belarusian"),
        //    ("bg",    "български — Bulgarian"),
        //    ("bi",    "Bislama — Bislama"),
        //    ("bm",    "Bamanankan — Bambara"),
        //    ("bn",    "বাংলা — Bengali"),
        //    ("bo",    "བོད་སྐད — Tibetan"),
        //    ("br",    "brezhoneg — Breton"),
        //    ("bs",    "bosanski — Bosnian"),
        //    ("ca",    "Català — Catalan"),
        //    ("ce",    "нохчийн — Chechen"),
        //    ("ch",    "Chamoru — Chamorro"),
        //    ("co",    "corsu — Corsican"),
        //    ("cr",    "ᓀᐦᐃᔭᐍᐏᐣ — Cree"),
        //    ("cs",    "čeština — Czech"),
        //    ("cu",    "ѩзыкъ словѣньскъ — Church Slavic"),
        //    ("cv",    "чӑваш — Chuvash"),
        //    ("cy",    "Cymraeg — Welsh"),
        //    ("da",    "Dansk — Danish"),
        //    ("de",    "Deutsch — German"),
        //    ("dv",    "ދިވެހިބަސް — Divehi"),
        //    ("dz",    "རྫོང་ཁ — Dzongkha"),
        //    ("ee",    "Ɛʋɛ — Ewe"),
        //    ("el",    "Ελληνικά — Greek"),
        //    ("eo",    "Esperanto — Esperanto"),
        //    ("es",    "Español — Spanish"),
        //    ("et",    "eesti — Estonian"),
        //    ("eu",    "euskara — Basque"),
        //    ("fa",    "فارسی — Persian"),
        //    ("ff",    "Fulfulde — Fulah"),
        //    ("fi",    "suomi — Finnish"),
        //    ("fj",    "vosa Vakaviti — Fijian"),
        //    ("fo",    "føroyskt — Faroese"),
        //    ("fr",    "Français — French"),
        //    ("fy",    "Frysk — Western Frisian"),
        //    ("ga",    "Gaeilge — Irish"),
        //    ("gd",    "Gàidhlig — Scottish Gaelic"),
        //    ("gl",    "Galego — Galician"),
        //    ("gn",    "Avañe'ẽ — Guarani"),
        //    ("gu",    "ગુજરાતી — Gujarati"),
        //    ("gv",    "Gaelg — Manx"),
        //    ("ha",    "Hausa — Hausa"),
        //    ("hi",    "हिन्दी — Hindi"),
        //    ("ho",    "Hiri Motu — Hiri Motu"),
        //    ("hr",    "Hrvatski — Croatian"),
        //    ("ht",    "Kreyòl ayisyen — Haitian Creole"),
        //    ("hu",    "Magyar — Hungarian"),
        //    ("hy",    "Հայերեն — Armenian"),
        //    ("hz",    "Otjiherero — Herero"),
        //    ("ia",    "Interlingua — Interlingua"),
        //    ("id",    "Bahasa Indonesia — Indonesian"),
        //    ("ie",    "Interlingue — Interlingue"),
        //    ("ig",    "Igbo — Igbo"),
        //    ("ii",    "ꆆꉙ — Sichuan Yi"),
        //    ("ik",    "Iñupiaq — Inupiaq"),
        //    ("io",    "Ido — Ido"),
        //    ("is",    "Íslenska — Icelandic"),
        //    ("it",    "Italiano — Italian"),
        //    ("iu",    "ᐃᓄᒃᑎᑐᑦ — Inuktitut"),
        //    ("ja",    "日本語 — Japanese"),
        //    ("jv",    "Basa Jawa — Javanese"),
        //    ("ka",    "ქართული — Georgian"),
        //    ("kg",    "Kikongo — Kongo"),
        //    ("ki",    "Gĩkũyũ — Kikuyu"),
        //    ("kj",    "Kuanyama — Kuanyama"),
        //    ("kk",    "қазақ тілі — Kazakh"),
        //    ("kl",    "kalaallisut — Kalaallisut"),
        //    ("km",    "ភាសាខ្មែរ — Khmer"),
        //    ("kn",    "ಕನ್ನಡ — Kannada"),
        //    ("ko",    "한국어 — Korean"),
        //    ("kr",    "Kanuri — Kanuri"),
        //    ("ks",    "کٲشُر — Kashmiri"),
        //    ("ku",    "Kurdî — Kurdish"),
        //    ("kv",    "коми кыв — Komi"),
        //    ("kw",    "Kernewek — Cornish"),
        //    ("ky",    "Кыргызча — Kirghiz"),
        //    ("la",    "latine — Latin"),
        //    ("lb",    "Lëtzebuergesch — Luxembourgish"),
        //    ("lg",    "Luganda — Ganda"),
        //    ("li",    "Limburgs — Limburgish"),
        //    ("ln",    "Lingála — Lingala"),
        //    ("lo",    "ລາວ — Lao"),
        //    ("lt",    "lietuvių — Lithuanian"),
        //    ("lu",    "Tshiluba — Luba-Katanga"),
        //    ("lv",    "latviešu — Latvian"),
        //    ("mg",    "Malagasy — Malagasy"),
        //    ("mh",    "Kajin M̧ajeļ — Marshallese"),
        //    ("mi",    "te reo Māori — Māori"),
        //    ("mk",    "македонски — Macedonian"),
        //    ("ml",    "മലയാളം — Malayalam"),
        //    ("mn",    "монгол — Mongolian"),
        //    ("mr",    "मराठी — Marathi"),
        //    ("ms",    "Bahasa Melayu — Malay"),
        //    ("mt",    "Malti — Maltese"),
        //    ("my",    "မြန်မာ — Burmese"),
        //    ("na",    "Ekakairũ Naoero — Nauru"),
        //    ("nb",    "bokmål — Norwegian Bokmål"),
        //    ("nd",    "isiNdebele — Ndebele, North"),
        //    ("ne",    "नेपाली — Nepali"),
        //    ("ng",    "Owambo — Ndonga"),
        //    ("nl",    "Nederlands — Dutch"),
        //    ("nn",    "nynorsk — Norwegian Nynorsk"),
        //    ("no",    "Norsk — Norwegian"),
        //    ("nr",    "isiNdebele — Ndebele, South"),
        //    ("nv",    "Diné bizaad — Navajo"),
        //    ("ny",    "chiCheŵa — Chichewa"),
        //    ("oc",    "occitan — Occitan"),
        //    ("oj",    "ᐊᓂᔑᓈᐯᒧᐎᓐ — Ojibwe"),
        //    ("om",    "Oromoo — Oromo"),
        //    ("or",    "ଓଡ଼ିଆ — Odia"),
        //    ("os",    "ирон — Ossetian"),
        //    ("pa",    "ਪੰਜਾਬੀ — Punjabi"),
        //    ("pi",    "पाऴि — Pāli"),
        //    ("pl",    "Polski — Polish"),
        //    ("ps",    "پښتو — Pashto"),
        //    ("pt",    "Português — Portuguese"),
        //    ("qu",    "Runa Simi — Quechua"),
        //    ("rm",    "Rumantsch — Romansh"),
        //    ("rn",    "Kirundi — Kirundi"),
        //    ("ro",    "Română — Romanian"),
        //    ("ru",    "Русский — Russian"),
        //    ("rw",    "Kinyarwanda — Kinyarwanda"),
        //    ("sa",    "संस्कृतम् — Sanskrit"),
        //    ("sc",    "sardu — Sardinian"),
        //    ("sd",    "सिन्धी — Sindhi"),
        //    ("se",    "Davvisámegiella — Northern Sami"),
        //    ("sg",    "Sängö — Sango"),
        //    ("si",    "සිංහල — Sinhala"),
        //    ("sk",    "slovenčina — Slovak"),
        //    ("sl",    "slovenščina — Slovenian"),
        //    ("sm",    "gagana fa'a Samoa — Samoan"),
        //    ("sn",    "chiShona — Shona"),
        //    ("so",    "Soomaaliga — Somali"),
        //    ("sq",    "Shqip — Albanian"),
        //    ("sr",    "српски — Serbian"),
        //    ("ss",    "SiSwati — Swati"),
        //    ("st",    "Sesotho — Sotho, Southern"),
        //    ("su",    "Basa Sunda — Sundanese"),
        //    ("sv",    "Svenska — Swedish"),
        //    ("sw",    "Kiswahili — Swahili"),
        //    ("ta",    "தமிழ் — Tamil"),
        //    ("te",    "తెలుగు — Telugu"),
        //    ("tg",    "Тоҷикӣ — Tajik"),
        //    ("th",    "ไทย — Thai"),
        //    ("ti",    "ትግርኛ — Tigrinya"),
        //    ("tk",    "Türkmen — Turkmen"),
        //    ("tl",    "Wikang Tagalog — Tagalog"),
        //    ("tn",    "Setswana — Tswana"),
        //    ("to",    "faka Tonga — Tonga (Tonga Islands)"),
        //    ("tr",    "Türkçe — Turkish"),
        //    ("ts",    "Xitsonga — Tsonga"),
        //    ("tt",    "татарча — Tatar"),
        //    ("tw",    "Twi — Twi"),
        //    ("ty",    "Reo Tahiti — Tahitian"),
        //    ("ug",    "ئۇيغۇرچە — Uyghur"),
        //    ("uk",    "Українська — Ukrainian"),
        //    ("ur",    "اردو — Urdu"),
        //    ("uz",    "Oʻzbek — Uzbek"),
        //    ("ve",    "Tshivenda — Venda"),
        //    ("vo",    "Volapük — Volapük"),
        //    ("wa",    "walon — Walloon"),
        //    ("wo",    "Wollof — Wolof"),
        //    ("xh",    "isiXhosa — Xhosa"),
        //    ("yi",    "ייִדיש — Yiddish"),
        //    ("yo",    "Yorùbá — Yoruba"),
        //    ("za",    "Saɯ cueŋƅ — Zhuang"),
        //    ("zu",    "isiZulu — Zulu")
        //    // …add any others you want…
        //};

        //private readonly (string Code, string DisplayName)[] _supportedLanguages = new[]
        //{
        //    // Favorites (always appear at the top)
        //    ("en", "English — English"),
        //    ("ar", "العربية — Arabic"),

        //    //---------------------------------------------------------------------
        //    // All other languages (sorted alphabetically by English name)
        //    //---------------------------------------------------------------------

        //    ("aa",    "Afaraf — Afar"),
        //    ("af",    "Afrikaans — Afrikaans"),
        //    ("ak",    "Akan — Akan"),
        //    ("sq",    "Shqip — Albanian"),
        //    ("am",    "አማርኛ — Amharic"),
        //    ("ar",    "العربية — Arabic"),            // Duplicate of favorite (can safely be removed if desired)
        //    ("an",    "aragonés — Aragonese"),
        //    ("hy",    "Հայերեն — Armenian"),
        //    ("as",    "অসমীয়া — Assamese"),
        //    ("av",    "авар мац — Avaric"),
        //    ("ae",    "avesta — Avestan"),
        //    ("ay",    "aymar aru — Aymara"),
        //    ("az",    "azərbaycan — Azerbaijani"),
        //    ("bm",    "Bamanankan — Bambara"),
        //    ("eu",    "euskara — Basque"),
        //    ("be",    "беларуская — Belarusian"),
        //    ("bn",    "বাংলা — Bengali"),
        //    ("bg",    "български — Bulgarian"),
        //    ("bs",    "bosanski — Bosnian"),
        //    ("br",    "brezhoneg — Breton"),
        //    ("ca",    "Català — Catalan"),
        //    ("ny",    "chiCheŵa — Chichewa"),
        //    ("zh",    "中文 — Chinese"),
        //    ("zh-CN", "中文（简体）— Chinese (Simplified)"),
        //    ("zh-TW", "中文（繁體）— Chinese (Traditional)"),
        //    ("zh-HK", "中文（香港）— Chinese (Hong Kong)"),
        //    ("zh-SG", "中文（新加坡）— Chinese (Singapore)"),
        //    ("co",    "corsu — Corsican"),
        //    ("hr",    "Hrvatski — Croatian"),
        //    ("cs",    "čeština — Czech"),
        //    ("da",    "Dansk — Danish"),
        //    ("dv",    "ދިވެހިބަސް — Divehi"),
        //    ("nl",    "Nederlands — Dutch"),
        //    ("nl-BE", "Nederlands (België) — Dutch (Belgium)"),
        //    ("en-GB", "English (United Kingdom) — English"),
        //    ("en-US", "English (United States) — English"),
        //    ("en-AU", "English (Australia) — English"),
        //    ("eo",    "Esperanto — Esperanto"),
        //    ("et",    "eesti — Estonian"),
        //    ("fo",    "føroyskt — Faroese"),
        //    ("fj",    "vosa Vakaviti — Fijian"),
        //    ("fi",    "suomi — Finnish"),
        //    ("fi-FI", "suomi (Suomi) — Finnish (Finland)"),
        //    ("fr",    "Français — French"),
        //    ("fr-FR", "Français (France) — French (France)"),
        //    ("fr-CA", "Français (Canada) — French (Canada)"),
        //    ("fr-CH", "Français (Suisse) — French (Switzerland)"),
        //    ("fy",    "Frysk — Western Frisian"),
        //    ("gl",    "Galego — Galician"),
        //    ("ka",    "ქართული — Georgian"),
        //    ("de",    "Deutsch — German"),
        //    ("de-AT", "Deutsch (Österreich) — German (Austria)"),
        //    ("de-CH", "Deutsch (Schweiz) — German (Switzerland)"),
        //    ("el",    "Ελληνικά — Greek"),
        //    ("gn",    "Avañe'ẽ — Guarani"),
        //    ("gu",    "ગુજરાતી — Gujarati"),
        //    ("gu-IN", "ગુજરાતી (ભારત) — Gujarati (India)"),
        //    ("ht",    "Kreyòl ayisyen — Haitian Creole"),
        //    ("ha",    "Hausa — Hausa"),
        //    //("he",    "—"), // deliberately omitted—Hebrew is excluded
        //    ("hz",    "Otjiherero — Herero"),
        //    ("hi",    "हिन्दी — Hindi"),
        //    ("hi-IN", "हिन्दी (भारत) — Hindi (India)"),
        //    ("ho",    "Hiri Motu — Hiri Motu"),
        //    ("hu",    "Magyar — Hungarian"),
        //    ("ia",    "Interlingua — Interlingua"),
        //    ("id",    "Bahasa Indonesia — Indonesian"),
        //    ("id-ID", "Bahasa Indonesia (Indonesia) — Indonesian (Indonesia)"),
        //    ("ie",    "Interlingue — Interlingue"),
        //    ("ga",    "Gaeilge — Irish"),
        //    ("is",    "Íslenska — Icelandic"),
        //    ("it",    "Italiano — Italian"),
        //    ("ja",    "日本語 — Japanese"),
        //    ("ja-JP", "日本語 (日本) — Japanese (Japan)"),
        //    ("jv",    "Basa Jawa — Javanese"),
        //    ("kl",    "kalaallisut — Kalaallisut"),
        //    ("kn",    "ಕನ್ನಡ — Kannada"),
        //    ("kn-IN", "ಕನ್ನಡ (ಭಾರತ) — Kannada (India)"),
        //    ("ks",    "کٲشُر — Kashmiri"),
        //    ("kk",    "қазақ тілі — Kazakh"),
        //    ("km",    "ភាសាខ្មែរ — Khmer"),
        //    ("km-KH", "ភាសាខ្មែរ (កម្ពុជា) — Khmer (Cambodia)"),
        //    ("ky",    "Кыргызча — Kirghiz"),
        //    ("rw",    "Kinyarwanda — Kinyarwanda"),
        //    ("rn",    "Kirundi — Kirundi"),
        //    ("ko",    "한국어 — Korean"),
        //    ("ko-KR", "한국어 (대한민국) — Korean (South Korea)"),
        //    ("ku",    "Kurdî — Kurdish"),
        //    ("lo",    "ລາວ — Lao"),
        //    ("lo-LA", "ລາວ (ລາວ) — Lao (Laos)"),
        //    ("la",    "latine — Latin"),
        //    ("lv",    "latviešu — Latvian"),
        //    ("lt",    "lietuvių — Lithuanian"),
        //    ("lb",    "Lëtzebuergesch — Luxembourgish"),
        //    ("mk",    "македонски — Macedonian"),
        //    ("mg",    "Malagasy — Malagasy"),
        //    ("ms",    "Bahasa Melayu — Malay"),
        //    ("ms-MY", "Bahasa Melayu (Malaysia) — Malay (Malaysia)"),
        //    ("ms-SG", "Bahasa Melayu (Singapura) — Malay (Singapore)"),
        //    ("mt",    "Malti — Maltese"),
        //    ("mi",    "te reo Māori — Māori"),
        //    ("mr",    "मराठी — Marathi"),
        //    ("mr-IN", "मराठी (भारत) — Marathi (India)"),
        //    ("mn",    "монгол — Mongolian"),
        //    ("na",    "Ekakairũ Naoero — Nauru"),
        //    ("nv",    "Diné bizaad — Navajo"),
        //    ("nd",    "isiNdebele — Ndebele, North"),
        //    ("nr",    "isiNdebele — Ndebele, South"),
        //    ("ng",    "Owambo — Ndonga"),
        //    ("ne",    "नेपाली — Nepali"),
        //    ("ne-NP", "नेपाली (नेपाल) — Nepali (Nepal)"),
        //    ("se",    "Davvisámegiella — Northern Sami"),
        //    ("no",    "Norsk — Norwegian"),
        //    ("nb",    "bokmål — Norwegian Bokmål"),
        //    ("nn",    "nynorsk — Norwegian Nynorsk"),
        //    ("oc",    "occitan — Occitan"),
        //    ("or",    "ଓଡ଼ିଆ — Odia"),
        //    ("or-IN", "ଓଡ଼ିଆ (ଭାରତ) — Odia (India)"),
        //    ("os",    "ирон — Ossetian"),
        //    ("ps",    "پښتو — Pashto"),
        //    ("fa",    "فارسی — Persian"),
        //    ("fa-IR", "فارسی (ایران) — Persian (Iran)"),
        //    ("fa-AF", "دری (افغانستان) — Dari (Afghanistan)"),
        //    ("pl",    "Polski — Polish"),
        //    ("pt",    "Português — Portuguese"),
        //    ("pt-BR", "Português (Brasil) — Portuguese (Brazil)"),
        //    ("pt-PT", "Português (Portugal) — Portuguese (Portugal)"),
        //    ("qu",    "Runa Simi — Quechua"),
        //    ("ro",    "Română — Romanian"),
        //    ("ru",    "Русский — Russian"),
        //    ("sm",    "gagana fa'a Samoa — Samoan"),
        //    ("sg",    "Sängö — Sango"),
        //    ("sa",    "संस्कृतम् — Sanskrit"),
        //    ("sc",    "sardu — Sardinian"),
        //    ("gd",    "Gàidhlig — Scottish Gaelic"),
        //    ("sr",    "српски — Serbian"),
        //    ("sh",    "Srpskohrvatski — Serbo-Croatian"),
        //    ("sn",    "chiShona — Shona"),
        //    ("sd",    "سندھی — Sindhi"),
        //    ("si",    "සිංහල — Sinhala"),
        //    ("si-LK", "සිංහල (ශ්‍රී ලංකාව) — Sinhala (Sri Lanka)"),
        //    ("sk",    "slovenčina — Slovak"),
        //    ("sl",    "slovenščina — Slovenian"),
        //    ("so",    "Soomaaliga — Somali"),
        //    ("st",    "Sesotho — Sotho, Southern"),
        //    ("es",    "Español — Spanish"),
        //    ("es-ES","Español (España) — Spanish (Spain)"),
        //    ("es-MX","Español (México) — Spanish (Mexico)"),
        //    ("es-AR","Español (Argentina) — Spanish (Argentina)"),
        //    ("es-CO","Español (Colombia) — Spanish (Colombia)"),
        //    ("su",    "Basa Sunda — Sundanese"),
        //    ("sw",    "Kiswahili — Swahili"),
        //    ("sw-KE", "Kiswahili (Kenya) — Swahili (Kenya)"),
        //    ("sw-TZ", "Kiswahili (Tanzania) — Swahili (Tanzania)"),
        //    ("sw-UG", "Kiswahili (Uganda) — Swahili (Uganda)"),
        //    ("sw-RW", "Kiswahili (Rwanda) — Swahili (Rwanda)"),
        //    ("sw-CD", "Kiswahili (Congo) — Swahili (Congo)"),
        //    ("sw-MZ", "Kiswahili (Mozambique) — Swahili (Mozambique)"),
        //    ("sw-ZA", "Kiswahili (South Africa) — Swahili (South Africa)"),
        //    ("sw-BW", "Kiswahili (Botswana) — Swahili (Botswana)"),
        //    ("sw-ZM", "Kiswahili (Zambia) — Swahili (Zambia)"),
        //    ("sw-SZ", "Kiswahili (Eswatini) — Swahili (Eswatini)"),
        //    ("sw-MW", "Kiswahili (Malawi) — Swahili (Malawi)"),
        //    ("sv",    "Svenska — Swedish"),
        //    ("sv-SE","Svenska (Sverige) — Swedish (Sweden)"),
        //    ("tl",    "Wikang Tagalog — Tagalog"),
        //    ("tl-PH", "Wikang Tagalog (Pilipinas) — Tagalog (Philippines)"),
        //    ("tg",    "Тоҷикӣ — Tajik"),
        //    ("ta",    "தமிழ் — Tamil"),
        //    ("ta-IN", "தமிழ் (இந்தியா) — Tamil (India)"),
        //    ("tt",    "татарча — Tatar"),
        //    ("te",    "తెలుగు — Telugu"),
        //    ("te-IN", "తెలుగు (భారత్) — Telugu (India)"),
        //    ("th",    "ไทย — Thai"),
        //    ("ti",    "ትግርኛ — Tigrinya"),
        //    ("to",    "faka Tonga — Tonga (Tonga Islands)"),
        //    ("tr",    "Türkçe — Turkish"),
        //    ("tk",    "Türkmen — Turkmen"),
        //    ("tw",    "Twi — Twi"),
        //    ("ug",    "ئۇيغۇرچە — Uyghur"),
        //    ("uk",    "Українська — Ukrainian"),
        //    ("ur",    "اردو — Urdu"),
        //    ("ur-IN","اردو (بھارت) — Urdu (India)"),
        //    ("ur-PK","اردو (پاکستان) — Urdu (Pakistan)"),
        //    ("ur-AE","اردو (متحدہ عرب امارات) — Urdu (United Arab Emirates)"),
        //    ("uz",    "Oʻzbek — Uzbek"),
        //    ("ve",    "Tshivenda — Venda"),
        //    ("vi",    "Tiếng Việt — Vietnamese"),
        //    ("vo",    "Volapük — Volapük"),
        //    ("wa",    "walon — Walloon"),
        //    ("wo",    "Wollof — Wolof"),
        //    ("xh",    "isiXhosa — Xhosa"),
        //    ("yi",    "ייִדיש — Yiddish"),
        //    ("yo",    "Yorùbá — Yoruba"),
        //    ("zu",    "isiZulu — Zulu"),
        //    ("za",    "Saɯ cueŋƅ — Zhuang")
        //};

        private readonly (string Code, string DisplayName)[] _supportedLanguages = new[]
        {
            // Favorites (always appear at the top)
            ("en", "English — English"),
            ("ar", "العربية — Arabic"),

            //---------------------------------------------------------------------
            // ChatGPT-supported languages (sorted alphabetically by English name)
            //---------------------------------------------------------------------

            ("sq", "Shqip — Albanian"),             // :contentReference[oaicite:0]{index=0}
            ("am", "አማርኛ — Amharic"),             // :contentReference[oaicite:1]{index=1}
            ("hy", "Հայերեն — Armenian"),            // :contentReference[oaicite:2]{index=2}
            ("bn", "বাংলা — Bengali"),               // :contentReference[oaicite:3]{index=3}
            ("bs", "bosanski — Bosnian"),            // :contentReference[oaicite:4]{index=4}
            ("bg", "български — Bulgarian"),         // :contentReference[oaicite:5]{index=5}
            ("my", "မြန်မာ — Burmese"),              // :contentReference[oaicite:6]{index=6}
            ("ca", "Català — Catalan"),              // :contentReference[oaicite:7]{index=7}
            ("zh", "中文 — Chinese"),                 // :contentReference[oaicite:8]{index=8}
            ("hr", "Hrvatski — Croatian"),           // :contentReference[oaicite:9]{index=9}
            ("cs", "čeština — Czech"),               // :contentReference[oaicite:10]{index=10}
            ("da", "Dansk — Danish"),                // :contentReference[oaicite:11]{index=11}
            ("nl", "Nederlands — Dutch"),            // :contentReference[oaicite:12]{index=12}
            ("et", "eesti — Estonian"),              // :contentReference[oaicite:13]{index=13}
            ("fi", "suomi — Finnish"),               // :contentReference[oaicite:14]{index=14}
            ("fr", "Français — French"),             // :contentReference[oaicite:15]{index=15}
            ("ka", "ქართული — Georgian"),            // :contentReference[oaicite:16]{index=16}
            ("de", "Deutsch — German"),              // :contentReference[oaicite:17]{index=17}
            ("el", "Ελληνικά — Greek"),              // :contentReference[oaicite:18]{index=18}
            ("gu", "ગુજરાતી — Gujarati"),             // :contentReference[oaicite:19]{index=19}
            ("hi", "हिन्दी — Hindi"),                // :contentReference[oaicite:20]{index=20}
            ("hu", "Magyar — Hungarian"),            // :contentReference[oaicite:21]{index=21}
            ("is", "Íslenska — Icelandic"),           // :contentReference[oaicite:22]{index=22}
            ("id", "Bahasa Indonesia — Indonesian"),  // :contentReference[oaicite:23]{index=23}
            ("it", "Italiano — Italian"),             // :contentReference[oaicite:24]{index=24}
            ("ja", "日本語 — Japanese"),               // :contentReference[oaicite:25]{index=25}
            ("kn", "ಕನ್ನಡ — Kannada"),               // :contentReference[oaicite:26]{index=26}
            ("kk", "қазақ тілі — Kazakh"),            // :contentReference[oaicite:27]{index=27}
            ("ko", "한국어 — Korean"),                 // :contentReference[oaicite:28]{index=28}
            ("lv", "latviešu — Latvian"),             // :contentReference[oaicite:29]{index=29}
            ("lt", "lietuvių — Lithuanian"),          // :contentReference[oaicite:30]{index=30}
            ("mk", "македонски — Macedonian"),         // :contentReference[oaicite:31]{index=31}
            ("ms", "Bahasa Melayu — Malay"),          // :contentReference[oaicite:32]{index=32}
            ("ml", "മലയാളം — Malayalam"),             // :contentReference[oaicite:33]{index=33}
            ("mr", "मराठी — Marathi"),                // :contentReference[oaicite:34]{index=34}
            ("mn", "монгол — Mongolian"),             // :contentReference[oaicite:35]{index=35}
            ("no", "Norsk — Norwegian"),              // :contentReference[oaicite:36]{index=36}
            ("fa", "فارسی — Persian"),                // :contentReference[oaicite:37]{index=37}
            ("pl", "Polski — Polish"),                // :contentReference[oaicite:38]{index=38}
            ("pt", "Português — Portuguese"),          // :contentReference[oaicite:39]{index=39}
            ("pa", "ਪੰਜਾਬੀ — Punjabi"),               // :contentReference[oaicite:40]{index=40}
            ("ro", "Română — Romanian"),              // :contentReference[oaicite:41]{index=41}
            ("ru", "Русский — Russian"),               // :contentReference[oaicite:42]{index=42}
            ("sr", "српски — Serbian"),                // :contentReference[oaicite:43]{index=43}
            ("sk", "slovenčina — Slovak"),             // :contentReference[oaicite:44]{index=44}
            ("sl", "slovenščina — Slovenian"),          // :contentReference[oaicite:45]{index=45}
            ("so", "Soomaaliga — Somali"),              // :contentReference[oaicite:46]{index=46}
            ("es", "Español — Spanish"),               // :contentReference[oaicite:47]{index=47}
            ("sw", "Kiswahili — Swahili"),             // :contentReference[oaicite:48]{index=48}
            ("sv", "Svenska — Swedish"),               // :contentReference[oaicite:49]{index=49}
            ("tl", "Wikang Tagalog — Tagalog"),         // :contentReference[oaicite:50]{index=50}
            ("ta", "தமிழ் — Tamil"),                    // :contentReference[oaicite:51]{index=51}
            ("te", "తెలుగు — Telugu"),                 // :contentReference[oaicite:52]{index=52}
            ("th", "ไทย — Thai"),                      // :contentReference[oaicite:53]{index=53}
            ("tr", "Türkçe — Turkish"),                  // :contentReference[oaicite:54]{index=54}
            ("uk", "Українська — Ukrainian"),            // :contentReference[oaicite:55]{index=55}
            ("ur", "اردو — Urdu"),                      // :contentReference[oaicite:56]{index=56}
            ("vi", "Tiếng Việt — Vietnamese")           // :contentReference[oaicite:57]{index=57}
        };





        //private void cmbGeneralLang_SelectedIndexChanged(object sender, EventArgs e)
        //{

        //}

        //private void cmbVocabLang_SelectedIndexChanged(object sender, EventArgs e)
        //{

        //}

        private void cmbGeneralLang_SelectedIndexChanged(object sender, EventArgs e)
        {
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
            var selectedDisplay = cmbVocabLang.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(selectedDisplay))
            {
                Properties.Settings.Default.VocabLanguage = selectedDisplay;
                Properties.Settings.Default.Save();
            }
        }

    }
}