using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Approvals;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Shell.UI.Internal;

namespace Ascend2016.Business.ApprovalDemo
{
    [ServiceConfiguration(typeof(IEventListener), Lifecycle = ServiceInstanceScope.Singleton)]
    public class ApprovalInitialize : IEventListener
    {
        private readonly IApprovalEngine _approvalEngine;
        private readonly IApprovalEngineEvents _approvalEngineEvents;
        private readonly IApprovalRepository _approvalRepository;
        private readonly IApprovalDefinitionVersionRepository _approvalDefinitionVersionRepository;

        public void Start()
        {
            _approvalEngineEvents.StepStarted += OnStepStarted;
            // TODO: _approvalEngineEvents.Service.Started instead?
        }

        private async void OnStepStarted(ApprovalStepEventArgs e)
        {
            var approval = await _approvalRepository.GetAsync(e.ApprovalID).ConfigureAwait(false);
            var approvalDefinition = await _approvalDefinitionVersionRepository.GetAsync(approval.DefinitionVersionID).ConfigureAwait(false);
            var acceptedApprovers = approvalDefinition.Steps[approval.ActiveStepIndex].Approvers.Select(x => x.Username);
            var stepDaemons = _daemons.Where(x => acceptedApprovers.Contains(x.Username));

            // Auto run all approvers or just one?
            var approved = 0;
            var rejected = 0;

            foreach (var approver in stepDaemons)
            {
                //// Get all approvals waiting for approval
                //var query = new ApprovalsQuery
                //{
                //    Status = ApprovalStatus.Pending,
                //    Username = approver.Username,
                //    OnlyActiveSteps = true,
                //    //Language = // TODO: Demonstrate it? MS API's only support certain languages so it could work.
                //};
                //var approvals = _approvalRepository.ListAsync(query).Result;

                // Approve or reject all of them
                // TODO: Only reject, so that several approvals can be on the same step and all of them have a chance to run.
                var page = _contentRepository.Get<PageData>(approval.ContentLink);
                var decision = approver.DoDecide(page);

                if (decision.Item1 == ApprovalStatus.Approved)
                {
                    // TODO: Remove the ApprovalDecisionScope param when updating to latest Approvals API
                    _approvalEngine.ApproveAsync(approval.ID, approver.Username, approval.ActiveStepIndex,
                        ApprovalDecisionScope.Step).Wait();
                    approved++; ;
                }
                else if (decision.Item1 == ApprovalStatus.Rejected)
                {
                    // TODO: Remove the ApprovalDecisionScope param when updating to latest Approvals API
                    _approvalEngine.RejectAsync(approval.ID, approver.Username, approval.ActiveStepIndex,
                        ApprovalDecisionScope.Step).Wait();
                    rejected++;
                }
            }

        }


        #region Not important for Notifications API demonstration

        private readonly IContentRepository _contentRepository;
        private readonly IEnumerable<ILegionApprover> _daemons;

        public ApprovalInitialize(IApprovalEngineEvents approvalEngineEvents, IApprovalRepository approvalRepository, IContentRepository contentRepository, IApprovalEngine approvalEngine, IApprovalDefinitionVersionRepository approvalDefinitionVersionRepository)
        {
            _approvalEngineEvents = approvalEngineEvents;
            _approvalRepository = approvalRepository;
            _contentRepository = contentRepository;
            _approvalEngine = approvalEngine;
            _approvalDefinitionVersionRepository = approvalDefinitionVersionRepository;

            _daemons = new ILegionApprover[]
            {
                new SpellCheckApprover(),
                new ImageCheckApprover()
            };
        }

        public void Stop()
        {
        }

        #endregion
    }
}
