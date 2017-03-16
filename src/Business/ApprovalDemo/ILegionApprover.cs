using System;
using EPiServer.Approvals;
using EPiServer.Core;

namespace Approvals.Business.ApprovalDemo
{
    public interface ILegionApprover
    {
        string Username { get; }
        Tuple<ApprovalStatus, string> DoDecide(PageData page);
    }
}
