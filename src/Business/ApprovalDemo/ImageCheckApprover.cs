using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using Ascend2016.Models.Pages;
using EPiServer;
using EPiServer.Approvals;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;

namespace Ascend2016.Business.ApprovalDemo
{
    public class ImageCheckApprover : ILegionApprover
    {
        // Eleanor, Dr. Eleanor Abernathy MD JD http://simpsons.wikia.com/wiki/Eleanor_Abernathy
        private readonly Injected<IContentRepository> _contentRepository;

        public string Username => "Eleanor";

        public Tuple<ApprovalStatus, string> DoDecide(PageData page)
        {
            using (var httpClient = new HttpClient())
            {
                // Using Bing Computer Vision service to look for cat images.
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key",
                    System.Configuration.ConfigurationManager.AppSettings["BingComputerVisionKey"]);

                var sitePageData = page as SitePageData;
                if (sitePageData != null)
                {
                    var pageImage = _contentRepository.Service.Get<ImageData>(sitePageData.PageImage);
                    var language = page.Language.TwoLetterISOLanguageName; // Obviously this API wants "en" when the spell checker API wanted "en-US".

                    var model = BingComputerVision(pageImage, language, httpClient);

                    if (!model.Categories.Any(x => x.Name.Contains("cat")))
                    {
                        return new Tuple<ApprovalStatus, string>(ApprovalStatus.Rejected, "Not a cat!");
                    }
                }
            }

            return new Tuple<ApprovalStatus, string>(ApprovalStatus.Approved, "There were cats, and it was good.");
        }

        private static BingComputerVisionResponse BingComputerVision(ImageData image, string language, HttpClient httpClient)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["visualFeatures"] = "Categories"; // Other interesting checks: Description and Tags for metadata to add. Faces for diversity checking. Color for site color profile matching.
            queryString["language"] = language; // Note: Only English (en) and Simplified Chinese (zh) are supported.

            var uri = $"https://api.projectoxford.ai/vision/v1.0/analyze?{queryString}";

            byte[] byteData;
            using (var stream = image.BinaryData.OpenRead())
            {
                byteData = new byte[stream.Length];
                stream.Read(byteData, 0, (int)stream.Length);
            }
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                var response = httpClient.PostAsync(uri, content).Result;
                var jsonString = response.Content.ReadAsStringAsync().Result;

                return JsonConvert.DeserializeObject<BingComputerVisionResponse>(jsonString);
            }
        }
    }
}
