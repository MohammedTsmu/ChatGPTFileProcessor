# ChatGPTFileProcessor

**ChatGPTFileProcessor** is a C# application that reads and processes documents (`.txt`, `.pdf`, and `.docx`) using OpenAI's ChatGPT to generate structured educational content. The application extracts definitions, multiple-choice questions (MCQs), flashcards, and vocabulary lists with Arabic translations, producing well-organized output files for each type. It’s ideal for educators, students, and content creators looking for a streamlined, AI-driven approach to content generation.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
  - [Prerequisites](#prerequisites)
  - [Setup Steps](#setup-steps)
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

- **.NET 5.0 or higher**
- **OpenAI API Key**
- **Microsoft Office** (for Word document processing)

### Setup Steps

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

This project is licensed under the [MIT License](LICENSE).

---
