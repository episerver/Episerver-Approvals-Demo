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
        // Eleanor, Dr. Eleanor Abernathy MD JD, AKA Crazy Cat Lady http://simpsons.wikia.com/wiki/Eleanor_Abernathy
        public string Username => "Eleanor";

        private readonly Injected<IContentRepository> _contentRepository;

        public Tuple<ApprovalStatus, string> DoDecide(PageData page)
        {
            var sitePageData = page as SitePageData;
            if (sitePageData == null || sitePageData.PageImage == null)
            {
                return Tuple.Create(
                    ApprovalStatus.Rejected,
                    "There can't be cats on your page type!");
            }

            using (var httpClient = new HttpClient())
            {
                // Using Bing Computer Vision service to look for cat images.
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key",
                    System.Configuration.ConfigurationManager.AppSettings["BingComputerVisionKey"]);

                var pageImage = _contentRepository.Service
                    .Get<ImageData>(sitePageData.PageImage);
                var language = sitePageData.Language.TwoLetterISOLanguageName;

                var model = BingComputerVision(pageImage, language, httpClient);

                if (!model.Categories.Any(x => x.Name.Contains("cat")))
                {
                    return Tuple.Create(
                        ApprovalStatus.Rejected,
                        "Not a cat!");
                }
            }

            return Tuple.Create(
                ApprovalStatus.Approved,
                "There were cats, and it was good.");
        }

        #region Not important for Content Approvals API demonstration

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

        /// <summary>
        /// A class used to deserialize the JSON response from Bing's Computer Vision API.
        /// https://www.microsoft.com/cognitive-services/en-us/computer-vision-api
        /// </summary>
        public class BingComputerVisionResponse
        {
            public Category[] Categories { get; set; }
            public string RequestId { get; set; }
            public MetadataObject Metadata { get; set; }

            public class MetadataObject
            {
                public int Width { get; set; }
                public int Height { get; set; }
                public string Format { get; set; }
            }

            public class Category
            {
                public string Name { get; set; }
                public float Score { get; set; }
            }
        }

        #endregion
    }
}
