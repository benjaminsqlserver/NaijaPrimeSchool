using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Domain.Messaging;

public class ThreadMessage : BaseEntity
{
    public Guid MessageThreadId { get; set; }
    public MessageThread? MessageThread { get; set; }

    public Guid SenderUserId { get; set; }
    public ApplicationUser? SenderUser { get; set; }

    // True when a SuperAdmin / HeadTeacher wrote it on behalf of the office.
    public bool IsFromStaff { get; set; }

    public string Body { get; set; } = string.Empty;
    public DateTimeOffset SentOn { get; set; } = DateTimeOffset.UtcNow;
}
