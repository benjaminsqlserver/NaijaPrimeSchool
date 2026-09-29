using NaijaPrimeSchool.Domain.Common;

namespace NaijaPrimeSchool.Domain.Messaging;

// Lookup: OPEN (awaiting / in conversation) or CLOSED (resolved by the office).
public class MessageThreadStatus : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<MessageThread> Threads { get; set; } = [];
}
