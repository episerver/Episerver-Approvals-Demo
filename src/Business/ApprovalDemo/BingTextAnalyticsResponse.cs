namespace Ascend2016.Business.ApprovalDemo
{
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
}
