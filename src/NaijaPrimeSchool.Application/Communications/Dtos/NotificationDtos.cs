namespace NaijaPrimeSchool.Application.Communications.Dtos;

public class AnnouncementNotificationDto
{
    public Guid Id { get; set; }

    // "ANNOUNCEMENT" or "MESSAGE" — what the notification is about.
    public string Kind { get; set; } = string.Empty;
    public bool IsMessageAlert => Kind == "MESSAGE";

    public Guid? AnnouncementId { get; set; }
    public Guid? MessageThreadId { get; set; }

    // The announcement title or the conversation subject.
    public string Title { get; set; } = string.Empty;

    public Guid UserId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;

    public Guid ChannelId { get; set; }
    public string ChannelName { get; set; } = string.Empty;
    public string ChannelCode { get; set; } = string.Empty;

    public Guid StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;

    public DateTimeOffset ScheduledFor { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? LastAttemptOn { get; set; }
    public DateTimeOffset? SentOn { get; set; }
    public string? LastError { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
}

public class NotificationFilter
{
    public string? Search { get; set; }
    public Guid? AnnouncementId { get; set; }
    public Guid? MessageThreadId { get; set; }
    // "ANNOUNCEMENT", "MESSAGE" or null for both.
    public string? Kind { get; set; }
    public Guid? ChannelId { get; set; }
    public Guid? StatusId { get; set; }
    public int Take { get; set; } = 500;
}

// Queued, AlreadyQueued, MissingContact and OptedOut count messages (one per
// recipient per enabled channel); Recipients and AlreadyRead count people.
public class NotificationQueueResult
{
    // New rows created, plus Failed / Skipped rows re-armed for another try.
    public int Queued { get; set; }

    // Messages that already had a Pending or Sent row.
    public int AlreadyQueued { get; set; }

    // Messages not queued because the recipient has no usable email / phone.
    public int MissingContact { get; set; }

    // Messages not queued because the recipient turned that channel off.
    public int OptedOut { get; set; }

    // People in the audience who have already read it in the portal.
    public int AlreadyRead { get; set; }

    // People in the audience with an active portal account.
    public int Recipients { get; set; }
}

public class NotificationCounts
{
    public int Pending { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public int Total => Pending + Sent + Failed + Skipped;
}

public static class NotificationKinds
{
    public const string Announcement = "ANNOUNCEMENT";
    public const string Message = "MESSAGE";
}
