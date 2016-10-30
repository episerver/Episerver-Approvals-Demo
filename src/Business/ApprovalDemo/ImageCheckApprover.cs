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

        public ApprovalStatus DoDecide(PageData page)
        {
            using (var httpClient = new HttpClient())
            {
                // Using Bing Spell Check service to look for spelling mistakes.
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
                        // TODO: Add a rejection reason.
                        return ApprovalStatus.Rejected;
                    }
                }
            }

            return ApprovalStatus.Approved;
        }

        private static BingComputerVisionResponse BingComputerVision(ImageData image, string language, HttpClient httpClient)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["visualFeatures"] = "Categories"; // Other interesting checks: Description and Tags for metadata to add. Faces for diversity checking. Color for site color profile matching.
            queryString["language"] = language; // Note: Only English (en) and Simplified Chinese (zh) are supported.

            var uri = $"https://api.projectoxford.ai/vision/v1.0/analyze?{queryString}";

            //// Note: Demonstration of cat detection.
            //using (HttpContent content = new StringContent(
            //            @"{""url"":""https://upload.wikimedia.org/wikipedia/commons/b/b0/PSM_V37_D105_English_tabby_cat.jpg""}")
            //)
            //{
            //    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            //    var response = httpClient.PostAsync(uri, content).Result;
            //    var jsonString = response.Content.ReadAsStringAsync().Result;
            //    return JsonConvert.DeserializeObject<BingComputerVisionResponse>(jsonString);
            //}

            byte[] byteData;
            using (var stream = image.BinaryData.OpenRead())
            {
                byteData = new byte[stream.Length];
                stream.Read(byteData, 0, (int)stream.Length);
            }
            using (var content = new ByteArrayContent(byteData))
            //using (var content = new MultipartFormDataContent())
            //using (HttpContent content = new StreamContent(image.BinaryData.OpenRead())) // TODO: Add bufferSize param
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream"); // TODO: Shouldn't there be a XContent class that sets this? StreamContent doesn't.
                var response = httpClient.PostAsync(uri, content).Result;
                var jsonString = response.Content.ReadAsStringAsync().Result;

                return JsonConvert.DeserializeObject<BingComputerVisionResponse>(jsonString);
            }
        }
    }
}
