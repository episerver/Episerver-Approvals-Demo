using System.Linq;
using System.Net.Http;
using System.Web;
using Ascend2016.Models.Pages;
using EPiServer.Approvals;
using EPiServer.Core;
using Newtonsoft.Json;

namespace Ascend2016.Business.ApprovalDemo
{
    public class SpellCheckApprover : ILegionApprover
    {
        public string Username => "Linguo";

        public ApprovalStatus DoDecide(PageData page)
        {
            using (var httpClient = new HttpClient())
            {
                // Using Bing Spell Check service to look for spelling mistakes.
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key",
                    System.Configuration.ConfigurationManager.AppSettings["BingSpellCheckKey"]);

                var sitePageData = page as SitePageData;
                if (sitePageData != null)
                {
                    var teaserText = sitePageData.TeaserText;
                    //teaserText = "Bill Gatas"; // Note: Used to demo a spelling mistake.
                    var language = page.Language.TextInfo.CultureName;

                    var model = BingSpellChecker(teaserText, language, httpClient);

                    if (model.FlaggedTokens.Any())
                    {
                        // TODO: Grab the reasons and use them as the rejection reason.
                        return ApprovalStatus.Rejected;
                    }
                }
            }

            return ApprovalStatus.Approved;
        }

        private static BingSpellCheckResponse BingSpellChecker(string text, string language, HttpClient httpClient)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["text"] = text;
            queryString["mkt"] = language;

            // As long as texts are short enough for a URL query, this is good enough.
            var uri = $"https://api.cognitive.microsoft.com/bing/v5.0/spellcheck/?{queryString}";
            var response = httpClient.GetStringAsync(uri).Result;
            var model = JsonConvert.DeserializeObject<BingSpellCheckResponse>(response);

            //// When texts are too long for a URL query then this is the way to go.
            //var formContent = new[]
            //{
            //    new KeyValuePair<string, string>("text", text)
            //};
            //using (var content = new FormUrlEncodedContent(formContent))
            //{
            //    var response = httpClient.PostAsync(uri, content).Result;
            //    var jsonString = response.Content.ReadAsStringAsync().Result;
            //    var model = JsonConvert.DeserializeObject<BingSpellCheckResponse>(jsonString);
            //}

            return model;
        }
    }
}
