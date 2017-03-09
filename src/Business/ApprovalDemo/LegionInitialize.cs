using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Approvals;
using EPiServer.Approvals.ContentApprovals;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Notification;

namespace Ascend2016.Business.ApprovalDemo
{
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class LegionInitialize : IInitializableModule
    {
        private IApprovalEngine _approvalEngine;
        private IApprovalEngineEvents _approvalEngineEvents;
        private IApprovalDefinitionVersionRepository _approvalDefinitionVersionRepository;

        private async void OnStepStarted(ApprovalStepEventArgs e)
        {
            var approvalDefinition = await _approvalDefinitionVersionRepository
                .GetAsync(e.DefinitionVersionID).ConfigureAwait(false);

            var approversInStep = approvalDefinition
                .Steps[e.StepIndex]
                .Approvers
                .Select(x => x.Username);

            var botsInStep = _bots
                .Where(x => approversInStep.Contains(x.Username));

            var page = _contentRepository
                .Get<PageData>(e.GetContentLink());

            foreach (var bot in botsInStep)
            {
                // Approve or reject. The first approver "wins" but the subsequent approves won't fail and in the future that information could be useful.
                var decision = bot.DoDecide(page);

                if (decision.Item1 == ApprovalStatus.Rejected)
                {
                    _approvalEngine.RejectAsync(
                        e.ApprovalID,
                        bot.Username,
                        e.StepIndex,
                        ApprovalDecisionScope.Step).Wait();
                }
                else if (decision.Item1 == ApprovalStatus.Approved)
                {
                    _approvalEngine.ApproveAsync(
                        e.ApprovalID,
                        bot.Username,
                        e.StepIndex,
                        ApprovalDecisionScope.Step).Wait();

                    // Note: Rejecting will throw an exception if the step has already been approved.
                    break;
                }

            }
        }

        #region Not important for Approvals API demonstration

        private IContentRepository _contentRepository;
        private IEnumerable<ILegionApprover> _bots;
        private INotifier _notifier;

        public void Initialize(InitializationEngine context)
        {
            _approvalEngine = context.Locate.Advanced.GetInstance<IApprovalEngine>();
            _approvalEngineEvents = context.Locate.Advanced.GetInstance<IApprovalEngineEvents>();
            _approvalDefinitionVersionRepository = context.Locate.Advanced.GetInstance<IApprovalDefinitionVersionRepository>();
            _contentRepository = context.Locate.Advanced.GetInstance<IContentRepository>();
            _notifier = context.Locate.Advanced.GetInstance<INotifier>();

            _bots = new ILegionApprover[]
            {
                new SpellCheckApprover(),
                new ImageCheckApprover(),
                new SentimentCheckApprover()
            };

            _approvalEngineEvents.StepStarted += OnStepStarted;
        }

        public void Uninitialize(InitializationEngine context)
        {
            _approvalEngineEvents.StepStarted -= OnStepStarted;
        }

        #endregion
    }
}
