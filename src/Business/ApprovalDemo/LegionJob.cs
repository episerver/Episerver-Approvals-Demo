using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Approvals;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;

namespace Ascend2016.Business.ApprovalDemo
{
    // TODO: This does the same thing as ApprovalInitialize. Pick one.
    [ScheduledPlugIn(DisplayName = "Legion")]
    public class LegionJob : ScheduledJobBase
    {
        private Injected<IApprovalEngine> _approvalEngine;
        private Injected<IApprovalRepository> _approvalRepository;
        private Injected<IContentRepository> _contentRepository;
        private readonly IEnumerable<ILegionApprover> _approvers;

        private string DoJob()
        {
            var approved = 0;
            var rejected = 0;

            foreach (var approver in _approvers)
            {
                // Get all approvals waiting for approval
                var query = new ApprovalsQuery
                {
                    Status = ApprovalStatus.Pending,
                    Username = approver.Username,
                    OnlyActiveSteps = true,
                    //Language = // TODO: Demonstrate it? MS API's only support certain languages so it could work.
                };
                var approvals = _approvalRepository.Service.ListAsync(query).Result;

                // Approve or reject all of them
                // TODO: Only reject? So that several approvals can be on the same step and all of them have a chance to run.
                foreach (var approval in approvals)
                {
                    var page = _contentRepository.Service.Get<PageData>(approval.ContentLink);
                    var decision = approver.DoDecide(page);

                    if (decision.Item1 == ApprovalStatus.Approved)
                    {
                        _approvalEngine.Service.ApproveAsync(approval.ID, approver.Username, approval.ActiveStepIndex,
                            ApprovalDecisionScope.Step).Wait();
                        approved++; ;
                    }
                    else if (decision.Item1 == ApprovalStatus.Rejected)
                    {
                        // Note: This will throw an exception if the step has already been approved.
                        _approvalEngine.Service.RejectAsync(approval.ID, approver.Username, approval.ActiveStepIndex,
                            ApprovalDecisionScope.Step).Wait();
                        rejected++;
                    }
                }
            }

            return $"Legion has {_approvers.Count()} daemons, that reports {approved} approvals and {rejected} rejections.";
        }

        #region Not important for Content Approvals API demonstration

        private bool _stopSignaled;

        public LegionJob()
        {
            IsStoppable = true;

            _approvers = new ILegionApprover[]
            {
                new SpellCheckApprover(),
                new ImageCheckApprover()
            };
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
