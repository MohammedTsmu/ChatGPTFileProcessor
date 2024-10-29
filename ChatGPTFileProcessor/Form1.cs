using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using Microsoft.Office.Interop.Word;
using System.Net.Http;
using System.Text.Json;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Data.SqlClient;




namespace ChatGPTFileProcessor
{
    public partial class Form1 : Form
    {
        private readonly string configPath = "config.txt";

        public Form1()
        {
            InitializeComponent();
            LoadAPIKey();  // Load API key on app start
        }

        private void LoadAPIKey()
        {
            if (File.Exists(configPath))
            {
                // Read the API key from the config file
                textBoxAPIKey.Text = File.ReadAllText(configPath);
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
                File.WriteAllText(configPath, apiKey);
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
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
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
                    string chatGptResponse = await SendToChatGPT(page);

                    if (!string.IsNullOrEmpty(chatGptResponse))
                    {
                        outputContent.AppendLine(chatGptResponse);
                        outputContent.AppendLine("\n--- End of Page ---\n");
                    }
                }

                // Output results after processing all pages (we'll implement saving to Word in the next step)
                textBoxStatus.AppendText("All pages processed. Ready to save results.");
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



        private async Task<string> SendToChatGPT(string pageContent)
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
                new { role = "system", content = "Extract definitions, MCQs, flashcards (front and back), and vocabularies." },
                new { role = "user", content = pageContent }
            }
                };

                string jsonContent = System.Text.Json.JsonSerializer.Serialize(requestContent, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

                StringContent httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("https://api.openai.com/v1/chat/completions", httpContent);
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    return result;
                }
                else
                {
                    UpdateStatus($"Error from ChatGPT: {response.StatusCode}");

                    //WdDeleteCells later it is SQLDebugging if api key not works return ErrorBars message
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    UpdateStatus($"Error from ChatGPT: {response.StatusCode} - {errorResponse}");
                    //return string.Empty;



                    return string.Empty;

                }

                
                    
                

            }
        }

    }
}
