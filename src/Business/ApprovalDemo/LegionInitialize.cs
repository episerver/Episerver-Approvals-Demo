using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Approvals;
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
        private IApprovalRepository _approvalRepository;
        private IApprovalDefinitionVersionRepository _approvalDefinitionVersionRepository;

        private async void OnStepStarted(ApprovalStepEventArgs e)
        {
            var approval = await _approvalRepository
                .GetAsync(e.ApprovalID).ConfigureAwait(false);
            var approvalDefinition = await _approvalDefinitionVersionRepository
                .GetAsync(approval.DefinitionVersionID).ConfigureAwait(false);
            var acceptedApprovers = approvalDefinition
                .Steps[e.StepIndex]
                .Approvers
                .Select(x => x.Username);
            var botsInStep = _bots
                .Where(x => acceptedApprovers.Contains(x.Username));

            var page = _contentRepository
                .Get<PageData>(approval.ContentLink);

            foreach (var bot in botsInStep)
            {
                // Approve or reject. The first approver "wins" but the subsequent approves won't fail and in the future that information could be useful.
                var decision = bot.DoDecide(page);

                if (decision.Item1 == ApprovalStatus.Approved)
                {
                    _approvalEngine.ApproveAsync(
                        approval.ID,
                        bot.Username,
                        e.StepIndex,
                        ApprovalDecisionScope.Step).Wait();
                    _notifier.PostNotificationAsync(new NotificationMessage
                    {
                        ChannelName = "Legion",
                        TypeName = "EventBased",
                        Subject = $"{bot.Username} approved {page.Name}",
                        Content = decision.Item2,
                        Sender = new NotificationUser(bot.Username),
                        Recipients = new []{ new NotificationUser(approval.StartedBy) }
                    }).Wait();
                }
                else if (decision.Item1 == ApprovalStatus.Rejected)
                {
                    _approvalEngine.RejectAsync(
                        approval.ID,
                        bot.Username,
                        e.StepIndex,
                        ApprovalDecisionScope.Step).Wait();
                    _notifier.PostNotificationAsync(new NotificationMessage
                    {
                        ChannelName = "Legion",
                        TypeName = "EventBased",
                        Subject = $"{bot.Username} DENIED {page.Name}!",
                        Content = decision.Item2,
                        Sender = new NotificationUser(bot.Username),
                        Recipients = new[] { new NotificationUser(approval.StartedBy) }
                    }).Wait();
                    // Note: Rejecting will throw an exception if the step has already been approved.
                    break;
                }
            }
        }

        #region Not important for Notifications API demonstration

        private IContentRepository _contentRepository;
        private IEnumerable<ILegionApprover> _bots;
        private INotifier _notifier;

        public void Initialize(InitializationEngine context)
        {
            _approvalEngine = context.Locate.Advanced.GetInstance<IApprovalEngine>();
            _approvalEngineEvents = context.Locate.Advanced.GetInstance<IApprovalEngineEvents>();
            _approvalRepository = context.Locate.Advanced.GetInstance<IApprovalRepository>();
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
