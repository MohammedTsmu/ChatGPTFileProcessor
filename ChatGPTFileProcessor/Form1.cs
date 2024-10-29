using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using Microsoft.Office.Interop.Word;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Word = Microsoft.Office.Interop.Word;
using System.Text.Json.Nodes;  // Add this at the top of your file if not present






namespace ChatGPTFileProcessor
{
    public partial class Form1 : Form
    {
        private readonly string apiKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "api_key.txt");
        private readonly string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model.txt");


        public Form1()
        {
            InitializeComponent();
            LoadAPIKey();  // Load API key on app start
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Populate ComboBox with available models
            comboBoxModel.Items.Add("gpt-3.5-turbo");
            comboBoxModel.Items.Add("gpt-3.5-turbo-16k");
            comboBoxModel.Items.Add("gpt-4");
            comboBoxModel.Items.Add("gpt-4-turbo");

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
                openFileDialog.Filter = "Text Files (*.txt)|*.txt|PDF Files (*.pdf)|*.pdf|Word Files (*.docx)|*.docx";
                openFileDialog.Title = "Select a File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Display the selected file path
                    labelFileName.Text = openFileDialog.FileName;
                    UpdateStatus("File selected: " + openFileDialog.FileName);
                }
            }
        }

        private async void buttonProcessFile_Click(object sender, EventArgs e)
        {
            string filePath = labelFileName.Text;

            if (filePath == "No file selected")
            {
                UpdateStatus("Please select a file to process.");
                return;
            }

            string fileContent = "";
            try
            {
                if (filePath.EndsWith(".txt"))
                {
                    fileContent = ReadTextFile(filePath);
                }
                else if (filePath.EndsWith(".docx"))
                {
                    fileContent = ReadWordFile(filePath);
                }
                else if (filePath.EndsWith(".pdf"))
                {
                    fileContent = ReadPdfFile(filePath);
                }
                else
                {
                    UpdateStatus("Unsupported file format.");
                    return;
                }

                UpdateStatus("File content read successfully.");

                // Split content by pages (assuming '\f' as page separator for text files)
                string[] pages = fileContent.Split(new[] { "\f" }, StringSplitOptions.None);
                StringBuilder outputContent = new StringBuilder();

                foreach (var page in pages)
                {
                    UpdateStatus("Processing page...");

                    int chunkSize = GetChunkSizeForModel();
                    var chunks = SplitTextIntoChunks(page, chunkSize);

                    foreach (var chunk in chunks)
                    {
                        string chatGptResponse = await SendToChatGPT(chunk);

                        if (!string.IsNullOrEmpty(chatGptResponse))
                        {
                            outputContent.AppendLine(chatGptResponse);
                            outputContent.AppendLine("\n--- End of Chunk ---\n");
                        }
                    }

                    outputContent.AppendLine("\n--- End of Page ---\n");
                }


                // Output results after processing all pages and chunks
                SaveResultsToWord(outputContent.ToString());
            }
            catch (Exception ex)
            {
                UpdateStatus("Error reading file: " + ex.Message);
            }
        }




        private string ReadTextFile(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        private string ReadWordFile(string filePath)
        {
            Microsoft.Office.Interop.Word.Application wordApp = new Microsoft.Office.Interop.Word.Application();
            Document doc = wordApp.Documents.Open(filePath);
            string text = doc.Content.Text;
            doc.Close(false);  // Close the document without saving changes
            wordApp.Quit(false);  // Quit Word Application
            System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
            return text;
        }

        private string ReadPdfFile(string filePath)
        {
            StringBuilder text = new StringBuilder();
            using (PdfReader pdfReader = new PdfReader(filePath))
            using (PdfDocument pdfDoc = new PdfDocument(pdfReader))
            {
                for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                {
                    text.Append(PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i)));
                }
            }
            return text.ToString();
        }

        private List<string> SplitTextIntoChunks(string text, int maxWords = 500, int overlapWords = 50)
        {
            var words = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var chunks = new List<string>();  // Specify <string> as type argument
            int totalWords = words.Length;

            for (int i = 0; i < totalWords; i += maxWords - overlapWords)
            {
                var chunk = string.Join(" ", words.Skip(i).Take(maxWords));  // Ensure that .Skip works with string arrays
                chunks.Add(chunk);
            }

            return chunks;
        }


        private readonly Dictionary<string, (int maxTokens, string prompt)> modelDetails = new Dictionary<string, (int, string)>
        {
            { "gpt-3.5-turbo", (4096, "Summarize each page with the following structure:\n\nDefinitions:\n1. Term: Definition\n\nMCQs:\n1. Question?\n   A) Option 1\n   B) Option 2\n   C) Option 3\n   D) Option 4\n   Answer: [Correct Option]\n\nFlashcards:\nFront: [Term]\nBack: [Definition]\n\nVocabulary:\n1. Term\n\nUse this structure for all responses, with labeled sections as shown.") },
            { "gpt-3.5-turbo-16k", (16384, "For each page, use the following structured format:\n\nDefinitions:\n1. Term: Definition\n\nMCQs:\n1. Question?\n   A) Option 1\n   B) Option 2\n   C) Option 3\n   D) Option 4\n   Answer: [Correct Option]\n\nFlashcards:\nFront: [Term]\nBack: [Definition]\n\nVocabulary:\n1. Term\n\nEnsure consistency by following this structure and labeling each section clearly.") },
            { "gpt-4", (8192, "Please analyze each page and follow this structured format:\n\nDefinitions:\n1. Term: Definition\n\nMCQs:\n1. Question?\n   A) Option 1\n   B) Option 2\n   C) Option 3\n   D) Option 4\n   Answer: [Correct Option]\n\nFlashcards:\nFront: [Term]\nBack: [Definition]\n\nVocabulary:\n1. Term\n\nKeep the format consistent and labeled as instructed.") },
            { "gpt-4-turbo", (128000, "For each page, provide a comprehensive response using the following structure:\n\nDefinitions:\n1. Term: Definition\n\nMCQs:\n1. Question?\n   A) Option 1\n   B) Option 2\n   C) Option 3\n   D) Option 4\n   Answer: [Correct Option]\n\nFlashcards:\nFront: [Term]\nBack: [Definition]\n\nVocabulary:\n1. Term\n\nPlease ensure the response strictly follows this format and includes labels and answer keys where applicable.") }
        };





        private int GetChunkSizeForModel()
        {
            string selectedModel = comboBoxModel.SelectedItem?.ToString() ?? "gpt-3.5-turbo";
            if (modelDetails.ContainsKey(selectedModel))
            {
                int maxTokens = modelDetails[selectedModel].maxTokens;
                return (int)(maxTokens * 0.80);  // Use 80% of max tokens for buffer
            }
            return 4096;  // Default chunk size if model not found
        }



        private async Task<string> SendToChatGPT(string pageContent)
        {
            string apiKey = textBoxAPIKey.Text.Trim();
            if (string.IsNullOrEmpty(apiKey))
            {
                UpdateStatus("API Key is missing. Please enter and save your API Key.");
                return string.Empty;
            }

            // Get the selected model and its prompt
            string selectedModel = comboBoxModel.SelectedItem?.ToString() ?? "gpt-3.5-turbo";
            string prompt = modelDetails.ContainsKey(selectedModel) ? modelDetails[selectedModel].prompt : modelDetails["gpt-3.5-turbo"].prompt;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);

                var requestContent = new
                {
                    model = selectedModel,
                    messages = new[]
                    {
                new { role = "system", content = prompt },
                new { role = "user", content = pageContent }
            }
                };

                string jsonContent = System.Text.Json.JsonSerializer.Serialize(requestContent, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                StringContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();

                    // Parse JSON response to get only the content
                    var jsonObject = JsonNode.Parse(result);
                    string content = jsonObject?["choices"]?[0]?["message"]?["content"]?.ToString();
                    return content ?? "No content extracted.";
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    UpdateStatus($"Error from ChatGPT: {response.StatusCode} - {errorResponse}");
                    return string.Empty;
                }
            }
        }



        private void SaveResultsToWord(string outputContent)
        {
            Word.Application wordApp = new Word.Application();
            Word.Document doc = wordApp.Documents.Add();

            // Separate content by pages for better structure
            string[] pages = outputContent.Split(new[] { "\n--- End of Page ---\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var page in pages)
            {
                // Add page title
                Word.Paragraph para = doc.Content.Paragraphs.Add();
                para.Range.Text = "Page Content:";
                para.Range.Font.Bold = 1;
                para.Range.InsertParagraphAfter();

                // Add page content
                para.Range.Text = page.Trim();
                para.Range.Font.Bold = 0;
                para.Range.InsertParagraphAfter();

                // Add a page break
                para.Range.InsertBreak(Word.WdBreakType.wdPageBreak);
            }

            // Save the document
            string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ChatGPT_Processed_Output.docx");
            doc.SaveAs2(outputPath);
            doc.Close();
            wordApp.Quit();

            UpdateStatus($"Results saved successfully to {outputPath}");
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


    }
}
