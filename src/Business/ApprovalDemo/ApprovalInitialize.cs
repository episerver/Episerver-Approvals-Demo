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
        }

        private async void OnStepStarted(ApprovalStepEventArgs e)
        {
            var approval = await _approvalRepository
                .GetAsync(e.ApprovalID).ConfigureAwait(false);
            var approvalDefinition = await _approvalDefinitionVersionRepository
                .GetAsync(approval.DefinitionVersionID).ConfigureAwait(false);
            var acceptedApprovers = approvalDefinition
                .Steps[approval.ActiveStepIndex]
                .Approvers
                .Select(x => x.Username);
            var botsInStep = _bots
                .Where(x => acceptedApprovers.Contains(x.Username));

            var page = _contentRepository
                .Get<PageData>(approval.ContentLink);

            foreach (var bot in botsInStep)
            {
                // Approve or reject. The first approver "wins" but the subsequent approves won't fail and in the future that information could be useful.
                // TODO: Jonas, maybe I could or should use IApprovalRepository.SaveDecisionAsync instead?
                var decision = bot.DoDecide(page);

                if (decision.Item1 == ApprovalStatus.Approved)
                {
                    _approvalEngine.ApproveAsync(
                        approval.ID,
                        bot.Username,
                        approval.ActiveStepIndex,
                        ApprovalDecisionScope.Step).Wait();
                }
                else if (decision.Item1 == ApprovalStatus.Rejected)
                {
                    // Note: Rejecting will throw an exception if the step has already been approved.
                    _approvalEngine.RejectAsync(
                        approval.ID,
                        bot.Username,
                        approval.ActiveStepIndex,
                        ApprovalDecisionScope.Step).Wait();
                }
            }

        }

        #region Not important for Notifications API demonstration

        private readonly IContentRepository _contentRepository;
        private readonly IEnumerable<ILegionApprover> _bots;

        public ApprovalInitialize(IApprovalEngineEvents approvalEngineEvents, IApprovalRepository approvalRepository, IContentRepository contentRepository, IApprovalEngine approvalEngine, IApprovalDefinitionVersionRepository approvalDefinitionVersionRepository)
        {
            _approvalEngineEvents = approvalEngineEvents;
            _approvalRepository = approvalRepository;
            _contentRepository = contentRepository;
            _approvalEngine = approvalEngine;
            _approvalDefinitionVersionRepository = approvalDefinitionVersionRepository;

            _bots = new ILegionApprover[]
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
