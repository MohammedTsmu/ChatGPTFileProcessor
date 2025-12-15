namespace ChatGPTFileProcessor.Helpers
{
    /// <summary>
    /// Contains constant values used throughout the application
    /// </summary>
    public static class Constants
    {
        // Image Processing Constants
        public const int MAX_IMAGE_WIDTH = 1024;
        public const long JPEG_QUALITY = 80L;
        public const int HIGH_DPI = 300;

        // API Retry Constants
        public const int MAX_API_RETRIES = 4;
        public const int INITIAL_RETRY_DELAY_MS = 1200;
        public const int API_TIMEOUT_MINUTES = 6;
        public const int REQUEST_TIMEOUT_MINUTES = 7;

        // File and Path Constants
        public const string APP_NAME = "ChatGPTFileProcessor";
        public const string API_KEY_FILENAME = "api_key.txt";
        public const string MODEL_FILENAME = "model.txt";
        public const string CONFIG_FILENAME = "config.txt";

        // API Endpoint
        public const string OPENAI_API_BASE_URL = "https://api.openai.com/";
        public const string CHAT_COMPLETIONS_ENDPOINT = "v1/chat/completions";
    }
}
