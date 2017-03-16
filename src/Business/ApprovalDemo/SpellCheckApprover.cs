using System;
using System.Linq;
using System.Net.Http;
using System.Web;
using Approvals.Models.Pages;
using EPiServer.Approvals;
using EPiServer.Core;
using Newtonsoft.Json;

namespace Approvals.Business.ApprovalDemo
{
    public class SpellCheckApprover : ILegionApprover
    {
        // Linguo, Lisa's spell checking robot http://simpsons.wikia.com/wiki/Linguo
        public string Username => "Linguo";

        public Tuple<ApprovalStatus, string> DoDecide(PageData page)
        {
            var sitePageData = page as SitePageData;
            if (sitePageData == null || sitePageData.TeaserText == null)
            {
                return Tuple.Create(
                    ApprovalStatus.Rejected,
                    "Sentence fragment!");
            }

            using (var httpClient = new HttpClient())
            {
                // Using Bing Spell Check service to look for spelling mistakes.
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key",
                    System.Configuration.ConfigurationManager.AppSettings["BingSpellCheckKey"]);

                var teaserText = sitePageData.TeaserText;
                var language = page.Language.TextInfo.CultureName;

                var model = BingSpellChecker(teaserText, language, httpClient);

                if (model.FlaggedTokens.Any())
                {
                    // Sample output: "'bene', did you mean 'been'? 'Gatas', did you mean 'Gates'?"
                    var corrections = model.FlaggedTokens
                        .Select(x => $"'{x.Token}', did you mean '{x.Suggestions.First().Suggestion}'?");

                    return Tuple.Create(
                        ApprovalStatus.Rejected,
                        string.Join(" ", corrections));
                }
            }

            return Tuple.Create(
                ApprovalStatus.Approved,
                "Spell check passed.");
        }

        #region Not important for Content Approvals API demonstration

        private static BingSpellCheckResponse BingSpellChecker(string text, string language, HttpClient httpClient)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["text"] = text;
            queryString["mkt"] = language;

            // As long as texts are short enough for a URL query, this is good enough.
            var uri = $"https://api.cognitive.microsoft.com/bing/v5.0/spellcheck/?{queryString}";
            var response = httpClient.GetStringAsync(uri).Result;
            var model = JsonConvert.DeserializeObject<BingSpellCheckResponse>(response);

            return model;
        }

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
                    public double Score { get; set; }
                }
            }

            public class Error
            {
                public string Code { get; set; }
                public string Message { get; set; }
                public string Parameter { get; set; }
            }
        }

        #endregion
    }
}
