using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Approvals;
using EPiServer.Approvals.ContentApprovals;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;

namespace Ascend2016.Business.ApprovalDemo
{
    // TODO: This does the same thing as LegionInitialize. Pick one.
    [ScheduledPlugIn(DisplayName = "Legion")]
    public class LegionJob : ScheduledJobBase
    {
        private Injected<IApprovalEngine> _approvalEngine;
        private Injected<IApprovalRepository> _approvalRepository;
        private Injected<IContentRepository> _contentRepository;
        private readonly IEnumerable<ILegionApprover> _bots;

        private string DoJob()
        {
            var approved = 0;
            var rejected = 0;

            foreach (var bot in _bots)
            {
                // Get all approvals waiting for approval
                var query = new ContentApprovalQuery
                {
                    Status = ApprovalStatus.Pending,
                    Username = bot.Username,
                    OnlyActiveSteps = true
                };
                var approvals = _approvalRepository.Service
                    .ListAsync<ContentApproval>(query).Result;

                // Approve or reject all of them
                foreach (var approval in approvals)
                {
                    var page = _contentRepository.Service
                        .Get<PageData>(approval.ContentLink);
                    var decision = bot.DoDecide(page);

                    if (decision.Item1 == ApprovalStatus.Approved)
                    {
                        _approvalEngine.Service.ApproveAsync(
                            approval.ID,
                            bot.Username,
                            approval.ActiveStepIndex,
                            ApprovalDecisionScope.Step).Wait();
                        approved++;
                    }
                    else if (decision.Item1 == ApprovalStatus.Rejected)
                    {
                        _approvalEngine.Service.RejectAsync(
                            approval.ID,
                            bot.Username,
                            approval.ActiveStepIndex,
                            ApprovalDecisionScope.Step).Wait();
                        rejected++;
                        // Note: Rejecting will throw an exception if the step has already been approved.
                        break;
                    }
                }
            }

            return $"Legion has {_bots.Count()} daemons, that reports {approved} approvals and {rejected} rejections.";
        }

        #region Not important for Content Approvals API demonstration

        private bool _stopSignaled;

        public LegionJob()
        {
            IsStoppable = true;

            _bots = new ILegionApprover[]
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
