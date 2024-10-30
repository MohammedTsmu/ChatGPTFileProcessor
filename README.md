# ChatGPTFileProcessor

**ChatGPTFileProcessor** is a C# application that reads and processes documents (.txt, .pdf, and .docx), using OpenAI's ChatGPT to generate structured educational content. The application extracts definitions, multiple-choice questions (MCQs), flashcards, and vocabulary lists with Arabic translations, producing well-organized output files for each type. It’s ideal for educators, students, and content creators looking for a streamlined, AI-driven approach to content generation.

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
- .NET Framework 5.0 or higher
- OpenAI API Key
- Microsoft Office (for Word document processing)

### Setup
1. Clone the repository:
   ```bash
   git clone https://github.com/MohammedTsmu/ChatGPTFileProcessor.git
