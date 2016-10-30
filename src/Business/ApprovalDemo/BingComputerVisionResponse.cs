namespace Ascend2016.Business.ApprovalDemo
{
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
}
