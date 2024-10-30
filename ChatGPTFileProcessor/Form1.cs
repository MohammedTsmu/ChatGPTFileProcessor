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
using System.Text.RegularExpressions;
using iText.Commons.Utils;




namespace ChatGPTFileProcessor
{
    public partial class Form1 : Form
    {
        private readonly string apiKeyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "api_key.txt");
        private readonly string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model.txt");

        // Define output file paths for each section
        private readonly string definitionsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Definitions_Output.docx");
        private readonly string mcqsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MCQs_Output.docx");
        private readonly string flashcardsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Flashcards_Output.docx");
        private readonly string vocabularyFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Vocabulary_Output.docx");



        private readonly Dictionary<string, (int maxTokens, string prompt)> modelDetails = new Dictionary<string, (int, string)>
        {
            {
                "gpt-3.5-turbo",
                (4096, "Summarize each page with the following structure:\n\nDefinitions:\nTerm: Definition\n\nMCQs:\nQuestion?\n   A) Option 1\n   B) Option 2\n   C) Option 3\n   D) Option 4\n   Answer: Correct Option\n\nFlashcards:\nFront: Term\nBack: Definition\n\nVocabulary:\nEnglish Term - Arabic Translation\n\nAvoid using numbering or bold text. Place a blank line after each entry for clarity.")
            },
            {
                "gpt-3.5-turbo-16k",
                (16384, "For each page, use the following structure:\n\nDefinitions:\nTerm: Definition\n\nMCQs:\nQuestion?\n   A) Option 1\n   B) Option 2\n   C) Option 3\n   D) Option 4\n   Answer: Correct Option\n\nFlashcards:\nFront: Term\nBack: Definition\n\nVocabulary:\nEnglish Term - Arabic Translation\n\nAvoid numbering and bold text. Add a blank line after each entry for clarity.")
            },
            {
                "gpt-4",
                (8192, "Analyze each page with this structure:\n\nDefinitions:\nTerm: Definition\n\nMCQs:\nQuestion?\n   A) Option 1\n   B) Option 2\n   C) Option 3\n   D) Option 4\n   Answer: Correct Option\n\nFlashcards:\nFront: Term\nBack: Definition\n\nVocabulary:\nEnglish Term - Arabic Translation\n\nNo numbering or bold. Use a blank line to separate each entry.")
            },
            {
                "gpt-4-turbo",
                (128000, "Provide a comprehensive response per page with the following structure:\n\nDefinitions:\nTerm: Definition\n\nMCQs:\nQuestion?\n   A) Option 1\n   B) Option 2\n   C) Option 3\n   D) Option 4\n   Answer: Correct Option\n\nFlashcards:\nFront: Term\nBack: Definition\n\nVocabulary:\nEnglish Term - Arabic Translation\n\nAvoid numbering and bold text. Place a blank line after each entry.")
            }
        };



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

        // Modified buttonProcessFile_Click to call individual file-saving methods
        private async void buttonProcessFile_Click(object sender, EventArgs e)
        {
            string filePath = labelFileName.Text;

            if (filePath == "No file selected")
            {
                UpdateStatus("Please select a file to process.");
                return;
            }

            try
            {
                string fileContent = ReadFileContent(filePath);
                UpdateStatus("File content read successfully.");

                string selectedModel = comboBoxModel.SelectedItem?.ToString() ?? "gpt-3.5-turbo";

                UpdateStatus("Processing definitions...");
                string definitionsContent = await GenerateDefinitions(fileContent, selectedModel);
                SaveContentToFile(FormatDefinitions(definitionsContent), definitionsFilePath, "Definitions");

                UpdateStatus("Processing MCQs...");
                string mcqsContent = await GenerateMCQs(fileContent, selectedModel);
                SaveContentToFile(mcqsContent, mcqsFilePath, "MCQs");

                UpdateStatus("Processing flashcards...");
                string flashcardsContent = await GenerateFlashcards(fileContent, selectedModel);
                SaveContentToFile(flashcardsContent, flashcardsFilePath, "Flashcards");

                UpdateStatus("Processing vocabulary...");
                string vocabularyContent = await GenerateVocabulary(fileContent, selectedModel);
                SaveContentToFile(vocabularyContent, vocabularyFilePath, "Vocabulary");

                UpdateStatus("All sections processed and saved successfully.");
            }
            catch (Exception ex)
            {
                UpdateStatus("Error reading file: " + ex.Message);
            }
        }

        private string ReadFileContent(string filePath)
        {
            if (filePath.EndsWith(".txt"))
                return File.ReadAllText(filePath);
            if (filePath.EndsWith(".docx"))
                return ReadWordFile(filePath);
            if (filePath.EndsWith(".pdf"))
                return ReadPdfFile(filePath);

            throw new NotSupportedException("Unsupported file format.");
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


        

        //private readonly Dictionary<string, (int maxTokens, string prompt)> modelDetails = new Dictionary<string, (int, string)>
        //{
        //    {
        //        "gpt-3.5-turbo",
        //        (4096, "Summarize each page with the following structure:\n\nDefinitions:\n1. Term: Definition\n\nMCQs:\n1. Question?\n   A) Option 1\n   B) Option 2\n   C) Option 3\n   D) Option 4\n   Answer: [Correct Option]\n\nFlashcards:\nFront: [Term]\nBack: [Definition]\n\nVocabulary:\n1. English Term - Arabic Translation\n\nEnsure each section is labeled and formatted as specified.")
        //    },
        //    {
        //        "gpt-3.5-turbo-16k",
        //        (16384, "For each page, use the following structure:\n\nDefinitions:\n1. Term: Definition\n\nMCQs:\n1. Question?\n   A) Option 1\n   B) Option 2\n   C) Option 3\n   D) Option 4\n   Answer: [Correct Option]\n\nFlashcards:\nFront: [Term]\nBack: [Definition]\n\nVocabulary:\n1. English Term - Arabic Translation\n\nMake sure each section is labeled and formatted consistently with this structure.")
        //    },
        //    {
        //        "gpt-4",
        //        (8192, "Analyze each page and follow this structured format:\n\nDefinitions:\n1. Term: Definition\n\nMCQs:\n1. Question?\n   A) Option 1\n   B) Option 2\n   C) Option 3\n   D) Option 4\n   Answer: [Correct Option]\n\nFlashcards:\nFront: [Term]\nBack: [Definition]\n\nVocabulary:\n1. English Term - Arabic Translation\n\nUse consistent labels and formatting as specified in this structure.")
        //    },
        //    {
        //        "gpt-4-turbo",
        //        (128000, "For each page, provide a comprehensive response using the following structure:\n\nDefinitions:\n1. Term: Definition\n\nMCQs:\n1. Question?\n   A) Option 1\n   B) Option 2\n   C) Option 3\n   D) Option 4\n   Answer: [Correct Option]\n\nFlashcards:\nFront: [Term]\nBack: [Definition]\n\nVocabulary:\n1. English Term - Arabic Translation\n\nEnsure each section strictly follows the specified format and includes labels and answer keys.")
        //    }
        //};

        private readonly Dictionary<string, int> modelContextLimits = new Dictionary<string, int>
        {
            { "gpt-3.5-turbo", 4096 },
            { "gpt-3.5-turbo-16k", 16384 },
            { "gpt-4", 8192 },
            { "gpt-4-turbo", 128000 }
        };

        // Definitions Prompt
        // Definitions Prompt with Chunking
        // Simplify each content type's processing method:
        private async Task<string> GenerateDefinitions(string content, string model)
        {
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


        // Function to ensure each MCQ includes an answer key
        private string EnsureAnswerKeyInMCQs(string mcqContent)
        {
            var mcqsWithAnswers = new List<string>();
            var mcqBlocks = mcqContent.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var block in mcqBlocks)
            {
                // Check if the block includes an answer key; if not, add a placeholder
                string mcqWithAnswer = block.Contains("Answer:") ? block : block + "\nAnswer: [To be provided]";
                mcqsWithAnswers.Add(mcqWithAnswer);
            }

            return string.Join("\n\n", mcqsWithAnswers);
        }


        // Flashcards Prompt with Chunking
        private async Task<string> GenerateFlashcards(string content, string model)
        {
            var maxTokens = modelContextLimits.ContainsKey(model) ? modelContextLimits[model] : 4096;
            var chunks = SplitTextIntoChunks(content, maxTokens);
            StringBuilder flashcardsResult = new StringBuilder();

            foreach (var chunk in chunks)
            {
                flashcardsResult.AppendLine(await SendToChatGPT(chunk, model, "Create flashcards for key terms and concepts."));
            }

            return flashcardsResult.ToString();
        }

        // Vocabulary Prompt with Chunking
        private async Task<string> GenerateVocabulary(string content, string model)
        {
            var maxTokens = modelContextLimits.ContainsKey(model) ? modelContextLimits[model] : 4096;
            var chunks = SplitTextIntoChunks(content, maxTokens);
            StringBuilder vocabularyResult = new StringBuilder();

            foreach (var chunk in chunks)
            {
                vocabularyResult.AppendLine(await SendToChatGPT(chunk, model, "List important vocabulary terms from the page with Arabic translations."));
            }

            return vocabularyResult.ToString();
        }





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



        //private void SaveResultsToWord(string outputContent)
        //{
        //    Word.Application wordApp = new Word.Application();
        //    Word.Document doc = wordApp.Documents.Add();

        //    // Split content into sections for organization
        //    string[] sections = outputContent.Split(new[] { "Definitions:", "MCQs:", "Flashcards:", "Vocabulary:" }, StringSplitOptions.RemoveEmptyEntries);

        //    // Define section titles
        //    string[] sectionTitles = { "Definitions", "MCQs", "Flashcards", "Vocabulary" };

        //    // Iterate over each section to format and add to Word doc
        //    for (int i = 0; i < sections.Length; i++)
        //    {
        //        // Add section title
        //        Word.Paragraph titlePara = doc.Content.Paragraphs.Add();
        //        titlePara.Range.Text = sectionTitles[i] + ":";
        //        titlePara.Range.Font.Size = 14; // Larger font for titles
        //        titlePara.Range.Font.Bold = 0;
        //        titlePara.Range.InsertParagraphAfter();

        //        // Add section content
        //        string[] entries = sections[i].Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        //        foreach (string entry in entries)
        //        {
        //            Word.Paragraph entryPara = doc.Content.Paragraphs.Add();
        //            entryPara.Range.Text = entry.Trim();
        //            entryPara.Range.Font.Size = 12; // Regular font size for content
        //            entryPara.Range.Font.Bold = 0;
        //            entryPara.Range.InsertParagraphAfter();

        //            // Add a line separator after each entry for clarity
        //            Word.Paragraph separatorPara = doc.Content.Paragraphs.Add();
        //            separatorPara.Range.Text = new string('_', 70);
        //            separatorPara.Range.Font.Size = 10;
        //            separatorPara.Range.InsertParagraphAfter();
        //        }

        //        // Add a page break after each section for organization
        //        if (i < sections.Length - 1)
        //        {
        //            doc.Content.Paragraphs.Add().Range.InsertBreak(Word.WdBreakType.wdPageBreak);
        //        }
        //    }

        //    // Save the document
        //    string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ChatGPT_Processed_Output_Formatted.docx");
        //    doc.SaveAs2(outputPath);
        //    doc.Close();
        //    wordApp.Quit();

        //    UpdateStatus($"Results saved successfully to {outputPath}");
        //}
        private void SaveResultsToWord(string outputContent)
        {
            Word.Application wordApp = new Word.Application();
            Word.Document doc = wordApp.Documents.Add();

            // Split content by main sections and remove extraneous symbols
            string[] sections = outputContent.Split(new[] { "Definitions:", "MCQs:", "Flashcards:", "Vocabulary:" }, StringSplitOptions.None);

            // Process each section individually, with uniform formatting
            string[] sectionHeaders = { "Definitions", "MCQs", "Flashcards", "Vocabulary" };
            for (int i = 1; i < sections.Length; i++)
            {
                // Add section header in bold
                Word.Paragraph headerPara = doc.Content.Paragraphs.Add();
                headerPara.Range.Text = $"{sectionHeaders[i - 1]}:";
                headerPara.Range.Font.Bold = 1;
                headerPara.Range.InsertParagraphAfter();

                // Insert section content without bolding
                Word.Paragraph contentPara = doc.Content.Paragraphs.Add();
                contentPara.Range.Text = PostProcessContent(sections[i]);
                contentPara.Range.Font.Bold = 0;
                contentPara.Range.InsertParagraphAfter();

                // Add spacing after each section
                contentPara.Range.InsertParagraphAfter();
            }

            // Save the document
            string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ChatGPT_Processed_Output_Formatted.docx");
            doc.SaveAs2(outputPath);
            doc.Close();
            wordApp.Quit();

            UpdateStatus($"Results saved successfully to {outputPath}");
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



        //Create Post-Processing Functions
        //Create functions to check each section’s structure and reformat as needed after generation.
        // Function to post-process generated content
        private string PostProcessContent(string generatedContent)
        {
            // Define expected section titles in the correct order
            string[] sectionHeaders = { "Definitions:", "MCQs:", "Flashcards:", "Vocabulary:" };
            StringBuilder processedContent = new StringBuilder();

            // Split content by main sections; ignore empty entries
            string[] sections = generatedContent.Split(sectionHeaders, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < sectionHeaders.Length; i++)
            {
                // Verify that the section exists in the array to avoid index errors
                if (i < sections.Length)
                {
                    // Add section title
                    processedContent.AppendLine(sectionHeaders[i]);
                    processedContent.AppendLine();

                    // Process the section content to ensure clean formatting
                    string formattedSectionContent = FormatSectionContent(sections[i].Trim());
                    processedContent.AppendLine(formattedSectionContent);
                    processedContent.AppendLine();
                }
                else
                {
                    // Log missing section data if it’s absent
                    UpdateStatus($"{sectionHeaders[i].Replace(":", "")} section is missing in the generated content.");
                }
            }
            return processedContent.ToString();
        }

        // Example helper to ensure clean, non-numbered formatting within each section
        private string FormatSectionContent(string sectionText)
        {
            StringBuilder formattedContent = new StringBuilder();
            string[] lines = sectionText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // Trim and add blank lines to space out each entry
                formattedContent.AppendLine(line.Trim());
                formattedContent.AppendLine();  // Space between each entry
            }
            return formattedContent.ToString();
        }




        // Function to format definitions
        //private string FormatDefinitions(string text)
        //    {
        //        var formattedDefinitions = new List<string>();
        //        var lines = text.Split('\n');
        //        int count = 1;

        //        foreach (var line in lines)
        //        {
        //            // Enforce "1. Term: Definition" format
        //            var match = Regex.Match(line, @"^(?<term>.+): (?<definition>.+)$");
        //            if (match.Success)
        //            {
        //                formattedDefinitions.Add($"{count}. {match.Groups["term"].Value}: {match.Groups["definition"].Value}");
        //                count++;
        //            }
        //        }
        //        return string.Join("\n", formattedDefinitions);
        //    }
        // Function to format definitions
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
                var formattedFlashcards = new List<string>();
                var flashcardBlocks = text.Split(new[] { "Front:", "Back:" }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < flashcardBlocks.Length; i += 2)
                {
                    var term = flashcardBlocks[i].Trim();
                    var definition = i + 1 < flashcardBlocks.Length ? flashcardBlocks[i + 1].Trim() : "[Definition missing]";
                    formattedFlashcards.Add($"Front: {term}\nBack: {definition}");
                }
                return string.Join("\n\n", formattedFlashcards);
            }

        // Function to format vocabulary
        private string FormatVocabulary(string text)
        {
            var formattedVocabulary = new List<string>();
            var terms = text.Split('\n');
            int count = 1;

            foreach (var line in terms)
            {
                var match = Regex.Match(line, @"^(?<english>.+?) - (?<arabic>.+)$");
                if (match.Success)
                {
                    string english = match.Groups["english"].Value.Trim();
                    string arabic = match.Groups["arabic"].Value.Trim();
                    formattedVocabulary.Add($"{count}. {english} - {arabic}");
                    count++;
                }
                else
                {
                    // If the format is incorrect, mark for review
                    formattedVocabulary.Add($"{count}. {line.Trim()} - [Arabic Translation Needed]");
                    count++;
                }
            }
            return string.Join("\n", formattedVocabulary);
        }

        private async Task<string> TranslateVocabularyToArabic(string vocabularyText)
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
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                new { role = "system", content = "Translate the following vocabulary terms from English to Arabic. Use this format:\n\n1. English Term - Arabic Translation" },
                new { role = "user", content = vocabularyText }
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
                    return content ?? "Translation not available.";
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    UpdateStatus($"Error from ChatGPT: {response.StatusCode} - {errorResponse}");
                    return string.Empty;
                }
            }
        }


    }
}