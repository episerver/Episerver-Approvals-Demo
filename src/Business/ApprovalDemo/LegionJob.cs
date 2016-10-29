using System.Linq;
using EPiServer;
using EPiServer.Approvals;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;

namespace Ascend2016.Business.ApprovalDemo
{
    [ScheduledPlugIn(DisplayName = "Legion")]
    public class LegionJob : ScheduledJobBase
    {
        private const string Username = "gandalf";
        private Injected<IApprovalEngine> _approvalEngine;
        private Injected<IApprovalRepository> _approvalRepository;
        private Injected<IContentRepository> _contentRepository;

        private string DoJob()
        {
            var linguo = new SpellCheckApprover();

            // Get all approvals waiting for user to approve
            var query = new ApprovalsQuery
            {
                Status = ApprovalStatus.Pending,
                Username = linguo.Username
            };
            var approvals = _approvalRepository.Service.ListAsync(query).Result;

            if (!approvals.Any())
            {
                return $"No approvals for {query.Username} to approve.";
            }

            // Spell check all of them
            var approved = 0;
            var rejected = 0;
            foreach (var approval in approvals)
            {
                var page = _contentRepository.Service.Get<PageData>(approval.ContentLink);
                var decision = linguo.DoDecide(page);

                if (decision == ApprovalStatus.Approved)
                {
                    // TODO: Remove the ApprovalDecisionScope param when updating to latest Approvals API
                    _approvalEngine.Service.ApproveAsync(approval.ID, Username, approval.ActiveStepIndex,
                        ApprovalDecisionScope.Step).Wait();
                    approved++;;
                }
                else if (decision == ApprovalStatus.Rejected)
                {
                    // TODO: Remove the ApprovalDecisionScope param when updating to latest Approvals API
                    _approvalEngine.Service.RejectAsync(approval.ID, Username, approval.ActiveStepIndex,
                        ApprovalDecisionScope.Step).Wait();
                    rejected++;
                }
            }

            return $"Legion reports {approved} approvals and {rejected} rejections.";
        }

        #region Not important for Content Approvals API demonstration

        private bool _stopSignaled;

        public LegionJob()
        {
            IsStoppable = true;
        }

        public override void Stop()
        {
            _stopSignaled = true;
        }

        public override string Execute()
        {
            // Call OnStatusChanged to periodically notify progress of job for manually started jobs
            OnStatusChanged($"Starting execution of {GetType()}");

            // Add implementation
            var jobResult = DoJob();

            // For long running jobs periodically check if stop is signaled and if so stop execution
            if (_stopSignaled)
            {
                return "Stop of job was called";
            }

            return jobResult;
        }

        #endregion
    }
}
