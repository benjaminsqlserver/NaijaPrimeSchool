# Sprint 15 — Online fee payment

## Purpose

Since Sprint 6, fees could only be paid at the bursary: cash, transfer,
POS or cheque, recorded by hand. Sprint 15 lets a parent (for a linked
ward) or a student (for themselves) pay an invoice online through
**Paystack**, Nigeria's most widely used payment provider. The receipt is
recorded automatically against the invoice once Paystack confirms the
payment.

The golden rule throughout: **only Paystack's own server-to-server answer
counts as proof of payment.** Nothing is recorded because a browser was
redirected to a "success" URL.

## Acceptance criteria

1. With online payment enabled, the **Fees & invoices** tab on a parent's
   ward page and the student **My fees** page show **Pay online** on every
   invoice that still has a balance and isn't draft or cancelled.
2. The pay page shows the invoice, pupil, term and outstanding balance.
   The payer can pay all of it or part of it, subject to a minimum of
   `OnlinePayments:MinimumAmount`, ₦100 by default. They then go to
   Paystack's secure checkout by card, bank transfer or USSD. The school
   never sees card details.
3. On return, the app asks Paystack for the payment's status and shows
   one of:
   - **Payment received**, with the receipt number;
   - **Waiting for confirmation**, with a *Check again* button;
   - **Payment not completed**, with a *Try again* button;
   - **Being checked by the bursary**.
4. A confirmed payment is recorded as a normal receipt with the
   **Online Payment** (`ONL`) method, the Paystack reference and
   transaction id, and the payer's name. It is allocated to the invoice,
   and the invoice's balance and status update as they do for any other
   payment.
5. Paystack's **webhook** (`POST /api/payments/paystack/webhook`) confirms
   payments even if the payer closes the browser before returning. Its
   HMAC-SHA512 signature is checked, and a bad signature gets **401**.
6. However many times a payment is confirmed (callback, webhook,
   redelivery, the bursar's *verify now*, or several at the same moment),
   **exactly one** receipt is recorded.
7. If the invoice's balance has fallen since checkout started (for
   example, cash was paid at the bursary in the meantime), the full amount
   is still recorded. Only what the invoice still owes is applied to it,
   and the rest is held as credit on the pupil's account.
8. If Paystack reports a different amount or currency, or the receipt
   can't be recorded, the attempt goes to **Needs review** for the
   bursar, and nothing is silently dropped.
9. **Finance → Online payments** (SuperAdmin, HeadTeacher, SchoolBursar)
   lists every attempt with its status, receipt link and notes. It has
   totals for amount received, awaiting confirmation and needs review, a
   search and status filter, and a *verify now* button for pending or
   abandoned attempts.
10. Online payment is **off by default**. Development uses a built-in
    **simulator** in place of Paystack, and the app refuses to start with
    the simulator in any other environment.

## Data model

| Table | Purpose |
| --- | --- |
| `OnlinePaymentStatuses` | lookup: `PENDING`, `SUCCEEDED`, `FAILED`, `ABANDONED`, `REVIEW` (*Needs review*) |
| `OnlinePayments` | one checkout attempt: `Reference` (unique, e.g. `NPS-20260929-8557A0B506B0`), invoice, pupil, payer user + email, `Amount`, `Currency`, `Provider`, status, Paystack transaction id / channel / `PaidAt`, last verification time and message, and `PaymentId` once a receipt exists |

Migration: `20260929211209_OnlinePayments` — adds tables only. Foreign
keys are `Restrict`, because these are financial records. A filtered
unique index on `PaymentId` guarantees one attempt maps to at most one
receipt.

## Payment flow

```text
Pay online ─► OnlinePaymentService.StartAsync
               checks: payer is the pupil or a linked parent; invoice open;
                       minimum ≤ amount ≤ balance; payer has an email
               creates OnlinePayment (PENDING) with a fresh reference
               Paystack /transaction/initialize (amount in kobo) ─► checkout URL
   ▼
Paystack checkout (card / transfer / USSD)
   ▼                                   ▼
browser returns to                    Paystack webhook (signed)
/portal/payments/return?reference=    /api/payments/paystack/webhook
   ▼                                   ▼
         OnlinePaymentService.ConfirmAsync(reference)
           BEGIN TRAN; SELECT … WITH (UPDLOCK, ROWLOCK)   ◄─ one at a time
           if PENDING / ABANDONED:
             Paystack /transaction/verify/{reference}
               success  → amounts match? → PaymentService.RecordAsync (ONL) → SUCCEEDED
                                        └ no → REVIEW
               failed   → FAILED     abandoned → ABANDONED     pending → unchanged
           COMMIT
```

**Why a row lock?** Paystack's webhook and the payer's browser often
arrive within milliseconds of each other. Each verifies with Paystack and
would record a receipt. `UPDLOCK` on the attempt's row makes the second
caller wait until the first has committed. It then sees `SUCCEEDED` and
does nothing. The harness fires four confirmations at once and checks
that exactly one receipt is recorded.

**Why re-check `ABANDONED`?** Paystack marks a checkout abandoned when
the payer leaves it, but a bank transfer can still land afterwards.
`FAILED` is final for a reference, and `REVIEW` waits for a person.

**Why is an unknown result "Pending", not "Failed"?** If Paystack is
unreachable or returns an error, the attempt stays `PENDING` so a later
verification can settle it. A payer's money is never marked as failed
because of a network blip.

## Configuration

`appsettings.json` (production defaults: switched off):

```json
"OnlinePayments": {
  "Enabled": false,
  "Provider": "Paystack",
  "MinimumAmount": 100,
  "Paystack": {
    "BaseUrl": "https://api.paystack.co",
    "SecretKey": ""
  }
}
```

`appsettings.Development.json` turns on the simulator:

```json
"OnlinePayments": { "Enabled": true, "Provider": "Simulator" }
```

### Going live with Paystack

1. In the Paystack dashboard, under **Settings → API Keys & Webhooks**:
   - copy the **secret key**;
   - set the **webhook URL** to `https://<your-domain>/api/payments/paystack/webhook`.
2. Store the key outside source control:
   ```bash
   dotnet user-secrets set "OnlinePayments:Paystack:SecretKey" "sk_test_..." --project src/NaijaPrimeSchool.Web
   ```
   In production, use the environment variable
   `OnlinePayments__Paystack__SecretKey`.
3. Set `OnlinePayments:Enabled` to `true` and `OnlinePayments:Provider`
   to `Paystack`.
4. Test with the `sk_test_…` key and Paystack's test cards first, then
   switch to `sk_live_…`.

The secret key is used for three things: authenticating API calls,
verifying transactions, and checking webhook signatures. It must never
reach the browser, and the app never sends it there.

## Code map

- `Domain/Finance/OnlinePayment.cs`, `OnlinePaymentStatus.cs`
- `Application/Finance/IOnlinePaymentService.cs` — `StartAsync`,
  `ConfirmAsync`, `HandleWebhookAsync`, `GetByReferenceAsync`,
  `ListAsync`, `GetSettings`.
- `Application/Finance/IPaymentGateway.cs` — provider abstraction:
  initialize, verify, webhook signature, webhook reference.
- `Application/Finance/Dtos/OnlinePaymentDtos.cs`
- `Infrastructure/Services/OnlinePaymentService.cs` — every rule above.
- `Infrastructure/Payments/PaystackGateway.cs` — Paystack REST: amounts in
  kobo, Bearer secret key, fixed-time HMAC-SHA512 webhook check, status
  mapping (`success`, `failed` / `reversed`, `abandoned`, and everything
  else treated as pending).
- `Infrastructure/Payments/SimulatorGateway.cs` — in-memory development
  gateway.
- `Infrastructure/Payments/OnlinePaymentOptions.cs`
- `Infrastructure/DependencyInjection.cs` — `AddOnlinePayments`; an
  unknown provider fails at startup.
- `Web/Program.cs`:
  - the webhook endpoint (anonymous, no antiforgery, and skipping
    status-code pages so it returns a real 401 instead of an HTML page);
  - the startup guard that refuses the simulator outside Development.
- `Web/.../Portals/PayInvoice.razor` (`/portal/pay/{invoiceId}`),
  `PaymentReturn.razor` (`/portal/payments/return`).
- `Web/.../Portals/StudentFees.razor`, `WardDetail.razor` — the
  **Pay online** column, for students and parents only. Staff previewing
  these pages don't see it.
- `Web/.../Finance/OnlinePayments.razor` (`/finance/online-payments`),
  `PaymentSimulator.razor` (`/payments/simulator`, development only).
- `Web/.../Layout/NavMenu.razor` — **Finance → Online payments**.

## How to test end-to-end

In Development the simulator is on by default:

1. Issue an invoice to a pupil who has a linked parent with a portal
   account.
2. Sign in as the parent, then go to **My wards → (pupil) → Fees &
   invoices → Pay online**.
3. Keep the full balance or lower it, then press **Pay ₦… securely**.
4. On the simulator page, choose **Pay (simulate success)**. You come back
   to **Payment received** with a receipt number.
5. As the bursar, open **Finance → Payments**. The receipt is there with
   method *Online Payment*, and the invoice shows as paid (or partially
   paid).
6. Repeat with **Card declined**, which shows *Payment not completed*, and
   with **Cancel and return**, which shows *Abandoned*. Neither records a
   receipt.
7. **Finance → Online payments** lists every attempt. Press *verify now*
   on a pending one to re-check it.

To check the webhook endpoint:

```bash
curl -i -X POST https://localhost:7141/api/payments/paystack/webhook \
     -H 'x-paystack-signature: wrong' -d '{}'
# 401 Unauthorized
```

## Known limits and follow-ups

- **Paystack only.** The `IPaymentGateway` abstraction leaves room for
  Flutterwave or Remita later.
- **Transaction fees** are absorbed by the school (Paystack's default).
  Passing them on to payers would need a surcharge line.
- **No automatic refunds.** Refunds are made in the Paystack dashboard and
  then marked in the app using the existing refund flow.
- **Receipt numbering under load.** Sprint 6's `NPS/RCP/<year>/<next>`
  numbering isn't concurrency-safe. Two *different* payments confirmed at
  the same instant can pick the same number. The unique index rejects one:
  its confirmation rolls back and stays *Pending*, and Paystack's webhook
  retry (or *verify now*) records it moments later, so no money is lost.
  A database sequence for receipt numbers would remove the retry.
- **Needs review is manual.** The bursar reconciles it, for example by
  recording the payment by hand with the Paystack reference.
