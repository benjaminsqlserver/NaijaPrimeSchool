using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Communications;
using NaijaPrimeSchool.Application.Messaging;
using NaijaPrimeSchool.Application.Messaging.Dtos;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Domain.Messaging;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class MessagingService(
    ApplicationDbContext db,
    ICurrentUser currentUser,
    INotificationService notifications) : IMessagingService
{
    private const string Open = "OPEN";
    private const string Closed = "CLOSED";
    private const int MaxSubject = 200;
    private const int MaxBody = 4000;
    private const int PreviewLength = 120;

    // The office has unread messages when the family posted after StaffLastReadOn,
    // and vice versa.
    private static readonly Expression<Func<MessageThread, bool>> UnreadForStaff = t =>
        t.Messages.Any(m => !m.IsFromStaff && (t.StaffLastReadOn == null || m.SentOn > t.StaffLastReadOn));

    private static readonly Expression<Func<MessageThread, bool>> UnreadForFamily = t =>
        t.Messages.Any(m => m.IsFromStaff && (t.FamilyLastReadOn == null || m.SentOn > t.FamilyLastReadOn));

    private bool IsStaff => currentUser.IsInRole(Roles.SuperAdmin) || currentUser.IsInRole(Roles.HeadTeacher);
    private bool IsFamily => currentUser.IsInRole(Roles.Parent) || currentUser.IsInRole(Roles.Student);

    public async Task<IReadOnlyList<MessageThreadSummaryDto>> ListThreadsAsync(InboxFilter filter, CancellationToken ct = default)
    {
        if (VisibleThreads() is not { } q) return [];

        if (filter.Open is { } open)
            q = q.Where(t => (t.MessageThreadStatus!.Code == Open) == open);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            q = q.Where(t =>
                t.Subject.ToLower().Contains(term)
                || (t.FamilyUser!.FirstName + " " + t.FamilyUser.LastName).ToLower().Contains(term)
                || (t.Student != null && (t.Student.FirstName + " " + t.Student.LastName).ToLower().Contains(term))
                || t.Messages.Any(m => m.Body.ToLower().Contains(term)));
        }

        var unread = IsStaff ? UnreadForStaff : UnreadForFamily;
        var unreadIds = (await q.Where(unread).Select(t => t.Id).ToListAsync(ct)).ToHashSet();
        if (filter.UnreadOnly) q = q.Where(t => unreadIds.Contains(t.Id));

        var rows = await q
            .OrderByDescending(t => t.LastMessageOn)
            .Take(500)
            .Select(t => new MessageThreadSummaryDto
            {
                Id = t.Id,
                Subject = t.Subject,
                FamilyUserId = t.FamilyUserId,
                FamilyName = t.FamilyUser!.FirstName + " " + t.FamilyUser.LastName,
                FamilyRole = db.Parents.Any(p => p.UserId == t.FamilyUserId) ? "Parent" : "Student",
                StudentId = t.StudentId,
                StudentName = t.Student == null ? null : t.Student.FirstName + " " + t.Student.LastName,
                StatusCode = t.MessageThreadStatus!.Code,
                StatusName = t.MessageThreadStatus.Name,
                LastMessageOn = t.LastMessageOn,
                LastMessageFromStaff = t.LastMessageFromStaff,
                LastMessagePreview = t.Messages.OrderByDescending(m => m.SentOn).Select(m => m.Body).FirstOrDefault() ?? "",
                MessageCount = t.Messages.Count,
            })
            .ToListAsync(ct);

        foreach (var r in rows)
        {
            r.HasUnread = unreadIds.Contains(r.Id);
            r.LastMessagePreview = Preview(r.LastMessagePreview);
        }
        return rows;
    }

    public async Task<MessageThreadDto?> GetThreadAsync(Guid threadId, CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } me || VisibleThreads() is not { } q) return null;

        var t = await q
            .Include(x => x.MessageThreadStatus)
            .Include(x => x.FamilyUser)
            .Include(x => x.Student)
            .Include(x => x.Messages.OrderBy(m => m.SentOn)).ThenInclude(m => m.SenderUser)
            .FirstOrDefaultAsync(x => x.Id == threadId, ct);
        if (t is null) return null;

        var staff = IsStaff;
        var seen = staff ? t.StaffLastReadOn : t.FamilyLastReadOn;
        var hadUnread = t.Messages.Any(m => m.IsFromStaff != staff && (seen is null || m.SentOn > seen));

        // Only touch the row when something was actually unread, so simply
        // viewing a conversation doesn't churn the audit columns.
        if (hadUnread)
        {
            if (staff) t.StaffLastReadOn = DateTimeOffset.UtcNow;
            else t.FamilyLastReadOn = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        var isParent = await db.Parents.AnyAsync(p => p.UserId == t.FamilyUserId, ct);
        return new MessageThreadDto
        {
            Id = t.Id,
            Subject = t.Subject,
            FamilyUserId = t.FamilyUserId,
            FamilyName = t.FamilyUser is null ? "" : $"{t.FamilyUser.FirstName} {t.FamilyUser.LastName}".Trim(),
            FamilyRole = isParent ? "Parent" : "Student",
            StudentId = t.StudentId,
            StudentName = t.Student is null ? null : $"{t.Student.FirstName} {t.Student.LastName}".Trim(),
            StatusCode = t.MessageThreadStatus!.Code,
            StatusName = t.MessageThreadStatus.Name,
            LastMessageOn = t.LastMessageOn,
            LastMessageFromStaff = t.LastMessageFromStaff,
            MessageCount = t.Messages.Count,
            StartedByStaff = t.StartedByStaff,
            CreatedOn = t.CreatedOn,
            Messages = t.Messages
                .OrderBy(m => m.SentOn)
                .Select(m => new ThreadMessageDto
                {
                    Id = m.Id,
                    SenderName = m.SenderUser is null ? "" : $"{m.SenderUser.FirstName} {m.SenderUser.LastName}".Trim(),
                    IsFromStaff = m.IsFromStaff,
                    IsMine = m.SenderUserId == me,
                    Body = m.Body,
                    SentOn = m.SentOn,
                })
                .ToList(),
        };
    }

    public async Task<OperationResult<Guid>> StartThreadAsync(StartThreadRequest request, CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } me)
            return OperationResult<Guid>.Failure("Not signed in.");

        var staff = IsStaff;
        if (!staff && !IsFamily)
            return OperationResult<Guid>.Failure("Only parents, students and the school office can send messages.");

        var subject = request.Subject?.Trim() ?? "";
        var body = request.Body?.Trim() ?? "";
        var errors = new List<string>();
        if (subject.Length == 0) errors.Add("Subject is required.");
        else if (subject.Length > MaxSubject) errors.Add($"Subject must be at most {MaxSubject} characters.");
        errors.AddRange(ValidateBody(body));

        Guid familyUserId;
        if (staff)
        {
            if (request.FamilyUserId is not { } target)
            {
                errors.Add("Choose a parent or student to write to.");
                return OperationResult<Guid>.Failure(errors);
            }
            if (!await IsActiveFamilyUserAsync(target, ct))
                errors.Add("That recipient does not have an active parent or student portal account.");
            familyUserId = target;
        }
        else
        {
            familyUserId = me;
        }

        if (request.StudentId is { } studentId
            && !(await GetStudentOptionsForAsync(familyUserId, ct)).Any(s => s.StudentId == studentId))
        {
            errors.Add("That pupil is not linked to this family.");
        }

        if (errors.Count > 0) return OperationResult<Guid>.Failure(errors);

        var now = DateTimeOffset.UtcNow;
        var thread = new MessageThread
        {
            Subject = subject,
            FamilyUserId = familyUserId,
            StudentId = request.StudentId,
            MessageThreadStatusId = await StatusIdAsync(Open, ct),
            StartedByStaff = staff,
            LastMessageOn = now,
            LastMessageFromStaff = staff,
            StaffLastReadOn = staff ? now : null,
            FamilyLastReadOn = staff ? null : now,
        };
        thread.Messages.Add(new ThreadMessage
        {
            SenderUserId = me,
            IsFromStaff = staff,
            Body = body,
            SentOn = now,
        });

        db.MessageThreads.Add(thread);
        await db.SaveChangesAsync(ct);
        if (staff) await notifications.QueueMessageAlertAsync(thread.Id, ct);
        return OperationResult<Guid>.Success(thread.Id);
    }

    public async Task<OperationResult> ReplyAsync(Guid threadId, string body, CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } me || VisibleThreads() is not { } q)
            return OperationResult.Failure("Not signed in.");

        body = body?.Trim() ?? "";
        var errors = ValidateBody(body);
        if (errors.Count > 0) return OperationResult.Failure(errors);

        var t = await q.Include(x => x.MessageThreadStatus).FirstOrDefaultAsync(x => x.Id == threadId, ct);
        if (t is null) return OperationResult.Failure("Conversation not found.");

        var staff = IsStaff;
        var now = DateTimeOffset.UtcNow;
        db.ThreadMessages.Add(new ThreadMessage
        {
            MessageThreadId = t.Id,
            SenderUserId = me,
            IsFromStaff = staff,
            Body = body,
            SentOn = now,
        });

        t.LastMessageOn = now;
        t.LastMessageFromStaff = staff;
        if (staff) t.StaffLastReadOn = now;
        else t.FamilyLastReadOn = now;

        // Any new message reopens a closed conversation.
        if (t.MessageThreadStatus!.Code == Closed)
        {
            t.MessageThreadStatusId = await StatusIdAsync(Open, ct);
            t.ClosedOn = null;
        }

        await db.SaveChangesAsync(ct);

        // Office wrote to the family: queue an email / SMS alert, sent only if
        // they haven't read it in the portal by the end of the grace period.
        if (staff) await notifications.QueueMessageAlertAsync(t.Id, ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SetClosedAsync(Guid threadId, bool closed, CancellationToken ct = default)
    {
        if (!IsStaff) return OperationResult.Failure("Only the school office can open or close conversations.");

        var t = await db.MessageThreads.FirstOrDefaultAsync(x => x.Id == threadId, ct);
        if (t is null) return OperationResult.Failure("Conversation not found.");

        t.MessageThreadStatusId = await StatusIdAsync(closed ? Closed : Open, ct);
        t.ClosedOn = closed ? DateTimeOffset.UtcNow : null;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<int> CountUnreadThreadsAsync(CancellationToken ct = default)
    {
        if (VisibleThreads() is not { } q) return 0;
        return await q.CountAsync(IsStaff ? UnreadForStaff : UnreadForFamily, ct);
    }

    public async Task<IReadOnlyList<MessageRecipientDto>> GetRecipientsAsync(CancellationToken ct = default)
    {
        if (!IsStaff) return [];

        var parents = await db.Parents
            .Where(p => p.IsActive && p.UserId != null && p.User!.IsActive)
            .Select(p => new MessageRecipientDto { UserId = p.UserId!.Value, Name = p.FirstName + " " + p.LastName, Role = "Parent" })
            .ToListAsync(ct);
        var students = await db.Students
            .Where(s => s.IsActive && s.UserId != null && s.User!.IsActive)
            .Select(s => new MessageRecipientDto { UserId = s.UserId!.Value, Name = s.FirstName + " " + s.LastName, Role = "Student" })
            .ToListAsync(ct);

        return parents.Concat(students).OrderBy(r => r.Name).ThenBy(r => r.Role).ToList();
    }

    public async Task<IReadOnlyList<MessageStudentOptionDto>> GetStudentOptionsAsync(Guid? familyUserId = null, CancellationToken ct = default)
    {
        // Families may only ask about themselves; staff must say whose wards.
        var target = IsStaff ? familyUserId : currentUser.UserId;
        return target is { } id ? await GetStudentOptionsForAsync(id, ct) : [];
    }

    // Staff see every thread; a parent or student only their own; anyone
    // else nothing (null).
    private IQueryable<MessageThread>? VisibleThreads()
    {
        if (currentUser.UserId is not { } me) return null;
        if (IsStaff) return db.MessageThreads;
        if (IsFamily) return db.MessageThreads.Where(t => t.FamilyUserId == me);
        return null;
    }

    private async Task<IReadOnlyList<MessageStudentOptionDto>> GetStudentOptionsForAsync(Guid familyUserId, CancellationToken ct)
    {
        var wards = await db.StudentParents
            .Where(l => l.Parent!.UserId == familyUserId && l.Student!.IsActive)
            .Select(l => new MessageStudentOptionDto { StudentId = l.StudentId, Name = l.Student!.FirstName + " " + l.Student.LastName })
            .ToListAsync(ct);
        var self = await db.Students
            .Where(s => s.UserId == familyUserId)
            .Select(s => new MessageStudentOptionDto { StudentId = s.Id, Name = s.FirstName + " " + s.LastName })
            .ToListAsync(ct);

        return wards.Concat(self).DistinctBy(s => s.StudentId).OrderBy(s => s.Name).ToList();
    }

    private async Task<bool> IsActiveFamilyUserAsync(Guid userId, CancellationToken ct) =>
        await db.Parents.AnyAsync(p => p.UserId == userId && p.IsActive && p.User!.IsActive, ct)
        || await db.Students.AnyAsync(s => s.UserId == userId && s.IsActive && s.User!.IsActive, ct);

    private static List<string> ValidateBody(string body)
    {
        var errors = new List<string>();
        if (body.Length == 0) errors.Add("Message cannot be empty.");
        else if (body.Length > MaxBody) errors.Add($"Message must be at most {MaxBody} characters.");
        return errors;
    }

    private Task<Guid> StatusIdAsync(string code, CancellationToken ct) =>
        db.MessageThreadStatuses.Where(s => s.Code == code).Select(s => s.Id).FirstAsync(ct);

    private static string Preview(string body)
    {
        var flat = string.Join(' ', body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return flat.Length <= PreviewLength ? flat : flat[..(PreviewLength - 3)] + "...";
    }
}
