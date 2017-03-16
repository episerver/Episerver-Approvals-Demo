using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using Approvals.Models.Pages;
using EPiServer.Approvals;
using EPiServer.Core;
using Newtonsoft.Json;

namespace Approvals.Business.ApprovalDemo
{
    public class SentimentCheckApprover : ILegionApprover
    {
        // Ned Flanders, genuinely good-natured http://simpsons.wikia.com/wiki/Ned_Flanders
        public string Username => "Ned";

        public Tuple<ApprovalStatus, string> DoDecide(PageData page)
        {
            var sitePageData = page as SitePageData;
            if (sitePageData == null || sitePageData.TeaserText == null)
            {
                return Tuple.Create(
                    ApprovalStatus.Approved,
                    "I don't know you, but I like you anyway page-erino!");
            }

            using (var httpClient = new HttpClient())
            {
                // Using Bing Text Analytics service to look for negative attitude.
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key",
                    System.Configuration.ConfigurationManager.AppSettings["BingTextAnalyticsKey"]);

                var teaserText = sitePageData.TeaserText;
                var language = page.Language.TwoLetterISOLanguageName;

                var model = BingTextAnalytics(teaserText, language, httpClient);

                // Negative sentiment
                if (model.Documents.First().Score < 0.5f)
                {
                    return Tuple.Create(
                        ApprovalStatus.Rejected,
                        "Don't be a negative-nilly!");
                }
            }

            return Tuple.Create(
                ApprovalStatus.Approved,
                "Okilly-dokilly!");
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

        /// <summary>
        /// A class used to deserialize the JSON response from Bing's Text Analytics API.
        /// https://www.microsoft.com/cognitive-services/en-us/text-analytics-api
        /// </summary>
        public class BingTextAnalyticsResponse
        {
            public Document[] Documents { get; set; }
            public object[] Errors { get; set; }

            public class Document
            {
                public float Score { get; set; }
                public string Id { get; set; }
            }
        }

        #endregion
    }
}
