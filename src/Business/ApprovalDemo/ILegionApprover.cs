using EPiServer.Approvals;
using EPiServer.Core;

namespace Ascend2016.Business.ApprovalDemo
{
    public interface ILegionApprover
    {
        string Username { get; }
        ApprovalStatus DoDecide(PageData page);
    }
}
