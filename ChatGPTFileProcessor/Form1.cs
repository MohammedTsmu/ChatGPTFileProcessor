using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using Microsoft.Office.Interop.Word;




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

        private void buttonProcessFile_Click(object sender, EventArgs e)
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
                // Here, we'll later add code to send the content to ChatGPT
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


    }
}
