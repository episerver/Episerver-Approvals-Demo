using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using Ascend2016.Models.Pages;
using EPiServer.Approvals;
using EPiServer.Core;
using Newtonsoft.Json;

namespace Ascend2016.Business.ApprovalDemo
{
    public class SentimentCheckApprover : ILegionApprover
    {
        public string Username => "Ned";

        public Tuple<ApprovalStatus, string> DoDecide(PageData page)
        {
            using (var httpClient = new HttpClient())
            {
                // Using Bing Text Analytics service to look for negative attitude.
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key",
                    System.Configuration.ConfigurationManager.AppSettings["BingTextAnalyticsKey"]);

                var sitePageData = page as SitePageData;
                if (sitePageData != null)
                {
                    var teaserText = sitePageData.TeaserText;
                    var language = page.Language.TwoLetterISOLanguageName;

                    var model = BingTextAnalytics(teaserText, language, httpClient);

                    // Negative sentiment
                    if (model.Documents.First().Score < 0.5f)
                    {
                        return new Tuple<ApprovalStatus, string>(ApprovalStatus.Rejected, "Don't be a negative nilly!");
                    }
                }
            }

            return new Tuple<ApprovalStatus, string>(ApprovalStatus.Approved, "Okilly-dokilly!");
        }

        #region Not important for Content Approvals API demonstration

        private static BingTextAnalyticsResponse BingTextAnalytics(string text, string language, HttpClient httpClient)
        {
            var json = JsonConvert.SerializeObject(new
            {
                Documents = new[]
                {
                    new
                    {
                        Language = language,
                        Id = "123",
                        Text = text
                    }
                }
            });

            var uri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";

            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                var response = httpClient.PostAsync(uri, content).Result;
                var jsonString = response.Content.ReadAsStringAsync().Result;

                return JsonConvert.DeserializeObject<BingTextAnalyticsResponse>(jsonString);
            }
        }

        #endregion
    }
}
