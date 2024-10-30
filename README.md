# ChatGPTFileProcessor

**ChatGPTFileProcessor** is a C# application that reads and processes documents (`.txt`, `.pdf`, and `.docx`) using OpenAI's ChatGPT to generate structured educational content. The application extracts definitions, multiple-choice questions (MCQs), flashcards, and vocabulary lists with Arabic translations, producing well-organized output files for each type. It’s ideal for educators, students, and content creators looking for a streamlined, AI-driven approach to content generation.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
  - [Prerequisites](#prerequisites)
  - [Installation Methods](#installation-methods)
    - [Method 1: Building from Source](#method-1-building-from-source)
    - [Method 2: Installing via Releases](#method-2-installing-via-releases)
- [Creating Your OpenAI ChatGPT API Key](#creating-your-openai-chatgpt-api-key)
- [Usage](#usage)
  - [Steps to Process a File](#steps-to-process-a-file)
- [Output Files](#output-files)
  - [Example Output (Vocabulary)](#example-output-vocabulary)
- [Troubleshooting](#troubleshooting)
- [Future Improvements](#future-improvements)
- [Contributing](#contributing)
- [License](#license)

## Features

- **Multi-Format Support**: Process text files, PDF documents, and Word files for diverse content sources.
- **Structured Content Generation**:
  - **Definitions**: Clear explanations for key terms.
  - **MCQs**: Multiple-choice questions with answer keys.
  - **Flashcards**: Term-definition pairs for effective learning.
  - **Vocabulary**: English terms translated to Arabic for bilingual content.
- **OpenAI API Integration**: Leverages ChatGPT models to provide precise and relevant content extraction.
- **Customizable Options**:
  - Select different GPT models to tailor the level of detail and processing scope.
  - Adjustable prompts and content structure for custom formatting and structure.
- **User-Friendly Interface**:
  - Model selection and API key management.
  - Real-time status updates and error handling.
- **Organized Output Files**: Each content type is saved to a unique, formatted file, ensuring easy access and readability.

## Installation

### Prerequisites

Before installing **ChatGPTFileProcessor**, ensure you have the following prerequisites:

- **.NET 5.0 or higher**
- **OpenAI API Key**
- **Microsoft Office** (for Word document processing)

### Installation Methods

You can install **ChatGPTFileProcessor** using one of the following methods based on your preference:

#### Method 1: Building from Source

Follow these steps to build and run the application from the source code.

1. **Clone the Repository**:

    ```bash
    git clone https://github.com/MohammedTsmu/ChatGPTFileProcessor.git
    ```

2. **Open the Project in Visual Studio**:

    - Navigate to the cloned repository folder.
    - Open the `ChatGPTFileProcessor.sln` solution file with Visual Studio.

3. **Restore NuGet Packages**:

    - If prompted, restore the required NuGet packages to ensure all dependencies are installed.

4. **Add Your OpenAI API Key**:

    - Run the application.
    - In the UI, navigate to the **API Key** section.
    - Input your OpenAI API key to enable API access.

5. **Run the Application**:

    - Start the application from Visual Studio.
    - Ensure that the API key is correctly entered to proceed with document processing.

#### Method 2: Installing via Releases

If you prefer not to build the application from source, you can download precompiled releases.

1. **Navigate to the Releases Page**:

    - Go to the [ChatGPTFileProcessor Releases](https://github.com/MohammedTsmu/ChatGPTFileProcessor/releases) page on GitHub.

2. **Download the Latest Release**:

    - Find the latest stable release.
    - Download the appropriate installer or executable for your operating system (e.g., `.exe` for Windows).

3. **Install the Application**:

    - **Windows**:
        - Run the downloaded `.exe` file.
        - Follow the on-screen instructions to complete the installation.
    - **macOS/Linux**:
        - Depending on the release assets, follow the provided instructions or extract the downloaded archive.
        - Ensure you have the necessary permissions to run the executable.

4. **Launch the Application**:

    - After installation, open **ChatGPTFileProcessor** from your applications menu or installation directory.

5. **Configure Your API Key**:

    - In the application's UI, navigate to the **API Key** section.
    - Input your OpenAI API key to enable API access.

> **⚠️ Note**: Always ensure you download releases from the official [GitHub Releases](https://github.com/MohammedTsmu/ChatGPTFileProcessor/releases) page to avoid malicious software.

## Creating Your OpenAI ChatGPT API Key

To use **ChatGPTFileProcessor**, you need an OpenAI API key. Follow the detailed steps below to create and obtain your own API key:

### Step 1: Sign Up for an OpenAI Account

1. **Visit OpenAI's Website**:
   
   Navigate to [OpenAI's Sign Up Page](https://platform.openai.com/signup).

2. **Create an Account**:
   
   - **New Users**: Click on **"Sign Up"** and provide the required information, including your email address and a secure password.
   - **Existing Users**: Click on **"Log In"** and enter your credentials.

3. **Verify Your Email**:
   
   After signing up, OpenAI will send a verification email to your registered email address. Click the verification link in the email to activate your account.

### Step 2: Access the API Section

1. **Log In to Your Account**:
   
   Go to [OpenAI's Platform](https://platform.openai.com/) and log in using your credentials.

2. **Navigate to API Keys**:
   
   - Once logged in, click on your profile icon located at the top-right corner.
   - From the dropdown menu, select **"API Keys"**.

   ![API Keys Navigation](https://i.imgur.com/your-image-link.png)  
   *_Figure: Navigating to API Keys section_*

### Step 3: Create a New API Key

1. **Generate a New Key**:
   
   - Click on the **"Create new secret key"** button.

   ![Create New API Key](https://i.imgur.com/your-image-link.png)  
   *_Figure: Creating a new API key_*

2. **Name Your Key**:
   
   - Provide a recognizable name for your API key (e.g., `ChatGPTFileProcessor Key`).

3. **Copy the API Key**:
   
   - Once generated, **copy the API key immediately**. For security reasons, this is the only time the full key will be displayed.

   ![Copy API Key](https://i.imgur.com/your-image-link.png)  
   *_Figure: Copying your new API key_*

4. **Store the Key Securely**:
   
   - Save the API key in a secure location, such as a password manager, to prevent unauthorized access.

### Step 4: Set Up Billing (If Required)

1. **Review Pricing Plans**:
   
   - OpenAI offers various pricing tiers. Review them [here](https://openai.com/pricing) to choose a plan that fits your usage needs.

2. **Add Payment Method**:
   
   - If prompted, add a valid payment method to activate your API key for usage beyond the free tier.

### Step 5: Integrate the API Key into ChatGPTFileProcessor

1. **Open the Application**:
   
   - Launch **ChatGPTFileProcessor** from Visual Studio or your installed applications.

2. **Navigate to API Key Section**:
   
   - In the application's UI, find the **API Key** section.

3. **Input Your API Key**:
   
   - Paste the copied API key into the designated field.

   ![API Key Input](https://i.imgur.com/your-image-link.png)  
   *_Figure: Inputting your API key into the application_*

4. **Save and Confirm**:
   
   - Save the changes and confirm that the application recognizes the API key. You should now be able to use the application's full functionality.

### Additional Resources

- **OpenAI API Documentation**:
  
  For more detailed information, visit the [OpenAI API Docs](https://platform.openai.com/docs/api-reference/introduction).

- **Managing Your API Keys**:
  
  Learn how to manage, regenerate, or revoke your API keys [here](https://platform.openai.com/account/api-keys).

> **⚠️ Important Security Notice**:
>
> - **Do Not Share Your API Key**: Treat your API key like a password. Do not share it publicly or commit it to version control systems.
> - **Regenerate if Compromised**: If you suspect your API key has been exposed, regenerate it immediately from the OpenAI dashboard.

## Usage

### Steps to Process a File

1. **Select Model**:

    - Choose from the available ChatGPT models to determine the desired processing depth and content generation style.

2. **Choose File**:

    - Upload a `.txt`, `.pdf`, or `.docx` document that you want to process.

3. **Start Processing**:

    - Click the **Process File** button to begin extracting definitions, MCQs, flashcards, and vocabulary from the uploaded document.

4. **Access Outputs**:

    - Once processing is complete, the generated files are saved to your Desktop.
    - Each file is uniquely named based on the content type and the model used for processing.

## Output Files

Each content type is saved as a separate file in the following structure:

- **Definitions_Output**: Provides terms with their definitions.
- **MCQs_Output**: Contains multiple-choice questions with answer keys.
- **Flashcards_Output**: Flashcards formatted with term-definition pairs.
- **Vocabulary_Output**: English terms with their Arabic translations.

### Example Output (Vocabulary)

| Term         | Arabic Translation |
|--------------|--------------------|
| Solubility   | الذوبانية          |
| Antiseptics  | مطهرات             |
| Absorption   | الامتصاص           |

## Troubleshooting

- **Errors in Output Format**:
  - If the output format is incorrect, adjust the prompts in the code to refine the structure as needed.

- **Index Errors**:
  - Ensure that the file you are trying to process is in a supported format (`.txt`, `.pdf`, `.docx`).
  - Retry processing after confirming the file format.

## Future Improvements

- **Custom Output Location**:
  - Allow users to specify custom save locations for the generated files.

- **Additional Language Support**:
  - Integrate more translation options to support additional languages beyond Arabic.

- **Batch Processing**:
  - Enable the processing of multiple files simultaneously to enhance workflow efficiency.

## Contributing

Contributions are welcome! Please follow these steps to contribute:

1. **Fork the Repository**.
2. **Create a New Branch** for your feature or bugfix.
3. **Commit Your Changes** with clear and descriptive messages.
4. **Submit a Pull Request** detailing the changes and the purpose behind them.

## License

This project is licensed under the [AGPL-3.0 License](https://github.com/MohammedTsmu/ChatGPTFileProcessor/tree/master?tab=AGPL-3.0-1-ov-file#readme).

---
