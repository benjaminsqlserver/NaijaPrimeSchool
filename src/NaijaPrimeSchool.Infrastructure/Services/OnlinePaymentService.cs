using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NaijaPrimeSchool.Application.Common;
using NaijaPrimeSchool.Application.Finance;
using NaijaPrimeSchool.Application.Finance.Dtos;
using NaijaPrimeSchool.Domain.Finance;
using NaijaPrimeSchool.Domain.Identity;
using NaijaPrimeSchool.Infrastructure.Notifications;
using NaijaPrimeSchool.Infrastructure.Payments;
using NaijaPrimeSchool.Infrastructure.Persistence;

namespace NaijaPrimeSchool.Infrastructure.Services;

public class OnlinePaymentService(
    ApplicationDbContext db,
    ICurrentUser currentUser,
    IPaymentGateway gateway,
    IPaymentService payments,
    IOptions<OnlinePaymentOptions> options,
    IOptions<NotificationOptions> notificationOptions,
    ILogger<OnlinePaymentService> logger) : IOnlinePaymentService
{
    private const string Pending = "PENDING";
    private const string Succeeded = "SUCCEEDED";
    private const string Failed = "FAILED";
    private const string Abandoned = "ABANDONED";
    private const string Review = "REVIEW";
    private const string OnlineMethodCode = "ONL";

    private bool IsFinanceStaff =>
        currentUser.IsInRole(Roles.SuperAdmin) || currentUser.IsInRole(Roles.HeadTeacher) || currentUser.IsInRole(Roles.SchoolBursar);

    public OnlinePaymentSettingsDto GetSettings() => new()
    {
        Enabled = options.Value.Enabled,
        ProviderName = gateway.Name,
        MinimumAmount = options.Value.MinimumAmount,
    };

    public async Task<OperationResult<StartOnlinePaymentResult>> StartAsync(StartOnlinePaymentRequest request, CancellationToken ct = default)
    {
        var opts = options.Value;
        if (!opts.Enabled)
            return OperationResult<StartOnlinePaymentResult>.Failure("Online payment is not available at the moment.");
        if (currentUser.UserId is not { } me)
            return OperationResult<StartOnlinePaymentResult>.Failure("Not signed in.");
        if (!Uri.TryCreate(request.ReturnUrl, UriKind.Absolute, out var returnUri)
            || (returnUri.Scheme != Uri.UriSchemeHttps && returnUri.Scheme != Uri.UriSchemeHttp))
            return OperationResult<StartOnlinePaymentResult>.Failure("Invalid return address.");

        var invoice = await db.Invoices
            .Include(i => i.InvoiceStatus)
            .Include(i => i.Student)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct);
        if (invoice is null || !await CanPayForStudentAsync(me, invoice.StudentId, ct))
            return OperationResult<StartOnlinePaymentResult>.Failure("Invoice not found.");
        if (invoice.InvoiceStatus?.Code is "CANCELLED" or "DRAFT")
            return OperationResult<StartOnlinePaymentResult>.Failure("This invoice cannot be paid online.");

        var amount = decimal.Round(request.Amount, 2);
        var balance = invoice.AmountDue - invoice.AmountPaid;
        if (balance <= 0)
            return OperationResult<StartOnlinePaymentResult>.Failure("This invoice is already fully paid.");
        if (amount > balance)
            return OperationResult<StartOnlinePaymentResult>.Failure($"You can pay at most the outstanding balance of ₦{balance:N2}.");
        var minimum = Math.Min(opts.MinimumAmount, balance);
        if (amount < minimum)
            return OperationResult<StartOnlinePaymentResult>.Failure($"The minimum online payment is ₦{minimum:N2}.");

        if (await RecipientContacts.ForUserAsync(db, me, ct) is not { Email: { } email } payer)
            return OperationResult<StartOnlinePaymentResult>.Failure(
                "We need a valid email address on your account to send your payment receipt. Please ask the school office to add one.");

        var attempt = new OnlinePayment
        {
            Reference = $"NPS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..25].ToUpperInvariant(),
            InvoiceId = invoice.Id,
            StudentId = invoice.StudentId,
            PayerUserId = me,
            PayerEmail = email,
            Amount = amount,
            Currency = "NGN",
            Provider = gateway.Name,
            OnlinePaymentStatusId = await StatusIdAsync(Pending, ct),
        };

        var init = await gateway.InitializeAsync(new GatewayInitializeRequest(
            attempt.Reference, amount, attempt.Currency, email, returnUri.ToString(),
            new Dictionary<string, string>
            {
                ["invoice_number"] = invoice.InvoiceNumber,
                ["student"] = invoice.Student is null ? "" : $"{invoice.Student.FirstName} {invoice.Student.LastName}",
                ["payer"] = payer.Name,
            }), ct);

        if (!init.Succeeded || init.AuthorizationUrl is null)
        {
            logger.LogWarning("Online payment initialisation failed for invoice {Invoice}: {Error}", invoice.InvoiceNumber, init.Error);
            return OperationResult<StartOnlinePaymentResult>.Failure("We couldn't reach the payment provider. Please try again shortly.");
        }

        attempt.AuthorizationUrl = init.AuthorizationUrl;
        db.OnlinePayments.Add(attempt);
        await db.SaveChangesAsync(ct);

        return OperationResult<StartOnlinePaymentResult>.Success(new StartOnlinePaymentResult
        {
            Reference = attempt.Reference,
            RedirectUrl = init.AuthorizationUrl,
        });
    }

    public async Task<OperationResult<OnlinePaymentDto>> ConfirmAsync(string reference, CancellationToken ct = default)
    {
        reference = reference?.Trim() ?? "";
        if (reference.Length is 0 or > 60)
            return OperationResult<OnlinePaymentDto>.Failure("Unknown payment reference.");

        // Row lock for the whole verify-and-record step: a browser callback
        // and a webhook arriving together are processed one after the other,
        // and the second sees the first's result instead of recording a
        // second receipt.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var attempt = await db.OnlinePayments
            .FromSqlInterpolated($"SELECT * FROM OnlinePayments WITH (UPDLOCK, ROWLOCK) WHERE Reference = {reference}")
            .Include(o => o.OnlinePaymentStatus)
            .FirstOrDefaultAsync(ct);
        if (attempt is null)
            return OperationResult<OnlinePaymentDto>.Failure("Unknown payment reference.");

        // Only open attempts are re-checked. Succeeded is done; Failed is final
        // for this reference; Review waits for the bursar.
        if (attempt.OnlinePaymentStatus!.Code is Pending or Abandoned)
        {
            await VerifyAndRecordAsync(attempt, ct);
            await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);

        return OperationResult<OnlinePaymentDto>.Success((await LoadDtoAsync(attempt.Id, ct))!);
    }

    public async Task<bool> HandleWebhookAsync(string body, string? signature, CancellationToken ct = default)
    {
        if (!gateway.IsValidWebhookSignature(body, signature)) return false;

        // Acknowledge events we don't handle so the gateway stops retrying.
        if (gateway.GetWebhookReference(body) is not { } reference) return true;
        if (!await db.OnlinePayments.AnyAsync(o => o.Reference == reference, ct)) return true;

        var result = await ConfirmAsync(reference, ct);
        logger.LogInformation("Webhook for {Reference}: {Status}", reference, result.Data?.StatusCode ?? string.Join("; ", result.Errors));
        return true;
    }

    public async Task<OnlinePaymentDto?> GetByReferenceAsync(string reference, CancellationToken ct = default)
    {
        if (currentUser.UserId is not { } me) return null;
        var staff = IsFinanceStaff;
        var id = await db.OnlinePayments
            .Where(o => o.Reference == reference && (staff || o.PayerUserId == me))
            .Select(o => (Guid?)o.Id)
            .FirstOrDefaultAsync(ct);
        return id is { } found ? await LoadDtoAsync(found, ct) : null;
    }

    public async Task<IReadOnlyList<OnlinePaymentDto>> ListAsync(OnlinePaymentFilter filter, CancellationToken ct = default)
    {
        if (!IsFinanceStaff) return [];

        var q = db.OnlinePayments.AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.StatusCode))
            q = q.Where(o => o.OnlinePaymentStatus!.Code == filter.StatusCode);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            q = q.Where(o =>
                o.Reference.ToLower().Contains(term)
                || o.PayerEmail.ToLower().Contains(term)
                || o.Invoice!.InvoiceNumber.ToLower().Contains(term)
                || (o.Student!.FirstName + " " + o.Student.LastName).ToLower().Contains(term)
                || (o.PayerUser!.FirstName + " " + o.PayerUser.LastName).ToLower().Contains(term));
        }

        return await Project(q.OrderByDescending(o => o.CreatedOn).Take(500)).ToListAsync(ct);
    }

    private async Task VerifyAndRecordAsync(OnlinePayment attempt, CancellationToken ct)
    {
        var v = await gateway.VerifyAsync(attempt.Reference, ct);
        attempt.LastVerifiedOn = DateTimeOffset.UtcNow;
        attempt.StatusMessage = Clip(v.Message);
        attempt.ProviderTransactionId ??= Clip(v.TransactionId, 100);
        attempt.Channel ??= Clip(v.Channel, 40);

        switch (v.Status)
        {
            case GatewayChargeStatus.Failed:
                attempt.OnlinePaymentStatusId = await StatusIdAsync(Failed, ct);
                return;
            case GatewayChargeStatus.Abandoned:
                attempt.OnlinePaymentStatusId = await StatusIdAsync(Abandoned, ct);
                return;
            case GatewayChargeStatus.Pending or GatewayChargeStatus.NotFound:
                return;
        }

        // Success: the money has moved. From here on nothing may be silently
        // dropped; anything unexpected goes to Review for the bursar.
        attempt.PaidAt = v.PaidAt ?? DateTimeOffset.UtcNow;

        if (!string.Equals(v.Currency, attempt.Currency, StringComparison.OrdinalIgnoreCase) || v.Amount != attempt.Amount)
        {
            attempt.OnlinePaymentStatusId = await StatusIdAsync(Review, ct);
            attempt.StatusMessage = Clip($"Gateway reports {v.Currency} {v.Amount:N2} but {attempt.Currency} {attempt.Amount:N2} was expected. Reconcile manually.");
            logger.LogWarning("Online payment {Reference} amount mismatch: {Message}", attempt.Reference, attempt.StatusMessage);
            return;
        }

        // Apply to the invoice what it still owes. If the balance shrank since
        // checkout (e.g. cash paid at the bursary meanwhile), the remainder is
        // recorded unallocated, as credit on the pupil's account.
        var invoice = await db.Invoices.Include(i => i.InvoiceStatus).FirstOrDefaultAsync(i => i.Id == attempt.InvoiceId, ct);
        var outstanding = invoice is null || invoice.InvoiceStatus?.Code == "CANCELLED"
            ? 0m
            : Math.Max(0m, invoice.AmountDue - invoice.AmountPaid);
        var applied = Math.Min(attempt.Amount, outstanding);

        var zone = QuietHours.ResolveZone(notificationOptions.Value.TimeZone);
        var paidOn = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(attempt.PaidAt.Value, zone).DateTime);
        var methodId = await db.PaymentMethods.Where(m => m.Code == OnlineMethodCode).Select(m => m.Id).FirstOrDefaultAsync(ct);
        var payerName = await db.Users.Where(u => u.Id == attempt.PayerUserId)
            .Select(u => u.FirstName + " " + u.LastName).FirstOrDefaultAsync(ct);

        var recorded = methodId == Guid.Empty
            ? OperationResult<Guid>.Failure("The 'Online Payment' (ONL) payment method is not seeded.")
            : await payments.RecordAsync(new RecordPaymentRequest
            {
                StudentId = attempt.StudentId,
                PaymentMethodId = methodId,
                PaidOn = paidOn,
                Amount = attempt.Amount,
                Reference = attempt.Reference,
                Notes = Clip($"Paid online via {attempt.Provider}{(attempt.Channel is null ? "" : $" ({attempt.Channel})")} by {payerName}. Transaction {attempt.ProviderTransactionId}.", 300),
                Allocations = applied > 0
                    ? [new AllocationLineRequest { InvoiceId = attempt.InvoiceId, AmountApplied = applied }]
                    : [],
            }, ct);

        if (!recorded.Succeeded)
        {
            attempt.OnlinePaymentStatusId = await StatusIdAsync(Review, ct);
            attempt.StatusMessage = Clip("Charge succeeded but the receipt could not be recorded: " + string.Join(" ", recorded.Errors));
            logger.LogError("Online payment {Reference} succeeded but recording failed: {Errors}", attempt.Reference, attempt.StatusMessage);
            return;
        }

        attempt.PaymentId = recorded.Data;
        attempt.OnlinePaymentStatusId = await StatusIdAsync(Succeeded, ct);
        if (applied < attempt.Amount)
        {
            attempt.StatusMessage = Clip($"₦{attempt.Amount - applied:N2} was more than the invoice still owed and is held as credit on the pupil's account.");
        }
    }

    // A parent may pay for a linked ward; a student for themselves.
    private async Task<bool> CanPayForStudentAsync(Guid userId, Guid studentId, CancellationToken ct)
    {
        if (currentUser.IsInRole(Roles.Parent)
            && await db.StudentParents.AnyAsync(l => l.StudentId == studentId && l.Parent!.UserId == userId && l.Parent.IsActive, ct))
            return true;
        return currentUser.IsInRole(Roles.Student)
               && await db.Students.AnyAsync(s => s.Id == studentId && s.UserId == userId, ct);
    }

    private async Task<OnlinePaymentDto?> LoadDtoAsync(Guid id, CancellationToken ct) =>
        await Project(db.OnlinePayments.Where(o => o.Id == id)).FirstOrDefaultAsync(ct);

    private static IQueryable<OnlinePaymentDto> Project(IQueryable<OnlinePayment> q) =>
        q.Select(o => new OnlinePaymentDto
        {
            Id = o.Id,
            Reference = o.Reference,
            InvoiceId = o.InvoiceId,
            InvoiceNumber = o.Invoice!.InvoiceNumber,
            StudentId = o.StudentId,
            StudentName = o.Student!.FirstName + " " + o.Student.LastName,
            PayerUserId = o.PayerUserId,
            PayerName = o.PayerUser!.FirstName + " " + o.PayerUser.LastName,
            PayerEmail = o.PayerEmail,
            Amount = o.Amount,
            Currency = o.Currency,
            Provider = o.Provider,
            StatusCode = o.OnlinePaymentStatus!.Code,
            StatusName = o.OnlinePaymentStatus.Name,
            StatusMessage = o.StatusMessage,
            ProviderTransactionId = o.ProviderTransactionId,
            Channel = o.Channel,
            CreatedOn = o.CreatedOn,
            PaidAt = o.PaidAt,
            LastVerifiedOn = o.LastVerifiedOn,
            PaymentId = o.PaymentId,
            ReceiptNumber = o.Payment == null ? null : o.Payment.ReceiptNumber,
        });

    private Task<Guid> StatusIdAsync(string code, CancellationToken ct) =>
        db.OnlinePaymentStatuses.Where(s => s.Code == code).Select(s => s.Id).FirstAsync(ct);

    private static string? Clip(string? s, int max = 500) => s is null || s.Length <= max ? s : s[..max];
}
