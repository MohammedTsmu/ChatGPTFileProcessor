using DevExpress.XtraSpellChecker.Parser;
using DevExpress.XtraTab;
//using AnkiSharp;
//using Python.Included;
//using Python.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xceed.Document.NET;
using Xceed.Words.NET;
using static DevExpress.XtraEditors.XtraInputBox;
using SDImage = System.Drawing.Image;
using Task = System.Threading.Tasks.Task;

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

        private string selectedReasoningEffort = "medium"; // default


        private bool pythonInitialized = false;
        private object pythonLock = new object();
        private string pythonHome = null;

        private Label progressLabel;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // أفرغ العناصر أولاً
            comboBoxEditModel.Properties.Items.Clear();

            // GPT-5 Series (Latest) - ALL support vision + reasoning
            comboBoxEditModel.Properties.Items.Add("gpt-5.2");              // ⭐ Recommended
            comboBoxEditModel.Properties.Items.Add("gpt-5.2-thinking");
            comboBoxEditModel.Properties.Items.Add("gpt-5.1");
            comboBoxEditModel.Properties.Items.Add("gpt-5");
            comboBoxEditModel.Properties.Items.Add("gpt-5-mini");
            comboBoxEditModel.Properties.Items.Add("gpt-5-nano");
            comboBoxEditModel.Properties.Items.Add("gpt-5-chat-latest");

            // O-Series Reasoning Models (ONLY models with vision support!)
            comboBoxEditModel.Properties.Items.Add("o3");                   // ✅ Vision + Reasoning
            comboBoxEditModel.Properties.Items.Add("o4-mini");              // ✅ Vision + Reasoning
            // NOTE: o3-mini, o1, o1-mini removed - they DON'T support vision in API!

            // GPT-4.1 Series (Vision, no reasoning)
            comboBoxEditModel.Properties.Items.Add("gpt-4.1");
            comboBoxEditModel.Properties.Items.Add("gpt-4.1-mini");
            comboBoxEditModel.Properties.Items.Add("gpt-4.1-nano");

            // GPT-4o Series (Legacy, vision only)
            comboBoxEditModel.Properties.Items.Add("chatgpt-4o-latest");
            comboBoxEditModel.Properties.Items.Add("gpt-4o");
            comboBoxEditModel.Properties.Items.Add("gpt-4o-mini");


            // ▼ Populate the "Reasoning Effort" dropdown
            comboBoxReasoningEffort.Properties.Items.Clear();
            comboBoxReasoningEffort.Properties.Items.Add("Auto (Recommended)");
            comboBoxReasoningEffort.Properties.Items.Add("Low - Fast");
            comboBoxReasoningEffort.Properties.Items.Add("Medium - Balanced");
            comboBoxReasoningEffort.Properties.Items.Add("High - Best Quality");

            // Load saved reasoning effort preference
            var savedEffort = Properties.Settings.Default.ReasoningEffort;
            if (!string.IsNullOrWhiteSpace(savedEffort))
            {
                for (int i = 0; i < comboBoxReasoningEffort.Properties.Items.Count; i++)
                {
                    if (comboBoxReasoningEffort.Properties.Items[i].ToString().ToLower().Contains(savedEffort))
                    {
                        comboBoxReasoningEffort.SelectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                comboBoxReasoningEffort.SelectedIndex = 2; // Default to "Medium - Balanced"
            }

            // Initially disable reasoning effort until a reasoning model is selected
            comboBoxReasoningEffort.Enabled = false;
            comboBoxReasoningEffort.Properties.Appearance.ForeColor = System.Drawing.Color.Gray;


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

            //Initialize Python for Anki export (runs in background)
            Task.Run(() => InitializePythonEnvironment());

            //// Position navigation buttons dynamically
            //PositionNavigationButtons();

            //// Re-position on resize
            //this.Resize += (s, ev) => PositionNavigationButtons();

            // Position buttons initially
            PositionNavigationButtons();

            // Re-position on window resize
            this.Resize += (s, ev) => PositionNavigationButtons();

            // Re-position when switching tabs
            xtraTabControl.SelectedPageChanged += (s, ev) => PositionNavigationButtons();
        }

        //private void PositionNavigationButtons()
        //{
        //    int margin = 40;
        //    int buttonHeight = 45;
        //    int bottomY = xtraTabControl.Height - buttonHeight - margin;

        //    // File tab
        //    if (btnNextToOutput != null)
        //    {
        //        btnNextToOutput.Location = new System.Drawing.Point(
        //            xtraTabControl.Width - btnNextToOutput.Width - margin,
        //            bottomY
        //        );
        //    }

        //    // Output tab
        //    if (btnBackToFile != null)
        //        btnBackToFile.Location = new System.Drawing.Point(margin, bottomY);

        //    if (btnNextToLanguage != null)
        //    {
        //        btnNextToLanguage.Location = new System.Drawing.Point(
        //            xtraTabControl.Width - btnNextToLanguage.Width - margin,
        //            bottomY
        //        );
        //    }

        //    // Language tab
        //    if (btnBackToOutput != null)
        //        btnBackToOutput.Location = new System.Drawing.Point(margin, bottomY);

        //    if (btnNextToModel != null)
        //    {
        //        btnNextToModel.Location = new System.Drawing.Point(
        //            xtraTabControl.Width - btnNextToModel.Width - margin,
        //            bottomY
        //        );
        //    }

        //    // Model tab
        //    if (btnBackToLanguage != null)
        //        btnBackToLanguage.Location = new System.Drawing.Point(margin, bottomY);

        //    if (buttonProcessFile != null)
        //    {
        //        buttonProcessFile.Location = new System.Drawing.Point(
        //            xtraTabControl.Width - buttonProcessFile.Width - margin,
        //            bottomY
        //        );
        //    }
        //}
        private void PositionNavigationButtons()
        {
            // Don't run if controls aren't initialized yet
            if (xtraTabControl == null || xtraTabControl.SelectedTabPage == null)
                return;

            int margin = 40;
            int buttonWidth = 200;  // Approximate button width
            int buttonHeight = 50;  // Approximate button height

            // Get the actual size of the current tab page
            var currentTab = xtraTabControl.SelectedTabPage;
            int tabWidth = currentTab.ClientSize.Width;
            int tabHeight = currentTab.ClientSize.Height;

            // Calculate positions
            int bottomY = tabHeight - buttonHeight - margin;
            int rightX = tabWidth - buttonWidth - margin;
            int leftX = margin;

            // Position buttons based on which tab is active
            try
            {
                if (currentTab == tabPageFile && btnNextToOutput != null)
                {
                    btnNextToOutput.Location = new System.Drawing.Point(rightX, bottomY);
                    btnNextToOutput.BringToFront();
                }
                else if (currentTab == tabPageOutput)
                {
                    if (btnBackToFile != null)
                    {
                        btnBackToFile.Location = new System.Drawing.Point(leftX, bottomY);
                        btnBackToFile.BringToFront();
                    }
                    if (btnNextToLanguage != null)
                    {
                        btnNextToLanguage.Location = new System.Drawing.Point(rightX, bottomY);
                        btnNextToLanguage.BringToFront();
                    }
                }
                else if (currentTab == tabPageLanguage)
                {
                    if (btnBackToOutput != null)
                    {
                        btnBackToOutput.Location = new System.Drawing.Point(leftX, bottomY);
                        btnBackToOutput.BringToFront();
                    }
                    if (btnNextToModel != null)
                    {
                        btnNextToModel.Location = new System.Drawing.Point(rightX, bottomY);
                        btnNextToModel.BringToFront();
                    }
                }
                else if (currentTab == tabPageModel)
                {
                    if (btnBackToLanguage != null)
                    {
                        btnBackToLanguage.Location = new System.Drawing.Point(leftX, bottomY);
                        btnBackToLanguage.BringToFront();
                    }
                    if (buttonProcessFile != null)
                    {
                        buttonProcessFile.Location = new System.Drawing.Point(rightX - 50, bottomY);
                        buttonProcessFile.BringToFront();
                    }
                }
            }
            catch
            {
                // Ignore errors during initialization
            }
        }

        #region Navigation Button Handlers
        private void btnNextToOutput_Click(object sender, EventArgs e)
        {
            xtraTabControl.SelectedTabPage = tabPageOutput;
        }

        private void btnBackToFile_Click(object sender, EventArgs e)
        {
            xtraTabControl.SelectedTabPage = tabPageFile;
        }

        private void btnNextToLanguage_Click(object sender, EventArgs e)
        {
            xtraTabControl.SelectedTabPage = tabPageLanguage;
        }

        private void btnBackToOutput_Click(object sender, EventArgs e)
        {
            xtraTabControl.SelectedTabPage = tabPageOutput;
        }

        private void btnNextToModel_Click(object sender, EventArgs e)
        {
            xtraTabControl.SelectedTabPage = tabPageModel;
        }

        private void btnBackToLanguage_Click(object sender, EventArgs e)
        {
            xtraTabControl.SelectedTabPage = tabPageLanguage;
        }
        #endregion

        private async Task InitializePythonEnvironment()
        {
            try
            {
                UpdateStatus("========================================");
                UpdateStatus("▶ INITIALIZING PYTHON FOR ANKI EXPORT");
                UpdateStatus("========================================");
                UpdateStatus("");

                // Try to find system Python
                UpdateStatus("▶ Looking for Python installation...");

                string[] possiblePaths = new string[]
                {
            @"C:\Python313\python.exe",
            @"C:\Python312\python.exe",
            @"C:\Python311\python.exe",
            @"C:\Python310\python.exe",
            @"C:\Python39\python.exe",
            @"C:\Python38\python.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         @"Programs\Python\Python313\python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         @"Programs\Python\Python312\python.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         @"Programs\Python\Python311\python.exe"),
                };

                // Try to find Python in PATH
                string pythonFromPath = FindPythonInPath();
                if (!string.IsNullOrEmpty(pythonFromPath))
                {
                    pythonHome = Path.GetDirectoryName(pythonFromPath);
                    UpdateStatus($"✅ Found Python in PATH: {pythonFromPath}");
                }
                else
                {
                    // Check known installation paths
                    foreach (var path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            pythonHome = Path.GetDirectoryName(path);
                            UpdateStatus($"✅ Found Python at: {path}");
                            break;
                        }
                    }
                }

                if (string.IsNullOrEmpty(pythonHome))
                {
                    UpdateStatus("❌ Python not found on system");
                    UpdateStatus("");
                    UpdateStatus("SOLUTION: Install Python from python.org");
                    UpdateStatus("1. Visit: https://www.python.org/downloads/");
                    UpdateStatus("2. Download and run installer");
                    UpdateStatus("3. ✅ CHECK 'Add Python to PATH' during installation");
                    UpdateStatus("4. Restart this application");
                    UpdateStatus("");

                    var result = MessageBox.Show(
                        "Python is not installed on your system.\n\n" +
                        "To enable Anki .apkg export, please:\n" +
                        "1. Visit https://www.python.org/downloads/\n" +
                        "2. Download and install Python\n" +
                        "3. ✅ CHECK 'Add Python to PATH' during installation\n" +
                        "4. Restart this application\n\n" +
                        "Click OK to open Python download page in browser.\n" +
                        "Click Cancel to skip (other exports will still work).",
                        "Python Installation Required",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Information
                    );

                    if (result == DialogResult.OK)
                    {
                        System.Diagnostics.Process.Start("https://www.python.org/downloads/");
                    }

                    return;
                }

                // Verify Python works
                string pythonExe = Path.Combine(pythonHome, "python.exe");
                UpdateStatus("▶ Verifying Python installation...");

                var verifyPsi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                string pythonVersion = "";
                using (var process = Process.Start(verifyPsi))
                {
                    pythonVersion = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                }

                UpdateStatus($"✅ Python version: {pythonVersion}");
                UpdateStatus("");

                // Install genanki
                UpdateStatus("▶ Installing genanki library...");
                UpdateStatus("(This may take 30-60 seconds on first run)");

                var pipPsi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "-m pip install genanki --quiet",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                string pipOutput = "";
                string pipError = "";
                using (var process = Process.Start(pipPsi))
                {
                    pipOutput = process.StandardOutput.ReadToEnd();
                    pipError = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        UpdateStatus($"⚠️ pip install had warnings: {pipError}");
                    }
                }

                UpdateStatus("✅ genanki installation complete");
                UpdateStatus("");

                // Verify genanki actually works
                UpdateStatus("▶ Verifying genanki...");

                var testPsi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "-c \"import genanki; print('genanki OK')\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                string testOutput = "";
                string testError = "";
                using (var process = Process.Start(testPsi))
                {
                    testOutput = process.StandardOutput.ReadToEnd();
                    testError = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0 && testOutput.Contains("genanki OK"))
                    {
                        UpdateStatus("✅ genanki verified successfully");
                        pythonInitialized = true;
                    }
                    else
                    {
                        UpdateStatus($"❌ genanki verification failed: {testError}");
                        return;
                    }
                }

                UpdateStatus("");
                UpdateStatus("========================================");
                UpdateStatus("✅ ANKI EXPORT READY!");
                UpdateStatus("========================================");
                UpdateStatus(".apkg files will now be created automatically");
                UpdateStatus("");
            }
            catch (Exception ex)
            {
                UpdateStatus("========================================");
                UpdateStatus($"❌ PYTHON INITIALIZATION FAILED");
                UpdateStatus("========================================");
                UpdateStatus($"Error: {ex.Message}");
                UpdateStatus("");
            }
        }

        private string FindPythonInPath()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "-c \"import sys; print(sys.executable)\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();

                    if (process.ExitCode == 0 && File.Exists(output))
                    {
                        return output;
                    }
                }
            }
            catch
            {
                // Python not in PATH
            }

            return null;
        }




        #region Helper Classes for Batch Processing

        /// <summary>
        /// Container for all prompt strings. Null values indicate disabled sections.
        /// </summary>
        private class ContentPrompts
        {
            public string Definitions { get; set; }
            public string MCQs { get; set; }
            public string Flashcards { get; set; }
            public string Vocabulary { get; set; }
            public string Summary { get; set; }
            public string Takeaways { get; set; }
            public string Cloze { get; set; }
            public string TrueFalse { get; set; }
            public string Outline { get; set; }
            public string ConceptMap { get; set; }
            public string TableExtract { get; set; }
            public string Simplified { get; set; }
            public string CaseStudy { get; set; }
            public string Keywords { get; set; }
            public string TranslatedSections { get; set; }
            public string ExplainTerms { get; set; }
        }

        /// <summary>
        /// Container for all StringBuilder instances that accumulate generated content.
        /// </summary>
        private class ContentBuilders
        {
            public StringBuilder Definitions { get; set; }
            public StringBuilder MCQs { get; set; }
            public StringBuilder Flashcards { get; set; }
            public StringBuilder Vocabulary { get; set; }
            public StringBuilder Summary { get; set; }
            public StringBuilder Takeaways { get; set; }
            public StringBuilder Cloze { get; set; }
            public StringBuilder TrueFalse { get; set; }
            public StringBuilder Outline { get; set; }
            public StringBuilder ConceptMap { get; set; }
            public StringBuilder TableExtract { get; set; }
            public StringBuilder Simplified { get; set; }
            public StringBuilder CaseStudy { get; set; }
            public StringBuilder Keywords { get; set; }
            public StringBuilder TranslatedSections { get; set; }
            public StringBuilder ExplainTerms { get; set; }
        }

        #endregion
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
            if (textBoxStatus.InvokeRequired)
            {
                textBoxStatus.Invoke(new Action(() =>
                {
                    textBoxStatus.AppendText(message + Environment.NewLine);
                    textBoxStatus.SelectionStart = textBoxStatus.Text.Length;
                    textBoxStatus.ScrollToCaret();
                }));
            }
            else
            {
                textBoxStatus.AppendText(message + Environment.NewLine);
                textBoxStatus.SelectionStart = textBoxStatus.Text.Length;
                textBoxStatus.ScrollToCaret();
            }
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


        //private void buttonBrowseFile_Click(object sender, EventArgs e)
        //{
        //    using (OpenFileDialog openFileDialog = new OpenFileDialog())
        //    {
        //        openFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";
        //        if (openFileDialog.ShowDialog() == DialogResult.OK)
        //        {
        //            selectedPdfPath = openFileDialog.FileName;
        //            _lastSelectedPdfPath = openFileDialog.FileName;

        //            using (var pageForm = new PageSelectionForm())
        //            {
        //                pageForm.PendingPdfPath = selectedPdfPath;

        //                if (pageForm.ShowDialog(this) == DialogResult.OK)
        //                {
        //                    selectedFromPage = pageForm.FromPage;
        //                    selectedToPage = pageForm.ToPage;
        //                    labelFileName.Text = selectedPdfPath;
        //                }
        //            }

        //        }
        //    }
        //}
        private async void buttonBrowseFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedPdfPath = openFileDialog.FileName;
                    _lastSelectedPdfPath = openFileDialog.FileName;

                    // Create and show page selection form
                    using (var pageForm = new PageSelectionForm())
                    {
                        try
                        {
                            // Load PDF asynchronously
                            await pageForm.LoadPdfPreviewAsync(selectedPdfPath);

                            // Show dialog to user
                            if (pageForm.ShowDialog(this) == DialogResult.OK)
                            {
                                selectedFromPage = pageForm.FromPage;
                                selectedToPage = pageForm.ToPage;
                                labelFileName.Text = selectedPdfPath;
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this,
                                $"Failed to load PDF:\n\n{ex.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
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
                // Disable the maximize and minimize of the processing form
                this.MaximizeBox = false; // Disable maximize button
                this.MinimizeBox = false; // Disable minimize button
                this.Text = "Processing PDF..."; // Update form title to indicate processing

                // اسم النموذج والـ timestamp لإنشاء مسارات الملفات
                //string modelName = comboBoxEditModel.SelectedItem?.ToString() ?? "gpt-4o"; // Use the new combo box for model selection
                string modelName = comboBoxEditModel.SelectedItem?.ToString() ?? "gpt-5.2"; // Updated to latest model
                //string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string timeStamp = DateTime.Now.ToString("yyyy_MM_dd___HH_mmss");

                UpdateOverlayLog("                                   ");
                UpdateOverlayLog("                                   ");
                ShowOverlay("▶▶▶ 🔄 Processing, please wait...");
                UpdateOverlayLog("▰▰▰▰▰ S T A R T   G E N E R A T I N G ▰▰▰▰▰");
                UpdateOverlayLog("▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△▲△");
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
                        ? "اكتب تعريفات طبية موجزة بالعربية لكل مصطلح طبي مذكور في هذه الصفحات. لكل مصطلح اكتب بالضبط:\n\n" +
                          "<المصطلح> - Definition: <تعريف سريري من 1–2 جملة بالعربية>\n\n" +
                          "مهم جداً: استخدم هذا الشكل بالضبط مع الشرطة والكلمة 'Definition'. افصل بين كل إدخال بسطر فارغ. لا تضف ترقيم أو نقاط أو أي تنسيق إضافي."
                        : $"In {generalLangName}, provide concise MEDICAL DEFINITIONS for each key medical term found on these page(s). " +
                          $"For each term, output EXACTLY in this format:\n\n" +
                          $"<Term Name> - Definition: <a 1–2 sentence clinical definition in {generalLangName}>\n\n" +
                          $"CRITICAL: Use this EXACT format with the dash and word 'Definition'. Separate each entry with ONE blank line. " +
                          $"Do NOT add numbering, bullets, or any extra formatting.";
                }
                else
                {
                    definitionsPrompt = targetArabic
                        ? "اكتب تعريفات موجزة بالعربية لكل مصطلح مهم في هذه الصفحات. لكل مصطلح اكتب بالضبط:\n\n" +
                          "<المصطلح> - Definition: <تعريف من 1–2 جملة بالعربية>\n\n" +
                          "مهم جداً: استخدم هذا الشكل بالضبط مع الشرطة والكلمة 'Definition'. افصل بين كل إدخال بسطر فارغ."
                        : $"In {generalLangName}, provide concise DEFINITIONS for each key term found on these page(s). " +
                          $"For each term, output EXACTLY in this format:\n\n" +
                          $"<Term Name> - Definition: <a 1–2 sentence definition in {generalLangName}>\n\n" +
                          $"CRITICAL: Use this EXACT format with the dash and word 'Definition'. Separate entries with ONE blank line.";
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
                    ? "أنشئ عبارات صح/خطأ بالعربية مبنية على هذه الصفحات. لكل إدخال اكتب بالضبط:\n\n" +
                      "Statement: <جملة يمكن الحكم عليها بالصواب أو الخطأ>\n" +
                      "Answer: <صحيح أو خطأ>\n\n" +
                      "مهم جداً: استخدم الكلمتين 'Statement:' و 'Answer:' بالضبط. اترك سطرًا فارغًا بين كل زوج. لا تضف شروحًا أو تفسيرات."
                    : $"Generate TRUE/FALSE statements (in {generalLangName}) based on these page(s). " +
                      $"For each entry, output EXACTLY:\n\n" +
                      $"Statement: <write a true-or-false sentence>\n" +
                      $"Answer: <True or False>\n\n" +
                      $"CRITICAL: Use the exact words 'Statement:' and 'Answer:'. " +
                      $"Leave exactly ONE blank line between each pair. " +
                      $"Do NOT provide explanations, justifications, or any extra text.";

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
                    $"Translate the text from {generalLangName} into {vocabLangName}. " +
                    $"CRITICAL FORMAT REQUIREMENTS:\n" +
                    $"1. Write ONLY the original sentence/paragraph\n" +
                    $"2. Leave ONE blank line\n" +
                    $"3. Write ONLY the translated sentence/paragraph\n" +
                    $"4. Leave ONE blank line before the next original text\n\n" +
                    $"Example format:\n" +
                    $"Original sentence in {generalLangName}.\n\n" +
                    $"Translated sentence in {vocabLangName}.\n\n" +
                    $"Next original sentence.\n\n" +
                    $"Next translation.\n\n" +
                    $"Do NOT add labels like 'Original:' or 'Translation:'. " +
                    $"Do NOT add bullet points, numbers, or any formatting. " +
                    $"Do NOT add explanations or notes. " +
                    $"ONLY output alternating original and translated paragraphs with blank lines between them.";


                // Explain Terms (رقم + IPA + مقاطع + بلوك عربي اختياري)
                string explainTermsPrompt;
                if (isMedical)
                {
                    explainTermsPrompt = targetArabic
                        ? "استخرج المصطلحات الطبية الأساسية. لكل مصطلح اكتب بالضبط:\n\n" +
                          "<المصطلح> - Definition: <شرح واضح من 2-3 جمل بالعربية>\n\n" +
                          "مهم جداً: استخدم الشكل '<المصطلح> - Definition: <الشرح>' بالضبط. افصل بين كل مصطلح بسطر فارغ واحد. لا تضف ترقيم أو نطق أو أي تنسيق إضافي."
                        : $"Identify KEY MEDICAL TERMS on these page(s). " +
                          $"For EACH term, output EXACTLY:\n\n" +
                          $"<Term> - Definition: <clear 2-3 sentence explanation in {generalLangName}>\n\n" +
                          $"CRITICAL: Use the format '<Term> - Definition: <explanation>' EXACTLY. " +
                          $"Separate each term with ONE blank line. " +
                          $"Do NOT add numbering, pronunciation, IPA, syllables, or any extra formatting.";
                }
                else
                {
                    explainTermsPrompt = targetArabic
                        ? "استخرج المصطلحات التقنية الأساسية. لكل مصطلح اكتب بالضبط:\n\n" +
                          "<المصطلح> - Definition: <شرح واضح من 2-3 جمل بالعربية>\n\n" +
                          "مهم جداً: استخدم الشكل '<المصطلح> - Definition: <الشرح>' بالضبط. افصل بين كل مصطلح بسطر فارغ واحد."
                        : $"Identify KEY TECHNICAL TERMS on these page(s). " +
                          $"For EACH term, output EXACTLY:\n\n" +
                          $"<Term> - Definition: <clear 2-3 sentence explanation in {generalLangName}>\n\n" +
                          $"CRITICAL: Use the format '<Term> - Definition: <explanation>' EXACTLY. " +
                          $"Separate each term with ONE blank line.";
                }




                // 6) تحديد حجم الدفعة (batch size) من الواجهة
                int batchSize = (int)radioPageBatchSize.EditValue; // reads 1, 2 or 3


                // Create container with prompt strings (null = section disabled)
                var prompts = new ContentPrompts
                {
                    Definitions = chkDefinitions.Checked ? definitionsPrompt : null,
                    MCQs = chkMCQs.Checked ? mcqsPrompt : null,
                    Flashcards = chkFlashcards.Checked ? flashcardsPrompt : null,
                    Vocabulary = chkVocabulary.Checked ? vocabularyPrompt : null,
                    Summary = chkSummary.Checked ? summaryPrompt : null,
                    Takeaways = chkTakeaways.Checked ? takeawaysPrompt : null,
                    Cloze = chkCloze.Checked ? clozePrompt : null,
                    TrueFalse = chkTrueFalse.Checked ? trueFalsePrompt : null,
                    Outline = chkOutline.Checked ? outlinePrompt : null,
                    ConceptMap = chkConceptMap.Checked ? conceptMapPrompt : null,
                    TableExtract = chkTableExtract.Checked ? tableExtractPrompt : null,
                    Simplified = chkSimplified.Checked ? simplifiedPrompt : null,
                    CaseStudy = chkCaseStudy.Checked ? caseStudyPrompt : null,
                    Keywords = chkKeywords.Checked ? keywordsPrompt : null,
                    TranslatedSections = chkTranslatedSections.Checked ? translatedSectionsPrompt : null,
                    ExplainTerms = chkExplainTerms.Checked ? explainTermsPrompt : null
                };

                // Create container with references to the StringBuilders
                var builders = new ContentBuilders
                {
                    Definitions = allDefinitions,
                    MCQs = allMCQs,
                    Flashcards = allFlashcards,
                    Vocabulary = allVocabulary,
                    Summary = allSummary,
                    Takeaways = allTakeaways,
                    Cloze = allCloze,
                    TrueFalse = allTrueFalse,
                    Outline = allOutline,
                    ConceptMap = allConceptMap,
                    TableExtract = allTableExtract,
                    Simplified = allSimplified,
                    CaseStudy = allCaseStudy,
                    Keywords = allKeywords,
                    TranslatedSections = allTranslatedSections,
                    ExplainTerms = allExplainTerms
                };

                // Validate batch size
                if (batchSize < 1 || batchSize > 4)
                {
                    throw new InvalidOperationException($"Unexpected batchSize: {batchSize}");
                }

                // Process all batches - this single line replaces the entire 720-line switch statement!
                await ProcessAllBatchesAsync(allPages, batchSize, apiKey, modelName, prompts, builders);




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

                    // ✨ This line should be here
                    SaveDefinitionsToApkg(definitionsText, definitionsFilePath, "Definitions");
                }

                //// 7.2) ملف MCQs (يمكن تكييف تنسيق MCQs إذا أردتم تنسيقًا أضبط)
                if (chkMCQs.Checked)
                {
                    string mcqsRaw = allMCQs.ToString();
                    // 1) still save the Word version:
                    SaveContentToFile(mcqsRaw, mcqsFilePath, "MCQs");

                    // 2) now parse & save out a .csv/.tsv
                    var parsed = ParseMcqs(mcqsRaw);

                    // 3) ✨ NEW: Create .apkg file for direct Anki import
                    SaveMcqsToApkg(parsed, mcqsFilePath, "MCQs");
                }


                if (chkFlashcards.Checked)
                {
                    // 1) Word export stays as-is
                    string flashcardsRaw = allFlashcards.ToString();
                    SaveContentToFile(flashcardsRaw, flashcardsFilePath, "Flashcards");

                    // 2) Parse into (Front,Back) pairs
                    var parsed = ParseFlashcards(flashcardsRaw);


                    // 5) ✨ NEW: Create .apkg file for direct Anki import
                    SaveFlashcardsToApkg(parsed, flashcardsFilePath, "Flashcards");
                }


                // 7.4) ملف Vocabulary (بعد تطبيق FormatVocabulary على الناتج)
                if (chkVocabulary.Checked)
                {
                    // 1) Word export stays the same
                    string vocabularyText = FormatVocabulary(allVocabulary.ToString());
                    SaveContentToFile(vocabularyText, vocabularyFilePath, "Vocabulary");

                    // Parse vocabulary for Anki export
                    var records = vocabularyText
                        .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(line =>
                        {
                            var parts = line.Split(new[] { " - " }, 2, StringSplitOptions.None);
                            if (parts.Length >= 2)
                                return Tuple.Create(parts[0].Trim(), parts[1].Trim());
                            else
                                return Tuple.Create(line.Trim(), string.Empty);
                        })
                        .Where(t => !string.IsNullOrWhiteSpace(t.Item1))
                        .ToList();

                    SaveVocabularyToApkg(records, vocabularyFilePath, "Vocabulary");
                }


                if (chkSummary.Checked)
                    SaveContentToFile(allSummary.ToString(), summaryFilePath, "Page Summaries");

                if (chkTakeaways.Checked)
                    SaveContentToFile(allTakeaways.ToString(), takeawaysFilePath, "Key Takeaways");

                //if (chkCloze.Checked)
                //{
                //    // 1) Word export (unchanged)
                //    string clozeRaw = allCloze.ToString();
                //    SaveContentToFile(clozeRaw, clozeFilePath, "Fill-in-the-Blank (Cloze)");

                //    // 2) Delimited export for Anki
                //    var parsed = ParseCloze(clozeRaw);                     // your (sentence,answer) pairs
                //    string outPath = Path.ChangeExtension(clozeFilePath, ext);

                //    using (var sw = new StreamWriter(outPath, false, Encoding.UTF8))
                //    {
                //        // _no header_ → Anki will import every line into the Text field
                //        foreach (var (sentence, answer) in parsed)
                //        {
                //            // inject the {{c1::answer}} into the blank
                //            var markup = $"{{{{c1::{answer}}}}}";
                //            var line = sentence.Replace("_______________", markup);

                //            // if CSV and the line itself has commas or newlines, wrap in quotes
                //            if (useComma && (line.Contains(',') || line.Contains('\n')))
                //                line = $"\"{line.Replace("\"", "\"\"")}\"";

                //            sw.WriteLine(line);
                //        }
                //    }

                //    UpdateStatus($"✅ Cloze exports saved: {Path.GetFileName(clozeFilePath)} and {Path.GetFileName(outPath)}");
                //}

                if (chkCloze.Checked)
                {
                    string clozeRaw = allCloze.ToString();
                    SaveContentToFile(clozeRaw, clozeFilePath, "Cloze Deletions");

                    // Export to Anki
                    var parsed = ParseCloze(clozeRaw);
                    SaveClozeToApkg(parsed, clozeFilePath, "Cloze");
                }

                if (chkTrueFalse.Checked)
                {
                    string trueFalseText = allTrueFalse.ToString();
                    SaveContentToFile(trueFalseText, tfFilePath, "True/False Questions");

                    // ✨ NEW: Create .apkg file
                    SaveTrueFalseToApkg(trueFalseText, tfFilePath, "TrueFalse");
                }

                if (chkOutline.Checked)
                    SaveContentToFile(allOutline.ToString(), outlineFilePath, "Outline");

                if (chkConceptMap.Checked)
                    SaveContentToFile(allConceptMap.ToString(), conceptMapFilePath, "Concept Relationships");

                if (chkTableExtract.Checked)
                {
                    SaveMarkdownTablesToWord(allTableExtract.ToString(), tableFilePath, "Table Extractions");
                    SaveTableExtractToApkg(allTableExtract.ToString(), tableFilePath, "Tables");
                }


                if (chkSimplified.Checked)
                    SaveContentToFile(allSimplified.ToString(), simplifiedFilePath, "Simplified Explanation");

                if (chkCaseStudy.Checked)
                    SaveContentToFile(allCaseStudy.ToString(), caseStudyFilePath, "Case Study Scenario");

                if (chkKeywords.Checked)
                    SaveContentToFile(allKeywords.ToString(), keywordsFilePath, "High-Yield Keywords");

                if (chkTranslatedSections.Checked)
                {
                    string translatedText = allTranslatedSections.ToString();
                    SaveContentToFile(translatedText, translatedSectionsFilePath, "Translated Sections");

                    // ✨ NEW: Create .apkg file
                    SaveTranslatedSectionsToApkg(translatedText, translatedSectionsFilePath, "Translated Sections");
                }

                if (chkExplainTerms.Checked)
                {
                    string explainTermsText = allExplainTerms.ToString();
                    SaveContentToFile(explainTermsText, explainTermsFilePath, "Explain Terms");

                    // ✨ NEW: Create .apkg file
                    SaveExplainTermsToApkg(explainTermsText, explainTermsFilePath, "Explain Terms");
                }

                UpdateOverlayLog("✅ All selected exports finished successfully.");


                //// 8) إظهار رسالة انتهاء المعالجة
                UpdateOverlayLog("✅ Processing complete. Files saved to Desktop as selected outputs.");
                UpdateOverlayLog("▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽▼▽");
                UpdateOverlayLog("▰▰▰▰▰ E N D   G E N E R A T I N G ▰▰▰▰▰");
                UpdateOverlayLog("------------------------------------------------------");
                UpdateOverlayLog("                                                     ");
                UpdateOverlayLog("                                                     ");
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
    
                // Dispose all images to prevent memory leaks
                if (allPages != null)
                {
                    foreach (var (pageNumber, image) in allPages)
                    {
                        image?.Dispose();
                    }
                }
            }
        }

      
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
            string modelName = comboBoxEditModel.SelectedItem?.ToString() ?? "";
            string modelType = GetReasoningModelType(modelName);

            UpdateStatus("▶ Model changed, saving selection...");
            SaveApiKeyAndModel();

            // Show model capabilities
            bool hasReasoning = (modelType != "none");

            if (hasReasoning)
            {
                // Reasoning model selected - enable reasoning effort
                comboBoxReasoningEffort.Enabled = true;
                comboBoxReasoningEffort.Properties.Appearance.ForeColor = System.Drawing.Color.Blue;
                UpdateStatus($"▶ 🧠 {modelName} supports: Vision ✓ + Reasoning ✓");
                UpdateStatus($"▶ Current reasoning effort: {comboBoxReasoningEffort.Text}");
            }
            else
            {
                // Non-reasoning model (but still has vision!)
                comboBoxReasoningEffort.Enabled = false;
                comboBoxReasoningEffort.Properties.Appearance.ForeColor = System.Drawing.Color.Gray;
                UpdateStatus($"▶ {modelName} supports: Vision ✓ (no reasoning)");
            }
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

                    var response = await _http.PostAsync("v1/chat/completions", content, cts.Token);
                    string body = await response.Content.ReadAsStringAsync(); // بدون Token في .NET Framework

                    if (!response.IsSuccessStatusCode)
                        throw new Exception("API Error: " + (int)response.StatusCode + " - " + body);

                    return body; // JSON خام
                }
                catch (TaskCanceledException)
                {
                    // Check if cancellation was requested (timeout) vs network issue
                    if (cts.IsCancellationRequested || attempt == maxRetries) throw;
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
        // ===========================================================================
        // FUNCTION 1: ProcessPdfPageMultimodal (Single Page Processing)
        // ===========================================================================
        // This function processes ONE page at a time

        /// <summary>
        /// Processes a single PDF page with multimodal vision API.
        /// Now includes reasoning effort control for o-series models.
        /// </summary>
        private async Task<string> ProcessPdfPageMultimodal(
            SDImage image, string apiKey, string taskPrompt, string modelName)
        {
            // Step 1: Resize and compress the image to reduce upload time
            string base64;
            using (var scaled = ResizeForApi(image, 1024))
            {
                base64 = ToBase64Jpeg(scaled, 80L);
            }

            // Step 2: Detect if this is an o-series reasoning model
            bool isReasoningModel = IsReasoningModel(modelName);

            // Step 3: Build the request body
            // For o-series models, we add the reasoning_effort parameter
            object requestBody;

            if (isReasoningModel)
            {
                // O-series model: Add reasoning_effort
                requestBody = new
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
                    },
                    reasoning_effort = GetReasoningEffort(modelName)  // ← Can be "low, medium, high" totally controlled from UI
                };
            }
            else
            {
                // Regular model (GPT-4o, GPT-5, etc.): No reasoning_effort needed
                requestBody = new
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
            }

            // Step 4: Serialize to JSON
            string jsonContent = System.Text.Json.JsonSerializer.Serialize(
                requestBody,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }
            );

            // Step 5: Send request with retry logic
            const int maxRetries = 4;
            int delayMs = 1200;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(7));
                try
                {
                    using (var req = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions"))
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

                            // Parse response
                            var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(resultJson);
                            var text = jsonNode?["choices"]?[0]?["message"]?["content"]?.ToString();
                            return string.IsNullOrEmpty(text) ? "No content returned." : text;
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    if (cts.IsCancellationRequested || attempt == maxRetries) throw;
                    await Task.Delay(delayMs);
                    delayMs *= 2;
                }
                catch (Exception ex)
                {
                    // Log the full error for debugging
                    UpdateOverlayLog($"❌ API Error: {ex.Message}");

                    // Check if it's a reasoning_effort error
                    if (ex.Message.Contains("reasoning_effort") && ex.Message.Contains("Unsupported"))
                    {
                        UpdateOverlayLog("❌ ERROR: Invalid reasoning_effort for this model!");
                        UpdateOverlayLog("▶ Tip: This model doesn't support the selected reasoning level.");
                        UpdateOverlayLog($"▶ Model type: {GetReasoningModelType(modelName)}");

                        // Suggest fix
                        if (ex.Message.Contains("minimal"))
                        {
                            UpdateOverlayLog("▶ Fix: Remove 'minimal' - use Low, Medium, or High instead");
                        }
                    }

                    // Check if it's a vision/image error (THE NEW ERROR YOU'RE GETTING!)
                    if (ex.Message.Contains("image_url") &&
                        (ex.Message.Contains("not supported") || ex.Message.Contains("only supported by certain")))
                    {
                        UpdateOverlayLog("❌ ERROR: This model does NOT support images!");
                        UpdateOverlayLog($"▶ You selected: {modelName}");
                        UpdateOverlayLog("▶ This model cannot process PDF images via API.");
                        UpdateOverlayLog("");
                        UpdateOverlayLog("💡 SOLUTION: Select a vision-capable model:");
                        UpdateOverlayLog("  • gpt-5.2 (recommended)");
                        UpdateOverlayLog("  • o3 (best reasoning with vision)");
                        UpdateOverlayLog("  • o4-mini (fast reasoning with vision)");
                        UpdateOverlayLog("  • gpt-4o (reliable, no reasoning)");
                    }

                    // Re-throw the exception
                    throw;
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
        // ===========================================================================
        // FUNCTION 2: ProcessPdfPagesMultimodal (Multiple Pages Processing)
        // ===========================================================================
        // This function processes MULTIPLE pages at once (2, 3, or 4 pages)

        /// <summary>
        /// Processes multiple PDF pages with multimodal vision API.
        /// Now includes reasoning effort control for o-series models.
        /// </summary>
        private async Task<string> ProcessPdfPagesMultimodal(
            List<(int pageNumber, SDImage image)> pageGroup,
            string apiKey,
            string taskPrompt,
            string modelName)
        {
            // Step 1: Convert all images to base64
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

            // Step 2: Build the content array (images + text prompt)
            var fullContent = new List<object>();
            fullContent.AddRange(imageContents);
            fullContent.Add(new { type = "text", text = taskPrompt });

            // Step 3: Detect if this is an o-series reasoning model
            bool isReasoningModel = IsReasoningModel(modelName);

            // Step 4: Build the request body
            object requestBody;

            if (isReasoningModel)
            {
                // O-series model: Add reasoning_effort
                requestBody = new
                {
                    model = modelName,
                    messages = new object[]
                    {
            new { role = "user", content = fullContent.ToArray() }
                    },
                    reasoning_effort = GetReasoningEffort(modelName) // ← Can be "low, medium, high" totally controlled from UI
                };
            }
            else
            {
                // Regular model: No reasoning_effort
                requestBody = new
                {
                    model = modelName,
                    messages = new object[]
                    {
                new { role = "user", content = fullContent.ToArray() }
                    }
                };
            }

            // Step 5: Serialize to JSON
            string jsonContent = System.Text.Json.JsonSerializer.Serialize(
                requestBody,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }
            );

            // Step 6: Send request with retry logic
            const int maxRetries = 4;
            int delayMs = 1200;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(7));
                try
                {
                    using (var req = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions"))
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
                                    throw new Exception("Transient: " + status + " – " + resultJson);

                                throw new Exception("API Error: " + status + " – " + resultJson);
                            }

                            // Parse response
                            var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(resultJson);
                            var text = jsonNode?["choices"]?[0]?["message"]?["content"]?.ToString();
                            return string.IsNullOrEmpty(text) ? "No content returned." : text;
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    if (cts.IsCancellationRequested || attempt == maxRetries) throw;
                    await Task.Delay(delayMs);
                    delayMs *= 2;
                }
                catch (Exception ex)
                {
                    if (!ex.Message.StartsWith("Transient") || attempt == maxRetries) throw;
                    await Task.Delay(delayMs);
                    delayMs *= 2;
                }
                finally
                {
                    cts.Dispose();
                }
            }

            return "No content returned.";
        }


        // ===========================================================================
        // HELPER FUNCTION: IsReasoningModel
        // ===========================================================================
        /// <summary>
        /// Checks if the given model supports reasoning_effort parameter.
        /// Returns the model type: "none", "o-series", "gpt5", or "gpt5-codex-max"
        /// </summary>
        private string GetReasoningModelType(string modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return "none";

            string model = modelName.ToLower().Trim();

            // O-series models with vision support
            if (model == "o3" || model == "o4-mini")
                return "o-series";

            // GPT-5 series (all support vision + reasoning)
            if (model.StartsWith("gpt-5"))
            {
                // Special handling for different GPT-5 variants
                if (model.Contains("codex-max"))
                    return "gpt5-codex-max";
                if (model.Contains("codex"))
                    return "gpt5-codex";
                return "gpt5";
            }

            // All other models don't support reasoning_effort
            return "none";
        }

        /// <summary>
        /// Simple check if model supports ANY reasoning_effort
        /// </summary>
        private bool IsReasoningModel(string modelName)
        {
            if (string.IsNullOrEmpty(modelName)) return false;

            string model = modelName.ToLower().Trim();

            // O-series models that SUPPORT vision (o3, o4-mini only)
            if (model == "o3" || model == "o4-mini")
                return true;

            // GPT-5 series models (all support vision + reasoning)
            if (model.StartsWith("gpt-5"))
                return true;

            // All other models
            return false;
        }

        /// <summary>
        /// Gets the appropriate reasoning effort for the given model.
        /// Returns null if model doesn't support reasoning_effort.
        /// Returns the correct value based on model type.
        /// </summary>
        /// 

        private string GetReasoningEffort(string modelName)
        {
            // Only return reasoning effort for reasoning models
            if (!IsReasoningModel(modelName))
                return null;

            // If user selected "Auto", use medium as default
            if (selectedReasoningEffort == "auto")
                return "medium";

            // Return the user's selection
            return selectedReasoningEffort;
        }


        #region Batch Processing Helper Methods

        /// <summary>
        /// Processes all pages in batches of the specified size.
        /// Replaces the 4-case switch statement with a single unified implementation.
        /// </summary>
        /// <param name="allPages">List of all pages to process</param>
        /// <param name="batchSize">Number of pages per batch (1-4)</param>
        /// <param name="apiKey">OpenAI API key</param>
        /// <param name="modelName">Model name (e.g., "gpt-4o")</param>
        /// <param name="prompts">Container with prompt strings (null = section disabled)</param>
        /// <param name="builders">Container with StringBuilders to accumulate results</param>
        private async Task ProcessAllBatchesAsync(
            List<(int pageNumber, SDImage image)> allPages,
            int batchSize,
            string apiKey,
            string modelName,
            ContentPrompts prompts,
            ContentBuilders builders)
        {
            for (int i = 0; i < allPages.Count; i += batchSize)
            {
                // Build a group of up to batchSize pages
                var pageGroup = new List<(int pageNumber, SDImage image)>();
                for (int j = i; j < i + batchSize && j < allPages.Count; j++)
                {
                    pageGroup.Add(allPages[j]);
                }

                // Create header label for this batch
                int startPage = pageGroup.First().pageNumber;
                int endPage = pageGroup.Last().pageNumber;
                string header = (startPage == endPage)
                    ? $"===== Page {startPage} ====="
                    : $"===== Pages {startPage}–{endPage} =====";

                // Process all enabled sections for this batch
                await ProcessPageBatchAsync(pageGroup, apiKey, modelName, prompts, builders, header, startPage, endPage);

                // Log completion
                string completionMsg = (startPage == endPage)
                    ? $"Page {startPage}"
                    : $"Pages {startPage}–{endPage}";
                UpdateOverlayLog($"▶▶▶ ✅ {completionMsg} done.");
            }
        }

        /// <summary>
        /// Processes a single batch of pages for all enabled content types.
        /// </summary>
        private async Task ProcessPageBatchAsync(
            List<(int pageNumber, SDImage image)> pageGroup,
            string apiKey,
            string modelName,
            ContentPrompts prompts,
            ContentBuilders builders,
            string header,
            int startPage,
            int endPage)
        {
            // Local helper function to process a single section
            // This avoids repeating the same pattern 16 times
            async Task ProcessSectionAsync(StringBuilder builder, string prompt, string sectionName)
            {
                // Skip if section is disabled (builder is null) or prompt is empty
                if (builder == null || string.IsNullOrEmpty(prompt))
                    return;

                // Log which section we're processing
                string pageLabel = (startPage == endPage)
                    ? $"page {startPage}"
                    : $"pages {startPage}–{endPage}";
                UpdateOverlayLog($"▶▶▶ Sending {pageLabel} to GPT ({sectionName})...");

                // Call the appropriate API method based on batch size
                string result;
                if (pageGroup.Count == 1)
                {
                    // Single page - use the single-image method
                    result = await ProcessPdfPageMultimodal(pageGroup[0].image, apiKey, prompt, modelName);
                }
                else
                {
                    // Multiple pages - use the multi-image method
                    result = await ProcessPdfPagesMultimodal(pageGroup, apiKey, prompt, modelName);
                }

                // Append results to the builder
                builder.AppendLine(header);
                builder.AppendLine(result);
                builder.AppendLine();
            }

            // Process each section in order (matches your original code order)
            await ProcessSectionAsync(builders.Definitions, prompts.Definitions, "Definitions");
            await ProcessSectionAsync(builders.MCQs, prompts.MCQs, "MCQs");
            await ProcessSectionAsync(builders.Flashcards, prompts.Flashcards, "Flashcards");
            await ProcessSectionAsync(builders.Vocabulary, prompts.Vocabulary, "Vocabulary");
            await ProcessSectionAsync(builders.Summary, prompts.Summary, "Summary");
            await ProcessSectionAsync(builders.Takeaways, prompts.Takeaways, "Key Takeaways");
            await ProcessSectionAsync(builders.Cloze, prompts.Cloze, "Cloze");
            await ProcessSectionAsync(builders.TrueFalse, prompts.TrueFalse, "True/False");
            await ProcessSectionAsync(builders.Outline, prompts.Outline, "Outline");
            await ProcessSectionAsync(builders.ConceptMap, prompts.ConceptMap, "Concept Map");
            await ProcessSectionAsync(builders.TableExtract, prompts.TableExtract, "Table Extract");
            await ProcessSectionAsync(builders.Simplified, prompts.Simplified, "Simplified Explanation");
            await ProcessSectionAsync(builders.CaseStudy, prompts.CaseStudy, "Case Study");
            await ProcessSectionAsync(builders.Keywords, prompts.Keywords, "Keywords");
            await ProcessSectionAsync(builders.TranslatedSections, prompts.TranslatedSections, "Translated Sections");
            await ProcessSectionAsync(builders.ExplainTerms, prompts.ExplainTerms, "Explain Terms");
        }

        #endregion

        // HttpClient مُشترك للتطبيق كله (أفضل ممارسة + نتفادى مشاكل المنافذ/المهلات)
        private static readonly HttpClient _http = new HttpClient(
            new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            })
        {
            Timeout = TimeSpan.FromMinutes(6), // زدها إذا تحتاج
            BaseAddress = new Uri("https://api.openai.com/")
        };

        // (اختياري) تأكيد TLS 1.2
        static Form1()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        }

        // تصغير الصورة قبل الإرسال
        private static SDImage ResizeForApi(SDImage src, int maxWidth = 1280)
        {
            if (src.Width <= maxWidth) return new Bitmap(src);
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
            // Semi-transparent modern overlay
            overlayPanel = new Panel
            {
                Size = this.ClientSize,
                BackColor = Color.FromArgb(220, 15, 20, 35),
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            };

            int centerX = overlayPanel.Width / 2;
            int centerY = overlayPanel.Height / 2;

            // Modern card with gradient
            var cardPanel = new Panel
            {
                Size = new Size(900, 500),
                Location = new Point(centerX - 450, centerY - 250),
                BackColor = Color.FromArgb(33, 47, 90),
                Anchor = AnchorStyles.None
            };

            // Draw gradient and border
            cardPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var rect = cardPanel.ClientRectangle;

                // Gradient background
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    rect,
                    Color.FromArgb(30, 40, 100),
                    Color.FromArgb(80, 40, 120),
                    System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                {
                    g.FillRectangle(brush, rect);
                }

                // Glow border
                using (var pen = new Pen(Color.FromArgb(150, 100, 150, 255), 3))
                {
                    g.DrawRectangle(pen, 1, 1, rect.Width - 3, rect.Height - 3);
                }
            };

            // Title with glow effect
            var titleLabel = new Label
            {
                Text = "🔄 PROCESSING",
                Location = new Point(0, 40),
                Size = new Size(900, 50),
                Font = new System.Drawing.Font("Segoe UI", 24, System.Drawing.FontStyle.Bold),  // ← FIXED
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            // Animated loading
            loadingIcon = new PictureBox
            {
                Size = new Size(180, 180),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = Properties.Resources.loading_gif,
                Location = new Point(360, 110),
                BackColor = Color.Transparent
            };

            // Status message
            statusLabel = new Label
            {
                Text = "⏳ Processing your document...",
                Location = new Point(50, 310),
                Size = new Size(800, 30),
                Font = new System.Drawing.Font("Segoe UI", 12),  // ← FIXED
                ForeColor = Color.FromArgb(200, 220, 255),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            // Modern log box
            logTextBox = new TextBox
            {
                Location = new Point(50, 350),
                Size = new Size(800, 120),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(20, 25, 50),
                ForeColor = Color.FromArgb(100, 255, 218),
                Font = new System.Drawing.Font("Consolas", 9),  // ← FIXED
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle  // ← FIXED
            };

            // Add to card
            cardPanel.Controls.Add(titleLabel);
            cardPanel.Controls.Add(loadingIcon);
            cardPanel.Controls.Add(statusLabel);
            cardPanel.Controls.Add(logTextBox);

            // Add to overlay
            overlayPanel.Controls.Add(cardPanel);
            this.Controls.Add(overlayPanel);

            // Keep centered on resize
            this.Resize += (s, e) =>
            {
                if (cardPanel != null)
                {
                    int cx = overlayPanel.Width / 2;
                    int cy = overlayPanel.Height / 2;
                    cardPanel.Location = new Point(cx - 450, cy - 250);
                }
            };
        }

        private void UpdateProgress(int percentage)
        {
            if (progressLabel != null && progressLabel.InvokeRequired)
            {
                progressLabel.Invoke(new Action(() => progressLabel.Text = $"{percentage}%"));
            }
            else if (progressLabel != null)
            {
                progressLabel.Text = $"{percentage}%";
            }
        }

        private void UpdateOverlayLog(string message)
        {
            var textBox = logTextBox; // Create local copy for thread safety
            if (textBox == null) return; // prevent error if not initialized

            if (textBox.InvokeRequired)
            {
                textBox.Invoke(new System.Action(() => textBox.AppendText(message + Environment.NewLine)));
            }
            else
            {
                textBox.AppendText(message + Environment.NewLine);
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
                // only add if we got at least a question and answer
                if (!string.IsNullOrEmpty(front) && !string.IsNullOrEmpty(back))
                    cards.Add((front, back));
            }
            return cards;
        }


        

        /// <summary>
        /// Creates an Anki .apkg file from flashcard data
        /// </summary>

        
        
        /// <summary>
        /// Helper to parse definitions from raw text
        /// Expects format: "Term: Definition" or blocks separated by blank lines
        /// </summary>
        private List<(string Term, string Definition)> ParseDefinitions(string rawText)
        {
            var definitions = new List<(string, string)>();

            // Split by blank lines (definition blocks)
            var blocks = Regex.Split(rawText.Trim(), @"\r?\n\s*\r?\n");

            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block)) continue;

                var trimmed = block.Trim();

                // ✨ Handle format "Term - Definition: explanation"
                // Example: "Hepatic lobule - Definition: The microscopic structural unit..."
                var match = Regex.Match(trimmed, @"^(.+?)\s*-\s*Definition:\s*(.+)$", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string termText = match.Groups[1].Value.Trim();
                    string defText = match.Groups[2].Value.Trim();

                    if (!string.IsNullOrWhiteSpace(termText) && !string.IsNullOrWhiteSpace(defText))
                    {
                        definitions.Add((termText, defText));
                        continue;
                    }
                }

                // Fallback: Try other patterns
                var lines = block.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0) continue;

                string currentTerm = null;
                string currentDefinition = null;

                foreach (var line in lines)
                {
                    var lineTrimmed = line.Trim();

                    // Pattern: "Term: xxx"
                    if (lineTrimmed.StartsWith("Term:", StringComparison.OrdinalIgnoreCase))
                    {
                        currentTerm = lineTrimmed.Substring(5).Trim();
                    }
                    // Pattern: "Definition: xxx"
                    else if (lineTrimmed.StartsWith("Definition:", StringComparison.OrdinalIgnoreCase))
                    {
                        currentDefinition = lineTrimmed.Substring(11).Trim();
                    }
                    // Pattern: "xxx: yyy" (first colon splits term and definition)
                    else if (lineTrimmed.Contains(":") && currentTerm == null)
                    {
                        var colonIndex = lineTrimmed.IndexOf(':');
                        currentTerm = lineTrimmed.Substring(0, colonIndex).Trim();
                        currentDefinition = lineTrimmed.Substring(colonIndex + 1).Trim();
                    }
                    // Multi-line: append to definition
                    else if (!string.IsNullOrWhiteSpace(lineTrimmed))
                    {
                        if (currentDefinition != null)
                        {
                            currentDefinition += " " + lineTrimmed;
                        }
                        else if (currentTerm != null)
                        {
                            currentDefinition = lineTrimmed;
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(currentTerm) && !string.IsNullOrWhiteSpace(currentDefinition))
                {
                    definitions.Add((currentTerm, currentDefinition));
                }
            }

            return definitions;
        }

        
        /// <summary>
        /// Helper to parse True/False questions
        /// Expects: Statement, Answer: True/False, Explanation (optional)
        /// </summary>
        private List<(string Statement, string Answer, string Explanation)> ParseTrueFalse(string rawText)
        {
            var questions = new List<(string, string, string)>();

            // Split by blank lines
            var blocks = Regex.Split(rawText.Trim(), @"\r?\n\s*\r?\n");

            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block)) continue;

                string statement = null;
                string answer = null;

                var lines = block.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed)) continue;

                    if (trimmed.StartsWith("Statement:", StringComparison.OrdinalIgnoreCase))
                    {
                        statement = trimmed.Substring(10).Trim();
                    }
                    else if (trimmed.StartsWith("Answer:", StringComparison.OrdinalIgnoreCase))
                    {
                        answer = trimmed.Substring(7).Trim();
                    }
                }

                // Validate and add
                if (!string.IsNullOrWhiteSpace(statement) &&
                    !string.IsNullOrWhiteSpace(answer) &&
                    statement.Length > 5 &&
                    (answer.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                     answer.Equals("False", StringComparison.OrdinalIgnoreCase) ||
                     answer.Equals("صحيح", StringComparison.OrdinalIgnoreCase) ||
                     answer.Equals("خطأ", StringComparison.OrdinalIgnoreCase)))
                {
                    questions.Add((statement, answer, ""));  // No explanation in new format
                }
            }

            return questions;
        }



        /// <summary>
        /// FIXED Parser for Translated Sections
        /// Handles the format: English paragraph, then Arabic paragraph, separated by blank lines
        /// </summary>

        private List<(string Original, string Translation)> ParseTranslatedSections(string rawText)
        {
            var sections = new List<(string, string)>();

            // Remove page markers
            rawText = Regex.Replace(rawText, @"=+\s*Page\s+\d+\s*=+", "", RegexOptions.IgnoreCase);
            rawText = Regex.Replace(rawText, @"Translated Sections", "", RegexOptions.IgnoreCase);

            // Split by blank lines
            var blocks = Regex.Split(rawText.Trim(), @"(?:\r?\n){2,}");

            var cleanBlocks = new List<string>();

            foreach (var block in blocks)
            {
                var trimmed = block.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length < 3)
                    continue;

                cleanBlocks.Add(trimmed);
            }

            // Pair everything: English followed by Arabic
            for (int i = 0; i < cleanBlocks.Count - 1; i++)
            {
                string current = cleanBlocks[i];
                string next = cleanBlocks[i + 1];

                bool currentIsEnglish = !Regex.IsMatch(current, @"[\u0600-\u06FF]");
                bool nextIsArabic = Regex.IsMatch(next, @"[\u0600-\u06FF]");

                if (currentIsEnglish && nextIsArabic)
                {
                    sections.Add((current, next));
                    i++; // Skip next block
                }
            }

            return sections;
        }


        /// <summary>
        /// Universal Anki deck creator using Python genanki
        /// </summary>
        private void CreateAnkiDeck(string deckName, List<Dictionary<string, string>> cards,
                           List<string> fieldNames, string template, string outputPath)
        {
            if (!pythonInitialized)
            {
                UpdateOverlayLog("⚠️ Python not ready yet - skipping .apkg export");
                return;
            }

            try
            {
                // Create temp files
                string tempDir = Path.Combine(Path.GetTempPath(), "AnkiExport_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(tempDir);

                string scriptPath = Path.Combine(tempDir, "create_deck.py");
                string dataPath = Path.Combine(tempDir, "data.json");

                // FIXED: Split template properly into front and back
                string[] templateParts = template.Split(new[] { "<hr id='answer'>" }, StringSplitOptions.None);
                string qfmt = templateParts.Length > 0 ? templateParts[0] : template;
                string afmt = template; // Full template for answer side

                // Prepare data
                var data = new
                {
                    deckName = deckName,
                    cards = cards,
                    fields = fieldNames,
                    qfmt = qfmt,
                    afmt = afmt,
                    outputPath = outputPath
                };

                // Write JSON data (without BOM)
                File.WriteAllText(dataPath, JsonConvert.SerializeObject(data), new UTF8Encoding(false));

                // Create Python script with FIXED template handling
                string pythonScript = @"
import genanki
import json
import random

# Read data
with open(r'" + dataPath.Replace("\\", "\\\\") + @"', 'r', encoding='utf-8') as f:
    data = json.load(f)

# Generate unique IDs
model_id = random.randint(1000000000, 9999999999)
deck_id = random.randint(1000000000, 9999999999)

# Use the pre-split templates
qfmt = data['qfmt']
afmt = data['afmt']

# Create model
model = genanki.Model(
    model_id,
    data['deckName'] + ' Model',
    fields=[{'name': f} for f in data['fields']],
    templates=[{
        'name': 'Card 1',
        'qfmt': qfmt,
        'afmt': afmt
    }]
)

# Create deck
deck = genanki.Deck(deck_id, data['deckName'])

# Add notes
for card in data['cards']:
    values = [card.get(f, '') for f in data['fields']]
    note = genanki.Note(model=model, fields=values)
    deck.add_note(note)

# Export
genanki.Package(deck).write_to_file(data['outputPath'])
print('SUCCESS')
";

                File.WriteAllText(scriptPath, pythonScript, new UTF8Encoding(false));

                // Find python.exe
                string pythonExe = Path.Combine(pythonHome, "python.exe");
                if (!File.Exists(pythonExe))
                {
                    UpdateOverlayLog($"❌ Python executable not found at: {pythonExe}");
                    return;
                }

                // Build PYTHONPATH - add all possible locations for site-packages
                List<string> pythonPaths = new List<string>();

                // 1. Main Lib and site-packages
                string libPath = Path.Combine(pythonHome, "Lib");
                string sitePackagesPath = Path.Combine(libPath, "site-packages");
                if (Directory.Exists(libPath)) pythonPaths.Add(libPath);
                if (Directory.Exists(sitePackagesPath)) pythonPaths.Add(sitePackagesPath);

                // 2. Alternate location
                string altSitePackages = Path.Combine(pythonHome, "site-packages");
                if (Directory.Exists(altSitePackages)) pythonPaths.Add(altSitePackages);

                // 3. Python home itself
                pythonPaths.Add(pythonHome);

                string pythonPathEnv = string.Join(";", pythonPaths.Distinct());

                // Execute Python script with PYTHONPATH set
                var psi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"\"{scriptPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = pythonHome
                };

                // Set PYTHONPATH environment variable
                psi.EnvironmentVariables["PYTHONPATH"] = pythonPathEnv;
                psi.EnvironmentVariables["PYTHONHOME"] = pythonHome;

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0 && output.Contains("SUCCESS"))
                    {
                        UpdateOverlayLog($"✅ Anki deck saved: {Path.GetFileName(outputPath)}");
                    }
                    else
                    {
                        UpdateOverlayLog($"❌ Python error: {error}");
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            UpdateOverlayLog($"Output: {output}");
                        }
                    }
                }

                // Cleanup temp directory
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            catch (Exception ex)
            {
                UpdateOverlayLog($"❌ Error creating Anki deck: {ex.Message}");
            }
        }


        private void VerifyGenankiInstallation()
        {
            try
            {
                string pythonExe = Path.Combine(pythonHome, "python.exe");

                var psi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "-c \"import sys; import genanki; print('genanki found at:', genanki.__file__); print('sys.path:', sys.path)\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    UpdateStatus("=== Genanki Installation Check ===");
                    UpdateStatus(output);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        UpdateStatus("Error: " + error);
                    }
                    UpdateStatus("===================================");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Verification failed: {ex.Message}");
            }
        }



        // ========================================
        // 1. SaveFlashcardsToApkg
        // ========================================

        private void SaveFlashcardsToApkg(List<(string Front, string Back)> cards,
                                          string outputPath, string deckName)
        {
            try
            {
                if (cards == null || cards.Count == 0)
                {
                    UpdateOverlayLog($"⚠️ No flashcards to export to Anki");
                    return;
                }

                string apkgPath = Path.ChangeExtension(outputPath, ".apkg");

                // Convert to dictionary format
                var cardData = cards.Select(c => new Dictionary<string, string>
        {
            { "Front", CleanTextForAnki(c.Front) },
            { "Back", CleanTextForAnki(c.Back) }
        }).ToList();

                // Define improved mobile-friendly template
                string template = @"
<style>
.card {
    font-family: Arial, Helvetica, sans-serif;
    font-size: 20px;
    text-align: center;
    color: black;
    background-color: white;
    padding: 20px;
    line-height: 1.6;
}
.front {
    font-size: 24px;
    font-weight: bold;
    margin-bottom: 20px;
}
.back {
    font-size: 20px;
    margin-top: 20px;
    padding: 15px;
    background-color: #f0f0f0;
    border-radius: 8px;
}
hr {
    border: none;
    border-top: 2px solid #4CAF50;
    margin: 30px 0;
}
@media (max-width: 600px) {
    .card { font-size: 18px; padding: 15px; }
    .front { font-size: 22px; }
    .back { font-size: 18px; }
}
</style>
<div class='card'>
    <div class='front'>{{Front}}</div>
</div>
<hr id='answer'>
<div class='card'>
    <div class='back'>{{Back}}</div>
</div>";

                // Create deck
                CreateAnkiDeck(deckName, cardData, new List<string> { "Front", "Back" },
                              template, apkgPath);
            }
            catch (Exception ex)
            {
                UpdateOverlayLog($"❌ Error creating Flashcards Anki deck: {ex.Message}");
            }
        }

        // ========================================
        // 2. SaveMcqsToApkg
        // ========================================

        private void SaveMcqsToApkg(List<McqItem> items, string outputPath, string deckName)
        {
            try
            {
                if (items == null || items.Count == 0)
                {
                    UpdateOverlayLog($"⚠️ No MCQs to export to Anki");
                    return;
                }

                string apkgPath = Path.ChangeExtension(outputPath, ".apkg");

                // Convert to dictionary format
                var cardData = items.Select(mcq => new Dictionary<string, string>
        {
            { "Question", CleanTextForAnki(mcq.Question) },
            { "Options", CleanTextForAnki($"A) {mcq.OptionA}<br><br>B) {mcq.OptionB}<br><br>C) {mcq.OptionC}<br><br>D) {mcq.OptionD}") },
            { "Answer", CleanTextForAnki(mcq.Answer) }
        }).ToList();

                // Define improved mobile-friendly template
                string template = @"
<style>
.card {
    font-family: Arial, Helvetica, sans-serif;
    font-size: 18px;
    text-align: left;
    color: black;
    background-color: white;
    padding: 20px;
    line-height: 1.6;
}
.question {
    font-size: 20px;
    font-weight: bold;
    margin-bottom: 20px;
    color: #2C3E50;
}
.options {
    font-size: 18px;
    margin: 15px 0;
    padding: 10px;
    background-color: #f9f9f9;
    border-left: 4px solid #3498db;
}
.answer {
    font-size: 20px;
    font-weight: bold;
    color: #27AE60;
    margin-top: 20px;
    padding: 15px;
    background-color: #E8F8F5;
    border-radius: 8px;
}
hr {
    border: none;
    border-top: 2px solid #3498db;
    margin: 30px 0;
}
@media (max-width: 600px) {
    .card { font-size: 16px; padding: 15px; }
    .question { font-size: 18px; }
    .options { font-size: 16px; }
}
</style>
<div class='card'>
    <div class='question'>{{Question}}</div>
    <div class='options'>{{Options}}</div>
</div>
<hr id='answer'>
<div class='card'>
    <div class='answer'>✓ Correct Answer: {{Answer}}</div>
</div>";

                // Create deck
                CreateAnkiDeck(deckName, cardData,
                              new List<string> { "Question", "Options", "Answer" },
                              template, apkgPath);
            }
            catch (Exception ex)
            {
                UpdateOverlayLog($"❌ Error creating MCQ Anki deck: {ex.Message}");
            }
        }

        // ========================================
        // 3. SaveVocabularyToApkg
        // ========================================

        private void SaveVocabularyToApkg(List<Tuple<string, string>> records,
                                          string outputPath, string deckName)
        {
            try
            {
                if (records == null || records.Count == 0)
                {
                    UpdateOverlayLog($"⚠️ No vocabulary to export to Anki");
                    return;
                }

                string apkgPath = Path.ChangeExtension(outputPath, ".apkg");

                // Convert to dictionary format
                var cardData = records.Select(r => new Dictionary<string, string>
        {
            { "Term", CleanTextForAnki(r.Item1) },
            { "Translation", CleanTextForAnki(r.Item2) }
        }).ToList();

                // Define improved mobile-friendly template
                string template = @"
<style>
.card {
    font-family: Arial, Helvetica, sans-serif;
    font-size: 20px;
    text-align: center;
    color: black;
    background-color: white;
    padding: 20px;
    line-height: 1.6;
}
.term {
    font-size: 28px;
    font-weight: bold;
    color: #8E44AD;
    margin: 20px 0;
}
.translation {
    font-size: 24px;
    margin-top: 20px;
    padding: 20px;
    background-color: #F4ECF7;
    border-radius: 8px;
    border: 2px solid #8E44AD;
}
hr {
    border: none;
    border-top: 2px solid #8E44AD;
    margin: 30px 0;
}
@media (max-width: 600px) {
    .term { font-size: 24px; }
    .translation { font-size: 20px; padding: 15px; }
}
</style>
<div class='card'>
    <div class='term'>{{Term}}</div>
</div>
<hr id='answer'>
<div class='card'>
    <div class='translation'>{{Translation}}</div>
</div>";

                // Create deck
                CreateAnkiDeck(deckName, cardData,
                              new List<string> { "Term", "Translation" },
                              template, apkgPath);
            }
            catch (Exception ex)
            {
                UpdateOverlayLog($"❌ Error creating Vocabulary Anki deck: {ex.Message}");
            }
        }

        // ========================================
        // 4. SaveDefinitionsToApkg
        // ========================================

        private void SaveDefinitionsToApkg(string rawText, string outputPath, string deckName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rawText))
                {
                    UpdateOverlayLog($"⚠️ No definitions to export to Anki");
                    return;
                }

                var definitions = ParseDefinitions(rawText);

                if (definitions.Count == 0)
                {
                    UpdateOverlayLog($"⚠️ No valid definitions found");
                    return;
                }

                string apkgPath = Path.ChangeExtension(outputPath, ".apkg");

                // Convert to dictionary format
                var cardData = definitions.Select(d => new Dictionary<string, string>
        {
            { "Term", CleanTextForAnki(d.Term) },
            { "Definition", CleanTextForAnki(d.Definition) }
        }).ToList();

                // Define improved mobile-friendly template
                string template = @"
<style>
.card {
    font-family: Arial, Helvetica, sans-serif;
    font-size: 18px;
    text-align: left;
    color: black;
    background-color: white;
    padding: 20px;
    line-height: 1.7;
}
.term {
    font-size: 26px;
    font-weight: bold;
    color: #16A085;
    margin-bottom: 10px;
    text-align: center;
}
.definition {
    font-size: 19px;
    margin-top: 20px;
    padding: 20px;
    background-color: #E8F6F3;
    border-radius: 8px;
    border-left: 5px solid #16A085;
    text-align: left;
}
hr {
    border: none;
    border-top: 2px solid #16A085;
    margin: 30px 0;
}
@media (max-width: 600px) {
    .card { font-size: 16px; padding: 15px; }
    .term { font-size: 22px; }
    .definition { font-size: 17px; padding: 15px; }
}
</style>
<div class='card'>
    <div class='term'>{{Term}}</div>
</div>
<hr id='answer'>
<div class='card'>
    <div class='definition'>{{Definition}}</div>
</div>";

                // Create deck
                CreateAnkiDeck(deckName, cardData,
                              new List<string> { "Term", "Definition" },
                              template, apkgPath);
            }
            catch (Exception ex)
            {
                UpdateOverlayLog($"❌ Error creating Definitions Anki deck: {ex.Message}");
            }
        }

        // ========================================
        // 5. SaveExplainTermsToApkg
        // ========================================

        private void SaveExplainTermsToApkg(string rawText, string outputPath, string deckName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rawText))
                {
                    UpdateOverlayLog($"⚠️ No terms to export to Anki");
                    return;
                }

                var terms = ParseDefinitions(rawText); // Reuse parser

                if (terms.Count == 0)
                {
                    UpdateOverlayLog($"⚠️ No valid terms found");
                    return;
                }

                string apkgPath = Path.ChangeExtension(outputPath, ".apkg");

                // Convert to dictionary format
                var cardData = terms.Select(t => new Dictionary<string, string>
        {
            { "Term", CleanTextForAnki(t.Term) },
            { "Explanation", CleanTextForAnki(t.Definition) }
        }).ToList();

                // Define improved mobile-friendly template
                string template = @"
<style>
.card {
    font-family: Arial, Helvetica, sans-serif;
    font-size: 18px;
    text-align: left;
    color: black;
    background-color: white;
    padding: 20px;
    line-height: 1.7;
}
.term {
    font-size: 26px;
    font-weight: bold;
    color: #D35400;
    margin-bottom: 10px;
    text-align: center;
}
.explanation {
    font-size: 19px;
    margin-top: 20px;
    padding: 20px;
    background-color: #FEF5E7;
    border-radius: 8px;
    border-left: 5px solid #D35400;
    text-align: left;
}
hr {
    border: none;
    border-top: 2px solid #D35400;
    margin: 30px 0;
}
@media (max-width: 600px) {
    .card { font-size: 16px; padding: 15px; }
    .term { font-size: 22px; }
    .explanation { font-size: 17px; padding: 15px; }
}
</style>
<div class='card'>
    <div class='term'>{{Term}}</div>
</div>
<hr id='answer'>
<div class='card'>
    <div class='explanation'>{{Explanation}}</div>
</div>";

                // Create deck
                CreateAnkiDeck(deckName, cardData,
                              new List<string> { "Term", "Explanation" },
                              template, apkgPath);
            }
            catch (Exception ex)
            {
                UpdateOverlayLog($"❌ Error creating Explain Terms Anki deck: {ex.Message}");
            }
        }

        // ========================================
        // 6. SaveTrueFalseToApkg
        // ========================================

        private void SaveTrueFalseToApkg(string rawText, string outputPath, string deckName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rawText))
                {
                    UpdateOverlayLog($"⚠️ No True/False questions to export to Anki");
                    return;
                }

                var questions = ParseTrueFalse(rawText);

                if (questions.Count == 0)
                {
                    UpdateOverlayLog($"⚠️ No valid True/False questions found");
                    return;
                }

                string apkgPath = Path.ChangeExtension(outputPath, ".apkg");

                // Convert to dictionary format
                var cardData = questions.Select(q => new Dictionary<string, string>
        {
            { "Statement", CleanTextForAnki(q.Statement) },
            { "Answer", CleanTextForAnki(q.Answer) }
        }).ToList();

                // Define improved mobile-friendly template
                string template = @"
<style>
.card {
    font-family: Arial, Helvetica, sans-serif;
    font-size: 19px;
    text-align: center;
    color: black;
    background-color: white;
    padding: 20px;
    line-height: 1.7;
}
.statement {
    font-size: 21px;
    margin: 20px 0;
    padding: 20px;
    background-color: #EBF5FB;
    border-radius: 8px;
    border: 2px solid #2980B9;
    text-align: left;
}
.answer {
    font-size: 26px;
    font-weight: bold;
    margin-top: 20px;
    padding: 20px;
    border-radius: 8px;
    color: #27AE60;
    background-color: #E8F8F5;
    border: 2px solid #27AE60;
}
hr {
    border: none;
    border-top: 2px solid #2980B9;
    margin: 30px 0;
}
@media (max-width: 600px) {
    .statement { font-size: 18px; padding: 15px; }
    .answer { font-size: 22px; padding: 15px; }
}
</style>
<div class='card'>
    <div class='statement'>{{Statement}}</div>
</div>
<hr id='answer'>
<div class='card'>
    <div class='answer'>{{Answer}}</div>
</div>";

                // Create deck
                CreateAnkiDeck(deckName, cardData,
                              new List<string> { "Statement", "Answer" },
                              template, apkgPath);
            }
            catch (Exception ex)
            {
                UpdateOverlayLog($"❌ Error creating True/False Anki deck: {ex.Message}");
            }
        }

        // ========================================
        // 7. SaveClozeToApkg
        // ========================================

        private void SaveClozeToApkg(List<(string Sentence, string Answer)> items,
                                     string outputPath, string deckName)
        {
            try
            {
                if (items == null || items.Count == 0)
                {
                    UpdateOverlayLog($"⚠️ No cloze items to export to Anki");
                    return;
                }

                string apkgPath = Path.ChangeExtension(outputPath, ".apkg");

                // Convert to dictionary format
                var cardData = items.Select(c => new Dictionary<string, string>
        {
            { "Sentence", CleanTextForAnki(c.Sentence) },
            { "Answer", CleanTextForAnki(c.Answer) }
        }).ToList();

                // Define improved mobile-friendly template
                string template = @"
<style>
.card {
    font-family: Arial, Helvetica, sans-serif;
    font-size: 20px;
    text-align: center;
    color: black;
    background-color: white;
    padding: 20px;
    line-height: 1.8;
}
.sentence {
    font-size: 22px;
    margin: 20px 0;
    padding: 20px;
    background-color: #FFF9E6;
    border-radius: 8px;
    border: 2px solid #FFD700;
}
.answer {
    font-size: 24px;
    font-weight: bold;
    color: #E74C3C;
    margin-top: 20px;
    padding: 15px;
    background-color: #FADBD8;
    border-radius: 8px;
}
hr {
    border: none;
    border-top: 2px solid #FFD700;
    margin: 30px 0;
}
@media (max-width: 600px) {
    .sentence { font-size: 18px; padding: 15px; }
    .answer { font-size: 20px; }
}
</style>
<div class='card'>
    <div class='sentence'>{{Sentence}}</div>
</div>
<hr id='answer'>
<div class='card'>
    <div class='answer'>{{Answer}}</div>
</div>";

                // Create deck
                CreateAnkiDeck(deckName, cardData,
                              new List<string> { "Sentence", "Answer" },
                              template, apkgPath);
            }
            catch (Exception ex)
            {
                UpdateOverlayLog($"❌ Error creating Cloze Anki deck: {ex.Message}");
            }
        }

        // ========================================
        // 8. SaveTranslatedSectionsToApkg
        // ========================================

        private void SaveTranslatedSectionsToApkg(string rawText, string outputPath, string deckName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rawText))
                {
                    UpdateOverlayLog($"⚠️ No translated sections to export to Anki");
                    return;
                }

                var sections = ParseTranslatedSections(rawText);

                if (sections.Count == 0)
                {
                    UpdateOverlayLog($"⚠️ No valid translated sections found");
                    return;
                }

                string apkgPath = Path.ChangeExtension(outputPath, ".apkg");

                // Convert to dictionary format
                var cardData = sections.Select(s => new Dictionary<string, string>
        {
            { "Original", CleanTextForAnki(s.Original) },
            { "Translation", CleanTextForAnki(s.Translation) }
        }).ToList();

                // Define improved mobile-friendly template
                string template = @"
<style>
.card {
    font-family: Arial, Helvetica, sans-serif;
    font-size: 18px;
    text-align: left;
    color: black;
    background-color: white;
    padding: 20px;
    line-height: 1.7;
}
.label {
    font-size: 14px;
    font-weight: bold;
    text-transform: uppercase;
    color: #7F8C8D;
    margin-bottom: 10px;
}
.original {
    font-size: 20px;
    padding: 20px;
    background-color: #E8F4F8;
    border-radius: 8px;
    border-left: 5px solid #2980B9;
    margin-bottom: 20px;
}
.translation {
    font-size: 20px;
    padding: 20px;
    background-color: #FEF5E7;
    border-radius: 8px;
    border-left: 5px solid #D68910;
    direction: rtl;
    text-align: right;
}
hr {
    border: none;
    border-top: 2px solid #2980B9;
    margin: 30px 0;
}
@media (max-width: 600px) {
    .original, .translation { font-size: 17px; padding: 15px; }
}
</style>
<div class='card'>
    <div class='label'>Original</div>
    <div class='original'>{{Original}}</div>
</div>
<hr id='answer'>
<div class='card'>
    <div class='label'>Translation</div>
    <div class='translation'>{{Translation}}</div>
</div>";

                // Create deck
                CreateAnkiDeck(deckName, cardData,
                              new List<string> { "Original", "Translation" },
                              template, apkgPath);
            }
            catch (Exception ex)
            {
                UpdateOverlayLog($"❌ Error creating Translated Sections Anki deck: {ex.Message}");
            }
        }


        // ========================================
        // 9. SaveTableExtractToApkg
        // ========================================
        private void SaveTableExtractToApkg(string rawText, string outputPath, string deckName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rawText))
                {
                    UpdateOverlayLog($"⚠️ No tables to export to Anki");
                    return;
                }

                var tables = ParseMarkdownTables(rawText);

                if (tables.Count == 0)
                {
                    UpdateOverlayLog($"⚠️ No valid tables found");
                    return;
                }

                string apkgPath = Path.ChangeExtension(outputPath, ".apkg");

                // Convert to dictionary format
                var cardData = tables.Select(t => new Dictionary<string, string>
        {
            { "Title", CleanTextForAnki(t.Title) },
            { "Table", t.HtmlTable } // Already HTML, no need to clean
        }).ToList();

                // Define improved mobile-friendly template
                string template = @"
<style>
.card {
    font-family: Arial, Helvetica, sans-serif;
    font-size: 16px;
    color: black;
    background-color: white;
    padding: 15px;
    line-height: 1.5;
}
.title {
    font-size: 22px;
    font-weight: bold;
    color: #2874A6;
    margin-bottom: 15px;
    text-align: center;
}
.table-container {
    overflow-x: auto;
    margin-top: 15px;
}
table {
    width: 100%;
    border-collapse: collapse;
    font-size: 15px;
    margin: 0 auto;
}
th {
    background-color: #2874A6;
    color: white;
    padding: 12px 8px;
    text-align: left;
    font-weight: bold;
}
td {
    padding: 10px 8px;
    border: 1px solid #ddd;
    background-color: white;
}
tr:nth-child(even) {
    background-color: #f2f2f2;
}
tr:hover {
    background-color: #e8f4f8;
}
hr {
    border: none;
    border-top: 2px solid #2874A6;
    margin: 20px 0;
}
@media (max-width: 600px) {
    .card { font-size: 14px; padding: 10px; }
    .title { font-size: 18px; }
    table { font-size: 13px; }
    th, td { padding: 8px 5px; }
}
</style>
<div class='card'>
    <div class='title'>{{Title}}</div>
</div>
<hr id='answer'>
<div class='card'>
    <div class='table-container'>
        {{Table}}
    </div>
</div>";

                // Create deck
                CreateAnkiDeck(deckName, cardData,
                              new List<string> { "Title", "Table" },
                              template, apkgPath);
            }
            catch (Exception ex)
            {
                UpdateOverlayLog($"❌ Error creating Table Extract Anki deck: {ex.Message}");
            }
        }

        /// Represents one parsed table with title and HTML content
        private class TableData
        {
            public string Title { get; set; }
            public string HtmlTable { get; set; }
        }

        /// Parse markdown tables from raw text into a list of TableData
        private List<TableData> ParseMarkdownTables(string rawText)
        {
            var tables = new List<TableData>();

            if (string.IsNullOrWhiteSpace(rawText))
                return tables;

            var lines = rawText.Replace("\r\n", "\n").Split('\n');
            var alignRow = new Regex(@"^\|\s*:?-+\s*(\|\s*:?-+\s*)+\|$");

            int i = 0;
            int tableNumber = 1;

            while (i < lines.Length)
            {
                string line = lines[i].Trim();

                // Look for table title (lines that start with "Table:" or "جدول:")
                string tableTitle = null;
                if (line.StartsWith("Table:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("جدول:", StringComparison.OrdinalIgnoreCase))
                {
                    tableTitle = line.Substring(line.IndexOf(':') + 1).Trim();
                    i++;
                    if (i >= lines.Length) break;
                    line = lines[i].Trim();
                }

                // Check if this line starts a table
                if (!line.StartsWith("|"))
                {
                    i++;
                    continue;
                }

                // Collect all consecutive table lines
                var tableLines = new List<string>();
                while (i < lines.Length && lines[i].Trim().StartsWith("|"))
                {
                    tableLines.Add(lines[i].Trim());
                    i++;
                }

                if (tableLines.Count == 0)
                    continue;

                // Parse table into rows
                var rows = new List<string[]>();
                bool firstRow = true;

                foreach (var tl in tableLines)
                {
                    // Skip alignment row (--- | --- | ---)
                    if (alignRow.IsMatch(tl))
                        continue;

                    // Parse cells
                    var inner = tl.Trim('|');
                    var cells = inner.Split('|').Select(c => c.Trim()).ToArray();

                    // First row is header
                    if (firstRow)
                    {
                        rows.Add(cells);
                        firstRow = false;
                    }
                    else
                    {
                        rows.Add(cells);
                    }
                }

                if (rows.Count == 0)
                    continue;

                // Convert to HTML
                var htmlTable = new StringBuilder();
                htmlTable.AppendLine("<table>");

                // Header row
                if (rows.Count > 0)
                {
                    htmlTable.AppendLine("<thead><tr>");
                    foreach (var cell in rows[0])
                    {
                        htmlTable.AppendLine($"<th>{CleanTextForAnki(cell)}</th>");
                    }
                    htmlTable.AppendLine("</tr></thead>");
                }

                // Data rows
                if (rows.Count > 1)
                {
                    htmlTable.AppendLine("<tbody>");
                    for (int r = 1; r < rows.Count; r++)
                    {
                        htmlTable.AppendLine("<tr>");
                        foreach (var cell in rows[r])
                        {
                            htmlTable.AppendLine($"<td>{CleanTextForAnki(cell)}</td>");
                        }
                        htmlTable.AppendLine("</tr>");
                    }
                    htmlTable.AppendLine("</tbody>");
                }

                htmlTable.AppendLine("</table>");

                // Create table data
                tables.Add(new TableData
                {
                    Title = string.IsNullOrWhiteSpace(tableTitle)
                        ? $"Table {tableNumber}"
                        : tableTitle,
                    HtmlTable = htmlTable.ToString()
                });

                tableNumber++;
            }

            return tables;
        }



        /// <summary>
        /// Helper method to clean text for Anki export - removes/escapes problematic characters
        /// </summary>
        private string CleanTextForAnki(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Remove or replace characters that break SQL queries
            text = text.Replace("'", "&#39;");  // Replace single quotes with HTML entity
            text = text.Replace("\"", "&quot;"); // Replace double quotes with HTML entity
            text = text.Replace("\r\n", "<br>"); // Replace Windows newlines
            text = text.Replace("\n", "<br>");   // Replace Unix newlines
            text = text.Replace("\r", "<br>");   // Replace Mac newlines

            return text.Trim();
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

        private void buttonShowApi_Click(object sender, EventArgs e)
        {
            // Disable system password char to use custom char
            textEditAPIKey.Properties.UseSystemPasswordChar = false;

            // Toggle password visibility
            if (textEditAPIKey.Properties.PasswordChar == '*')
            {
                textEditAPIKey.Properties.PasswordChar = '\0';  // Show
            }
            else
            {
                textEditAPIKey.Properties.PasswordChar = '*';  // Hide
            }

            // Force refresh
            textEditAPIKey.Refresh();
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
                MessageBox.Show("❌ Cannot open folder: " + ex.Message);
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

        private void comboBoxReasoningEffort_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = comboBoxReasoningEffort.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selected))
                return;

            // Extract the key part (Auto, Low, Medium, High)
            string effort = "medium"; // default
            if (selected.Contains("Low"))
                effort = "low";
            else if (selected.Contains("Medium"))
                effort = "medium";
            else if (selected.Contains("High"))
                effort = "high";
            else if (selected.Contains("Auto"))
                effort = "auto";

            // Save to variable and settings
            selectedReasoningEffort = effort;
            Properties.Settings.Default.ReasoningEffort = effort;
            Properties.Settings.Default.Save();

            // Update status
            UpdateStatus($"▶ Reasoning Effort set to: {selected}");

            // Show info message if o-series model is selected
            string currentModel = comboBoxEditModel.SelectedItem?.ToString() ?? "";
            string modelType = GetReasoningModelType(currentModel);

            if (modelType != "none")
            {
                UpdateStatus($"▶ This setting will be used with {currentModel}");
            }
        }
    }
}