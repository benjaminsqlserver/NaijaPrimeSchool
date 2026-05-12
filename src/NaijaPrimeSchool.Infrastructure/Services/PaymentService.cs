using Microsoft.EntityFrameworkCore;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Finance;
using NaijaPrimeSchool.Application.Finance.Dtos;
using NaijaPrimeSchool.Domain.Finance;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class PaymentService(
    ApplicationDbContext db,
    InvoiceService invoiceService) : IPaymentService
{
    private static IQueryable<PaymentDto> Project(IQueryable<Payment> q) =>
        q.Select(p => new PaymentDto
        {
            Id = p.Id,
            ReceiptNumber = p.ReceiptNumber,
            PaidOn = p.PaidOn,
            Amount = p.Amount,
            StudentId = p.StudentId,
            StudentName = (p.Student!.FirstName + " " + p.Student!.LastName).Trim(),
            StudentAdmissionNumber = p.Student!.AdmissionNumber,
            StudentPhotoUrl = p.Student!.PhotoUrl,
            StudentFirstName = p.Student!.FirstName,
            StudentLastName = p.Student!.LastName,
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
        });

    public async Task<IReadOnlyList<PaymentDto>> ListAsync(PaymentFilter filter, CancellationToken ct = default)
    {
        var q = db.Payments.AsQueryable();
        if (filter.StudentId.HasValue) q = q.Where(p => p.StudentId == filter.StudentId.Value);
        if (filter.PaymentMethodId.HasValue) q = q.Where(p => p.PaymentMethodId == filter.PaymentMethodId.Value);
        if (filter.PaymentStatusId.HasValue) q = q.Where(p => p.PaymentStatusId == filter.PaymentStatusId.Value);
        if (filter.FromDate.HasValue) q = q.Where(p => p.PaidOn >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) q = q.Where(p => p.PaidOn <= filter.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            q = q.Where(p =>
                p.ReceiptNumber.ToLower().Contains(term)
                || (p.Reference != null && p.Reference.ToLower().Contains(term))
                || p.Student!.FirstName.ToLower().Contains(term)
                || p.Student!.LastName.ToLower().Contains(term)
                || p.Student!.AdmissionNumber.ToLower().Contains(term));
        }

        return await Project(q.OrderByDescending(p => p.PaidOn).ThenByDescending(p => p.ReceiptNumber))
            .ToListAsync(ct);
    }

    public async Task<PaymentDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var payment = await Project(db.Payments.Where(p => p.Id == id))
            .FirstOrDefaultAsync(ct);
        if (payment is null) return null;

        var allocations = await db.PaymentAllocations
            .Where(a => a.PaymentId == id)
            .OrderBy(a => a.Invoice!.IssuedOn)
            .Select(a => new PaymentAllocationDto
            {
                Id = a.Id,
                PaymentId = a.PaymentId,
                InvoiceId = a.InvoiceId,
                InvoiceNumber = a.Invoice!.InvoiceNumber,
                AmountApplied = a.AmountApplied,
                InvoiceBalanceAfter = a.Invoice!.AmountDue - a.Invoice!.AmountPaid,
            })
            .ToListAsync(ct);

        return new PaymentDetailDto { Payment = payment, Allocations = allocations };
    }

    public async Task<OperationResult<Guid>> RecordAsync(RecordPaymentRequest request, CancellationToken ct = default)
    {
        if (request.Amount <= 0)
            return OperationResult<Guid>.Failure("Payment amount must be greater than zero.");

        if (!await db.Students.AnyAsync(s => s.Id == request.StudentId, ct))
            return OperationResult<Guid>.Failure("Student not found.");
        if (!await db.PaymentMethods.AnyAsync(m => m.Id == request.PaymentMethodId, ct))
            return OperationResult<Guid>.Failure("Payment method not found.");

        var confirmedStatusId = await db.PaymentStatuses
            .Where(s => s.Code == "CONFIRMED")
            .Select(s => s.Id)
            .FirstOrDefaultAsync(ct);
        if (confirmedStatusId == Guid.Empty)
            return OperationResult<Guid>.Failure("Payment statuses are not seeded.");

        // Validate allocations against the student's invoices.
        var allocations = request.Allocations
            .Where(a => a.AmountApplied > 0m)
            .ToList();
        var totalAllocated = allocations.Sum(a => a.AmountApplied);
        if (totalAllocated > request.Amount)
            return OperationResult<Guid>.Failure(
                $"Allocations ({totalAllocated:N2}) exceed payment amount ({request.Amount:N2}).");

        var invoiceIds = allocations.Select(a => a.InvoiceId).ToList();
        var invoices = await db.Invoices
            .Where(i => invoiceIds.Contains(i.Id))
            .Include(i => i.InvoiceStatus)
            .ToListAsync(ct);

        if (invoices.Count != invoiceIds.Distinct().Count())
            return OperationResult<Guid>.Failure("One or more invoices were not found.");

        foreach (var allocation in allocations)
        {
            var invoice = invoices.First(i => i.Id == allocation.InvoiceId);
            if (invoice.StudentId != request.StudentId)
                return OperationResult<Guid>.Failure(
                    $"Invoice {invoice.InvoiceNumber} does not belong to the selected pupil.");
            if (invoice.InvoiceStatus?.Code == "CANCELLED")
                return OperationResult<Guid>.Failure(
                    $"Invoice {invoice.InvoiceNumber} is cancelled — cannot accept payment.");

            var outstanding = invoice.AmountDue - invoice.AmountPaid;
            if (allocation.AmountApplied > outstanding)
                return OperationResult<Guid>.Failure(
                    $"Allocation for {invoice.InvoiceNumber} ({allocation.AmountApplied:N2}) "
                    + $"exceeds the outstanding balance ({outstanding:N2}).");
        }

        // Receipt number is NPS/RCP/<year>/<sequence>.
        var year = request.PaidOn.Year;
        var prefix = $"NPS/RCP/{year}/";
        var existingNumbers = await db.Payments
            .Where(p => p.ReceiptNumber.StartsWith(prefix))
            .Select(p => p.ReceiptNumber)
            .ToListAsync(ct);
        var nextSeq = ParseHighestSequence(existingNumbers, prefix) + 1;

        var payment = new Payment
        {
            StudentId = request.StudentId,
            PaymentMethodId = request.PaymentMethodId,
            PaymentStatusId = confirmedStatusId,
            ReceiptNumber = prefix + nextSeq.ToString("D4"),
            PaidOn = request.PaidOn,
            Amount = request.Amount,
            Reference = request.Reference,
            Notes = request.Notes,
            CollectedById = request.CollectedById,
        };

        foreach (var a in allocations)
        {
            payment.Allocations.Add(new PaymentAllocation
            {
                InvoiceId = a.InvoiceId,
                AmountApplied = a.AmountApplied,
            });
        }
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        foreach (var invoiceId in invoiceIds.Distinct())
        {
            await invoiceService.RecomputeInvoiceTotalsAsync(invoiceId, ct);
        }
        await db.SaveChangesAsync(ct);

        return OperationResult<Guid>.Success(payment.Id);
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

    public async Task<OperationResult> UpdateAsync(UpdatePaymentRequest request, CancellationToken ct = default)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == request.Id, ct);
        if (payment is null) return OperationResult.Failure("Payment not found.");

        payment.Reference = request.Reference;
        payment.Notes = request.Notes;
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> RefundAsync(Guid id, CancellationToken ct = default)
    {
        var payment = await db.Payments
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (payment is null) return OperationResult.Failure("Payment not found.");

        var refundedId = await db.PaymentStatuses
            .Where(s => s.Code == "REFUNDED")
            .Select(s => s.Id)
            .FirstOrDefaultAsync(ct);
        if (refundedId == Guid.Empty)
            return OperationResult.Failure("Refunded status is not seeded.");

        var touchedInvoices = payment.Allocations.Select(a => a.InvoiceId).Distinct().ToList();

        // Drop the allocations so the corresponding invoices no longer count this
        // payment toward their AmountPaid. We do not delete the Payment row —
        // the receipt history stays intact.
        db.PaymentAllocations.RemoveRange(payment.Allocations);
        payment.PaymentStatusId = refundedId;
        await db.SaveChangesAsync(ct);

        foreach (var invoiceId in touchedInvoices)
        {
            await invoiceService.RecomputeInvoiceTotalsAsync(invoiceId, ct);
        }
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<OperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var payment = await db.Payments
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (payment is null) return OperationResult.Failure("Payment not found.");

        if (payment.Allocations.Any())
            return OperationResult.Failure(
                "Refund the payment first so its allocations are released, then delete.");

        db.Payments.Remove(payment);
        await db.SaveChangesAsync(ct);
        return OperationResult.Success();
    }

    public async Task<FinanceSummaryDto> GetSummaryAsync(Guid? termId, CancellationToken ct = default)
    {
        var termName = termId.HasValue
            ? await db.Terms
                .Where(t => t.Id == termId.Value)
                .Select(t => t.TermType!.Name + " — " + t.Session!.Name)
                .FirstOrDefaultAsync(ct)
            : null;

        var invQuery = db.Invoices.AsQueryable();
        if (termId.HasValue) invQuery = invQuery.Where(i => i.TermId == termId.Value);

        var invoiceRows = await invQuery
            .Select(i => new
            {
                i.AmountDue,
                i.AmountPaid,
                StatusCode = i.InvoiceStatus!.Code,
            })
            .ToListAsync(ct);

        var summary = new FinanceSummaryDto
        {
            TermId = termId,
            TermName = termName,
            InvoiceCount = invoiceRows.Count,
            PaidInvoiceCount = invoiceRows.Count(r => r.StatusCode == "PAID"),
            PartiallyPaidInvoiceCount = invoiceRows.Count(r => r.StatusCode == "PARTIAL"),
            UnpaidInvoiceCount = invoiceRows.Count(r =>
                r.StatusCode == "ISSUED" || r.StatusCode == "OVERDUE"),
            TotalInvoiced = invoiceRows.Sum(r => r.AmountDue),
            TotalCollected = invoiceRows.Sum(r => r.AmountPaid),
        };
        summary.TotalOutstanding = summary.TotalInvoiced - summary.TotalCollected;

        var payQuery = db.Payments.AsQueryable();
        if (termId.HasValue)
        {
            payQuery = payQuery.Where(p =>
                p.Allocations.Any(a => a.Invoice!.TermId == termId.Value));
        }
        summary.PaymentCount = await payQuery.CountAsync(ct);
        summary.ByMethod = await payQuery
            .GroupBy(p => new { p.PaymentMethodId, p.PaymentMethod!.Name })
            .Select(g => new MethodBreakdownDto
            {
                PaymentMethodId = g.Key.PaymentMethodId,
                PaymentMethodName = g.Key.Name,
                TotalAmount = g.Sum(p => p.Amount),
                PaymentCount = g.Count(),
            })
            .OrderByDescending(b => b.TotalAmount)
            .ToListAsync(ct);

        var lineQuery = db.InvoiceLines.AsQueryable();
        if (termId.HasValue)
            lineQuery = lineQuery.Where(l => l.Invoice!.TermId == termId.Value);
        summary.ByCategory = await lineQuery
            .GroupBy(l => new { l.FeeCategoryId, l.FeeCategory!.Name })
            .Select(g => new CategoryBreakdownDto
            {
                FeeCategoryId = g.Key.FeeCategoryId,
                FeeCategoryName = g.Key.Name,
                TotalInvoiced = g.Sum(l => l.Amount - l.Discount),
            })
            .OrderByDescending(b => b.TotalInvoiced)
            .ToListAsync(ct);

        return summary;
    }
}
