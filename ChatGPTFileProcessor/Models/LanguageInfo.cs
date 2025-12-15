namespace ChatGPTFileProcessor.Models
{
    /// <summary>
    /// Represents a supported language with its code and display name
    /// </summary>
    public class LanguageInfo
    {
        public string Code { get; set; }
        public string DisplayName { get; set; }

        public LanguageInfo(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
        }
    }
}
