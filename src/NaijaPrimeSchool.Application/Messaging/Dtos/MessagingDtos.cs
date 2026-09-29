namespace NaijaPrimeSchool.Application.Messaging.Dtos;

public class MessageThreadSummaryDto
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;

    public Guid FamilyUserId { get; set; }
    public string FamilyName { get; set; } = string.Empty;
    // "Parent" or "Student".
    public string FamilyRole { get; set; } = string.Empty;

    public Guid? StudentId { get; set; }
    public string? StudentName { get; set; }

    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public bool IsClosed => StatusCode == "CLOSED";

    public DateTimeOffset LastMessageOn { get; set; }
    public bool LastMessageFromStaff { get; set; }
    public string LastMessagePreview { get; set; } = string.Empty;
    public int MessageCount { get; set; }

    // Unread from the point of view of whoever is asking (family or office).
    public bool HasUnread { get; set; }
}

public class MessageThreadDto : MessageThreadSummaryDto
{
    public bool StartedByStaff { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public IReadOnlyList<ThreadMessageDto> Messages { get; set; } = [];
}

public class ThreadMessageDto
{
    public Guid Id { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public bool IsFromStaff { get; set; }
    public bool IsMine { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset SentOn { get; set; }
}

public class InboxFilter
{
    public string? Search { get; set; }
    // null = all, true = open only, false = closed only.
    public bool? Open { get; set; } = true;
    public bool UnreadOnly { get; set; }
}

public class StartThreadRequest
{
    // Staff only: the parent / student portal user to write to. Ignored for
    // family senders (they always write as themselves to the office).
    public Guid? FamilyUserId { get; set; }
    public Guid? StudentId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class MessageRecipientDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Display => $"{Name} ({Role})";
}

public class MessageStudentOptionDto
{
    public Guid StudentId { get; set; }
    public string Name { get; set; } = string.Empty;
}
