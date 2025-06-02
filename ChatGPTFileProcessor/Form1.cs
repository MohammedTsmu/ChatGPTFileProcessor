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



        private readonly Dictionary<string, (int maxTokens, string prompt)> modelDetails = new Dictionary<string, (int, string)>
        {
            {
                "gpt-4o",
                (128000, "Analyze each page with this structure:\n\nDefinitions:\nTerm: Definition\n\nMCQs:\nQuestion?\n   A) Option 1\n   B) Option 2\n   C) Option 3\n   D) Option 4\n   Answer: Correct Option\n\nFlashcards:\nFront: Term\nBack: Definition\n\nVocabulary:\nEnglish Term - Arabic Translation\n\nNo numbering or bold. Use a blank line to separate each entry.")
            }

        };



        public Form1()
        {
            InitializeComponent();
            LoadAPIKey();  // Load API key on app start


        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBoxModel.Items.Add("gpt-4o"); // Add gpt-4o model

            InitializeOverlay();

            // Load API key and model selection
            LoadApiKeyAndModel();

        }


        private void LoadAPIKey()
        {
            if (File.Exists(apiKeyPath))
            {
                // Read the API key from the config file
                textBoxAPIKey.Text = File.ReadAllText(apiKeyPath);
                UpdateStatus("API Key loaded successfully.");
            }
            else
            {
                UpdateStatus("No API Key found. Please enter and save your API Key.");
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

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                MessageBox.Show("Please enter your API key.", "API Key Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (filePath == "No file selected" || !File.Exists(filePath))
            {
                MessageBox.Show("Please select a valid PDF file.", "File Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Disable buttons to prevent multiple clicks during processing
                buttonProcessFile.Enabled = false;
                buttonBrowseFile.Enabled = false;


                ShowOverlay("🔄 Processing, please wait...");
                UpdateOverlayLog("🚀 Starting GPT-4o vision processing...");

                string modelName = "gpt-4o";
                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string basePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                string definitionsFilePath = Path.Combine(basePath, $"Definitions_{modelName}_{timeStamp}.docx");
                string mcqsFilePath = Path.Combine(basePath, $"MCQs_{modelName}_{timeStamp}.docx");
                string flashcardsFilePath = Path.Combine(basePath, $"Flashcards_{modelName}_{timeStamp}.docx");
                string vocabularyFilePath = Path.Combine(basePath, $"Vocabulary_{modelName}_{timeStamp}.docx");

                UpdateStatus("⏳ Starting Vision-Based PDF Processing...");
                UpdateOverlayLog("⏳ Starting Vision-Based PDF Processing...");

                System.Windows.Forms.Application.DoEvents();

                string extractedContent = await ProcessPdfWithVision(filePath, apiKey);

                if (string.IsNullOrWhiteSpace(extractedContent))
                {
                    UpdateStatus("⚠️ No content was extracted. Please verify the file and API key.");
                    buttonProcessFile.Enabled = true;
                    buttonBrowseFile.Enabled = true;
                    HideOverlay();
                    return;
                }

                memoEditResult.Text = extractedContent;
                UpdateStatus("✅ Vision-based content extraction completed successfully.");
                UpdateOverlayLog("✅ Vision-based content extraction completed successfully.");

                UpdateStatus("⏳ Generating definitions...");
                UpdateOverlayLog("⏳ Generating definitions...");
                string definitions = await GenerateDefinitions(extractedContent, modelName);
                SaveContentToFile(FormatDefinitions(definitions), definitionsFilePath, "Definitions");

                UpdateStatus("⏳ Generating MCQs...");
                UpdateOverlayLog("⏳ Generating MCQs...");
                string mcqs = await GenerateMCQs(extractedContent, modelName);
                SaveContentToFile(mcqs, mcqsFilePath, "MCQs");

                UpdateStatus("⏳ Generating flashcards...");
                UpdateOverlayLog("⏳ Generating flashcards...");
                string flashcards = await GenerateFlashcards(extractedContent, modelName);
                SaveContentToFile(flashcards, flashcardsFilePath, "Flashcards");




                UpdateStatus("⏳ Generating vocabulary...");
                UpdateOverlayLog("⏳ Generating vocabulary...");
                string vocabulary = await GenerateVocabulary(extractedContent, modelName);
                SaveContentToFile(vocabulary, vocabularyFilePath, "Vocabulary");

                UpdateStatus("✅ All files processed and saved to desktop.");
                UpdateOverlayLog("✅ All files processed and saved to desktop.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Error: " + ex.Message, "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("❌ An error occurred during processing.");
                UpdateOverlayLog("❌ An error occurred during processing: " + ex.Message);
            }
            finally
            {

                // Re-enable buttons after processing is complete
                buttonProcessFile.Enabled = true;
                buttonBrowseFile.Enabled = true;

                HideOverlay(); // ✅ Hide it whether successful or failed
            }
        }




        // Function to split text into manageable chunks based on model token limits
        private List<string> SplitTextIntoChunks(string text, int maxTokens, int overlapTokens = 50)
        {
            var words = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var chunks = new List<string>();
            int totalWords = words.Length;
            int maxWords = (int)(maxTokens * 0.5); // Approximate words per chunk; adjust as needed

            for (int i = 0; i < totalWords; i += maxWords - overlapTokens)
            {
                var chunk = string.Join(" ", words.Skip(i).Take(maxWords));
                chunks.Add(chunk);
            }

            return chunks;
        }



        private readonly Dictionary<string, int> modelContextLimits = new Dictionary<string, int>
        {
            //Only this one works since it depends on images processing in gpt itself not local process to text or chunks
            {"gpt-4o", 128000 }
        };

        // Definitions Prompt
        // Definitions Prompt with Chunking
        private async Task<string> GenerateDefinitions(string content, string model)
        {
            //var maxTokens = modelDetails[model].maxTokens;
            if (!modelDetails.ContainsKey(model))
            {
                UpdateStatus($"❌ Model '{model}' not found in modelDetails. Falling back to gpt-3.5-turbo.");
                model = "gpt-3.5-turbo"; // fallback
            }
            var maxTokens = modelDetails[model].maxTokens;


            var chunks = SplitTextIntoChunks(content, maxTokens);
            StringBuilder definitionsResult = new StringBuilder();

            foreach (var chunk in chunks)
            {
                var generatedDefinition = await SendToChatGPT(chunk, model,
                    "Provide definitions for key terms in this text without numbering. Separate each definition with a blank line for clarity.");
                definitionsResult.AppendLine(generatedDefinition.Trim());
                definitionsResult.AppendLine("\n");
            }

            return definitionsResult.ToString();
        }



        // MCQs Prompt with Explicit Answer Key Request and Chunking
        private async Task<string> GenerateMCQs(string content, string model)
        {
            var maxTokens = modelContextLimits.ContainsKey(model) ? modelContextLimits[model] : 4096;
            var chunks = SplitTextIntoChunks(content, maxTokens);
            StringBuilder mcqsResult = new StringBuilder();

            foreach (var chunk in chunks)
            {
                string mcqResponse = await SendToChatGPT(chunk, model,
                    "Generate multiple-choice questions based on the content. For each question, provide four answer options labeled A, B, C, and D, followed by the correct answer as 'Answer: [Correct Option]'.");

                // Apply formatting to ensure consistency
                string processedMCQ = FormatMCQs(mcqResponse);
                mcqsResult.AppendLine(processedMCQ);
                mcqsResult.AppendLine();  // Separate each MCQ for readability
            }

            return mcqsResult.ToString();
        }



        private async Task<string> GenerateFlashcards(string content, string model)
        {
            var maxTokens = modelContextLimits.ContainsKey(model) ? modelContextLimits[model] : 4096;
            var chunks = SplitTextIntoChunks(content, maxTokens);
            StringBuilder flashcardsResult = new StringBuilder();

            foreach (var chunk in chunks)
            {
                // Use a strict “Front:/Back:” prompt so GPT always outputs exactly that format
                string rawFlashcards = await SendToChatGPT(chunk, model,
                    "Create flashcards for each key medical and pharmacy term in this text, using EXACTLY this format (do NOT deviate):\n\n" +
                    "Front: [Term]\n" +
                    "Back:  [Definition]\n\n" +
                    "Leave exactly one blank line between each card. Do not number or bullet anything.");

                // DEBUG: write raw GPT output to a file on Desktop, so you can inspect if something still isn't parsed
                try
                {
                    string debugPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "Flashcards_RawDebug.txt"
                    );
                    File.AppendAllText(debugPath, rawFlashcards + "\n\n---- End of chunk ----\n\n");
                }
                catch { /* ignore any file‐write errors */ }

                // Format it—this routine will pick up “Front:”/“Back:” or fallback on “Term – Definition” style
                string formattedFlashcards = FormatFlashcards(rawFlashcards);
                flashcardsResult.AppendLine(formattedFlashcards);
                flashcardsResult.AppendLine();
            }

            return flashcardsResult.ToString();
        }


        // Vocabulary Prompt with Chunking and Formatting
        private async Task<string> GenerateVocabulary(string content, string model)
        {
            var maxTokens = modelContextLimits.ContainsKey(model) ? modelContextLimits[model] : 4096;
            var chunks = SplitTextIntoChunks(content, maxTokens);
            StringBuilder vocabularyResult = new StringBuilder();

            foreach (var chunk in chunks)
            {
                var rawVocabulary = await SendToChatGPT(chunk, model, "Extract important vocabulary terms and translate them to Arabic. Use the format: 'English Term - Arabic Translation'. Avoid numbering or bullets, and place a blank line after each entry.");
                vocabularyResult.AppendLine(rawVocabulary);
            }

            // Apply formatting to clean up the output
            return FormatVocabulary(vocabularyResult.ToString());
        }


        // Centralized function to handle ChatGPT API calls
        private async Task<string> SendToChatGPT(string pageContent, string model, string taskPrompt)
        {
            string apiKey = textBoxAPIKey.Text.Trim();
            if (string.IsNullOrEmpty(apiKey))
            {
                UpdateStatus("API Key is missing. Please enter and save your API Key.");
                return string.Empty;
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);

                var requestContent = new
                {
                    model = model,
                    messages = new[]
                    {
                new { role = "system", content = taskPrompt },
                new { role = "user", content = pageContent }
            }
                };

                string jsonContent = System.Text.Json.JsonSerializer.Serialize(requestContent, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                StringContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    var jsonObject = JsonNode.Parse(result);
                    return jsonObject?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "No content extracted.";
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    UpdateStatus($"Error from ChatGPT: {response.StatusCode} - {errorResponse}");
                    return string.Empty;
                }
            }
        }





        // Method to save content to specific file
        private void SaveContentToFile(string content, string filePath, string sectionTitle)
        {
            Word.Application wordApp = new Word.Application();
            Word.Document doc = wordApp.Documents.Add();

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
            doc.Close();
            wordApp.Quit();
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



        // Function to format MCQs with an answer key
        private string FormatMCQs(string text)
        {
            var formattedMCQs = new List<string>();
            var mcqBlocks = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in mcqBlocks)
            {
                // Remove any numbering by using regex to strip numbers or bullets at the beginning of lines
                var cleanBlock = Regex.Replace(block, @"^\d+\.\s*", string.Empty).Trim();

                // Check if the block includes an "Answer" field; if not, add a placeholder
                if (!cleanBlock.Contains("Answer:"))
                {
                    cleanBlock += "\nAnswer: [To be provided]";
                }

                // Add the cleaned and standardized MCQ to the list, with consistent spacing
                formattedMCQs.Add(cleanBlock);
            }

            return string.Join("\n\n", formattedMCQs);
        }



        // Function to format flashcards

        private string FormatFlashcards(string text)
        {
            var flashcards = new List<string>();
            var lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();

                // 1) If GPT correctly used “Front:”/“Back:”
                if (trimmed.StartsWith("Front:", StringComparison.OrdinalIgnoreCase))
                {
                    string term = trimmed.Substring(6).Trim();  // remove “Front:”
                    string definition = "[Definition missing]";
                    if (i + 1 < lines.Length && lines[i + 1].StartsWith("Back:", StringComparison.OrdinalIgnoreCase))
                    {
                        definition = lines[i + 1].Substring(5).Trim();  // remove “Back:”
                        i++;
                    }
                    flashcards.Add($"Front: {term}\nBack: {definition}");
                }
                else
                {
                    // 2) Fallback: if line contains “–” (en dash) or “-” (hyphen), split into term/definition
                    var dashSplit = trimmed.Split(new[] { "–" }, 2, StringSplitOptions.None);
                    if (dashSplit.Length == 2)
                    {
                        string term = dashSplit[0].Trim();
                        string definition = dashSplit[1].Trim();
                        flashcards.Add($"Front: {term}\nBack: {definition}");
                    }
                    else
                    {
                        var hyphenSplit = trimmed.Split(new[] { '-' }, 2);
                        if (hyphenSplit.Length == 2 && hyphenSplit[1].Trim().Length > 0)
                        {
                            string term = hyphenSplit[0].Trim();
                            string definition = hyphenSplit[1].Trim();
                            flashcards.Add($"Front: {term}\nBack: {definition}");
                        }
                        else
                        {
                            // 3) Another fallback: “Term: Definition” style
                            var colonSplit = trimmed.Split(new[] { ':' }, 2);
                            if (colonSplit.Length == 2 && colonSplit[1].Trim().Length > 0)
                            {
                                string term = colonSplit[0].Trim();
                                string definition = colonSplit[1].Trim();
                                flashcards.Add($"Front: {term}\nBack: {definition}");
                            }
                        }
                    }
                }
            }

            return string.Join("\n\n", flashcards);
        }





        // Function to format vocabulary
        private string FormatVocabulary(string text)
        {
            var formattedVocabulary = new List<string>();
            var terms = text.Split('\n');

            foreach (var line in terms)
            {
                // Use regular expression to match vocabulary terms in the correct "English - Arabic" format
                var match = Regex.Match(line, @"^(?<english>[^-]+) - (?<arabic>.+)$");
                if (match.Success)
                {
                    string english = match.Groups["english"].Value.Trim();
                    string arabic = match.Groups["arabic"].Value.Trim();
                    formattedVocabulary.Add($"{english} - {arabic}");
                }
                else
                {
                    // If the format doesn't match, add a placeholder or flag for review
                    formattedVocabulary.Add($"{line.Trim()} - [Translation Needed]");
                }
            }

            // Join the cleaned and formatted vocabulary list without numbering
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


        public async Task<string> ProcessPdfWithVision(string filePath, string apiKey)
        {
            var allPages = ConvertPdfToImages(filePath);
            StringBuilder finalText = new StringBuilder();

            foreach (var (pageNumber, image) in allPages)
            {
                UpdateOverlayLog($"🖼️ Sending page {pageNumber} to GPT...");
                string result = await SendImageToGPTAsync(image, apiKey);
                finalText.AppendLine($"===== Page {pageNumber} =====");
                finalText.AppendLine(result);
                finalText.AppendLine();
                UpdateOverlayLog($"✅ Page {pageNumber} done.");
            }


            return finalText.ToString();
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


    }
}