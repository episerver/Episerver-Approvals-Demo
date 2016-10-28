using System.Linq;
using System.Net.Http;
using System.Web;
using Ascend2016.Models.Pages;
using EPiServer;
using EPiServer.Approvals;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;

namespace Ascend2016.Business.ApprovalDemo
{
    [ScheduledPlugIn(DisplayName = "Durin's Bridge")]
    public class DurinsBridgeJob : ScheduledJobBase
    {
        private const string Username = "gandalf";
        private Injected<IApprovalEngine> _approvalEngine;
        private Injected<IApprovalRepository> _approvalRepository;
        private Injected<IContentRepository> _contentRepository;

        private void DoJob()
        {
            // Get all approvals waiting for user to approve
            var query = new ApprovalsQuery
            {
                Status = ApprovalStatus.Pending,
                Username = Username
            };
            var approvals = _approvalRepository.Service.ListAsync(query).Result;

            // Spell check all of them
            foreach (var approval in approvals)
            {
                var decision = DoDecide(approval);

                if (decision == ApprovalStatus.Approved)
                {
                    // TODO: Remove the ApprovalDecisionScope param when updating to latest Approvals API
                    _approvalEngine.Service.ApproveAsync(approval.ID, Username, approval.ActiveStepIndex,
                        ApprovalDecisionScope.Step).Wait();
                }
                else if (decision == ApprovalStatus.Rejected)
                {
                    // TODO: Remove the ApprovalDecisionScope param when updating to latest Approvals API
                    _approvalEngine.Service.RejectAsync(approval.ID, Username, approval.ActiveStepIndex,
                        ApprovalDecisionScope.Step).Wait();
                }
            }
        }

        #region Not important for Content Approvals API demonstration

        private bool _stopSignaled;

        public DurinsBridgeJob()
        {
            IsStoppable = true;
        }

        public override void Stop()
        {
            _stopSignaled = true;
        }

        private ApprovalStatus DoDecide(Approval approval)
        {
            var page = _contentRepository.Service.Get<PageData>(approval.ContentLink);

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

        public override string Execute()
        {
            // Call OnStatusChanged to periodically notify progress of job for manually started jobs
            OnStatusChanged($"Starting execution of {GetType()}");

            // Add implementation
            DoJob();

            // For long running jobs periodically check if stop is signaled and if so stop execution
            if (_stopSignaled)
            {
                return "Stop of job was called";
            }

            return "Work executed beautifully.";
        }

        #endregion
    }
}
