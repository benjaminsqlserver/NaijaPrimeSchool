using NaijaPrimeSchool.Domain.Common;
using NaijaPrimeSchool.Domain.Family;
using NaijaPrimeSchool.Domain.Identity;

namespace NaijaPrimeSchool.Domain.Messaging;

// A private conversation between one family member (a parent or student
// portal user) and the school office. The office side is a shared inbox:
// any SuperAdmin / HeadTeacher can read and reply.
public class MessageThread : BaseEntity
{
    public string Subject { get; set; } = string.Empty;

    // The parent or student on the family side of the conversation.
    public Guid FamilyUserId { get; set; }
    public ApplicationUser? FamilyUser { get; set; }

    // Optional: the pupil the conversation is about. For a parent it must be
    // one of their linked wards; for a student it is themselves.
    public Guid? StudentId { get; set; }
    public Student? Student { get; set; }

    public Guid MessageThreadStatusId { get; set; }
    public MessageThreadStatus? MessageThreadStatus { get; set; }

    public bool StartedByStaff { get; set; }

    // Denormalised for fast inbox sorting and unread checks.
    public DateTimeOffset LastMessageOn { get; set; }
    public bool LastMessageFromStaff { get; set; }

    // Read markers, one per side. A side has unread messages when the other
    // side posted after its marker.
    public DateTimeOffset? FamilyLastReadOn { get; set; }
    public DateTimeOffset? StaffLastReadOn { get; set; }

    public DateTimeOffset? ClosedOn { get; set; }

    public ICollection<ThreadMessage> Messages { get; set; } = [];
}
