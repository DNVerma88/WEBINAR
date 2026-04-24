# Survey Module — Detailed Implementation Plan

**Date**: March 24, 2026  
**Author**: GitHub Copilot  
**Target**: KnowHub Platform — Organizational Survey Feature  
**Scope**: Plan only — no code changes made  
**Research**: Validated against internet sources — OTP auth patterns (ASP.NET Core .NET 10), timing attack prevention (Paragon Initiative), background job best practices (.NET BackgroundService + Hangfire), ASP.NET Core `ITimeLimitedDataProtector` docs, SurveyMonkey/Typeform per-respondent link design, NNG survey question type best practices

---

## Table of Contents

1. [Overview & Goals](#1-overview--goals)
2. [Functional Requirements](#2-functional-requirements)
3. [Non-Functional Requirements](#3-non-functional-requirements)
4. [Domain Model — Entities & Enums](#4-domain-model--entities--enums)
5. [Database Schema — SQL Migration](#5-database-schema--sql-migration)
6. [Application Layer — Contracts, DTOs, Validators](#6-application-layer--contracts-dtos-validators)
7. [Infrastructure Layer — Services & Email](#7-infrastructure-layer--services--email)
8. [API Layer — Controllers & Authorization](#8-api-layer--controllers--authorization)
9. [Frontend](#9-frontend)
10. [Security Design](#10-security-design)
11. [SOLID Principles Alignment](#11-solid-principles-alignment)
12. [Test Plan](#12-test-plan)
13. [Implementation Sequence](#13-implementation-sequence)
14. [Research Sources & Findings Applied](#14-research-sources--findings-applied)
15. [Survey Analytics & Reporting Module](#15-survey-analytics--reporting-module)

---

## 1. Overview & Goals

The Survey Module enables Admin and Super Admin users to create organizational surveys with configurable questions, launch them to all employees via secure one-time tokenized email links, and collect structured feedback. Admins can monitor response rates and re-invite users whose tokens have expired.

**Key user journeys:**

| Actor | Action |
|---|---|
| Admin / SuperAdmin | Configure survey title, description, and questions |
| Admin / SuperAdmin | Launch survey → all active employees receive a tokenized email link |
| Employee | Opens email link → fills out survey form (no login required) → submits |
| Admin / SuperAdmin | Monitor response rate, view results |
| Admin / SuperAdmin | Resend expired/unsubmitted invitations (single user, selected users, or all pending) |

---

## 2. Functional Requirements

### 2.1 Survey Configuration (Admin / SuperAdmin only)

- FR-001: Create a survey with `Title`, `Description`, `WelcomeMessage`, `ThankYouMessage`, and `TokenExpiryDays`.
- FR-002: Add, update, reorder, and delete questions on a **Draft** survey.
- FR-003: Supported question types: `Text`, `SingleChoice`, `MultipleChoice`, `Rating` (1–5 or 1–10, configurable scale), `YesNo`.
  > **Research note (NNG, SurveyMonkey)**: Likert scale (Strongly Disagree → Strongly Agree) is the most commonly used organizational survey format. It is NOT a separate type — it is implemented using `SingleChoice` with ordered options: `["Strongly Disagree", "Disagree", "Neutral", "Agree", "Strongly Agree"]`. NPS (Net Promoter Score 0–10) is covered by `Rating` with MinRating = 0 and MaxRating = 10. The Admin UI should provide a **quick-insert template** for Likert and NPS patterns.
- FR-004: Each question has `IsRequired` flag and `OrderSequence`.
- FR-005: `SingleChoice` and `MultipleChoice` questions carry a list of option strings (stored as JSONB).
- FR-006: Questions can only be modified while the survey is in **Draft** status. Once launched, the survey is read-only.
- FR-006b: A `SurveyQuestion` can only be **permanently deleted** if it has zero `SurveyAnswers` referencing it — meaning it has never been answered by any employee. Two conditions must both be satisfied to permit deletion:
  1. The parent survey must be in `Draft` status (FR-006).
  2. `_db.SurveyAnswers.Any(a => a.QuestionId == questionId)` must be `false`.

  If condition 1 fails → `BusinessRuleException("Questions cannot be deleted after the survey has been launched.")` (HTTP 422)
  If condition 2 fails → `ConflictException("This question has recorded answers and cannot be deleted. Use \"Copy Survey\" to create a new survey and exclude this question.")` (HTTP 409)

  > **Architecture note**: In practice, condition 2 can only fire as a defense-in-depth guard. A `Draft` survey cannot logically have answers because employees only receive tokens after launch. However, the service-level check is mandatory so the API returns a meaningful `409 Conflict` response rather than letting the PostgreSQL `ON DELETE RESTRICT` FK surface a raw `DbUpdateException` to the client. The SQL constraint on `SurveyAnswers.QuestionId` remains as the last-resort database safety net.
- FR-007: Delete survey is only permitted in **Draft** status.
- FR-007b: An optional `IsAnonymous` flag on a survey controls whether results API exposes individual respondent identity. When `IsAnonymous = true`, the `GetResultsAsync` and `GetResponsesAsync` endpoints mask `UserId`/`UserFullName` in their output. The `SurveyResponse.UserId` is still stored server-side for de-duplication; it is never exposed via API when anonymity is on.
  > **Research note**: Industry-standard enterprise survey platforms (SurveyMonkey Teams, CultureAmp) offer anonymity toggle at survey level. WHO/NNG recommend anonymity for sensitive organizational feedback to improve response honesty.
- FR-007c: Admin/SuperAdmin can **copy an existing survey** (any status) into a new `Draft` survey. The copy request includes an optional `ExcludeQuestionIds` list — any question IDs in this list are omitted from the copy. All other questions are cloned into the new Draft with reset `OrderSequence` values. The new survey title is prefixed with `"Copy of "` by default and can be edited before launch. This is the correct mechanism for reusing questions across survey cycles while excluding unwanted ones.

  > **Why not `IsDeleted` / `IsActive` on `SurveyQuestions`?**
  > Adding soft-delete columns to `SurveyQuestions` would not solve the reuse problem — questions are scoped to a single `SurveyId` and are never shared across surveys. Hiding a question on an **Active** survey is also intentionally blocked: some employees have already answered it; hiding mid-survey produces inconsistent results. The correct lifecycle is: **Draft** edits (add/delete freely while the question has no answers per FR-006b) → **Active** immutability → **Copy** for the next cycle with question exclusion.

### 2.2 Survey Launch

- FR-008: Launching a survey transitions its status from `Draft` → `Active`.
- FR-009: On launch, a `SurveyInvitation` record is created for **every active employee** in the tenant (Role includes `Employee` flag, `IsActive = true`).
- FR-010: Each invitation generates a cryptographically secure one-time token; the token plain-text is sent in the email link, only its SHA-256 hash is stored in the database.
- FR-011: Token expiry date (`ExpiresAt`) is set to `NOW() + TokenExpiryDays`.
- FR-012: A survey invite email is sent to each employee's registered email address with an inline CTA button linking to the survey form page.
- FR-013: Admin/SuperAdmin users who are also employees **do** receive the survey link (they are respondents too).
- FR-013b: The survey form link URL format is `{FrontendBaseUrl}/survey/{plainToken}` where the opaque token is in the **path segment** (not a query string parameter). This matches industry practice — Typeform uses path-segment form IDs and SurveyMonkey uses path-encoded identifiers. Path-segment tokens are less likely to appear in server-side request logs than query string params.
  > **Research note (Typeform)**: Typeform places per-respondent codes as URL parameters, which is simpler but exposes them in NGINX/CDN access logs. Keeping the token in the path is equivalent and avoids log-leakage if log scrubbing ever fails.

### 2.3 Survey Submission (Employee — token-based access)

- FR-014: Employee clicks the link `{FrontendBase}/survey/{plainToken}`. No JWT authentication is required to access the survey form.
- FR-015: The backend validates the token on GET (returns questions) and POST (accepts submission). Validation rules:
  - Token hash must exist in `SurveyInvitations`.
  - Invitation status must be `Sent` (not already `Submitted`, `Expired`, or `Failed`).
  - `ExpiresAt` must be in the future.
  - Parent survey status must be `Active`.
- FR-016: On successful submission, all answers are saved and the invitation status is updated to `Submitted`.
- FR-017: A duplicate-submission guard is enforced at the DB level via a `UNIQUE (TenantId, SurveyId, UserId)` constraint on `SurveyResponses`. The service also checks this before insert.
- FR-018: Required questions must have an answer; the service validates completeness before persisting.
- FR-018b: *(Future enhancement — not in v1)* Partial save / resume-later: some enterprise platforms (SurveyMonkey Advance) allow saving a partial response and resuming via the same token. V1 does NOT support this — submission is atomic. The `SurveyInvitationStatus` lifecycle and token invalidation model is designed so that partial-save could be added later by introducing a `PartiallyFilled` status and a `SurveyDraftResponse` table without breaking existing schemas.

### 2.4 Resend Invitations

- FR-019: Admin/SuperAdmin can resend the survey link to a **single employee** by `userId`.
- FR-020: Admin/SuperAdmin can resend to a **list of employees** (`userId[]`) in one API call.
- FR-021: Admin/SuperAdmin can resend to **all employees whose invitation is `Sent` but `ExpiresAt` has passed** (i.e., expired, not yet submitted).
- FR-022: Resend generates a **new token** (previous token is invalidated by status change to `Expired`) and sets a fresh `ExpiresAt`, increments `ResendCount`.
- FR-023: Resend is blocked if the invitation status is already `Submitted`.

### 2.5 Survey Closure & Results

- FR-024: Admin/SuperAdmin can manually close an `Active` survey (`Active` → `Closed`). Closing invalidates all remaining `Sent` invitations.
- FR-025: Results view provides: per-question aggregated statistics (response counts per option, average rating, text answers list).
- FR-026: Individual responses list shows which users responded (non-anonymous mode).

---

## 3. Non-Functional Requirements

- NFR-001: All survey management endpoints are protected by `[Authorize(Policy = "AdminOrAbove")]`.
- NFR-002: The public survey form endpoint does **not** require a JWT — the one-time token is the credential.
- NFR-003: Token generation uses `RandomNumberGenerator.GetBytes(32)` (32 bytes = 256 bits of entropy from a CSRNG), encoded as Base64Url. This provides $2^{256}$ possible values, making brute-force enumeration computationally infeasible even at 1 billion guesses/second (would take $\approx 3.67 \times 10^{67}$ years).
- NFR-003b: **Alternative technology considered — `ITimeLimitedDataProtector`**: ASP.NET Core provides `ITimeLimitedDataProtector` (from `Microsoft.AspNetCore.DataProtection.Extensions`) which encrypts a payload with a built-in expiry. When the token is past its lifetime, `Unprotect()` throws `CryptographicException` automatically. **Reason NOT chosen**: The Data Protection key ring is process-local/machine-scoped by default — rotating keys or redeploying the app would silently invalidate all outstanding survey tokens with no ability to recover them. For survey links with multi-day lifetimes, this is unacceptable. The SHA-256-hash-plus-DB-record approach survives restarts and key rotations.
- NFR-004: Only the SHA-256 hash of the token is persisted; the plaintext never touches the database.
  > **Research validation**: This exact pattern is used in ASP.NET Core Identity for password reset tokens and email confirmation tokens. The Elixir Phoenix `phx.gen.auth` generator uses the same approach (store hash, never store plaintext). The pattern is confirmed by industry-standard OTP authentication guides for .NET 10.
- NFR-005: Token lookup is by hash only — no `userId` is accepted from the URL or query string for submission.
- NFR-005b: **Timing attack consideration**: Token lookup is performed as a database index scan by `TokenHash`. Because the comparison happens in PostgreSQL (not in C# string comparison), there is no practical timing side-channel exploitable at the application layer. The threat model for timing attacks applies to in-memory string comparison (`==`); a DB round-trip dwarfs any hash-length timing delta. No Double-HMAC or `CryptographicOperations.FixedTimeEquals()` is needed for the lookup itself.
  > **Research source**: Paragon Initiative Enterprises — "Preventing Timing Attacks on String Comparison". Confirms the threat is in MAC validation via in-memory string ==, not in DB-indexed lookups where latency dominates.
- NFR-006: A custom rate-limit policy (10 req/min per IP) is applied on the public survey form endpoints to mitigate brute-force token enumeration.
- NFR-007: Survey questions cannot be modified once a survey is `Active` or `Closed` (immutability of launched survey structure).
- NFR-008: Pagination (`pageSize` capped at 100) is enforced on all list endpoints.
- NFR-009: Optimistic concurrency via `RecordVersion` for survey and question updates.
- NFR-010: Email sending is fire-and-forget via the existing `IEmailService` abstraction (SMTP or AWS SES, no new dependency).
- NFR-011: Large-scale launches (hundreds of employees) are processed by a dedicated background `IHostedService` to avoid blocking the HTTP request. The `POST /api/surveys/{id}/launch` endpoint queues the job and returns `202 Accepted` immediately.
- NFR-011b: **Background service DI lifetime rule (CRITICAL — from research)**: `BackgroundService` instances are **singletons** registered via `AddHostedService`. `DbContext` is **scoped**. Injecting `KnowHubDbContext` directly into a background service causes: (a) stale EF Core change tracker data across iterations; (b) thread-safety violations if async resumes on different threads; (c) memory leaks from a permanently pinned change tracker. **Resolution**: Both `SurveyLaunchJob` and `SurveyTokenExpiryJob` inject `IServiceScopeFactory` and call `_scopeFactory.CreateScope()` at the start of each iteration, resolving a fresh `KnowHubDbContext` per scope and disposing it via `using`. This pattern is the correct ASP.NET Core approach.
  > **Research source**: Medium / Suraj Pandey — "Background Jobs in ASP.NET Core Web API", January 2026 — includes working code examples. Microsoft docs hosted-services — confirms IServiceScopeFactory pattern.
- NFR-011c: **`OperationCanceledException` is not an error** in background services. When the ASP.NET Core host signals shutdown, `Task.Delay(stoppingToken)` throws `OperationCanceledException`. Both background jobs must catch this separately and log as `Information` (not `Error`) to avoid false-positive alerts in production monitoring.
- NFR-011d: The survey form URL embedded in emails is built from `FrontendBaseUrl` in `appsettings.json` — it is **never** derived from the incoming HTTP `Host` header. This prevents Host Header Injection (OWASP A05) where an attacker could manipulate the token link sent to all employees by spoofing the Host header on the launch request.

---

## 4. Domain Model — Entities & Enums

### 4.1 New Enums

**`SurveyStatus`** (`backend/src/KnowHub.Domain/Enums/SurveyStatus.cs`)
```
Draft   = 0   // Survey is being configured; not visible to employees
Active  = 1   // Survey is live; invitations have been sent
Closed  = 2   // Survey is no longer accepting responses
```

**`SurveyQuestionType`** (`backend/src/KnowHub.Domain/Enums/SurveyQuestionType.cs`)
```
Text            = 0   // Open-ended textarea
SingleChoice    = 1   // Radio buttons — exactly one option selected
                      // ↳ Covers Likert scale: options = ["Strongly Disagree",...,"Strongly Agree"]
MultipleChoice  = 2   // Checkboxes — one or more options selected
Rating          = 3   // Numeric slider/stars; scale defined by MinRating/MaxRating
                      // ↳ Covers NPS (0–10): set MinRating=0, MaxRating=10
YesNo           = 4   // Boolean — rendered as two radio buttons "Yes" / "No"
```

> **Research note (NNG / SurveyMonkey)**: Likert scale and NPS are among the top 5 question formats used in organizational surveys. Both are deliberately handled by existing types to keep the enum lean and avoid duplicate validation logic. The frontend Admin UI provides **quick-insert templates** for common patterns: "5-point Likert", "10-point NPS", "Department choice", "Custom rating scale". These templates pre-fill the question form — they don't add complexity to the backend.

**`SurveyInvitationStatus`** (`backend/src/KnowHub.Domain/Enums/SurveyInvitationStatus.cs`)
```
Pending     = 0   // Invitation record created, email not yet sent (queued in background job)
Sent        = 1   // Email successfully delivered; awaiting response
Submitted   = 2   // Employee has submitted their response; token is permanently invalidated
Expired     = 3   // Token expiry date has passed without a submission
                  // (set by SurveyTokenExpiryJob or by CloseAsync)
Failed      = 4   // Email delivery failed (SMTP / SES error); admin can resend
```

> **Research note (ASP.NET Core OTP article, murmusoftwareinfotech.com)**: The `IsUsed = true` pattern in OTP systems maps exactly to `Status = Submitted` here. One key addition from research: the `Expired` status must also be set by the token-expiry background job running on a schedule (not only on access). This prevents a scenario where a user tries to submit an expired token, finds it still shows `Sent`, and gets a confusing error — the background job proactively marks them `Expired` so the error message is accurate.

---

### 4.2 New Entities

All entities inherit `BaseEntity` (provides `Id`, `TenantId`, `CreatedDate`, `CreatedBy`, `ModifiedOn`, `ModifiedBy`, `RecordVersion`).

---

#### `Survey` (`backend/src/KnowHub.Domain/Entities/Survey.cs`)

```csharp
public class Survey : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? ThankYouMessage { get; set; }
    public SurveyStatus Status { get; set; } = SurveyStatus.Draft;
    public int TokenExpiryDays { get; set; } = 7;          // how long a generated token stays valid
    public DateTime? LaunchedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public int TotalInvited { get; set; } = 0;             // denormalized counter updated on launch/resend
    public int TotalResponded { get; set; } = 0;           // denormalized counter incremented on submission

    // Navigation
    public ICollection<SurveyQuestion>    Questions    { get; set; } = new List<SurveyQuestion>();
    public ICollection<SurveyInvitation>  Invitations  { get; set; } = new List<SurveyInvitation>();
    public ICollection<SurveyResponse>    Responses    { get; set; } = new List<SurveyResponse>();
}
```

---

#### `SurveyQuestion` (`backend/src/KnowHub.Domain/Entities/SurveyQuestion.cs`)

```csharp
public class SurveyQuestion : BaseEntity
{
    public Guid SurveyId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public SurveyQuestionType QuestionType { get; set; }
    /// <summary>
    /// For SingleChoice and MultipleChoice: JSON array of option strings.
    /// Null for Text, Rating, YesNo.
    /// Stored as JSONB. Sanitised to plain text on write (strip HTML).
    /// </summary>
    public string? OptionsJson { get; set; }               // JSONB ["Option A","Option B",...]
    public int MinRating { get; set; } = 1;                // For Rating type
    public int MaxRating { get; set; } = 5;                // For Rating type
    public bool IsRequired { get; set; } = true;
    public int OrderSequence { get; set; }

    // Navigation
    public Survey Survey { get; set; } = null!;
    public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
}
```

---

#### `SurveyInvitation` (`backend/src/KnowHub.Domain/Entities/SurveyInvitation.cs`)

```csharp
public class SurveyInvitation : BaseEntity
{
    public Guid SurveyId { get; set; }
    public Guid UserId { get; set; }
    /// <summary>
    /// SHA-256 hex digest of the one-time token (lowercase hex, 64 chars).
    /// The plaintext token is NEVER stored anywhere — it is generated, emailed, and discarded.
    /// Pattern validated by: ASP.NET Core Identity (password reset), Phoenix phx.gen.auth,
    /// OTP authentication systems (.NET 10 guide, murmusoftwareinfotech.com).
    /// Token has 256 bits of entropy from RandomNumberGenerator.GetBytes(32) → Base64Url.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;  // char(64) — SHA-256 lowercase hex
    public SurveyInvitationStatus Status { get; set; } = SurveyInvitationStatus.Pending;
    public DateTime? SentAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int ResendCount { get; set; } = 0;

    // Navigation
    public Survey Survey { get; set; } = null!;
    public User User { get; set; } = null!;
    public SurveyResponse? Response { get; set; }
}
```

> **Security note**: `UNIQUE (TokenHash)` index is global, not scoped to tenant — token hashes must be globally unique to prevent cross-tenant token collision attacks. With 256-bit entropy, the probability of any two tokens colliding is negligible ($\approx 2^{-256}$), but the DB constraint provides defense-in-depth.
>
> **Token generation flow** (validated against .NET OTP article):
> 1. `RandomNumberGenerator.GetBytes(32)` → cryptographically secure 32 bytes
> 2. `Base64UrlTextEncoder.Encode(bytes)` → URL-safe 43-char string (no `+`, `/`, `=`)
> 3. `SHA256.HashData(Encoding.UTF8.GetBytes(plainToken))` → 32-byte hash
> 4. `Convert.ToHexString(hash).ToLowerInvariant()` → 64-char lowercase hex stored in DB
> 5. `plainToken` is included in the email body/link and then discarded from memory
> 6. DB record created BEFORE email is sent (ensures token is persisted even if email fails)

---

#### `SurveyResponse` (`backend/src/KnowHub.Domain/Entities/SurveyResponse.cs`)

```csharp
public class SurveyResponse : BaseEntity
{
    public Guid SurveyId { get; set; }
    public Guid UserId { get; set; }
    public Guid InvitationId { get; set; }
    public DateTime SubmittedAt { get; set; }

    // Navigation
    public Survey Survey { get; set; } = null!;
    public User User { get; set; } = null!;
    public SurveyInvitation Invitation { get; set; } = null!;
    public ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
}
```

---

#### `SurveyAnswer` (`backend/src/KnowHub.Domain/Entities/SurveyAnswer.cs`)

```csharp
public class SurveyAnswer : BaseEntity
{
    public Guid ResponseId { get; set; }
    public Guid QuestionId { get; set; }
    /// <summary>Text answer for Text questions. Single selected label for YesNo/SingleChoice stored as text.</summary>
    public string? AnswerText { get; set; }
    /// <summary>JSONB array of selected option labels for MultipleChoice. Null for other types.</summary>
    public string? AnswerOptionsJson { get; set; }
    /// <summary>Numeric value for Rating questions. Null for other types.</summary>
    public int? RatingValue { get; set; }

    // Navigation
    public SurveyResponse Response { get; set; } = null!;
    public SurveyQuestion Question { get; set; } = null!;
}
```

---

### 4.3 Entity Relationships Summary

```
Survey           (1)→(many)  SurveyQuestion     via SurveyId
Survey           (1)→(many)  SurveyInvitation   via SurveyId
Survey           (1)→(many)  SurveyResponse     via SurveyId
SurveyInvitation (1)→(0..1)  SurveyResponse     via InvitationId
SurveyResponse   (1)→(many)  SurveyAnswer       via ResponseId
SurveyAnswer     (many)→(1)  SurveyQuestion     via QuestionId
```

---

## 5. Database Schema — SQL Migration

**File**: `database/sql/011_SurveyModule.sql`

```sql
-- ============================================================
-- Migration 011: Survey Module
-- ============================================================

-- ─── Enum Types (idempotent) ────────────────────────────────

DO $$ BEGIN
    CREATE TYPE "SurveyStatus" AS ENUM ('Draft','Active','Closed');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE "SurveyQuestionType" AS ENUM (
        'Text','SingleChoice','MultipleChoice','Rating','YesNo'
    );
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE "SurveyInvitationStatus" AS ENUM (
        'Pending','Sent','Submitted','Expired','Failed'
    );
EXCEPTION WHEN duplicate_object THEN null; END $$;

-- ─── Surveys ────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS "Surveys" (
    "Id"                UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"          UUID        NOT NULL,
    "Title"             VARCHAR(300) NOT NULL,
    "Description"       TEXT,
    "WelcomeMessage"    TEXT,
    "ThankYouMessage"   TEXT,
    "Status"            "SurveyStatus" NOT NULL DEFAULT 'Draft',
    "TokenExpiryDays"   INT         NOT NULL DEFAULT 7,
    "IsAnonymous"       BOOLEAN     NOT NULL DEFAULT FALSE, -- hides respondent identity in results API
    "LaunchedAt"        TIMESTAMPTZ,
    "ClosedAt"          TIMESTAMPTZ,
    "TotalInvited"      INT         NOT NULL DEFAULT 0,
    "TotalResponded"    INT         NOT NULL DEFAULT 0,
    "CreatedDate"       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID        NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID        NOT NULL,
    "RecordVersion"     INT         NOT NULL DEFAULT 1
);

CREATE INDEX IF NOT EXISTS "IX_Surveys_TenantId"
    ON "Surveys"("TenantId");

CREATE INDEX IF NOT EXISTS "IX_Surveys_TenantId_Status"
    ON "Surveys"("TenantId", "Status");

-- ─── SurveyQuestions ────────────────────────────────────────

CREATE TABLE IF NOT EXISTS "SurveyQuestions" (
    "Id"              UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"        UUID        NOT NULL,
    "SurveyId"        UUID        NOT NULL REFERENCES "Surveys"("Id") ON DELETE CASCADE,
    "QuestionText"    TEXT        NOT NULL,
    "QuestionType"    "SurveyQuestionType" NOT NULL,
    "OptionsJson"     JSONB,                           -- null for Text/Rating/YesNo
    "MinRating"       INT         NOT NULL DEFAULT 1,  -- for Rating type (0 allowed for NPS 0–10)
    "MaxRating"       INT         NOT NULL DEFAULT 5,  -- for Rating type
    "IsRequired"      BOOLEAN     NOT NULL DEFAULT TRUE,
    "OrderSequence"   INT         NOT NULL DEFAULT 0,
    "CreatedDate"     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"       UUID        NOT NULL,
    "ModifiedOn"      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"      UUID        NOT NULL,
    "RecordVersion"   INT         NOT NULL DEFAULT 1,
    CONSTRAINT "CHK_SurveyQuestions_RatingRange"
        CHECK ("MinRating" >= 0 AND "MaxRating" <= 10 AND "MinRating" < "MaxRating")
        -- MinRating >= 0 supports NPS (0–10 scale); original plan had >= 1 (corrected from research)
);

CREATE INDEX IF NOT EXISTS "IX_SurveyQuestions_TenantId"
    ON "SurveyQuestions"("TenantId");

CREATE INDEX IF NOT EXISTS "IX_SurveyQuestions_SurveyId_Order"
    ON "SurveyQuestions"("SurveyId", "OrderSequence");

-- ─── SurveyInvitations ──────────────────────────────────────

CREATE TABLE IF NOT EXISTS "SurveyInvitations" (
    "Id"            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"      UUID        NOT NULL,
    "SurveyId"      UUID        NOT NULL REFERENCES "Surveys"("Id") ON DELETE CASCADE,
    "UserId"        UUID        NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "TokenHash"     CHAR(64)    NOT NULL,               -- SHA-256 hex; globally unique
    "Status"        "SurveyInvitationStatus" NOT NULL DEFAULT 'Pending',
    "SentAt"        TIMESTAMPTZ,
    "ExpiresAt"     TIMESTAMPTZ,
    "SubmittedAt"   TIMESTAMPTZ,
    "ResendCount"   INT         NOT NULL DEFAULT 0,
    "CreatedDate"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID        NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID        NOT NULL,
    "RecordVersion" INT         NOT NULL DEFAULT 1,
    CONSTRAINT "UQ_SurveyInvitations_SurveyId_UserId"
        UNIQUE ("TenantId", "SurveyId", "UserId"),
    CONSTRAINT "UQ_SurveyInvitations_TokenHash"
        UNIQUE ("TokenHash")                            -- global — no cross-tenant collision
);

CREATE INDEX IF NOT EXISTS "IX_SurveyInvitations_TenantId"
    ON "SurveyInvitations"("TenantId");

CREATE INDEX IF NOT EXISTS "IX_SurveyInvitations_SurveyId_Status"
    ON "SurveyInvitations"("SurveyId", "Status");

CREATE INDEX IF NOT EXISTS "IX_SurveyInvitations_TokenHash"
    ON "SurveyInvitations"("TokenHash");               -- separate index for fast token lookups

CREATE INDEX IF NOT EXISTS "IX_SurveyInvitations_UserId"
    ON "SurveyInvitations"("UserId");

-- ─── SurveyResponses ────────────────────────────────────────

CREATE TABLE IF NOT EXISTS "SurveyResponses" (
    "Id"            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"      UUID        NOT NULL,
    "SurveyId"      UUID        NOT NULL REFERENCES "Surveys"("Id") ON DELETE RESTRICT,
    "UserId"        UUID        NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "InvitationId"  UUID        NOT NULL REFERENCES "SurveyInvitations"("Id") ON DELETE RESTRICT,
    "SubmittedAt"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedDate"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID        NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID        NOT NULL,
    "RecordVersion" INT         NOT NULL DEFAULT 1,
    CONSTRAINT "UQ_SurveyResponses_SurveyId_UserId"
        UNIQUE ("TenantId", "SurveyId", "UserId")       -- one response per user per survey
);

CREATE INDEX IF NOT EXISTS "IX_SurveyResponses_TenantId"
    ON "SurveyResponses"("TenantId");

CREATE INDEX IF NOT EXISTS "IX_SurveyResponses_SurveyId"
    ON "SurveyResponses"("SurveyId");

-- ─── SurveyAnswers ──────────────────────────────────────────

CREATE TABLE IF NOT EXISTS "SurveyAnswers" (
    "Id"                UUID    PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"          UUID    NOT NULL,
    "ResponseId"        UUID    NOT NULL REFERENCES "SurveyResponses"("Id") ON DELETE CASCADE,
    "QuestionId"        UUID    NOT NULL REFERENCES "SurveyQuestions"("Id") ON DELETE RESTRICT,
    "AnswerText"        TEXT,                           -- Text, SingleChoice label, YesNo label
    "AnswerOptionsJson" JSONB,                          -- MultipleChoice: ["Option A","Option C"]
    "RatingValue"       INT,                            -- Rating: 1..MaxRating
    "CreatedDate"       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "CreatedBy"         UUID    NOT NULL,
    "ModifiedOn"        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ModifiedBy"        UUID    NOT NULL,
    "RecordVersion"     INT     NOT NULL DEFAULT 1,
    CONSTRAINT "UQ_SurveyAnswers_ResponseId_QuestionId"
        UNIQUE ("ResponseId", "QuestionId")             -- one answer per question per response
);

CREATE INDEX IF NOT EXISTS "IX_SurveyAnswers_TenantId"
    ON "SurveyAnswers"("TenantId");

CREATE INDEX IF NOT EXISTS "IX_SurveyAnswers_ResponseId"
    ON "SurveyAnswers"("ResponseId");

CREATE INDEX IF NOT EXISTS "IX_SurveyAnswers_QuestionId"
    ON "SurveyAnswers"("QuestionId");
```

---

## 6. Application Layer — Contracts, DTOs, Validators

### 6.1 Folder Structure

```
backend/src/KnowHub.Application/
  Contracts/
    Surveys/
      ISurveyService.cs
      ISurveyInvitationService.cs
      ISurveyResponseService.cs
  Models/
    Surveys/
      SurveyDto.cs
      SurveyQuestionDto.cs
      SurveyInvitationDto.cs
      SurveyResponseDto.cs
      SurveyResultsDto.cs                  ← aggregated results per question
      CreateSurveyRequest.cs
      UpdateSurveyRequest.cs
      AddSurveyQuestionRequest.cs
      UpdateSurveyQuestionRequest.cs
      ReorderQuestionsRequest.cs
      SubmitSurveyRequest.cs               ← public, token-based
      GetSurveyFormRequest.cs              ← public, token-based
      ResendInvitationsRequest.cs
      GetSurveysRequest.cs                 ← pagination + filters
      GetSurveyResponsesRequest.cs
  Validators/
    Surveys/
      CreateSurveyRequestValidator.cs
      UpdateSurveyRequestValidator.cs
      AddSurveyQuestionRequestValidator.cs
      UpdateSurveyQuestionRequestValidator.cs
      SubmitSurveyRequestValidator.cs
```

---

### 6.2 Interface Contracts

#### `ISurveyService`
```csharp
public interface ISurveyService
{
    // Admin / SuperAdmin — survey lifecycle
    Task<PagedResult<SurveyDto>> GetSurveysAsync(GetSurveysRequest request, CancellationToken ct);
    Task<SurveyDto> GetByIdAsync(Guid surveyId, CancellationToken ct);
    Task<SurveyDto> CreateAsync(CreateSurveyRequest request, CancellationToken ct);
    Task<SurveyDto> UpdateAsync(Guid surveyId, UpdateSurveyRequest request, CancellationToken ct);
    Task DeleteAsync(Guid surveyId, CancellationToken ct);
    /// <summary>
    /// Copies an existing survey (any status) into a new Draft. Questions listed in
    /// ExcludeQuestionIds are omitted. Title defaults to "Copy of {OriginalTitle}".
    /// </summary>
    Task<SurveyDto> CopyAsync(Guid surveyId, CopySurveyRequest request, CancellationToken ct);
    Task<SurveyDto> LaunchAsync(Guid surveyId, CancellationToken ct);   // returns 202 - queues job
    Task<SurveyDto> CloseAsync(Guid surveyId, CancellationToken ct);
    Task<SurveyResultsDto> GetResultsAsync(Guid surveyId, CancellationToken ct);

    // Question management
    Task<SurveyQuestionDto>  AddQuestionAsync(Guid surveyId, AddSurveyQuestionRequest request, CancellationToken ct);
    Task<SurveyQuestionDto>  UpdateQuestionAsync(Guid surveyId, Guid questionId, UpdateSurveyQuestionRequest request, CancellationToken ct);
    Task                     DeleteQuestionAsync(Guid surveyId, Guid questionId, CancellationToken ct);
    Task                     ReorderQuestionsAsync(Guid surveyId, ReorderQuestionsRequest request, CancellationToken ct);
}
```

#### `ISurveyInvitationService`
```csharp
public interface ISurveyInvitationService
{
    // Admin / SuperAdmin — invitation management
    Task<PagedResult<SurveyInvitationDto>> GetInvitationsAsync(Guid surveyId, GetInvitationsRequest request, CancellationToken ct);
    Task ResendToUserAsync(Guid surveyId, Guid userId, CancellationToken ct);
    Task ResendBulkAsync(Guid surveyId, ResendInvitationsRequest request, CancellationToken ct);
    Task ResendAllPendingAsync(Guid surveyId, CancellationToken ct);

    // Internal — called by background service / launch job
    Task CreateInvitationsAsync(Guid surveyId, IReadOnlyList<Guid> userIds, CancellationToken ct);
    Task SendInvitationEmailAsync(Guid invitationId, CancellationToken ct);
    Task MarkExpiredAsync(CancellationToken ct);     // called by background job
}
```

#### `ISurveyResponseService`
```csharp
public interface ISurveyResponseService
{
    // Public — token-based, no JWT
    Task<SurveyFormDto>     GetFormByTokenAsync(string plainToken, CancellationToken ct);
    Task<SurveyResponseDto> SubmitAsync(string plainToken, SubmitSurveyRequest request, CancellationToken ct);

    // Admin / SuperAdmin — results
    Task<PagedResult<SurveyResponseDto>> GetResponsesAsync(Guid surveyId, GetSurveyResponsesRequest request, CancellationToken ct);
}
```

---

### 6.3 Key DTOs

#### `SurveyDto`
```csharp
public record SurveyDto(
    Guid Id,
    Guid TenantId,
    string Title,
    string? Description,
    string? WelcomeMessage,
    string? ThankYouMessage,
    string Status,                     // "Draft" | "Active" | "Closed"
    int TokenExpiryDays,
    bool IsAnonymous,
    DateTime? LaunchedAt,
    DateTime? ClosedAt,
    int TotalInvited,
    int TotalResponded,
    int ResponseRate,                  // TotalResponded / TotalInvited * 100 (0 if TotalInvited == 0)
    List<SurveyQuestionDto> Questions,
    DateTime CreatedDate,
    Guid CreatedBy
);
```

#### `SurveyQuestionDto`
```csharp
public record SurveyQuestionDto(
    Guid Id,
    string QuestionText,
    string QuestionType,               // "Text" | "SingleChoice" | "MultipleChoice" | "Rating" | "YesNo"
    List<string>? Options,             // deserialized from OptionsJson for choice questions
    int MinRating,
    int MaxRating,
    bool IsRequired,
    int OrderSequence
);
```

#### `SurveyInvitationDto`
```csharp
public record SurveyInvitationDto(
    Guid Id,
    Guid UserId,
    string UserFullName,
    string UserEmail,
    string Status,                     // "Pending" | "Sent" | "Submitted" | "Expired" | "Failed"
    DateTime? SentAt,
    DateTime? ExpiresAt,
    DateTime? SubmittedAt,
    int ResendCount
);
```

#### `SurveyResultsDto`
```csharp
public record SurveyResultsDto(
    Guid SurveyId,
    string Title,
    bool IsAnonymous,
    int TotalInvited,
    int TotalResponded,
    int ResponseRatePercent,
    List<QuestionResultDto> QuestionResults
    // Note: No individual user list here — use GetResponsesAsync for that (blocked when IsAnonymous)
);

public record QuestionResultDto(
    Guid QuestionId,
    string QuestionText,
    string QuestionType,
    int TotalAnswers,
    // For choice questions:
    List<OptionCountDto>? OptionCounts,
    // For rating questions:
    double? AverageRating,
    int? MinRatingGiven,
    int? MaxRatingGiven,
    // For text questions:
    List<string>? TextAnswers
);

public record OptionCountDto(string OptionLabel, int Count, double PercentageOfResponses);
```

#### `SurveyFormDto` (public — token validated)
```csharp
public record SurveyFormDto(
    Guid SurveyId,
    string Title,
    string? WelcomeMessage,
    string? ThankYouMessage,
    List<SurveyQuestionDto> Questions,
    DateTime ExpiresAt
);
```

---

### 6.4 Request Models

#### `CreateSurveyRequest`
```csharp
public record CreateSurveyRequest(
    string Title,           // required, max 300
    string? Description,    // max 2000
    string? WelcomeMessage, // max 1000
    string? ThankYouMessage,// max 1000
    int TokenExpiryDays,    // 1–90, default 7
    bool IsAnonymous        // default false; hides respondent identity in results
);
```

#### `CopySurveyRequest`
```csharp
public record CopySurveyRequest(
    /// <summary>
    /// Optional override for the new survey title.
    /// If null, defaults to "Copy of {OriginalTitle}" (capped at 300 chars).
    /// </summary>
    string? NewTitle,

    /// <summary>
    /// IDs of questions from the source survey to EXCLUDE from the copy.
    /// Any ID not found in the source survey is silently ignored.
    /// Pass an empty list (or omit) to copy all questions.
    /// </summary>
    List<Guid> ExcludeQuestionIds
);
```

#### `AddSurveyQuestionRequest`
```csharp
public record AddSurveyQuestionRequest(
    string QuestionText,         // required, max 1000, HTML-stripped server-side
    SurveyQuestionType QuestionType,
    List<string>? Options,       // required for SingleChoice/MultipleChoice; max 20 options, each max 200 chars
                                 // Likert template pre-fills: ["Strongly Disagree","Disagree","Neutral","Agree","Strongly Agree"]
                                 // NPS template uses Rating type with MinRating=0, MaxRating=10
    int MinRating,               // for Rating: default 1; 0 allowed for NPS
    int MaxRating,               // for Rating: 1–10, must be > MinRating
    bool IsRequired,
    int OrderSequence
);
```

#### `SubmitSurveyRequest` (public)
```csharp
public record SubmitSurveyRequest(
    List<SurveyAnswerRequest> Answers
);

public record SurveyAnswerRequest(
    Guid QuestionId,
    string? AnswerText,          // for Text / SingleChoice (option label) / YesNo ("Yes"/"No")
    List<string>? AnswerOptions, // for MultipleChoice
    int? RatingValue             // for Rating
);
```

#### `ResendInvitationsRequest`
```csharp
public record ResendInvitationsRequest(
    List<Guid> UserIds   // 1–500 userIds; validated server-side
);
```

---

### 6.5 Validators

**`CreateSurveyRequestValidator`** (FluentValidation):
- `Title`: `NotEmpty`, `MaxLength(300)`
- `Description`: `MaxLength(2000)` when not null
- `TokenExpiryDays`: `InclusiveBetween(1, 90)`

**`AddSurveyQuestionRequestValidator`**:
- `QuestionText`: `NotEmpty`, `MaxLength(1000)`, HTML is stripped server-side (whitelist = plain text only; prevent stored XSS)
- `Options`: required and `Count().GreaterThan(1).LessThanOrEqualTo(20)` when type is `SingleChoice` or `MultipleChoice`
- `Options[i]`: each item `NotEmpty`, `MaxLength(200)`, no duplicate labels (case-insensitive) allowed
- `Options`: must be null for `Text`, `Rating`, `YesNo`
- `MinRating`: `InclusiveBetween(0, 9)` when type is `Rating` (0 allowed to support NPS 0–10)
- `MaxRating`: `InclusiveBetween(1, 10)`, must be `GreaterThan(MinRating)` when type is `Rating`

**`SubmitSurveyRequestValidator`**:
- `Answers`: not empty, count must not exceed total question count
- (Completeness of required questions is validated in the service against the live question list)

---

## 7. Infrastructure Layer — Services & Email

### 7.1 Folder Structure

```
backend/src/KnowHub.Infrastructure/
  Services/
    Surveys/
      SurveyService.cs
      SurveyInvitationService.cs
      SurveyResponseService.cs
  BackgroundServices/
    SurveyLaunchJob.cs              ← IHostedService or Channels-based worker
    SurveyTokenExpiryJob.cs         ← periodic job to mark expired invitations
  Email/
    EmailServiceBase.cs             ← add new survey email methods here
```

---

### 7.2 `SurveyService` — Key Implementation Details

```
SurveyService : ISurveyService
  ctor(KnowHubDbContext, ICurrentUserAccessor)

  GetSurveysAsync:
    → Guard: _currentUser.IsAdminOrAbove
    → Filter by TenantId, optional Status filter, paginated (pageSize cap 100)

  CreateAsync:
    → Guard: _currentUser.IsAdminOrAbove
    → Validate via FluentValidation (CreateSurveyRequestValidator injected)
    → Create Survey entity with Status = Draft
    → Save and return SurveyDto

  UpdateAsync:
    → Guard: _currentUser.IsAdminOrAbove
    → Fetch by Id+TenantId, throw NotFoundException if not found
    → Guard: Status must be Draft, throw BusinessRuleException("Cannot edit a launched survey") otherwise
    → RecordVersion check → throw ConflictException if stale
    → Update allowed fields (Title, Description, WelcomeMessage, ThankYouMessage, TokenExpiryDays)
    → Increment RecordVersion

  DeleteAsync:
    → Guard: Status must be Draft
    → Cascade deletes SurveyQuestions (DB ON DELETE CASCADE)

  LaunchAsync:
    → Guard: _currentUser.IsAdminOrAbove
    → Guard: Status must be Draft
    → Guard: Survey must have at least one question
    → Transition Status = Active, set LaunchedAt = UtcNow
    → Fetch all active employees in tenant:
        db.Users.Where(u => u.TenantId == tenantId && u.IsActive && u.Role.HasFlag(UserRole.Employee))
    → Set TotalInvited = employees.Count
    → Publish launch command to in-memory channel (ISurveyLaunchQueue)
        — surveysController returns 202 Accepted immediately

  CloseAsync:
    → Guard: Status must be Active
    → Transition Status = Closed, set ClosedAt = UtcNow
    → Bulk-update all invitations with Status = Sent → Expired (they can no longer submit)

  AddQuestionAsync:
    → Guard: Status must be Draft
    → Strip HTML from QuestionText using HtmlSanitizer (whitelist = plain text only)
    → Strip HTML from each option string
    → Validate Options present for choice types
    → Append to SurveyQuestions

  UpdateQuestionAsync:
    → Guard: Status must be Draft
    → RecordVersion check on question
    → Same HTML stripping and option validation

  DeleteQuestionAsync:
    → Guard 1: Survey.Status must be Draft
         → throws BusinessRuleException if Active or Closed
    → Guard 2: Check _db.SurveyAnswers.AnyAsync(a => a.QuestionId == questionId)
         → throws ConflictException if any answers exist
         → Guards reporting integrity: answered questions must never be hard-deleted
    → Hard delete: _db.SurveyQuestions.Remove(...)
    (The PostgreSQL ON DELETE RESTRICT FK on SurveyAnswers.QuestionId is the DB-level backstop;
     Guard 2 ensures the API returns 409 Conflict with a helpful message before the DB is hit.)

  ReorderQuestionsAsync:
    → Guard: Status must be Draft
    → request.Ordered is List<Guid> (question IDs in desired order)
    → Validate all IDs belong to this survey
    → Update OrderSequence = index position

  GetResultsAsync:
    → Guard: _currentUser.IsAdminOrAbove
    → Load survey + all questions + all responses + all answers efficiently (two queries: EF Include chain)
    → Compute per-question aggregation:
        Text    → collect all AnswerText values (list)
        Rating  → Avg, Min, Max of RatingValue
        YesNo   → count "Yes" vs "No"
        Single/Multi Choice → count occurrences per option label
    → Return SurveyResultsDto
```

---

### 7.3 `SurveyInvitationService` — Key Implementation Details

**Token generation and storage** (critical security section):

> **Research validation**: This exact token flow is confirmed by:
> - ASP.NET Core Identity source code (password reset / email confirmation token generation)
> - Phoenix `phx.gen.auth` (Elixir) — same pattern, well-documented rationale on ElixirForum
> - murmusoftwareinfotech.com OTP guide for .NET 10 (generation → store hash → mark used)
>
> **Why NOT `ITimeLimitedDataProtector`** (from Microsoft Docs, `aspnetcore/security/data-protection/consumer-apis/limited-lifetime-payloads`): While `ITimeLimitedDataProtector.Protect(payload, TimeSpan.FromDays(7))` generates a self-expiring encrypted token that throws `CryptographicException` on expiry, it has a critical drawback for survey links: the Data Protection key ring is tied to server state. Restarting the app with different keys (e.g., horizontal scaling, Docker redeploy, key rotation) permanently invalidates all outstanding 7-day tokens with no recovery path. The SHA-256-hash-plus-DB approach is key-rotation-safe.

```csharp
private static (string plainToken, string tokenHash) GenerateToken()
{
    // 32 cryptographically random bytes → 256 bits entropy
    // RandomNumberGenerator is CSRNG — safe for cryptographic use
    var bytes = RandomNumberGenerator.GetBytes(32);
    // Base64Url encode for URL safety (no +, /, = characters)
    // Microsoft.AspNetCore.WebUtilities.Base64UrlTextEncoder
    var plainToken = Base64UrlTextEncoder.Encode(bytes);
    // SHA-256 of UTF-8 bytes of the plaintext token
    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainToken)));
    return (plainToken, hash.ToLowerInvariant()); // lowercase hex → stored in TokenHash CHAR(64)
    // NOTE: plainToken is returned to caller solely for email dispatch.
    // Caller MUST NOT log or persist it. Only the hash enters the DB.
}
// NOTE on timing attacks: Token comparison occurs as a DB index scan (WHERE TokenHash = @hash).
// PostgreSQL query latency (~1–5ms) dwarfs any in-memory string timing difference (nanoseconds).
// CryptographicOperations.FixedTimeEquals() is NOT needed here because no C# string == is used
// for the lookup. If you ever add in-memory comparison, add FixedTimeEquals() at that point.
// (Research: Paragon Initiative Enterprises, "Preventing Timing Attacks on String Comparison")
```

```
CreateInvitationsAsync(surveyId, userIds):
  → For each userId, call GenerateToken()
  → Build SurveyInvitation { ..., TokenHash = hash, Status = Pending, ExpiresAt = LaunchedAt + TokenExpiryDays }
  → Bulk insert (EF Core AddRange → SaveChanges)

SendInvitationEmailAsync(invitationId):
  → Load invitation + survey + user
  → Build survey URL: {FrontendBaseUrl}/survey/{plainToken}
    → plainToken NOT stored in DB; must be passed in OR re-generated? 
    
    *** IMPORTANT — token delivery approach ***:
    The plain token is used immediately at creation time — it is generated, emailed, and then discarded
    from the server's memory. The hash is all that persists.
    
    Implementation: CreateInvitationsAndSendAsync() generates token, stores hash, sends email
    in the same unit of work BEFORE discarding the plaintext:
    
    foreach (var user in users)
    {
        var (plainToken, tokenHash) = GenerateToken();
        var invitation = new SurveyInvitation { ..., TokenHash = tokenHash, Status = Pending };
        db.SurveyInvitations.Add(invitation);
        emailQueue.Enqueue((user.Email, user.FullName, plainToken, survey));
    }
    await db.SaveChangesAsync();
    // Now send emails (after DB commit so invitations are persisted)
    foreach (var (email, name, token, survey) in emailQueue)
        await _emailService.SendSurveyInvitationAsync(..., token, ...);
        invitation.Status = Sent; invitation.SentAt = UtcNow;
    await db.SaveChangesAsync();

ResendToUserAsync(surveyId, userId):
  → Load invitation (TenantId + SurveyId + UserId)
  → Guard: Status must NOT be Submitted (throw BusinessRuleException)
  → Guard: Parent survey Status must be Active
  → Invalidate old token: update Status = Expired
  → Generate new token, create NEW invitation record (do NOT reuse the old one — keeps audit trail)
    OR update the existing invitation with new TokenHash, Status = Pending, increment ResendCount
    → Decision: UPDATE existing record to maintain 1:1 (simpler, maintains UQ constraint)
  → Set ExpiresAt = UtcNow + survey.TokenExpiryDays
  → Generate plainToken, update TokenHash in DB, save
  → Send email with new token
  → After email success: Status = Sent, SentAt = UtcNow

ResendBulkAsync(surveyId, request):
  → Validate request.UserIds.Count in (1, 500)
  → Load all invitations matching TenantId + SurveyId + UserId in UserIds list
  → Filter out any with Status = Submitted
  → For each: same resend logic as above
  → Batch DB save after all generated

ResendAllPendingAsync(surveyId):
  → Load all invitations where SurveyId == surveyId AND Status IN (Sent) AND ExpiresAt < UtcNow
  → Apply resend logic to each

MarkExpiredAsync:
  → Called by background job on a schedule (every hour)
  → Batch UPDATE SurveyInvitations SET Status = 'Expired' WHERE Status = 'Sent' AND ExpiresAt < NOW()
  → Use raw SQL or EF Core ExecuteUpdateAsync for bulk efficiency
```

---

### 7.4 `SurveyResponseService` — Key Implementation Details

```
GetFormByTokenAsync(plainToken):
  → Hash the incoming token: SHA-256 of UTF-8 bytes
  → Look up invitation by TokenHash
  → If not found → throw NotFoundException("Survey not found or link is invalid")
    (generic message — do NOT reveal whether token exists)
  → If Status == Submitted → throw BusinessRuleException("You have already submitted this survey")
  → If Status == Expired OR ExpiresAt < UtcNow → throw BusinessRuleException("This survey link has expired. Please contact your administrator to request a new link.")
  → If Status == Failed → throw BusinessRuleException("This invitation link is not active.")
  → Load parent survey; if survey.Status != Active → throw BusinessRuleException("This survey is no longer accepting responses.")
  → Return SurveyFormDto (survey + ordered questions; do NOT include other users' data)

SubmitAsync(plainToken, request):
  → Same token validation as GetFormByTokenAsync (always re-validate on submit, never trust client state)
  → Load all questions for this survey ordered by OrderSequence
  → Validate that all IsRequired questions have a corresponding answer in request.Answers
    → throw ValidationException with list of missing required question IDs
  → Validate each answer value:
      Text        → AnswerText not empty (if required)
      YesNo       → AnswerText must be "Yes" or "No" (case-insensitive)
      SingleChoice→ AnswerText must match one of question.Options
      MultiChoice → each item in AnswerOptions must match one of question.Options; at least 1 item
      Rating      → RatingValue must be in [question.MinRating, question.MaxRating]
  → Double-submission guard:
      check db.SurveyResponses.AnyAsync(sr => sr.TenantId == invitation.TenantId
          && sr.SurveyId == invitation.SurveyId && sr.UserId == invitation.UserId)
      → throw ConflictException if already exists
  → Create SurveyResponse + SurveyAnswers in a transaction
  → Update invitation: Status = Submitted, SubmittedAt = UtcNow
  → Increment survey.TotalResponded
  → SaveChanges (all in one transaction — if any step fails nothing is partially committed)
```

---

### 7.5 `SurveyLaunchJob` — Background Service

> **Research findings on background service choices**:  
> The Medium article ("Background Jobs in ASP.NET Core Web API", Jan 2026) confirms `BackgroundService` + `System.Threading.Channels` as the lightweight, production-viable pattern. For enterprise-grade persistence and retry, **Hangfire with PostgreSQL** is the recommended upgrade path. Since KnowHub already runs PostgreSQL and the existing codebase uses `BackgroundServices/` for prior jobs, we keep `System.Threading.Channels` for v1. If survey launch reliability becomes a concern (email failures, app restarts), Hangfire can be added without changing service interfaces.

```csharp
// Uses System.Threading.Channels for a simple in-process queue
// Written as BackgroundService registered in DI
// CRITICAL: BackgroundService is SINGLETON — NEVER inject KnowHubDbContext directly.
// ALWAYS use IServiceScopeFactory to get a fresh scoped DbContext per iteration.
// (Research source: Medium Suraj Pandey, Jan 2026 — "Background Jobs in ASP.NET Core Web API")

public class SurveyLaunchJob : BackgroundService
{
    private readonly Channel<Guid> _channel;  // surveyId queue
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SurveyLaunchJob> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var surveyId in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    // Fresh scoped DbContext per survey launch — no stale change tracker.
                    var invitationService = scope.ServiceProvider
                        .GetRequiredService<ISurveyInvitationService>();
                    await invitationService.CreateInvitationsAndSendAsync(surveyId, stoppingToken);
                }
                catch (OperationCanceledException) { throw; }  // let outer catch handle shutdown
                catch (Exception ex)
                {
                    // Log but do NOT crash the worker — next survey launch must still be processable
                    _logger.LogError(ex, "SurveyLaunchJob failed for surveyId {SurveyId}", surveyId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // EXPECTED during ASP.NET Core graceful shutdown — NOT an error.
            // Do NOT log as error. Task.Delay / Channel.ReadAll throws this on cancellation.
            _logger.LogInformation("SurveyLaunchJob stopping (graceful shutdown).");
        }
    }
}
```

> **Why `System.Threading.Channels` over `Task.Run`**:  
> `Task.Run` in a controller has no lifecycle management, no graceful shutdown support, no CancellationToken threading, and leaks scoped services (research article explicitly warns against this). `System.Threading.Channels` participates in the application host lifecycle and can be cancelled cleanly.

---

### 7.6 `SurveyTokenExpiryJob` — Background Service

```csharp
// Runs hourly to mark Sent invitations whose ExpiresAt < UtcNow as Expired.
// CRITICAL: uses IServiceScopeFactory — not direct DbContext injection (singleton vs scoped issue).
public class SurveyTokenExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SurveyTokenExpiryJob> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait first so the job doesn't run immediately on app start
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var invService = scope.ServiceProvider
                        .GetRequiredService<ISurveyInvitationService>();
                    await invService.MarkExpiredAsync(stoppingToken);
                    _logger.LogInformation("SurveyTokenExpiryJob completed at {UtcNow}", DateTime.UtcNow);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SurveyTokenExpiryJob failed.");
                    // Continue loop — don't exit the worker on transient DB errors
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SurveyTokenExpiryJob stopping (graceful shutdown).");
        }
    }
}
```

> **Why hourly and not on-demand**: The `SurveyResponseService.GetFormByTokenAsync` already does lazy expiry (updates status to `Expired` if it detects an expired-but-still-Sent record). The hourly job ensures the invitation list in the admin dashboard reflects accurate status between visits, and ensures `ResendAllPendingAsync` correctly filters on `Status = Expired`.

---

### 7.7 Email — Survey Invitation Template

**New method added to `EmailServiceBase`**:

```csharp
public Task SendSurveyInvitationAsync(SurveyInvitationEmailData data, CancellationToken ct)
    => SendAsync(BuildSurveyInvitationEmail(data), ct);

private static SendEmailRequest BuildSurveyInvitationEmail(SurveyInvitationEmailData data)
{
    var subject = $"You're invited: {data.SurveyTitle}";
    var html = $"""
        <html><body style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;">
          <h2 style="color:#1976d2;">Organizational Survey</h2>
          <p>Dear {data.RecipientName},</p>
          <p>{data.WelcomeMessage ?? $"You have been invited to complete the survey: <strong>{data.SurveyTitle}</strong>."}</p>
          <p>Please complete the survey by <strong>{data.ExpiresAt:MMMM dd, yyyy}</strong>.</p>
          <div style="text-align:center;margin:30px 0;">
            <a href="{data.SurveyUrl}" 
               style="background:#1976d2;color:#fff;padding:14px 28px;text-decoration:none;border-radius:4px;font-size:16px;">
              Start Survey
            </a>
          </div>
          <p style="color:#666;font-size:13px;">
            If the button does not work, copy and paste this link into your browser:<br/>
            <a href="{data.SurveyUrl}">{data.SurveyUrl}</a>
          </p>
          <p style="color:#666;font-size:12px;">
            This link is unique to you and can only be used once. Do not share it with others.
          </p>
        </body></html>
        """;
    return new SendEmailRequest(data.RecipientEmail, subject, html, data.RecipientName);
}
```

**`SurveyInvitationEmailData`**:
```csharp
public record SurveyInvitationEmailData(
    string RecipientEmail,
    string RecipientName,
    string SurveyTitle,
    string? WelcomeMessage,
    string SurveyUrl,      // https://{frontend}/survey/{plainToken}
    DateTime ExpiresAt
);
```

---

## 8. API Layer — Controllers & Authorization

### 8.1 New Authorization Policy

In `ServiceCollectionExtensions.cs` (or `ApiServiceExtensions.cs`), register if not already present:
```csharp
options.AddPolicy("AdminOrAbove", policy =>
    policy.RequireAssertion(ctx =>
        ctx.User.HasClaim("role", ((int)UserRole.Admin).ToString()) ||
        ctx.User.HasClaim("role", ((int)UserRole.SuperAdmin).ToString())));
```
*(Pattern matches existing `KnowledgeTeamOrAbove` policy in the project.)*

---

### 8.2 New Rate-Limit Policy

In `ApiServiceExtensions.cs`, add a restrictive policy for the public survey form endpoint:
```csharp
options.AddFixedWindowLimiter("SurveyTokenPolicy", o =>
{
    o.PermitLimit = 10;
    o.Window = TimeSpan.FromMinutes(1);
    o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    o.QueueLimit = 0;
});
```

---

### 8.3 Rate-Limit Policy for Resend

Resend endpoints should be protected by an "AdminResendPolicy" with a slightly stricter limit to prevent bulk email abuse:
```csharp
options.AddFixedWindowLimiter("AdminResendPolicy", o =>
{
    o.PermitLimit = 20;
    o.Window = TimeSpan.FromMinutes(1);
});
```

---

### 8.4 `SurveysController`

**File**: `backend/src/KnowHub.Api/Controllers/SurveysController.cs`

```
[ApiController]
[Route("api/surveys")]
[Authorize(Policy = "AdminOrAbove")]            ← all management endpoints require Admin or SuperAdmin
public class SurveysController : ControllerBase
{
    // Dependencies: ISurveyService, ISurveyInvitationService

    GET    /api/surveys                              → GetSurveysAsync (paginated)       → 200 PagedResult<SurveyDto>
    POST   /api/surveys                              → CreateAsync                        → 201 SurveyDto
    GET    /api/surveys/{id}                         → GetByIdAsync                       → 200 SurveyDto
    PUT    /api/surveys/{id}                         → UpdateAsync                        → 200 SurveyDto
    DELETE /api/surveys/{id}                         → DeleteAsync                        → 204

    POST   /api/surveys/{id}/questions               → AddQuestionAsync                   → 201 SurveyQuestionDto
    PUT    /api/surveys/{id}/questions/{qId}         → UpdateQuestionAsync                → 200 SurveyQuestionDto
    DELETE /api/surveys/{id}/questions/{qId}         → DeleteQuestionAsync                → 204
    POST   /api/surveys/{id}/questions/reorder       → ReorderQuestionsAsync              → 204

    POST   /api/surveys/{id}/launch                  → LaunchAsync (queues background job) → 202 Accepted { message }
    POST   /api/surveys/{id}/close                   → CloseAsync                         → 200 SurveyDto
    GET    /api/surveys/{id}/results                 → GetResultsAsync                    → 200 SurveyResultsDto
    GET    /api/surveys/{id}/responses               → GetResponsesAsync                  → 200 PagedResult<SurveyResponseDto>

    GET    /api/surveys/{id}/invitations             → GetInvitationsAsync (paginated, filterable by status) → 200 PagedResult<SurveyInvitationDto>
    POST   /api/surveys/{id}/invitations/{userId}/resend        [EnableRateLimiting("AdminResendPolicy")] → ResendToUserAsync → 204
    POST   /api/surveys/{id}/invitations/resend-bulk            [EnableRateLimiting("AdminResendPolicy")] → ResendBulkAsync (body: ResendInvitationsRequest) → 204
    POST   /api/surveys/{id}/invitations/resend-all-pending     [EnableRateLimiting("AdminResendPolicy")] → ResendAllPendingAsync → 202 Accepted { message, count }
}
```

---

### 8.5 `SurveyFormController` (Public — No JWT)

**File**: `backend/src/KnowHub.Api/Controllers/SurveyFormController.cs`

```
[ApiController]
[Route("api/surveys/form")]
[AllowAnonymous]                                   ← token IS the credential; no JWT needed
public class SurveyFormController : ControllerBase
{
    // Dependencies: ISurveyResponseService

    GET  /api/surveys/form/{token}                  [EnableRateLimiting("SurveyTokenPolicy")]
         → GetFormByTokenAsync(token)
         → 200 SurveyFormDto
         → 404 if not found (generic error — no token existence leak)
         → 400 if expired / already submitted

    POST /api/surveys/form/{token}/submit            [EnableRateLimiting("SurveyTokenPolicy")]
         → SubmitAsync(token, [FromBody] SubmitSurveyRequest)
         → 201 SurveyResponseDto (minimal: confirmationId, title, thankYouMessage)
         → 400 validation error
         → 409 already submitted
}
```

> **Security rationale**: The public form endpoint is deliberately on a separate controller class so its `[AllowAnonymous]` attribute cannot accidentally bleed into protected endpoints. The strongly-typed token is the sole credential — no userId, surveyId, or tenantId is accepted from the URL path or query string.

---

### 8.6 Complete API Surface Summary

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `GET` | `/api/surveys` | Admin / SuperAdmin | List surveys (paginated, filter by status) |
| `POST` | `/api/surveys` | Admin / SuperAdmin | Create survey (Draft) |
| `GET` | `/api/surveys/{id}` | Admin / SuperAdmin | Get survey with questions |
| `PUT` | `/api/surveys/{id}` | Admin / SuperAdmin | Update survey metadata (Draft only) |
| `DELETE` | `/api/surveys/{id}` | Admin / SuperAdmin | Delete survey (Draft only) |
| `POST` | `/api/surveys/{id}/questions` | Admin / SuperAdmin | Add question to survey (Draft only) |
| `PUT` | `/api/surveys/{id}/questions/{qId}` | Admin / SuperAdmin | Update question (Draft only) |
| `DELETE` | `/api/surveys/{id}/questions/{qId}` | Admin / SuperAdmin | Delete question (Draft only) |
| `POST` | `/api/surveys/{id}/questions/reorder` | Admin / SuperAdmin | Reorder questions (Draft only) |
| `POST` | `/api/surveys/{id}/launch` | Admin / SuperAdmin | Launch survey → 202 Accepted |
| `POST` | `/api/surveys/{id}/close` | Admin / SuperAdmin | Close survey |
| `GET` | `/api/surveys/{id}/results` | Admin / SuperAdmin | Aggregated results |
| `GET` | `/api/surveys/{id}/responses` | Admin / SuperAdmin | Individual responses (paginated) |
| `GET` | `/api/surveys/{id}/invitations` | Admin / SuperAdmin | Invitation list (paginated, filterable) |
| `POST` | `/api/surveys/{id}/copy` | Admin / SuperAdmin | Copy survey to new Draft (with optional question exclusion) |
| `POST` | `/api/surveys/{id}/invitations/{userId}/resend` | Admin / SuperAdmin | Resend to one user |
| `POST` | `/api/surveys/{id}/invitations/resend-bulk` | Admin / SuperAdmin | Resend to multiple users |
| `POST` | `/api/surveys/{id}/invitations/resend-all-pending` | Admin / SuperAdmin | Resend to all expired/unsubmitted |
| `GET` | `/api/surveys/form/{token}` | **Public** (token-based) | Get survey form |
| `POST` | `/api/surveys/form/{token}/submit` | **Public** (token-based) | Submit survey response |

---

## 9. Frontend

### 9.1 Folder Structure

```
frontend/src/features/surveys/
  SurveysPage.tsx                 ← Admin view: list all surveys, launch, close
  SurveyBuilderPage.tsx           ← Admin view: question configuration wizard
  SurveyFormPage.tsx              ← Public view: employee fills out survey (route: /survey/:token)
  types.ts                        ← TypeScript types mirroring backend DTOs
  api/
    surveysApi.ts                 ← Admin API calls (CRUD, launch, close, copy, results, resend)
    surveyFormApi.ts              ← Public API calls (getForm, submit) — no auth header attached
  tabs/
    QuestionsTab.tsx              ← Drag-and-drop question list + add/edit question dialog
    ResultsTab.tsx                ← Charts: bar charts for choices, avg rating, text answer list
    InvitationsTab.tsx            ← Table: status, sent/expires, resend actions (single + bulk)
  components/
    QuestionEditor.tsx            ← Reusable question form (type picker, options management)
    SurveyStatusChip.tsx          ← Draft (gray) / Active (green) / Closed (red) chip
    ResendDialog.tsx              ← Confirmation dialog for bulk resend
    CopySurveyDialog.tsx          ← Dialog to copy survey: new title + checklist of questions to exclude
```

### 9.2 Routes

```typescript
// In routes.tsx — add to existing route config:

// Public route (no PrivateRoute wrapper)
{ path: '/survey/:token', element: <SurveyFormPage /> }

// Protected admin routes
{ path: '/admin/surveys', element: <PrivateRoute roles={['Admin', 'SuperAdmin']}><SurveysPage /></PrivateRoute> }
{ path: '/admin/surveys/:id', element: <PrivateRoute roles={['Admin', 'SuperAdmin']}><SurveyBuilderPage /></PrivateRoute> }
```

### 9.3 Page Descriptions

**`SurveysPage.tsx`**:
- MUI `DataGrid` / table listing surveys with columns: Title, Status (chip), Questions, Invited, Responded, Response Rate %, Created, Actions
- Actions: Edit (→ SurveyBuilderPage), Launch (confirm dialog), Close (confirm dialog), View Results/Invitations
- Floating action button → Create New Survey (opens modal form for title, description, tokenExpiryDays)

**`SurveyBuilderPage.tsx`** (tab-based layout like AssessmentPage):
- Tabs: `Questions`, `Results` (only when Active/Closed), `Invitations` (only when Active/Closed)
- **QuestionsTab**: Ordered list of questions with drag-to-reorder (only in Draft). Add/Edit question dialog with type picker, options list (chip input for choice types), rating scale config, isRequired toggle.
- **ResultsTab**: Per-question cards — bar chart (recharts / MUI) for choice distribution, avg/min/max display for ratings, scrollable text-answer list.
- **InvitationsTab**: MUI DataGrid with Status filter, Resend column action (single). Toolbar: "Resend All Expired" button, "Resend Selected" bulk action checkbox.

**`SurveyFormPage.tsx`** (public):
- Rendered at `/survey/:token` — no Navbar/AppLayout wrapper
- On mount: `GET /api/surveys/form/:token` 
  - If 200 → render questions
  - If 400 (expired) → show expiry message with admin contact info
  - If 400 (already submitted) → show "Thank You" screen
  - If 404 → generic "link not found" screen (never reveal token existence)
- Question rendering by type:
  - `Text` → `<TextField multiline />`
  - `SingleChoice` → `<RadioGroup />` with options
  - `MultipleChoice` → `<FormGroup />` with `<Checkbox />` per option
  - `Rating` → MUI `<Rating />` (1–5) or custom slider for wider range
  - `YesNo` → `<RadioGroup>` with "Yes" / "No" options
- Submit → `POST /api/surveys/form/:token/submit`
- On success (201) → show ThankYouMessage, no further interaction
- Loading state, error state, validation highlights for required unanswered fields
- No auth headers in `surveyFormApi.ts` requests

### 9.4 `surveysApi.ts` (Admin)
```typescript
export const surveysApi = {
  getSurveys:           (p) => axiosClient.get<PagedResult<SurveyDto>>('/surveys', { params: p }),
  getSurveyById:        (id) => axiosClient.get<SurveyDto>(`/surveys/${id}`),
  createSurvey:         (req) => axiosClient.post<SurveyDto>('/surveys', req),
  updateSurvey:         (id, req) => axiosClient.put<SurveyDto>(`/surveys/${id}`, req),
  deleteSurvey:         (id) => axiosClient.delete(`/surveys/${id}`),
  addQuestion:          (id, req) => axiosClient.post<SurveyQuestionDto>(`/surveys/${id}/questions`, req),
  updateQuestion:       (id, qId, req) => axiosClient.put<SurveyQuestionDto>(`/surveys/${id}/questions/${qId}`, req),
  deleteQuestion:       (id, qId) => axiosClient.delete(`/surveys/${id}/questions/${qId}`),
  reorderQuestions:     (id, req) => axiosClient.post(`/surveys/${id}/questions/reorder`, req),
  launchSurvey:         (id) => axiosClient.post(`/surveys/${id}/launch`),
  closeSurvey:          (id) => axiosClient.post(`/surveys/${id}/close`),
  getResults:           (id) => axiosClient.get<SurveyResultsDto>(`/surveys/${id}/results`),
  getResponses:         (id, p) => axiosClient.get<PagedResult<SurveyResponseDto>>(`/surveys/${id}/responses`, { params: p }),
  getInvitations:       (id, p) => axiosClient.get<PagedResult<SurveyInvitationDto>>(`/surveys/${id}/invitations`, { params: p }),
  resendToUser:         (id, userId) => axiosClient.post(`/surveys/${id}/invitations/${userId}/resend`),
  resendBulk:           (id, req) => axiosClient.post(`/surveys/${id}/invitations/resend-bulk`, req),
  resendAllPending:     (id) => axiosClient.post(`/surveys/${id}/invitations/resend-all-pending`),
}
```

### 9.5 `surveyFormApi.ts` (Public)
```typescript
// Uses a separate axios instance WITHOUT the auth interceptor
const publicAxios = axios.create({ baseURL: '/api', headers: { 'Content-Type': 'application/json' } });

export const surveyFormApi = {
  getForm:   (token: string) => publicAxios.get<SurveyFormDto>(`/surveys/form/${token}`).then(r => r.data),
  submit:    (token: string, req: SubmitSurveyRequest) =>
               publicAxios.post<SurveySubmitResultDto>(`/surveys/form/${token}/submit`, req).then(r => r.data),
}
```

---

## 10. Security Design

### 10.1 Token Security (OWASP A02 — Cryptographic Failures)

> **Research summary**: The token design was validated against published OTP authentication guides for .NET 10, ASP.NET Core Identity source patterns, Phoenix `phx.gen.auth` documentation, Paragon Initiative timing attack research, and Microsoft's `ITimeLimitedDataProtector` documentation. Findings were incorporated into the design.

| Threat | Mitigation |
|---|---|
| Token guessing / brute-force enumeration | 32 bytes = 256 bits entropy from `RandomNumberGenerator` (CSRNG); rate-limited to 10 req/min per IP. $2^{256}$ values — infeasible to enumerate. |
| Token storage in DB | Only SHA-256 hash stored; plaintext lives only in memory during generation + email dispatch, then discarded. Pattern matches ASP.NET Core Identity design. |
| Token replay after submission | `SurveyInvitation.Status` set to `Submitted` at DB commit time; second submission checked before insert (`409 Conflict`). DB UNIQUE constraint on `(TenantId, SurveyId, UserId)` as backstop. |
| Token replay across survey relaunches | Each launch/resend generates a NEW token; old token's status is set to `Expired` before the new one is created. |
| Token interception in email | HTTPS-only transport (infrastructure level). Industry-standard risk of email delivery — acceptable trade-off; mitigated by short expiry (default 7 days, configurable to as low as 1 day). |
| Token in URL (browser history / logs) | Token in **path segment** (not query string). Research confirms path segments are less likely to appear in proxy/CDN query logs. Advise suppressing `/survey/*` path logging in NGINX config. Typeform confirmed using path segment for similar reasons. |
| Survey URL constructed from Host header | `FrontendBaseUrl` from `appsettings.json` only — never derived from request `Host` header (Host Header Injection / OWASP A05 prevention). |
| Timing attack on token comparison | NOT applicable: comparison is a DB index lookup (PostgreSQL ~1–5ms round trip dominates). No C# `==` string comparison. Paragon Initiative Enterprises research confirms the threat only applies to in-memory MAC comparisons, not DB lookups. |
| `ITimeLimitedDataProtector` as alternative | Evaluated and rejected: Data Protection key-ring tied to app instance — cannot survive restarts, key rotation, or horizontal scaling for 7-day tokens. SHA-256 hash + DB is key-rotation-safe. |

### 10.2 Access Control (OWASP A01 — Broken Access Control)

| Endpoint group | Access control |
|---|---|
| All survey management, results, resend | `[Authorize(Policy = "AdminOrAbove")]` at controller class level AND enforced inside service layer via `_currentUser.IsAdminOrAbove` |
| Survey form (public) | Separated into `SurveyFormController` with `[AllowAnonymous]`; no authenticated user context used |
| Tenant isolation | All DB queries scoped by `TenantId = _currentUser.TenantId`, never accept TenantId from client |
| UserId not in URL for public form | Token lookup is by token hash only → no IDOR possible on form submission |
| Results access | `_currentUser.IsAdminOrAbove` checked in `SurveyService.GetResultsAsync`; individual responses include PII (user names) — log access |

### 10.3 Injection Prevention (OWASP A03)

- `QuestionText` and each option string are stripped of HTML tags using a whitelist-based sanitizer before storage. Plain text only — no HTML allowed in questions.
- `AnswerText` is stored as-is (user input). When rendered in the admin results view, the frontend uses MUI Typography (React's JSX renders as text nodes — no `dangerouslySetInnerHTML`). This prevents stored XSS.
- `OptionsJson` and `AnswerOptionsJson` are stored as JSONB; EF Core's parameterized queries prevent SQL injection.
- Validated answer option labels are cross-referenced against the stored question options on the server side before insertion — prevents arbitrary data injection into JSONB fields.

### 10.4 Rate Limiting (OWASP A04 — Insecure Design / DoS)

- `SurveyTokenPolicy`: 10 req/min per IP for GET and POST on `/api/surveys/form/{token}` — mitigates token enumeration and response flooding.
- `AdminResendPolicy`: 20 req/min per IP for resend endpoints — prevents bulk email bombing via rapid resend calls.
- All token lookups return the **same generic error message and same HTTP status code** for not-found, expired, and submitted states — no distinct error messages that would allow enumeration.
  > **Research note**: The OTP authentication article (murmusoftwareinfotech.com) emphasizes returning consistent error responses for auth failures. A `404` for unknown tokens and a different code for expired tokens would leak information about which tokens exist in the system.

### 10.5 Data Integrity

- `UNIQUE (TenantId, SurveyId, UserId)` on `SurveyResponses` — database-level duplicate submission prevention.
- `UNIQUE (TenantId, SurveyId, UserId)` on `SurveyInvitations` — one active invitation per user per survey.
- `UNIQUE (TokenHash)` globally on `SurveyInvitations` — no cross-tenant token collision.
- `RecordVersion` optimistic concurrency on survey and question updates — prevents lost-update race conditions in concurrent admin sessions.
- Survey questions locked once survey `Status = Active` — structural integrity of in-flight responses.

### 10.6 Background Job Security

- `SurveyLaunchJob` uses `IServiceScopeFactory` — EF Core DbContext is scoped correctly and not shared across threads.
- Job failure is logged; failed email sends set `Invitation.Status = Failed` — admin can see and resend.
- Background job does not accept external input — only `surveyId` (Guid) from the internal channel, validated against DB before any email is sent.

---

## 11. SOLID Principles Alignment

| Principle | How Applied |
|---|---|
| **S — Single Responsibility** | `SurveyService` manages survey lifecycle + questions. `SurveyInvitationService` manages token generation, email dispatch, and expiry. `SurveyResponseService` manages form access and submission. Each has one reason to change. |
| **O — Open/Closed** | `SurveyQuestionType` enum + switch-based answer validation means adding a new question type (e.g., `Matrix`) requires adding a new enum value and a new validation branch, without changing existing validation code for other types. |
| **L — Liskov Substitution** | `SurveyInvitationService` depends on `IEmailService` (the base contract). `SmtpEmailService` and `AwsSesEmailService` are interchangeable — swapping transport doesn't affect the survey feature. The Template Method pattern in `EmailServiceBase` ensures derived classes only override transport, not domain logic. |
| **I — Interface Segregation** | Three narrow interfaces (`ISurveyService`, `ISurveyInvitationService`, `ISurveyResponseService`) rather than one fat interface. The `SurveyFormController` only depends on `ISurveyResponseService` — it never needs survey management capabilities. |
| **D — Dependency Inversion** | All controllers depend on application-layer interfaces (defined in `KnowHub.Application.Contracts`), not concrete service classes. The concrete services are registered in the DI container in `ServiceCollectionExtensions.cs`. |

---

## 12. Test Plan

**File location**: `backend/tests/KnowHub.Tests/Services/`

### 12.1 `SurveyServiceTests.cs`

| Test | Scenario |
|---|---|
| `CreateAsync_AsAdmin_Returns_Draft_Survey` | Happy path |
| `CreateAsync_AsEmployee_Throws_ForbiddenException` | Non-admin blocked |
| `UpdateAsync_DraftSurvey_Updates_Fields` | Happy path |
| `UpdateAsync_ActiveSurvey_Throws_BusinessRuleException` | Cannot edit launched survey |
| `UpdateAsync_StaleRecordVersion_Throws_ConflictException` | Optimistic concurrency |
| `DeleteAsync_DraftSurvey_Succeeds` | Happy path |
| `DeleteAsync_ActiveSurvey_Throws_BusinessRuleException` | Cannot delete live survey |
| `LaunchAsync_NoQuestions_Throws_BusinessRuleException` | Must have at least one question |
| `LaunchAsync_DraftSurvey_Returns_Active_Status` | Happy path |
| `LaunchAsync_ActiveSurvey_Throws_BusinessRuleException` | Cannot relaunch |
| `CloseAsync_ActiveSurvey_Returns_Closed_Status` | Happy path |
| `AddQuestionAsync_DraftSurvey_Succeeds` | Happy path |
| `AddQuestionAsync_ChoiceType_Without_Options_Throws_ValidationException` | Validation |
| `AddQuestionAsync_ActiveSurvey_Throws_BusinessRuleException` | Immutability |
| `DeleteQuestionAsync_DraftSurvey_NoAnswers_Succeeds` | Happy path |
| `DeleteQuestionAsync_DraftSurvey_HasAnswers_Throws_ConflictException` | Reporting guard (defense-in-depth) |
| `DeleteQuestionAsync_ActiveSurvey_Throws_BusinessRuleException` | Status immutability |
| `ReorderQuestionsAsync_UpdatesSequence` | Happy path |
| `GetResultsAsync_AggregatesCorrectly` | Logic test |
| `CopyAsync_AllQuestions_Creates_NewDraftWithSameQuestions` | Happy path (no exclusions) |
| `CopyAsync_WithExcludeQuestionIds_OmitsThoseQuestions` | Question exclusion |
| `CopyAsync_DefaultTitle_Prefixed_CopyOf` | Title defaulting |
| `CopyAsync_CustomNewTitle_UsesProvidedTitle` | Title override |
| `CopyAsync_ExcludeQuestionIds_NotInSource_AreIgnored` | Defensive — unknown IDs silently skipped |

### 12.2 `SurveyInvitationServiceTests.cs`

| Test | Scenario |
|---|---|
| `GenerateToken_ProducesUnique_NonStoredPlaintext` | Security invariant |
| `CreateInvitationsAsync_CorrectCount_Matching_ActiveEmployees` | Happy path |
| `ResendToUserAsync_AlreadySubmitted_Throws_BusinessRuleException` | Guard |
| `ResendToUserAsync_ClosedSurvey_Throws_BusinessRuleException` | Guard |
| `ResendToUserAsync_GeneratesNewToken_OldStatusExpired` | Token invalidation |
| `ResendBulkAsync_SkipsSubmittedUsers` | Business rule |
| `MarkExpiredAsync_OnlyExpiresSentBefore_UtcNow` | Boundary |

### 12.3 `SurveyResponseServiceTests.cs`

| Test | Scenario |
|---|---|
| `GetFormByTokenAsync_ValidToken_Returns_SurveyForm` | Happy path |
| `GetFormByTokenAsync_ExpiredToken_Throws_BusinessRuleException` | Expiry |
| `GetFormByTokenAsync_AlreadySubmittedToken_Throws_BusinessRuleException` | Replay prevention |
| `GetFormByTokenAsync_UnknownToken_Throws_NotFoundException` | Not found (generic) |
| `GetFormByTokenAsync_ClosedSurvey_Throws_BusinessRuleException` | Survey closed |
| `SubmitAsync_ValidToken_AllRequired_Saves_Correctly` | Happy path |
| `SubmitAsync_MissingRequiredAnswer_Throws_ValidationException` | Required field |
| `SubmitAsync_InvalidRatingValue_Throws_ValidationException` | Rating out of range |
| `SubmitAsync_InvalidChoiceOption_Throws_ValidationException` | Option not in list |
| `SubmitAsync_DuplicateSubmission_Throws_ConflictException` | Anti-replay |
| `SubmitAsync_ExpiredToken_Throws_BusinessRuleException` | Token state check on submit |

### 12.4 `SurveyValidatorTests.cs`

| Test | Scenario |
|---|---|
| `CreateSurveyRequest_EmptyTitle_Fails` | Validation |
| `CreateSurveyRequest_TokenExpiryDays_OutOfRange_Fails` | Boundary (0 → fail, 91 → fail) |
| `AddSurveyQuestionRequest_MultiChoiceWithNoOptions_Fails` | Validation |
| `AddSurveyQuestionRequest_RatingWithInvertedRange_Fails` | MinRating > MaxRating |
| `SubmitSurveyRequest_EmptyAnswers_Fails` | Validation |

---

## 13. Implementation Sequence

Implement in this order to avoid forward dependencies:

| Step | Task | Layer |
|---|---|---|
| 1 | Add `SurveyStatus`, `SurveyQuestionType`, `SurveyInvitationStatus` enums | Domain |
| 2 | Add `Survey` (with `IsAnonymous`), `SurveyQuestion`, `SurveyInvitation`, `SurveyResponse`, `SurveyAnswer` entities | Domain |
| 3 | Register entities in `KnowHubDbContext` (DbSet + `OnModelCreating` configuration) | Infrastructure |
| 4 | Write `011_SurveyModule.sql` migration (add `IsAnonymous` column to `Surveys`) | Database |
| 5 | Add `SurveyInvitationEmailData` record and `SendSurveyInvitationAsync` to `EmailServiceBase` | Infrastructure |
| 6 | Write DTOs, request models, and validators in Application layer | Application |
| 7 | Write `ISurveyService`, `ISurveyInvitationService`, `ISurveyResponseService` interfaces | Application |
| 8 | Implement `SurveyService` — including `CopyAsync` (clone survey + filtered questions into new Draft) | Infrastructure |
| 9 | Implement `SurveyInvitationService` (including token generation) | Infrastructure |
| 10 | Implement `SurveyResponseService` (public token validation + submission) | Infrastructure |
| 11 | Implement `SurveyLaunchJob` (background channel consumer) | Infrastructure |
| 12 | Implement `SurveyTokenExpiryJob` (periodic expiry refresh) | Infrastructure |
| 13 | Register all services, jobs, and the launch channel in `ServiceCollectionExtensions.cs` | Infrastructure |
| 14 | Add `AdminOrAbove` + `SurveyTokenPolicy` + `AdminResendPolicy` authorization/rate-limit policies | API |
| 15 | Write `SurveysController` | API |
| 16 | Write `SurveyFormController` (`[AllowAnonymous]`) | API |
| 17 | Write unit tests (steps 8–10 above) | Tests |
| 18 | Write frontend `types.ts` | Frontend |
| 19 | Write `surveysApi.ts` and `surveyFormApi.ts` | Frontend |
| 20 | Build `SurveysPage.tsx` (admin list) | Frontend |
| 21 | Build `SurveyBuilderPage.tsx` (Questions, Results, Invitations tabs) | Frontend |
| 22 | Build `SurveyFormPage.tsx` (public employee form) | Frontend |
| 23 | Add routes in `routes.tsx` | Frontend |

---

---

## 14. Research Sources & Findings Applied

The following internet sources were consulted and their findings directly incorporated into this plan:

| Source | Finding Applied |
|---|---|
| murmusoftwareinfotech.com — "Secure OTP Authentication in ASP.NET Core (.NET 10)" | Confirmed token flow: generate → hash → store hash → mark used. Added `IsUsed`/`Status = Submitted` equivalence. Recommended background cleanup job for expired tokens. |
| Paragon Initiative Enterprises — "Preventing Timing Attacks on String Comparison" | Timing attack threat does NOT apply to DB index lookups (only in-memory == comparisons). `CryptographicOperations.FixedTimeEquals` not needed for hash-indexed DB lookup. Added as explicit note in Security section. |
| ElixirForum — "Why does hashing tokens prevent timing attacks?" | Cross-validated the SHA-256 hash storage pattern. Confirmed the reasoning: hashing prevents plaintext exposure, not primarily timing attacks for DB lookups. |
| Microsoft Docs — `ITimeLimitedDataProtector` (ASP.NET Core 10) | Evaluated as alternative token mechanism. Rejected: Data Protection key-ring is not key-rotation-safe for multi-day tokens. Added "Alternative considered" note in NFR-003b. |
| Medium / Suraj Pandey — "Background Jobs in ASP.NET Core Web API" (Jan 2026) | CRITICAL: BackgroundService = singleton; DbContext = scoped — direct injection causes stale data and memory leaks. Must use `IServiceScopeFactory`. Updated both background service specs. Added OperationCanceledException handling pattern. |
| Microsoft Docs — Hosted Services (aspnetcore-10.0) | Confirmed `IServiceScopeFactory` pattern for background services with scoped dependencies. |
| NNG (Nielsen Norman Group) — "Writing Good Survey Questions: 10 Best Practices" | 10 best practices for survey design. Added guidance on question wording instructions forAdmin UI; confirmed question type selection. |
| SurveyMonkey — "Survey Question Types" | Confirmed Likert and NPS are top organizational survey formats. Decision: Likert = SingleChoice with standard options; NPS = Rating (0–10). Added quick-insert template note for Admin UI. |
| Typeform Help — "Can I generate unique codes for each respondent?" | Confirmed per-respondent URL token pattern is industry standard. Typeform uses URL parameters; our approach (path segment) is more secure and log-resistant. |
| SurveyMonkey Help — "Customizing Survey Links" | Confirmed opaque token approach matches enterprise survey practice. Token in path (our design) vs. custom variables (?c=...) — path tokens are more opaque. |

---

## 15. Survey Analytics & Reporting Module

> **Architecture Decision**: Survey Analytics is placed here as a dedicated sub-module within the Survey Module plan rather than inside the Talent Module. The Talent Module is concerned with AI-powered candidate screening (`ScreeningJobs`, `ScreeningCandidates`, resume scoring) — an entirely different domain. Survey Analytics operates over the same data model defined in Sections 4–6 and is a natural extension of the survey lifecycle. The scope is large enough to warrant its own controller, service interface, and frontend tab set.

---

### 15.1 Functional Requirements

| ID | Requirement |
|---|---|
| FR-027 | Admin/SuperAdmin can view an **Executive Dashboard** for any Active or Closed survey showing: total invited, total submitted, response rate %, health status badge, and a completion trend sparkline. |
| FR-028 | Admin/SuperAdmin can view **per-question statistics**: option distribution (count + percentage), average/min/max for Rating questions, and a scrollable list of text answers for Text questions. |
| FR-029 | Admin/SuperAdmin can filter question statistics by **department** — results recalculate to show only responses from employees in that department. |
| FR-030 | Admin/SuperAdmin can view the **NPS Report** for any survey containing a Rating question configured as NPS (MinRating=0, MaxRating=10): promoter / passive / detractor counts, the calculated NPS score, and percentage breakdown. |
| FR-031 | Admin/SuperAdmin can view an **NPS Trend** chart by selecting 2 or more surveys — each survey's NPS score is plotted on a timeline. |
| FR-032 | Admin/SuperAdmin can view a **Participation Funnel**: Invited → Emails Sent → Token Accessed (started) → Fully Submitted, with count and conversion rate at each stage. |
| FR-033 | Admin/SuperAdmin can view a **Department × Question Heatmap** for Rating-type questions showing the average score per department per question as a colour-coded matrix. |
| FR-034 | Admin/SuperAdmin can perform a **Cross-Survey Comparison** between any two surveys — shared questions (matched by identical question text) are shown side-by-side with their aggregated stats. |
| FR-035 | Admin/SuperAdmin can **export raw responses to CSV** — for non-anonymous surveys, respondent name and email are included; for anonymous surveys, PII columns are omitted and the backend enforces this regardless of the request flag. |
| FR-036 | Admin/SuperAdmin can **export a PDF summary report** containing: dashboard KPIs, per-question charts, NPS gauge (if applicable), and participation funnel. The PDF is generated server-side and returned as a file download. |
| FR-037 | Admin/SuperAdmin can filter all analytics views by a **date range** (submission date). |
| FR-038 | Admin/SuperAdmin can filter all analytics views by **employee work role**. |
| FR-039 | Analytics results for **anonymous surveys** must never expose any PII — no respondent names, emails, or user IDs, even in raw-data views. |
| FR-040 | The analytics API must handle surveys with up to **50,000 responses** without timeout; expensive queries use **output caching** with a 5-minute TTL (invalidated when a new response is submitted). |

---

### 15.2 Non-Functional Requirements

| ID | Requirement |
|---|---|
| NFR-A01 | Dashboard and question-stats endpoints: < 2 s p95 for up to 50,000 response rows. Achieve via indexed joins and selective projection (avoid loading full entity graphs). |
| NFR-A02 | PDF and large CSV exports are generated synchronously for surveys < 5,000 responses; for larger surveys a background export job writes to storage and returns a signed URL (integration point with existing `IStorageService`). |
| NFR-A03 | Output-cached endpoints use `OutputCache` policy `"SurveyAnalytics"` (ASP.NET Core Output Caching middleware) with a 5-min TTL; the cache tag `survey-{id}` is evicted when `SurveyResponseService.SubmitAsync` completes. |
| NFR-A04 | No new database tables. All analytics are computed queries over `Surveys`, `SurveyQuestions`, `SurveyInvitations`, `SurveyResponses`, and `SurveyAnswers`, joined to `Users` for department/role segmentation. |

---

### 15.3 Domain — No New Entities Required

All analytics data is derived at query time from the five entities defined in Section 4. The only addition is a **PostgreSQL view** to accelerate repeated aggregate queries:

```sql
-- In migration 012_SurveyAnalytics.sql
-- Materialized view for per-question option statistics
CREATE MATERIALIZED VIEW mv_SurveyOptionStats AS
SELECT
    sq."Id"                        AS "QuestionId",
    sq."SurveyId",
    sq."QuestionType",
    sa."TextValue"                 AS "OptionValue",
    COUNT(*)                       AS "AnswerCount"
FROM "SurveyAnswers" sa
JOIN "SurveyQuestions" sq ON sq."Id" = sa."SurveyQuestionId"
GROUP BY sq."Id", sq."SurveyId", sq."QuestionType", sa."TextValue";

CREATE UNIQUE INDEX idx_mv_SurveyOptionStats
    ON mv_SurveyOptionStats ("QuestionId", "OptionValue");

-- Manual refresh called by SurveyAnalyticsService after cache eviction
-- (or via background job on a 10-min schedule)

-- Lightweight index additions for analytics join performance
CREATE INDEX IF NOT EXISTS idx_SurveyAnswers_QuestionId
    ON "SurveyAnswers" ("SurveyQuestionId");

CREATE INDEX IF NOT EXISTS idx_SurveyAnswers_NumericValue
    ON "SurveyAnswers" ("SurveyQuestionId", "NumericValue")
    WHERE "NumericValue" IS NOT NULL;
```

---

### 15.4 Application Layer Additions

#### 15.4.1 New DTOs — `Application/Models/Surveys/Analytics/`

```csharp
// Overall survey health card
public record SurveyAnalyticsSummaryDto(
    int     SurveyId,
    string  SurveyTitle,
    int     TotalInvited,
    int     TotalSubmitted,
    double  ResponseRatePct,          // TotalSubmitted / TotalInvited * 100
    double  AvgCompletionTimeSeconds,
    string  HealthStatus              // "Healthy" ≥70%, "AtRisk" 40–69%, "LowEngagement" <40%
);

// Per-question aggregate
public record SurveyQuestionAnalyticsDto(
    int                         QuestionId,
    string                      QuestionText,
    SurveyQuestionType          QuestionType,
    int                         TotalAnswers,
    IReadOnlyList<OptionStatDto> OptionStats,  // choice / rating distribution
    double?                     AverageRating,
    int?                        MinRating,
    int?                        MaxRating,
    IReadOnlyList<string>       TextAnswers    // for Text type; empty otherwise
);

public record OptionStatDto(
    string OptionValue,
    int    Count,
    double Percentage
);

// Department-level row for heatmap / breakdown
public record DepartmentRowDto(
    string Department,
    double AverageScore,
    int    ResponseCount
);

public record SurveyDepartmentBreakdownDto(
    int                         QuestionId,
    string                      QuestionText,
    IReadOnlyList<DepartmentRowDto> Rows
);

// NPS report for a single survey
public record SurveyNpsReportDto(
    int    SurveyId,
    string SurveyTitle,
    int    Promoters,        // 9–10
    int    Passives,         // 7–8
    int    Detractors,       // 0–6
    int    NpsScore,         // (Promoters - Detractors) / Total * 100, rounded
    double PromoterPct,
    double PassivePct,
    double DetractorPct
);

// NPS trend across multiple survey waves
public record NpsTrendPointDto(
    int      SurveyId,
    string   SurveyTitle,
    DateTime LaunchedAt,
    int      NpsScore
);

public record SurveyNpsTrendDto(
    IReadOnlyList<NpsTrendPointDto> DataPoints
);

// Participation funnel
public record SurveyParticipationFunnelDto(
    int    TotalInvited,
    int    TotalEmailsSent,     // status != Draft (sent or later)
    int    TotalTokensAccessed, // SurveyInvitations with TokenAccessedAt != null
    int    TotalSubmitted,
    double SubmissionRatePct,
    double StartToSubmitRatePct // TotalSubmitted / TotalTokensAccessed * 100
);

// Heatmap: departments × questions matrix of avg ratings
public record SurveyHeatmapDto(
    IReadOnlyList<string>  QuestionTexts,    // column headers
    IReadOnlyList<string>  Departments,      // row headers
    double[][]             Matrix            // [deptIndex][questionIndex] = avg rating, NaN = no data
);

// Cross-survey question comparison
public record SharedQuestionCompDto(
    string                              QuestionText,
    IReadOnlyList<SurveyQuestionAnalyticsDto> SurveyStats   // one entry per survey
);

public record SurveyCompSummaryDto(
    int      SurveyId,
    string   Title,
    DateTime? LaunchedAt,
    double   ResponseRatePct
);

public record SurveyComparisonDto(
    IReadOnlyList<SurveyCompSummaryDto>   Surveys,
    IReadOnlyList<SharedQuestionCompDto>  SharedQuestions
);

// Export request (passed as query params, not body — GET endpoint)
public record SurveyExportRequest(
    int          SurveyId,
    ExportFormat Format,
    bool         IncludeRespondentInfo,  // ignored for anonymous surveys
    DateTime?    FromDate,
    DateTime?    ToDate
);

public enum ExportFormat { Csv, Pdf }
```

> **`SurveyInvitation` entity addition**: Add `TokenAccessedAt DateTime?` column to `SurveyInvitations` to track when an employee first opened the form (funnel stage 3). Set this in `SurveyResponseService.GetFormByTokenAsync` before returning the form data. Migration `012_SurveyAnalytics.sql` includes:
>
> ```sql
> ALTER TABLE "SurveyInvitations"
>     ADD COLUMN "TokenAccessedAt" TIMESTAMPTZ NULL;
> ```

#### 15.4.2 New Interface — `Application/Contracts/Surveys/ISurveyAnalyticsService.cs`

```csharp
public interface ISurveyAnalyticsService
{
    // Executive dashboard KPIs
    Task<SurveyAnalyticsSummaryDto> GetDashboardAsync(
        int surveyId, CancellationToken ct = default);

    // Per-question stats; departmentFilter is optional
    Task<IReadOnlyList<SurveyQuestionAnalyticsDto>> GetQuestionStatsAsync(
        int surveyId, string? departmentFilter, string? roleFilter,
        DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);

    // Department breakdown for one specific question
    Task<SurveyDepartmentBreakdownDto> GetDepartmentBreakdownAsync(
        int surveyId, int questionId, CancellationToken ct = default);

    // NPS for one survey (throws BusinessRuleException if no NPS question configured)
    Task<SurveyNpsReportDto> GetNpsReportAsync(
        int surveyId, CancellationToken ct = default);

    // NPS trend across several survey waves (ordered by LaunchedAt)
    Task<SurveyNpsTrendDto> GetNpsTrendAsync(
        IReadOnlyList<int> surveyIds, CancellationToken ct = default);

    // Participation funnel counts
    Task<SurveyParticipationFunnelDto> GetParticipationFunnelAsync(
        int surveyId, CancellationToken ct = default);

    // Department × question heatmap (Rating questions only)
    Task<SurveyHeatmapDto> GetHeatmapAsync(
        int surveyId, CancellationToken ct = default);

    // Side-by-side comparison of two surveys (matched on question text)
    Task<SurveyComparisonDto> CompareSurveysAsync(
        int surveyIdA, int surveyIdB, CancellationToken ct = default);

    // File exports
    Task<(byte[] Data, string FileName)> ExportToCsvAsync(
        SurveyExportRequest request, CancellationToken ct = default);

    Task<(byte[] Data, string FileName)> ExportToPdfAsync(
        SurveyExportRequest request, CancellationToken ct = default);
}
```

---

### 15.5 Infrastructure Layer — `SurveyAnalyticsService`

**File**: `Infrastructure/Services/Surveys/SurveyAnalyticsService.cs`

**Dependencies injected**:
- `KnowHubDbContext _db` — scoped, no scope factory needed (analytics service is also scoped)
- `IMemoryCache _cache` — for short-lived aggregate results during heavy read periods
- `ILogger<SurveyAnalyticsService> _logger`

**NuGet packages to install**:

| Package | Purpose |
|---|---|
| `CsvHelper` ≥ 33.x | CSV serialisation for `ExportToCsvAsync` |
| `QuestPDF` ≥ 2024.x | Server-side PDF generation for `ExportToPdfAsync` |

**Key implementation notes**:

```csharp
// ── GetDashboardAsync ──────────────────────────────────────────────────────
// 1. Load Survey.TotalInvited, Survey.TotalResponded (already maintained on entity).
// 2. Compute AvgCompletionTimeSeconds via:
//    AVG(SurveyResponses.SubmittedAt - SurveyInvitations.TokenAccessedAt) 
//    WHERE both are not null.
// 3. HealthStatus rule: ≥70% → "Healthy", 40–69% → "AtRisk", <40% → "LowEngagement".

// ── GetQuestionStatsAsync ──────────────────────────────────────────────────
// Use EF Core projection — never load SurveyAnswer text content for rating/choice
// questions if not needed (select only NumericValue or TextValue conditionally).
// Outer join to Users for department/role filters:
//   .Join(_db.Users, r => r.UserId, u => u.Id, ...)
//   .Where(u => departmentFilter == null || u.Department == departmentFilter)
// For Text questions: return up to 200 text answers (cap to prevent oversized payloads).
// For anonymised surveys: do NOT include any user-linked columns in projection.

// ── GetNpsReportAsync ──────────────────────────────────────────────────────
// Identify NPS question: QuestionType == Rating AND MaxRating == 10 AND MinRating == 0.
// If none found → throw BusinessRuleException("This survey has no NPS (0–10 rating) question.");
// Grouping:
//   Promoters:  NumericValue >= 9
//   Passives:   NumericValue >= 7 && NumericValue <= 8
//   Detractors: NumericValue <= 6
// NPS = (int)Math.Round((promoters - detractors) / (double)total * 100)

// ── GetHeatmapAsync ────────────────────────────────────────────────────────
// Only include Rating-type questions.
// Group by (User.Department, SurveyQuestionId) → AVG(NumericValue).
// Build jagged double[][] matrix; use double.NaN for department/question combos with 0 answers.
// Cap matrix to 20 departments × 10 questions to prevent unmanageable payloads.

// ── CompareSurveysAsync ────────────────────────────────────────────────────
// Load questions from both surveys.
// Match "shared" questions by identical QuestionText (case-insensitive, trimmed).
// For each shared question, call GetQuestionStatsAsync for each survey independently.

// ── ExportToCsvAsync ──────────────────────────────────────────────────────
// Use CsvHelper with anonymous-aware projection:
//   If survey.IsAnonymous || !request.IncludeRespondentInfo:
//     Columns: ResponseId, SubmittedAt, QuestionText, Answer
//   Else:
//     Columns: ResponseId, RespondentName, RespondentEmail, SubmittedAt, QuestionText, Answer
// Stream to MemoryStream, return byte[].
// FileName: $"survey-{surveyId}-responses-{DateTime.UtcNow:yyyyMMdd}.csv"

// ── ExportToPdfAsync ──────────────────────────────────────────────────────
// QuestPDF Document with sections:
//   1. Cover: Survey title, date range, response rate
//   2. Executive Dashboard KPI table
//   3. Per-question pages: question text, bar chart (QuestPDF BarChart component),
//      avg/min/max for ratings
//   4. NPS page (if applicable): score banner, promoter/passive/detractor table
//   5. Participation Funnel: step table with conversion rates
// FileName: $"survey-{surveyId}-report-{DateTime.UtcNow:yyyyMMdd}.pdf"
```

**Cache invalidation pattern**:

```csharp
// In SurveyResponseService.SubmitAsync — after saving response:
_cache.Remove($"survey-analytics-dashboard-{surveyId}");
_cache.Remove($"survey-analytics-questions-{surveyId}");
// (only dashboard and question-stats are cached; NPS/heatmap computed on demand)

// In SurveyAnalyticsService.GetDashboardAsync:
var cacheKey = $"survey-analytics-dashboard-{request.SurveyId}";
if (!_cache.TryGetValue(cacheKey, out SurveyAnalyticsSummaryDto? cached))
{
    cached = await ComputeDashboardAsync(surveyId, ct);
    _cache.Set(cacheKey, cached, TimeSpan.FromMinutes(5));
}
return cached!;
```

---

### 15.6 API Layer — `SurveyAnalyticsController`

**File**: `Api/Controllers/SurveyAnalyticsController.cs`

```csharp
[ApiController]
[Route("api/surveys")]
[Authorize(Policy = "AdminOrAbove")]
public class SurveyAnalyticsController : ControllerBase
{
    private readonly ISurveyAnalyticsService _analytics;
    public SurveyAnalyticsController(ISurveyAnalyticsService analytics)
        => _analytics = analytics;

    [HttpGet("{id:int}/analytics/dashboard")]
    public async Task<IActionResult> GetDashboard(int id, CancellationToken ct)
        => Ok(await _analytics.GetDashboardAsync(id, ct));

    [HttpGet("{id:int}/analytics/questions")]
    public async Task<IActionResult> GetQuestionStats(
        int id,
        [FromQuery] string?   department,
        [FromQuery] string?   role,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken ct)
        => Ok(await _analytics.GetQuestionStatsAsync(id, department, role, fromDate, toDate, ct));

    [HttpGet("{id:int}/analytics/questions/{questionId:int}/department-breakdown")]
    public async Task<IActionResult> GetDepartmentBreakdown(int id, int questionId, CancellationToken ct)
        => Ok(await _analytics.GetDepartmentBreakdownAsync(id, questionId, ct));

    [HttpGet("{id:int}/analytics/nps")]
    public async Task<IActionResult> GetNpsReport(int id, CancellationToken ct)
        => Ok(await _analytics.GetNpsReportAsync(id, ct));

    // ?surveyIds=1,2,3  (comma-separated)
    [HttpGet("analytics/nps-trend")]
    public async Task<IActionResult> GetNpsTrend(
        [FromQuery] string surveyIds, CancellationToken ct)
    {
        var ids = surveyIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var n) ? n : 0)
            .Where(n => n > 0)
            .ToList();
        return Ok(await _analytics.GetNpsTrendAsync(ids, ct));
    }

    [HttpGet("{id:int}/analytics/funnel")]
    public async Task<IActionResult> GetParticipationFunnel(int id, CancellationToken ct)
        => Ok(await _analytics.GetParticipationFunnelAsync(id, ct));

    [HttpGet("{id:int}/analytics/heatmap")]
    public async Task<IActionResult> GetHeatmap(int id, CancellationToken ct)
        => Ok(await _analytics.GetHeatmapAsync(id, ct));

    // ?a=1&b=2
    [HttpGet("analytics/compare")]
    public async Task<IActionResult> CompareSurveys(
        [FromQuery] int a, [FromQuery] int b, CancellationToken ct)
        => Ok(await _analytics.CompareSurveysAsync(a, b, ct));

    [HttpGet("{id:int}/export")]
    public async Task<IActionResult> Export(
        int id,
        [FromQuery] ExportFormat format,
        [FromQuery] bool         includeRespondentInfo = false,
        [FromQuery] DateTime?    fromDate = null,
        [FromQuery] DateTime?    toDate   = null,
        CancellationToken        ct       = default)
    {
        var request = new SurveyExportRequest(id, format, includeRespondentInfo, fromDate, toDate);

        if (format == ExportFormat.Csv)
        {
            var (data, fileName) = await _analytics.ExportToCsvAsync(request, ct);
            return File(data, "text/csv", fileName);
        }
        else
        {
            var (data, fileName) = await _analytics.ExportToPdfAsync(request, ct);
            return File(data, "application/pdf", fileName);
        }
    }
}
```

**Security notes**:
- All endpoints are under `[Authorize(Policy = "AdminOrAbove")]` — no authenticated employee can access raw survey data of other employees.
- `surveyIds` query string parsed defensively: non-integer segments are silently dropped, capped at 10 survey IDs for the trend endpoint to prevent abuse.
- `includeRespondentInfo` is ignored (overridden to `false`) at the service layer when `Survey.IsAnonymous == true`. The controller never enforces this — it is a **backend invariant** in `SurveyAnalyticsService`.
- Export endpoints return `Content-Disposition: attachment` via `File(...)` return — browser triggers a download, not inline rendering. This prevents XSS via PDF/CSV content injection.

**API Endpoint Summary**:

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/api/surveys/{id}/analytics/dashboard` | Admin/SuperAdmin | KPI cards, response rate, health |
| `GET` | `/api/surveys/{id}/analytics/questions` | Admin/SuperAdmin | Per-question stats, optional filters |
| `GET` | `/api/surveys/{id}/analytics/questions/{qId}/department-breakdown` | Admin/SuperAdmin | Avg per dept for one question |
| `GET` | `/api/surveys/{id}/analytics/nps` | Admin/SuperAdmin | NPS score + breakdown |
| `GET` | `/api/surveys/analytics/nps-trend?surveyIds=1,2,3` | Admin/SuperAdmin | NPS over time across surveys |
| `GET` | `/api/surveys/{id}/analytics/funnel` | Admin/SuperAdmin | Participation funnel |
| `GET` | `/api/surveys/{id}/analytics/heatmap` | Admin/SuperAdmin | Dept × Question rating matrix |
| `GET` | `/api/surveys/analytics/compare?a=1&b=2` | Admin/SuperAdmin | Side-by-side survey comparison |
| `GET` | `/api/surveys/{id}/export?format=csv` | Admin/SuperAdmin | CSV download |
| `GET` | `/api/surveys/{id}/export?format=pdf` | Admin/SuperAdmin | PDF summary download |

---

### 15.7 Frontend — Analytics Tab & Pages

#### 15.7.1 Folder Additions

```
frontend/src/features/surveys/
  api/
    surveyAnalyticsApi.ts          ← All analytics + export endpoints
  tabs/
    AnalyticsTab.tsx               ← NEW — 4th tab in SurveyBuilderPage
  components/
    NpsGauge.tsx                   ← Recharts RadialBarChart -100…+100 gauge
    SurveyHeatmapView.tsx          ← MUI Table with colour-coded rating cells
    SurveyFunnelChart.tsx          ← Horizontal step chart (Invited→Sent→Started→Submitted)
    QuestionStatsCard.tsx          ← Reusable per-question bar chart card
    ExportButtons.tsx              ← CSV + PDF download buttons with loading state
  SurveyComparePage.tsx            ← NEW — standalone page /admin/surveys/compare
```

#### 15.7.2 `surveyAnalyticsApi.ts`

```typescript
import { apiClient } from '@/shared/api/apiClient';

export const surveyAnalyticsApi = {
  getDashboard: (surveyId: number) =>
    apiClient.get<SurveyAnalyticsSummaryDto>(`/surveys/${surveyId}/analytics/dashboard`),

  getQuestionStats: (surveyId: number, filters?: QuestionStatsFilters) =>
    apiClient.get<SurveyQuestionAnalyticsDto[]>(
      `/surveys/${surveyId}/analytics/questions`,
      { params: filters }
    ),

  getDepartmentBreakdown: (surveyId: number, questionId: number) =>
    apiClient.get<SurveyDepartmentBreakdownDto>(
      `/surveys/${surveyId}/analytics/questions/${questionId}/department-breakdown`
    ),

  getNpsReport: (surveyId: number) =>
    apiClient.get<SurveyNpsReportDto>(`/surveys/${surveyId}/analytics/nps`),

  getNpsTrend: (surveyIds: number[]) =>
    apiClient.get<SurveyNpsTrendDto>(`/surveys/analytics/nps-trend`, {
      params: { surveyIds: surveyIds.join(',') },
    }),

  getParticipationFunnel: (surveyId: number) =>
    apiClient.get<SurveyParticipationFunnelDto>(`/surveys/${surveyId}/analytics/funnel`),

  getHeatmap: (surveyId: number) =>
    apiClient.get<SurveyHeatmapDto>(`/surveys/${surveyId}/analytics/heatmap`),

  compareSurveys: (a: number, b: number) =>
    apiClient.get<SurveyComparisonDto>(`/surveys/analytics/compare`, { params: { a, b } }),

  exportCsv: async (surveyId: number, includeRespondentInfo = false): Promise<void> => {
    const resp = await apiClient.get(`/surveys/${surveyId}/export`, {
      params: { format: 'csv', includeRespondentInfo },
      responseType: 'blob',
    });
    triggerDownload(resp.data, `survey-${surveyId}-responses.csv`, 'text/csv');
  },

  exportPdf: async (surveyId: number): Promise<void> => {
    const resp = await apiClient.get(`/surveys/${surveyId}/export`, {
      params: { format: 'pdf' },
      responseType: 'blob',
    });
    triggerDownload(resp.data, `survey-${surveyId}-report.pdf`, 'application/pdf');
  },
};

// Helper — triggers browser file download from a Blob
function triggerDownload(data: Blob, fileName: string, mimeType: string): void {
  const url = URL.createObjectURL(new Blob([data], { type: mimeType }));
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}
```

#### 15.7.3 `AnalyticsTab.tsx` Layout

The `AnalyticsTab` is added as the 4th tab in `SurveyBuilderPage.tsx` (visible only when `survey.status !== 'Draft'`).

```
┌─────────────────────────────────────────────────────────┐
│  SURVEY ANALYTICS                     [Export CSV] [Export PDF]  │
├──────────┬──────────┬──────────┬──────────────────────────┤
│ Response │  Total   │  Total   │  Health          NPS       │
│ Rate     │ Invited  │ Submitted│  [AtRisk badge]  [42]      │
│  58%     │  120     │   70     │                           │
├──────────┴──────────┴──────────┴──────────────────────────┤
│  [Department Filter ▼]  [Role Filter ▼]  [Date Range]    │
├─────────────────────────────────────────────────────────┤
│  PARTICIPATION FUNNEL                                    │
│  Invited(120) → Sent(118) → Opened(95) → Submitted(70)  │
├─────────────────────────────────────────────────────────┤
│  NPS BREAKDOWN  (shown if NPS question exists)          │
│  [Gauge: 42]  Promoters 45%  Passives 30%  Detractors 25%│
├─────────────────────────────────────────────────────────┤
│  PER-QUESTION STATISTICS                                 │
│  ▼ Q1: How satisfied are you with... [bar chart]        │
│  ▼ Q2: Rate your manager (NPS)       [bar chart]        │
│  ▼ Q3: What can we improve?          [text list]        │
├─────────────────────────────────────────────────────────┤
│  DEPARTMENT HEATMAP  (click question bar for breakdown) │
│  [SurveyHeatmapView: Dept × Question colour matrix]     │
└─────────────────────────────────────────────────────────┘
```

**Key implementation details**:
- Use `recharts` (`BarChart`, `RadialBarChart`, `LineChart`) — already used in existing assessment analytics.
- `NpsGauge.tsx` uses `recharts` `RadialBarChart` forced into a semicircle; score label rendered in the centre.
- `SurveyHeatmapView.tsx` uses a MUI `Table` where each cell background is interpolated between `theme.palette.error.light` (low) and `theme.palette.success.light` (high) using a linear colour scale.
- `SurveyFunnelChart.tsx` uses `recharts` `FunnelChart` with the four stages.
- Department filter and Role filter are `MUI Autocomplete` fields populated from existing `/api/users/departments` endpoint (or extracted from returned analytics data).
- Export buttons call `surveyAnalyticsApi.exportCsv` / `exportPdf` and show `CircularProgress` while the blob is downloading.

#### 15.7.4 `SurveyComparePage.tsx`

Route: `/admin/surveys/compare`

- Two `MUI Autocomplete` pickers (each pulls from `/api/surveys` list).
- On both surveys selected → calls `surveyAnalyticsApi.compareSurveys(a, b)`.
- Renders a `MUI Table` with columns for Survey A and Survey B, rows for each shared question.
- Below: side-by-side response rate bars.
- Button: "View NPS Trend for These Surveys" → navigates to a trend chart using `surveyAnalyticsApi.getNpsTrend([a, b])`.

#### 15.7.5 Route Addition

```typescript
// In routes.tsx — add alongside existing survey routes:
{
  path: '/admin/surveys/compare',
  element: (
    <PrivateRoute roles={['Admin', 'SuperAdmin']}>
      <SurveyComparePage />
    </PrivateRoute>
  ),
}
```

---

### 15.8 Security Considerations

| Threat | Mitigation |
|---|---|
| **PII exposure on anonymous surveys** | `SurveyAnalyticsService` always checks `Survey.IsAnonymous` before including any `userId`, `respondentName`, or `respondentEmail` in results. This is an invariant in the service layer — the controller flag is advisory only. |
| **IDOR on analytics endpoints** | Every analytics method loads the `Survey` entity first and calls `_currentUser.IsAdminOrAbove()`. A regular employee hitting `/api/surveys/{id}/analytics/dashboard` is rejected by the `[Authorize(Policy = "AdminOrAbove")]` policy before the service is called. |
| **CSV injection** (formula injection) | `CsvHelper` writes values as strings; any cell value starting with `=`, `+`, `-`, `@` is prefixed with a tab character (`\t`) before writing to prevent formula injection in Excel/LibreOffice. |
| **PDF content injection** | QuestPDF renders text as static document content (not HTML parsing), eliminating CSS/HTML injection risk. |
| **Large export DoS** | Export endpoints check `Survey.TotalResponded > 5000` and return `202 Accepted` with a `jobId` instead of blocking; a background `ExportJob` writes to storage and notifies admin via email. |
| **NPS trend abuse** | `surveyIds` parameter is parsed and capped at 10 entries; each `id` is validated as a positive integer before the DB query runs. |
| **Enumeration via compare endpoint** | `CompareSurveysAsync` validates that both `surveyIdA` and `surveyIdB` exist and the calling user has `AdminOrAbove`; throws `NotFoundException` (with a generic message) rather than distinguishing "not found" vs. "forbidden". |

---

### 15.9 Tests — `SurveyAnalyticsServiceTests.cs`

All tests use in-memory EF Core with hand-rolled seed helpers (matching existing test patterns in `KnowHub.Tests/`).

| Test | Scenario |
|---|---|
| `GetDashboardAsync_50PercentResponseRate_Returns_AtRiskHealth` | 10 invited, 5 submitted → ResponseRatePct=50, Health="AtRisk" |
| `GetDashboardAsync_70PercentResponseRate_Returns_HealthyStatus` | 10 invited, 7 submitted → Health="Healthy" |
| `GetQuestionStatsAsync_RatingQuestion_Returns_AvgAndDistribution` | 5 answers (8,9,9,10,7) → avg=8.6, option dist correct |
| `GetQuestionStatsAsync_DepartmentFilter_Returns_OnlyFilteredDeptAnswers` | Dept "Engineering" only — other dept answers excluded |
| `GetNpsReportAsync_MixedRatings_Returns_CorrectNpsScore` | 4 promoters, 2 passives, 4 detractors out of 10 → NPS = 0 |
| `GetNpsReportAsync_AllPromoters_Returns_NpsScore100` | 10 promoters → NPS = 100 |
| `GetNpsReportAsync_NoNpsQuestion_Throws_BusinessRuleException` | Survey has no 0–10 Rating question |
| `GetParticipationFunnelAsync_Returns_CorrectStageCounts` | 10 invited, 9 sent, 7 accessed, 5 submitted |
| `GetHeatmapAsync_TwoDepts_TwoRatingQuestions_Returns_CorrectMatrix` | [Eng][Q1]=8.5, [HR][Q1]=7.0, etc. |
| `GetHeatmapAsync_NonRatingQuestions_Excluded` | Text-type questions must not appear in heatmap |
| `CompareSurveysAsync_SharedQuestionByText_Returns_MatchedQuestion` | Both surveys have "How satisfied are you" → shared |
| `CompareSurveysAsync_NoSharedQuestions_Returns_EmptySharedList` | Different question texts → SharedQuestions.Count == 0 |
| `ExportToCsvAsync_AnonymousSurvey_DoesNotIncludeRespondentColumns` | Even with includeRespondentInfo=true, no PII in output |
| `ExportToCsvAsync_NonAnonymous_IncludeRespondents_ContainsPiiColumns` | RespondentName, RespondentEmail present |
| `ExportToCsvAsync_CsvInjection_ValuesAreSanitised` | Answer starting with "=" is prefixed with \t |
| `GetNpsTrendAsync_MultipleSurveys_Returns_ChronologicalOrder` | Ordered by LaunchedAt ascending |
| `GetDepartmentBreakdownAsync_RatingQuestion_Returns_AvgPerDept` | Validated against manually computed averages |

---

### 15.10 Updated Implementation Sequence

The original 23 steps from Section 13 are unchanged. Add the following steps after completing step 23:

| Step | Task | Layer |
|---|---|---|
| 24 | Add `012_SurveyAnalytics.sql` migration: `TokenAccessedAt` column + materialized view + indexes | Database |
| 25 | Update `SurveyInvitation` entity: add `TokenAccessedAt DateTime?` property; set in `SurveyResponseService.GetFormByTokenAsync` | Domain / Infrastructure |
| 26 | Install `CsvHelper` and `QuestPDF` NuGet packages in `KnowHub.Infrastructure.csproj` | Infrastructure |
| 27 | Add analytics DTOs to `Application/Models/Surveys/Analytics/` (all records from §15.4.1) | Application |
| 28 | Add `ExportFormat` enum to `Application/Enums/` | Application |
| 29 | Write `ISurveyAnalyticsService` interface | Application |
| 30 | Implement `SurveyAnalyticsService` — start with `GetDashboardAsync`, `GetQuestionStatsAsync`, `GetNpsReportAsync` | Infrastructure |
| 31 | Implement remaining analytics methods: heatmap, funnel, comparison, NPS trend | Infrastructure |
| 32 | Implement `ExportToCsvAsync` (CsvHelper) and `ExportToPdfAsync` (QuestPDF) | Infrastructure |
| 33 | Register `ISurveyAnalyticsService` / `SurveyAnalyticsService` in `ServiceCollectionExtensions.cs` | Infrastructure |
| 34 | Write `SurveyAnalyticsController` with all endpoints | API |
| 35 | Write `SurveyAnalyticsServiceTests.cs` (all 17 test cases from §15.9) | Tests |
| 36 | Write `surveyAnalyticsApi.ts` including `triggerDownload` helper | Frontend |
| 37 | Build reusable components: `NpsGauge.tsx`, `SurveyHeatmapView.tsx`, `SurveyFunnelChart.tsx`, `QuestionStatsCard.tsx`, `ExportButtons.tsx` | Frontend |
| 38 | Build `AnalyticsTab.tsx` and add it as 4th tab in `SurveyBuilderPage.tsx` (status !== Draft guard) | Frontend |
| 39 | Build `SurveyComparePage.tsx` and add route `/admin/surveys/compare` to `routes.tsx` | Frontend |

---

*End of Survey Module Plan (v3 — Analytics & Reporting Module Added)*
