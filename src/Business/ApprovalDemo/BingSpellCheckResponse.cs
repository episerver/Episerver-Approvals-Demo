namespace Ascend2016.Business.ApprovalDemo
{
    /// <summary>
    /// A class used to deserialize the JSON response from Bing's Spell Check API.
    /// https://www.microsoft.com/cognitive-services/en-us/bing-spell-check-api
    /// </summary>
    public class BingSpellCheckResponse
    {
        public string _type { get; set; }
        public Flaggedtoken[] FlaggedTokens { get; set; }
        public Error[] Errors { get; set; }

        public class Flaggedtoken
        {
            public int Offset { get; set; }
            public string Token { get; set; }
            public string Type { get; set; }
            public SuggestionObject[] Suggestions { get; set; }

            public class SuggestionObject
            {
                public string Suggestion { get; set; }
                public int Score { get; set; }
            }
        }

        public class Error
        {
            public string Code { get; set; }
            public string Message { get; set; }
            public string Parameter { get; set; }
        }
    }
}
