namespace ChatGPTFileProcessor.Models
{
    /// <summary>
    /// Represents a multiple-choice question item
    /// </summary>
    public class McqItem
    {
        public string Question { get; set; }
        public string OptionA { get; set; }
        public string OptionB { get; set; }
        public string OptionC { get; set; }
        public string OptionD { get; set; }
        public string Answer { get; set; }

        /// <summary>
        /// Combines the four options into a single "Options" cell,
        /// with line-breaks between them
        /// </summary>
        public string OptionsCell =>
            $"A) {OptionA}\nB) {OptionB}\nC) {OptionC}\nD) {OptionD}";
    }
}
