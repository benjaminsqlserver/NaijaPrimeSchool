using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Messaging.Dtos;

namespace NaijaPrimeSchool.Application.Messaging;

// Two-way messaging between families and the school office. Every method
// works out from ICurrentUser which side the caller is on: SuperAdmin /
// HeadTeacher act as the office and see every thread; a parent or student
// only ever sees threads where they are the family participant.
public interface IMessagingService
{
    // Office inbox (all threads) for staff; the caller's own threads for a family.
    Task<IReadOnlyList<MessageThreadSummaryDto>> ListThreadsAsync(InboxFilter filter, CancellationToken ct = default);

    // Returns null when the thread does not exist or the caller may not see
    // it. Marks the caller's side as read.
    Task<MessageThreadDto?> GetThreadAsync(Guid threadId, CancellationToken ct = default);

    Task<OperationResult<Guid>> StartThreadAsync(StartThreadRequest request, CancellationToken ct = default);
    Task<OperationResult> ReplyAsync(Guid threadId, string body, CancellationToken ct = default);

    // Staff only.
    Task<OperationResult> SetClosedAsync(Guid threadId, bool closed, CancellationToken ct = default);

    // Threads with messages from the other side the caller hasn't read yet.
    Task<int> CountUnreadThreadsAsync(CancellationToken ct = default);

    // Staff only: parents and students with an active portal account.
    Task<IReadOnlyList<MessageRecipientDto>> GetRecipientsAsync(CancellationToken ct = default);

    // Pupils a thread can be about: the family user's linked wards (parent)
    // or themselves (student). Staff pass the family user; families pass null.
    Task<IReadOnlyList<MessageStudentOptionDto>> GetStudentOptionsAsync(Guid? familyUserId = null, CancellationToken ct = default);
}
