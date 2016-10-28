using EPiServer;
using EPiServer.Approvals;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;

namespace Ascend2016.Business.ApprovalDemo
{
    [ScheduledPlugIn(DisplayName = "Durin's Bridge")]
    public class DurinsBridgeJob : ScheduledJobBase
    {
        private Injected<IApprovalEngine> _approvalEngine;
        private Injected<IApprovalRepository> _approvalRepository;
        private Injected<IContentRepository> _contentRepository;

        private void DoJob()
        {
            var query = new ApprovalsQuery
            {
                Status = ApprovalStatus.Pending
            };

            var approvals = _approvalRepository.Service.ListAsync(query).Result;

            foreach (var approval in approvals)
            {
                DoFun(approval);
            }
        }

        private void DoFun(Approval approval)
        {
            var decision = DoDecide(approval);

            const string username = "gandalf";
            if (decision == ApprovalStatus.Approved)
            {
                // TODO: Remove the ApprovalDecisionScope param when updating to latest Approvals API
                _approvalEngine.Service.ApproveAsync(approval.ID, username, approval.ActiveStepIndex,
                    ApprovalDecisionScope.Step).Wait();
            }
            else if (decision == ApprovalStatus.Rejected)
            {
                // TODO: Remove the ApprovalDecisionScope param when updating to latest Approvals API
                _approvalEngine.Service.RejectAsync(approval.ID, username, approval.ActiveStepIndex,
                    ApprovalDecisionScope.Step).Wait();
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

            if (page.PageName.ToLower().Contains("balrog"))
            {
                return ApprovalStatus.Rejected;
            }

            return ApprovalStatus.Approved;
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
