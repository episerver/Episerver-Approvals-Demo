using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Approvals;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Shell.UI.Internal;

namespace Ascend2016.Business.ApprovalDemo
{
    // TODO: This does the same thing as LegionJob. Pick one.
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
            // TODO: Show _approvalEngineEvents.Service.Started instead?
        }

        private async void OnStepStarted(ApprovalStepEventArgs e)
        {
            var approval = await _approvalRepository.GetAsync(e.ApprovalID).ConfigureAwait(false);
            var approvalDefinition = await _approvalDefinitionVersionRepository.GetAsync(approval.DefinitionVersionID).ConfigureAwait(false);
            var acceptedApprovers = approvalDefinition.Steps[approval.ActiveStepIndex].Approvers.Select(x => x.Username);
            var stepDaemons = _daemons.Where(x => acceptedApprovers.Contains(x.Username));

            foreach (var approver in stepDaemons)
            {
                // Approve or reject. The first one "wins" but the subsequent calls won't fail and in the future that information could be useful.
                // TODO: Only reject? So that several approvals can be on the same step and all of them have a chance to run.
                var page = _contentRepository.Get<PageData>(approval.ContentLink);
                var decision = approver.DoDecide(page);

                if (decision.Item1 == ApprovalStatus.Approved)
                {
                    _approvalEngine.ApproveAsync(approval.ID, approver.Username, approval.ActiveStepIndex,
                        ApprovalDecisionScope.Step).Wait();
                }
                else if (decision.Item1 == ApprovalStatus.Rejected)
                {
                    _approvalEngine.RejectAsync(approval.ID, approver.Username, approval.ActiveStepIndex,
                        ApprovalDecisionScope.Step).Wait();
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
                new ImageCheckApprover(),
                new SentimentCheckApprover()
            };
        }

        public void Stop()
        {
        }

        #endregion
    }
}
