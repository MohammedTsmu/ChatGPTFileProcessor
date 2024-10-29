using System;
using System.IO;
using System.Windows.Forms;

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
    }
}
