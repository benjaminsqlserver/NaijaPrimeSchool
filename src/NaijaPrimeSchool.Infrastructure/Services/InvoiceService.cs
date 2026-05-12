using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Finance;
using NaijaPrimeSchool.Application.Finance.Dtos;
using NaijaPrimeSchool.Domain.Finance;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class InvoiceService(ApplicationDbContext db) : IInvoiceService
{
    private static IQueryable<InvoiceDto> Project(IQueryable<Invoice> q) =>
        q.Select(i => new InvoiceDto
        {
            Id = i.Id,
            InvoiceNumber = i.InvoiceNumber,
            IssuedOn = i.IssuedOn,
            DueDate = i.DueDate,
            StudentId = i.StudentId,
            StudentName = (i.Student!.FirstName + " " + i.Student!.LastName).Trim(),
            StudentAdmissionNumber = i.Student!.AdmissionNumber,
            StudentPhotoUrl = i.Student!.PhotoUrl,
            StudentFirstName = i.Student!.FirstName,
            StudentLastName = i.Student!.LastName,
            TermId = i.TermId,
            TermName = i.Term!.TermType!.Name + " — " + i.Term!.Session!.Name,
            SessionId = i.Term!.SessionId,
            SessionName = i.Term!.Session!.Name,
            SchoolClassId = i.SchoolClassId,
            SchoolClassName = i.SchoolClass!.Name,
            InvoiceStatusId = i.InvoiceStatusId,
            InvoiceStatusName = i.InvoiceStatus!.Name,
            InvoiceStatusCode = i.InvoiceStatus!.Code,
            Subtotal = i.Subtotal,
            DiscountTotal = i.DiscountTotal,
            AmountDue = i.AmountDue,
            AmountPaid = i.AmountPaid,
            Notes = i.Notes,
        });

    public async Task<IReadOnlyList<InvoiceDto>> ListAsync(InvoiceFilter filter, CancellationToken ct = default)
    {
        var q = db.Invoices.AsQueryable();
        if (filter.StudentId.HasValue) q = q.Where(i => i.StudentId == filter.StudentId.Value);
        if (filter.TermId.HasValue) q = q.Where(i => i.TermId == filter.TermId.Value);
        if (filter.SessionId.HasValue) q = q.Where(i => i.Term!.SessionId == filter.SessionId.Value);
        if (filter.SchoolClassId.HasValue) q = q.Where(i => i.SchoolClassId == filter.SchoolClassId.Value);
        if (filter.InvoiceStatusId.HasValue) q = q.Where(i => i.InvoiceStatusId == filter.InvoiceStatusId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            q = q.Where(i =>
                i.InvoiceNumber.ToLower().Contains(term)
                || i.Student!.FirstName.ToLower().Contains(term)
                || i.Student!.LastName.ToLower().Contains(term)
                || i.Student!.AdmissionNumber.ToLower().Contains(term));
        }

        return await Project(q.OrderByDescending(i => i.IssuedOn).ThenBy(i => i.InvoiceNumber))
            .ToListAsync(ct);
    }

    public async Task<InvoiceDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await Project(db.Invoices.Where(i => i.Id == id))
            .FirstOrDefaultAsync(ct);
        if (invoice is null) return null;

        var lines = await db.InvoiceLines
            .Where(l => l.InvoiceId == id)
            .OrderBy(l => l.FeeCategory!.DisplayOrder)
            .Select(l => new InvoiceLineDto
            {
                Id = l.Id,
                InvoiceId = l.InvoiceId,
                FeeCategoryId = l.FeeCategoryId,
                FeeCategoryName = l.FeeCategory!.Name,
                Description = l.Description,
                Amount = l.Amount,
                Discount = l.Discount,
            })
            .ToListAsync(ct);

        var payments = await db.PaymentAllocations
            .Where(a => a.InvoiceId == id)
            .OrderByDescending(a => a.Payment!.PaidOn)
            .Select(a => new InvoicePaymentDto
            {
                PaymentId = a.PaymentId,
                ReceiptNumber = a.Payment!.ReceiptNumber,
                PaidOn = a.Payment!.PaidOn,
                PaymentMethodName = a.Payment!.PaymentMethod!.Name,
                AmountApplied = a.AmountApplied,
                Reference = a.Payment!.Reference,
            })
            .ToListAsync(ct);

        return new InvoiceDetailDto { Invoice = invoice, Lines = lines, Payments = payments };
    }

    public async Task<OperationResult<IssueInvoicesResponse>> IssueAsync(IssueInvoicesRequest request, CancellationToken ct = default)
    {
        var schedule = await db.FeeSchedules
            .Include(s => s.Items)
                .ThenInclude(i => i.FeeCategory)
            .Include(s => s.Term)
            .FirstOrDefaultAsync(s => s.Id == request.FeeScheduleId, ct);
        if (schedule is null)
            return OperationResult<IssueInvoicesResponse>.Failure("Fee schedule not found.");
        if (!schedule.IsPublished)
            return OperationResult<IssueInvoicesResponse>.Failure(
                "Publish the fee schedule before issuing invoices from it.");
        if (schedule.Items.Count == 0)
            return OperationResult<IssueInvoicesResponse>.Failure(
                "The fee schedule has no line items.");

        var schoolClass = await db.SchoolClasses.FirstOrDefaultAsync(c => c.Id == request.SchoolClassId, ct);
        if (schoolClass is null)
            return OperationResult<IssueInvoicesResponse>.Failure("Class not found.");

        if (schoolClass.ClassLevelId != schedule.ClassLevelId)
            return OperationResult<IssueInvoicesResponse>.Failure(
                "Class belongs to a different class level than the schedule.");

        var enrolledStudents = await db.Enrolments
            .Where(e => e.SchoolClassId == request.SchoolClassId && e.WithdrawnOn == null)
            .Select(e => e.StudentId)
            .Distinct()
            .ToListAsync(ct);

        if (request.StudentIds is { Count: > 0 })
            enrolledStudents = enrolledStudents.Intersect(request.StudentIds).ToList();

        if (enrolledStudents.Count == 0)
            return OperationResult<IssueInvoicesResponse>.Failure(
                "No actively-enrolled pupils in the selected class.");

        var existing = await db.Invoices
            .Where(i => i.TermId == schedule.TermId
                && i.SchoolClassId == request.SchoolClassId
                && enrolledStudents.Contains(i.StudentId))
            .Select(i => i.StudentId)
            .ToListAsync(ct);

        var issuedStatusId = await db.InvoiceStatuses
            .Where(s => s.Code == "ISSUED")
            .Select(s => s.Id)
            .FirstOrDefaultAsync(ct);
        if (issuedStatusId == Guid.Empty)
            return OperationResult<IssueInvoicesResponse>.Failure(
                "Invoice statuses are not seeded.");

        // Compute a per-issuance starting sequence by counting existing invoices
        // for the issue year.
        var year = request.IssuedOn.Year;
        var yearPrefix = $"NPS/INV/{year}/";
        var maxSeq = await db.Invoices
            .Where(i => i.InvoiceNumber.StartsWith(yearPrefix))
            .Select(i => i.InvoiceNumber)
            .ToListAsync(ct);
        int nextSeq = ParseHighestSequence(maxSeq, yearPrefix) + 1;

        var issued = 0;
        var skipped = 0;
        var warnings = new List<string>();
        var subtotal = schedule.Items.Sum(i => i.Amount);

        foreach (var studentId in enrolledStudents)
        {
            if (existing.Contains(studentId))
            {
                skipped++;
                continue;
            }

            var invoice = new Invoice
            {
                StudentId = studentId,
                TermId = schedule.TermId,
                SchoolClassId = request.SchoolClassId,
                InvoiceStatusId = issuedStatusId,
                InvoiceNumber = yearPrefix + nextSeq.ToString("D4"),
                IssuedOn = request.IssuedOn,
                DueDate = request.DueDate,
                Subtotal = subtotal,
                DiscountTotal = 0m,
                AmountDue = subtotal,
                AmountPaid = 0m,
            };
            foreach (var item in schedule.Items)
            {
                invoice.Lines.Add(new InvoiceLine
                {
                    FeeCategoryId = item.FeeCategoryId,
                    FeeScheduleItemId = item.Id,
                    Description = item.Description,
                    Amount = item.Amount,
                    Discount = 0m,
                });
            }
            db.Invoices.Add(invoice);
            issued++;
            nextSeq++;
        }

        if (skipped > 0)
            warnings.Add($"{skipped} pupil(s) already had an invoice for this term/class. Skipped.");

        await db.SaveChangesAsync(ct);
        return OperationResult<IssueInvoicesResponse>.Success(new IssueInvoicesResponse
        {
            Issued = issued,
            Skipped = skipped,
            Warnings = warnings,
        });
    }

    private static int ParseHighestSequence(IEnumerable<string> numbers, string prefix)
    {
        var max = 0;
        foreach (var n in numbers)
        {
            if (!n.StartsWith(prefix)) continue;
            if (int.TryParse(n[prefix.Length..], out var seq) && seq > max) max = seq;
        }
        return max;
    }

    public async Task<OperationResult> SetLineDiscountAsync(UpdateInvoiceLineDiscountRequest request, CancellationToken ct = default)
    {
        var line = await db.InvoiceLines
            .Include(l => l.Invoice)
            .FirstOrDefaultAsync(l => l.Id == request.InvoiceLineId, ct);
        if (line is null) return OperationResult.Failure("Invoice line not found.");

        if (request.Discount < 0 || request.Discount > line.Amount)
            return OperationResult.Failure(
                $"Discount must be between 0 and {line.Amount:N2}.");

        var invoice = line.Invoice!;
        if (invoice.InvoiceStatus is null)
            invoice.InvoiceStatus = await db.InvoiceStatuses
                .FirstOrDefaultAsync(s => s.Id == invoice.InvoiceStatusId, ct);

        if (invoice.InvoiceStatus?.Code == "CANCELLED")
            return OperationResult.Failure("Cancelled invoices cannot be edited.");

        line.Discount = request.Discount;

        // Recompute totals from the lines.
        await RecomputeInvoiceTotalsAsync(invoice.Id, ct);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.Allocations)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null) return OperationResult.Failure("Invoice not found.");

        if (invoice.Allocations.Any(a => a.AmountApplied > 0))
            return OperationResult.Failure(
                "Cannot cancel an invoice that has payments applied. Refund the payments first.");

        var cancelledId = await db.InvoiceStatuses
            .Where(s => s.Code == "CANCELLED")
            .Select(s => s.Id)
            .FirstOrDefaultAsync(ct);
        if (cancelledId == Guid.Empty)
            return OperationResult.Failure("Cancelled status is not seeded.");

        invoice.InvoiceStatusId = cancelledId;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var invoice = await db.Invoices
            .Include(i => i.Allocations)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null) return OperationResult.Failure("Invoice not found.");

        if (invoice.Allocations.Any(a => a.AmountApplied > 0))
            return OperationResult.Failure(
                "Cannot delete an invoice with payments applied.");

        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<StudentLedgerDto?> GetStudentLedgerAsync(Guid studentId, Guid? termId = null, CancellationToken ct = default)
    {
        var student = await db.Students
            .Where(s => s.Id == studentId)
            .Select(s => new
            {
                s.Id,
                Name = (s.FirstName + " " + s.LastName).Trim(),
                s.AdmissionNumber,
                s.PhotoUrl,
                s.FirstName,
                s.LastName,
            })
            .FirstOrDefaultAsync(ct);
        if (student is null) return null;

        var invoiceQuery = db.Invoices.Where(i => i.StudentId == studentId);
        if (termId.HasValue) invoiceQuery = invoiceQuery.Where(i => i.TermId == termId.Value);

        var invoices = await Project(invoiceQuery.OrderByDescending(i => i.IssuedOn))
            .ToListAsync(ct);

        var paymentQuery = db.Payments.Where(p => p.StudentId == studentId);
        if (termId.HasValue)
        {
            // Restrict payments to those that touched invoices in the term.
            paymentQuery = paymentQuery.Where(p =>
                p.Allocations.Any(a => a.Invoice!.TermId == termId.Value));
        }
        var payments = await paymentQuery
            .OrderByDescending(p => p.PaidOn)
            .Select(p => new PaymentDto
            {
                Id = p.Id,
                ReceiptNumber = p.ReceiptNumber,
                PaidOn = p.PaidOn,
                Amount = p.Amount,
                StudentId = p.StudentId,
                StudentName = student.Name,
                StudentAdmissionNumber = student.AdmissionNumber,
                StudentPhotoUrl = student.PhotoUrl,
                StudentFirstName = student.FirstName,
                StudentLastName = student.LastName,
                PaymentMethodId = p.PaymentMethodId,
                PaymentMethodName = p.PaymentMethod!.Name,
                PaymentStatusId = p.PaymentStatusId,
                PaymentStatusName = p.PaymentStatus!.Name,
                PaymentStatusCode = p.PaymentStatus!.Code,
                Reference = p.Reference,
                Notes = p.Notes,
                CollectedById = p.CollectedById,
                CollectedByName = p.CollectedBy == null
                    ? null
                    : (p.CollectedBy!.FirstName + " " + p.CollectedBy!.LastName).Trim(),
                AmountAllocated = p.Allocations.Sum(a => (decimal?)a.AmountApplied) ?? 0m,
            })
            .ToListAsync(ct);

        return new StudentLedgerDto
        {
            StudentId = studentId,
            StudentName = student.Name,
            StudentAdmissionNumber = student.AdmissionNumber,
            StudentPhotoUrl = student.PhotoUrl,
            StudentFirstName = student.FirstName,
            StudentLastName = student.LastName,
            TotalInvoiced = invoices.Sum(i => i.AmountDue),
            TotalPaid = invoices.Sum(i => i.AmountPaid),
            Invoices = invoices.ToList(),
            Payments = payments,
        };
    }

    // Internal helper — also used by PaymentService after allocations.
    internal async Task RecomputeInvoiceTotalsAsync(Guid invoiceId, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Allocations)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        if (invoice is null) return;

        invoice.Subtotal = invoice.Lines.Sum(l => l.Amount);
        invoice.DiscountTotal = invoice.Lines.Sum(l => l.Discount);
        invoice.AmountDue = invoice.Subtotal - invoice.DiscountTotal;
        invoice.AmountPaid = invoice.Allocations.Sum(a => a.AmountApplied);

        // Bring status into line with money flow.
        var statusCode = invoice.AmountPaid <= 0m
            ? "ISSUED"
            : invoice.AmountPaid >= invoice.AmountDue
                ? "PAID"
                : "PARTIAL";

        // Never overwrite a CANCELLED row from here.
        var currentStatus = await db.InvoiceStatuses
            .FirstOrDefaultAsync(s => s.Id == invoice.InvoiceStatusId, ct);
        if (currentStatus?.Code == "CANCELLED") return;

        var targetStatus = await db.InvoiceStatuses
            .FirstOrDefaultAsync(s => s.Code == statusCode, ct);
        if (targetStatus is not null) invoice.InvoiceStatusId = targetStatus.Id;
    }
}
