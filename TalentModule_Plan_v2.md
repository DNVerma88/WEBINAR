# Talent Module — Implementation Plan v2.0
## Updated: Cloud Storage Integration + Multi-File Upload + Bulk Screening

> **Status**: Planning Complete — Ready for Implementation  
> **Supersedes**: Original Talent Module plan from previous session  
> **Scope**: Feature A — Resume Builder | Feature B — AI Resume Screener with Cloud Storage

---

## 1. What's New in v2 (Delta from v1)

| Area | v1 Plan | v2 Change |
|---|---|---|
| Resume upload | Single file upload (local only) | **Multi-file drag-drop + Cloud Storage picker** (OneDrive, SharePoint, S3, Azure Blob) |
| JD input | Text paste only | **Text + file upload + cloud storage file reference** |
| Processing | Synchronous per-file | **Async bulk job queue with real-time SignalR progress** |
| Storage | Local temp only | **Pluggable `IStorageProvider` adapter pattern** (Local, AzureBlob, S3, SharePoint/OneDrive) |
| Screening job | One-shot | **Batch job with per-candidate progress, retry failed, partial results** |
| Frontend | Single upload page | **Cloud file picker modal, drag-drop zone, live progress dashboard** |

---

## 2. Problem Statement (Updated)

HR teams typically store resumes in **SharePoint document libraries** or **OneDrive folders** — not on local disks. They may receive 50–200 resumes for a single role. The v1 plan required downloading and re-uploading every file manually.

### Core User Stories

| # | As a... | I want to... | So that... |
|---|---|---|---|
| US-1 | HR Manager | Create a screening job and paste/upload the JD | AI can understand role requirements |
| US-2 | HR Manager | Pick JD from my SharePoint/OneDrive | I don't have to download-then-upload |
| US-3 | HR Manager | Select multiple resumes from a SharePoint folder | I can screen 50+ candidates without manual downloads |
| US-4 | HR Manager | Drop multiple PDF/DOCX files onto the screen | I can also upload local resumes |
| US-5 | HR Manager | See real-time progress as each resume is screened | I know the job is running |
| US-6 | HR Manager | View ranked results with score breakdown | I can shortlist the right candidates |
| US-7 | HR Manager | Export results to CSV | I can share findings with managers |
| US-8 | Admin | Configure which storage provider is active | Company can use whichever storage they have |

---

## 3. Architecture Overview

### 3.1 Backend Layers

```
KnowHub.Domain
  └── Entities/Talent/
        ├── ScreeningJob.cs          (job per JD, with status + progress %)
        ├── ScreeningCandidate.cs    (one per resume, with scores + storage ref)
        └── ResumeProfile.cs        (resume builder profile per user)

KnowHub.Application
  └── Contracts/Talent/
        ├── IResumeBuilderService.cs
        ├── IResumeScreenerService.cs
        └── IStorageProvider.cs      ← NEW pluggable adapter

KnowHub.Infrastructure
  ├── Services/Talent/
  │     ├── ResumeBuilderService.cs
  │     ├── ResumeScreenerService.cs
  │     ├── ResumeGenerator.cs       (QuestPDF + OpenXml)
  │     └── ResumeScorer.cs          (3-layer AI scoring)
  ├── Storage/            ← NEW storage layer
  │     ├── LocalStorageProvider.cs
  │     ├── AzureBlobStorageProvider.cs
  │     ├── AwsS3StorageProvider.cs
  │     └── SharePointOneDriveStorageProvider.cs
  └── BackgroundServices/
        └── BulkScreeningBackgroundService.cs   ← NEW

KnowHub.Api
  ├── Controllers/
  │     ├── ResumeBuilderController.cs
  │     ├── ResumeScreenerController.cs
  │     └── StorageController.cs    ← NEW (presigned URLs, file listing)
  └── Hubs/
        └── NotificationHub.cs     ← REUSE EXISTING (add ReceiveScreeningProgress)
```

### 3.2 Frontend Structure

```
frontend/src/features/talent/
  ├── types.ts
  ├── talentApi.ts
  ├── storageApi.ts                    ← NEW
  ├── hooks/
  │     ├── useStoragePicker.ts        ← NEW
  │     └── useScreeningProgress.ts   ← NEW
  ├── resume-builder/
  │     ├── ResumeBuilderPage.tsx
  │     └── sections/...
  └── screening/
        ├── ScreeningListPage.tsx
        ├── CreateScreeningDialog.tsx
        ├── JdInputPanel.tsx           ← NEW (text/upload/cloud)
        ├── ResumeSourcePanel.tsx      ← NEW (drag-drop + cloud pickers)
        ├── CloudFilePicker/
        │     ├── OneDrivePickerModal.tsx    ← NEW
        │     ├── S3BrowserModal.tsx         ← NEW
        │     └── AzureBlobBrowserModal.tsx  ← NEW
        ├── BulkProgressPanel.tsx      ← NEW (real-time SignalR)
        ├── ScreeningDetailPage.tsx
        └── CandidateResultCard.tsx
```

---

## 4. Cloud Storage Integration Design

### 4.1 Pluggable Storage Provider (Backend)

#### Interface — `IStorageProvider.cs`
```csharp
// Application layer — no external dependencies
public interface IStorageProvider
{
    string ProviderType { get; }  // "Local" | "AzureBlob" | "S3" | "OneDrive" | "SharePoint"

    /// Download file content from this storage into a Stream
    Task<StorageDownloadResult> DownloadFileAsync(StorageFileReference fileRef, CancellationToken ct);

    /// List files in a folder/container/prefix
    Task<IEnumerable<StorageFileItem>> ListFilesAsync(string path, CancellationToken ct);
}

public record StorageFileReference(
    string ProviderType,    // "OneDrive" | "S3" | "AzureBlob" | "SharePoint" | "Local"
    string FileId,          // driveId+itemId for Graph, key for S3/Blob, temp path for local
    string FileName,
    long FileSizeBytes,
    string? ContainerOrDrive = null,   // S3 bucket / Azure container / Graph driveId
    string? AccessToken = null         // delegated token for Graph provider
);

public record StorageDownloadResult(Stream Content, string ContentType, string FileName);
public record StorageFileItem(string FileId, string FileName, long SizeBytes, string MimeType, DateTimeOffset LastModified);
```

#### Storage Configuration — `StorageConfiguration.cs`
```csharp
public class StorageConfiguration
{
    public string DefaultProvider { get; set; } = "Local";

    public AzureBlobConfig? AzureBlob { get; set; }
    public AwsS3Config? AwsS3 { get; set; }
    public MicrosoftGraphConfig? MicrosoftGraph { get; set; }
    public LocalStorageConfig Local { get; set; } = new();
}

public class AzureBlobConfig
{
    public string ConnectionString { get; set; } = "";
    public string ContainerName { get; set; } = "resumes";
}

public class AwsS3Config
{
    public string BucketName { get; set; } = "";
    public string Region { get; set; } = "us-east-1";
    // Credentials from environment / IAM role (not in config file)
}

public class MicrosoftGraphConfig
{
    public string TenantId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    // For delegated flow, token is passed per-request from frontend
}

public class LocalStorageConfig
{
    public string UploadPath { get; set; } = "uploads/resumes";
}
```

### 4.2 Provider Implementations

#### Local Storage
- Saves multipart form uploads to `wwwroot/uploads/resumes/{tenantId}/{jobId}/`
- Returns relative path as `FileId`
- Download: `File.OpenRead(path)`

#### Azure Blob Storage (`Azure.Storage.Blobs` NuGet)
```csharp
public class AzureBlobStorageProvider : IStorageProvider
{
    public string ProviderType => "AzureBlob";

    // List: BlobContainerClient.GetBlobsAsync(prefix: path)
    // Download: BlobClient.DownloadStreamingAsync()
    // Security: Uses connection string from config (not exposed to frontend)
    // SAS tokens: Generated server-side for temporary read access only
}
```

#### AWS S3 (`AWSSDK.S3` NuGet — AWS SDK already in project)
```csharp
public class AwsS3StorageProvider : IStorageProvider
{
    public string ProviderType => "S3";

    // List: IAmazonS3.ListObjectsV2Async(bucket, prefix)
    // Download: IAmazonS3.GetObjectAsync(bucket, key) → stream
    // Presigned URLs: NOT exposed to frontend — download happens server-side
    // Security: IAM role or environment credentials (never in appsettings)
}
```

#### SharePoint / OneDrive (Microsoft Graph `Microsoft.Graph` NuGet)
```csharp
public class SharePointOneDriveStorageProvider : IStorageProvider
{
    public string ProviderType => "OneDrive"; // also handles "SharePoint"

    // Uses delegated access token passed from frontend (user's own M365 token)
    // List: GET /drives/{driveId}/items/{itemId}/children
    // Download: GET /drives/{driveId}/items/{itemId}/content
    // Security: User's own M365 permissions govern what they can access
    //           Token validation: verify aud claim for Graph
}
```

### 4.3 Frontend: OneDrive/SharePoint File Picker v8

The **Microsoft File Picker v8** is a hosted control (page at `{tenantUrl}/_layouts/15/FilePicker.aspx`) that communicates via `postMessage`. It uses the **same UI as M365** and respects all existing SharePoint permissions.

#### Authentication Flow
1. User clicks "Pick from OneDrive/SharePoint" button
2. Frontend launches **MSAL popup** (`@azure/msal-browser`)
3. Acquires token with scope `MyFiles.Read` / `AllSites.Read`
4. Opens picker in **popup window** via form POST with token
5. Picker sends **`pick` command** via postMessage when user selects files
6. Frontend receives: `{ id, parentReference.driveId, "@sharePoint.endpoint", name, size }`
7. Frontend sends `StorageFileReference[]` to backend (with access token)
8. Backend `SharePointOneDriveStorageProvider` downloads the file using Graph API

```typescript
// useOneDrivePicker.ts — simplified flow
export function useOneDrivePicker(tenantUrl: string) {
  const [selectedFiles, setSelectedFiles] = useState<StorageFileRef[]>([]);

  const openPicker = async () => {
    const token = await acquireMsalToken([`${tenantUrl}/.default`]);
    const channelId = crypto.randomUUID();
    const options = {
      sdk: "8.0",
      entry: { oneDrive: {} },  // or sharePoint: { byPath: { web: siteUrl } }
      authentication: {},
      messaging: { origin: window.location.origin, channelId },
      typesAndSources: {
        filters: ["pdf", "docx"],  // restrict to resume file types
        mode: "multiple"           // allow multi-select
      }
    };

    const win = window.open("", "Picker", "width=1080,height=680");
    // ... form POST to FilePicker.aspx, set up MessageChannel
    // ... on 'pick' command: setSelectedFiles(items.map(mapToRef))
  };

  return { openPicker, selectedFiles };
}
```

### 4.4 Frontend: S3 / Azure Blob Browser

Since S3 and Azure Blob don't have a native file picker UI, we provide a **backend-mediated browser**:

1. Frontend calls `GET /api/talent/storage/list?provider=S3&path=resumes/` 
2. Backend calls `IStorageProvider.ListFilesAsync()` with server-side credentials
3. Frontend renders a file tree/grid dialog
4. User selects files → frontend sends `StorageFileReference[]` to create-screening endpoint
5. Backend downloads files during screening via `IStorageProvider.DownloadFileAsync()`

**Security Note**: S3/Blob credentials are **never** sent to the frontend. All enumeration and download happens server-side. The frontend only receives file metadata (name, size, last-modified).

### 4.5 Frontend: Local Drag-and-Drop Upload (Always Available)

- MUI-based drop zone (custom component, no extra lib needed)
- Supports `application/pdf` and `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
- Max 10MB per file, max 100 files per job
- Files chunked to `POST /api/talent/screening/{jobId}/upload` as `multipart/form-data`
- Per-file `XMLHttpRequest` (or `axios` with `onUploadProgress`) shows individual progress

---

## 5. JD Input Design

The Job Description can be provided three ways. Only **one** is required per screening job.

| Mode | How It Works | Use Case |
|---|---|---|
| **Text Paste** | `<TextField multiline>` — HR pastes JD text | Quick screening, JD already in clipboard |
| **File Upload** | `<input type="file">` — PDF or DOCX JD | Local JD file |
| **Cloud File Ref** | Opens OneDrive/SharePoint/S3/Blob picker | JD stored in company SharePoint |

Backend handling:
- If `JdText` is provided → use directly
- If `JdFileReference` is provided → download at job-start via `IStorageProvider`, extract text via `PdfPig` or `OpenXml`, cache as `JdText` on the `ScreeningJob`
- `JdText` is stored on `ScreeningJob` (VARCHAR max) after extraction

---

## 6. Bulk Screening Engine

### 6.1 Job Lifecycle

```
CreateScreeningJob (POST)
  → ScreeningJob created (Status=Pending)
  → Resume refs added (Status=Queued per candidate)
  → StartScreening (POST) triggered
      → Status=Processing, ProgressPercent=0
      → For each candidate (parallel, max 5 concurrent):
            → Download resume from storage
            → Extract text (PdfPig / OpenXml)
            → Layer 1: Embedding cosine similarity (OpenAI text-embedding-3-small)
            → Layer 2: Skills depth scoring (GPT-4o-mini JSON mode)
            → Layer 3: Legitimacy check (GPT-4o-mini)
            → Update ScreeningCandidate: scores, summary, recommendation
            → SignalR push: ReceiveScreeningProgress({ jobId, processed, total, candidate })
      → All done: Status=Completed, ProgressPercent=100
      → SignalR push: ScreeningJobCompleted({ jobId })
```

### 6.2 Concurrency Control

```csharp
// Max 5 resumes in parallel to respect OpenAI rate limits
private static readonly SemaphoreSlim _semaphore = new(5, 5);

foreach (var candidate in candidates)
{
    await _semaphore.WaitAsync(cancellationToken);
    _ = Task.Run(async () =>
    {
        try { await ScoreResumeAsync(job, candidate, cancellationToken); }
        finally { _semaphore.Release(); }
    }, cancellationToken);
}
```

### 6.3 Background Service vs In-Process

Because the project already has `WeeklyDigestBackgroundService` and no Hangfire, we use:
- **`IHostedService` + `Channel<ScreeningJobId>`** (`System.Threading.Channels`) — zero new dependencies, integrates cleanly with existing DI
- `BulkScreeningBackgroundService : BackgroundService` reads from the channel
- API controller enqueues a job ID into the channel after `StartScreening` is called
- This is simpler than Hangfire and fits the existing codebase pattern

```csharp
// Application layer
public interface IScreeningJobQueue
{
    void Enqueue(Guid jobId);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct);
}

// Infrastructure layer
public sealed class ScreeningJobQueue : IScreeningJobQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();
    // Enqueue: _channel.Writer.TryWrite(jobId)
    // DequeueAllAsync: _channel.Reader.ReadAllAsync(ct)
}
```

### 6.4 SignalR Progress Events

New methods added to the **existing `NotificationHub`** (no new hub needed):

```csharp
// Client receives these on the existing /hubs/notifications connection
context.Clients.User(userId).SendAsync("ReceiveScreeningProgress", new {
    jobId,
    processed,         // 0..total
    total,
    percentComplete,   // 0..100
    latestCandidate = new { candidateId, fileName, overallScore, recommendation }
});

context.Clients.User(userId).SendAsync("ScreeningJobCompleted", new { jobId });
context.Clients.User(userId).SendAsync("ScreeningJobFailed", new { jobId, error });
```

---

## 7. Updated Domain Entities

### 7.1 `ScreeningJob.cs`

```csharp
public class ScreeningJob
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string JobTitle { get; set; } = "";      // Role being screened for
    public string? JdText { get; set; }              // Full extracted/pasted JD text
    public string? JdFileReference { get; set; }    // JSON of StorageFileReference (if picked from storage)
    public ScreeningJobStatus Status { get; set; }  // Pending | Processing | Completed | Failed | Cancelled
    public int TotalCandidates { get; set; }
    public int ProcessedCandidates { get; set; }
    public int ProgressPercent { get; set; }        // 0–100
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<ScreeningCandidate> Candidates { get; set; } = [];
}
```

### 7.2 `ScreeningCandidate.cs`

```csharp
public class ScreeningCandidate
{
    public Guid Id { get; set; }
    public Guid ScreeningJobId { get; set; }
    public string FileName { get; set; } = "";
    public string StorageProviderType { get; set; } = "Local";  // "Local"|"S3"|"AzureBlob"|"OneDrive"|"SharePoint"
    public string FileReference { get; set; } = "";              // JSON of StorageFileReference
    public CandidateStatus Status { get; set; }  // Queued | Processing | Scored | Failed
    public string? ErrorMessage { get; set; }

    // Extracted data
    public string? ExtractedText { get; set; }    // Full resume text (not stored if > 50KB — trimmed)
    public string? CandidateName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    // Scores (populated after AI scoring)
    public decimal? SemanticSimilarityScore { get; set; }  // 0.00–1.00 (cosine sim)
    public decimal? SkillsDepthScore { get; set; }          // 0–100 (GPT evaluation)
    public decimal? LegitimacyScore { get; set; }            // 0–100 (red flag check)
    public decimal? OverallScore { get; set; }               // Weighted composite
    public string? Recommendation { get; set; }              // "StrongFit" | "GoodFit" | "MaybeFit" | "NoFit"
    public string? ScoreSummary { get; set; }               // 2-3 sentence AI explanation
    public string? SkillsMatched { get; set; }              // JSON array of matched skills
    public string? SkillsGap { get; set; }                  // JSON array of missing skills
    public string? RedFlags { get; set; }                    // JSON array of legitimacy concerns

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ScoredAt { get; set; }

    public ScreeningJob ScreeningJob { get; set; } = null!;
}
```

### 7.3 Enums

```csharp
public enum ScreeningJobStatus { Pending, Processing, Completed, Failed, Cancelled }
public enum CandidateStatus { Queued, Processing, Scored, Failed }
public enum RecommendationLevel { StrongFit, GoodFit, MaybeFit, NoFit }
```

---

## 8. API Endpoints

### Resume Builder

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/talent/resume` | Get authenticated user's resume profile |
| `PUT` | `/api/talent/resume` | Save/update resume profile (JSON body) |
| `GET` | `/api/talent/resume/pdf` | Generate and download PDF (QuestPDF) |
| `GET` | `/api/talent/resume/word` | Generate and download Word .docx (OpenXml) |

### Storage Browser

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/talent/storage/providers` | List configured + available storage providers |
| `GET` | `/api/talent/storage/list` | List files: `?provider=S3&path=resumes/` |
| `POST` | `/api/talent/storage/verify-reference` | Verify a cloud file reference is accessible |

### Resume Screener

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/talent/screening` | Create screening job (JD text or file ref) |
| `GET` | `/api/talent/screening` | List all jobs for tenant (HR/Admin only) |
| `GET` | `/api/talent/screening/{jobId}` | Get job details + candidates list |
| `POST` | `/api/talent/screening/{jobId}/upload` | Upload local resume files (multipart, multi-file) |
| `POST` | `/api/talent/screening/{jobId}/add-from-storage` | Add resumes from cloud storage references |
| `POST` | `/api/talent/screening/{jobId}/start` | Start bulk screening (enqueues background job) |
| `DELETE` | `/api/talent/screening/{jobId}` | Cancel / delete job |
| `GET` | `/api/talent/screening/{jobId}/results` | Get scored results (sorted by OverallScore desc) |
| `GET` | `/api/talent/screening/{jobId}/export-csv` | Export results as CSV download |
| `GET` | `/api/talent/screening/{jobId}/candidates/{candidateId}` | Full detail for one candidate |

---

## 9. Security Design

### OWASP Compliance

| Threat | Mitigation |
|---|---|
| **Path traversal** (LFI) | Uploaded files stored with `Guid.NewGuid()` filename, never user-provided names on disk |
| **Malicious file upload** | Magic byte check: PDF starts with `%PDF`, DOCX is a ZIP with specific entry names; reject mismatches |
| **SSRF via cloud file reference** | Graph API calls: validate `@sharePoint.endpoint` domain against allowed tenant domain (allowlist). S3/Blob: never follow external URLs from user input |
| **Credential exposure** | S3/Blob credentials never sent to frontend. AWS uses IAM roles / env vars. Azure uses Managed Identity in prod |
| **Token leakage** (OneDrive) | Delegated Graph tokens handled in memory only, never logged, never persisted to DB |
| **XSS in resume content** | All AI-generated text stored as plain text; rendered via MUI `<Typography>`, never `dangerouslySetInnerHTML` |
| **Injection** | All DB queries via EF Core parameterised queries only |
| **Oversized files** | Hard limit: 10MB per file, 100 files per job; enforced in controller `[RequestSizeLimit]` |
| **Unauthorised access** | All talent endpoints require `[Authorize]`. Storage list requires `HR` or `Admin` role |
| **Insecure temp file handling** | Temp files written to `Path.GetTempPath()` with GUID name; deleted in `finally` block |

### File Validation Code Pattern

```csharp
// Magic byte validation — called before any processing
private static bool IsValidFileType(Stream fileStream, string fileName)
{
    var ext = Path.GetExtension(fileName).ToLowerInvariant();
    Span<byte> header = stackalloc byte[8];
    fileStream.Read(header);
    fileStream.Position = 0;

    return ext switch
    {
        ".pdf" => header[..4].SequenceEqual("%PDF"u8.ToArray().AsSpan(0, 4)),
        ".docx" => header[..4].SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }),  // ZIP magic
        _ => false
    };
}
```

---

## 10. Database Migration — `009_TalentModule.sql`

```sql
-- =======================================================
-- Migration 009: Talent Module (Resume Builder + Screener)
-- =======================================================

-- Enum types
DO $$ BEGIN
    CREATE TYPE "ScreeningJobStatus" AS ENUM ('Pending', 'Processing', 'Completed', 'Failed', 'Cancelled');
EXCEPTION WHEN duplicate_object THEN null; END $$;

DO $$ BEGIN
    CREATE TYPE "CandidateStatus" AS ENUM ('Queued', 'Processing', 'Scored', 'Failed');
EXCEPTION WHEN duplicate_object THEN null; END $$;

-- Resume Builder profile
CREATE TABLE IF NOT EXISTS "ResumeProfiles" (
    "Id"            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"      UUID NOT NULL,
    "UserId"        UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
    "Template"      VARCHAR(50)  NOT NULL DEFAULT 'Professional',
    "PersonalInfo"  JSONB NOT NULL DEFAULT '{}',
    "Summary"       TEXT,
    "WorkExperience" JSONB NOT NULL DEFAULT '[]',
    "Education"     JSONB NOT NULL DEFAULT '[]',
    "Skills"        JSONB NOT NULL DEFAULT '[]',
    "Certifications" JSONB NOT NULL DEFAULT '[]',
    "Projects"      JSONB NOT NULL DEFAULT '[]',
    "Languages"     JSONB NOT NULL DEFAULT '[]',
    "Publications"  JSONB NOT NULL DEFAULT '[]',
    "CreatedAt"     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt"     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE("TenantId", "UserId")
);
CREATE INDEX IF NOT EXISTS "IX_ResumeProfiles_TenantId_UserId" ON "ResumeProfiles"("TenantId", "UserId");

-- Screening Jobs
CREATE TABLE IF NOT EXISTS "ScreeningJobs" (
    "Id"                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "TenantId"            UUID NOT NULL,
    "CreatedByUserId"     UUID NOT NULL REFERENCES "Users"("Id") ON DELETE RESTRICT,
    "JobTitle"            VARCHAR(300) NOT NULL,
    "JdText"              TEXT,
    "JdFileReference"     JSONB,
    "Status"              "ScreeningJobStatus" NOT NULL DEFAULT 'Pending',
    "TotalCandidates"     INTEGER NOT NULL DEFAULT 0,
    "ProcessedCandidates" INTEGER NOT NULL DEFAULT 0,
    "ProgressPercent"     INTEGER NOT NULL DEFAULT 0,
    "ErrorMessage"        TEXT,
    "CreatedAt"           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "StartedAt"           TIMESTAMPTZ,
    "CompletedAt"         TIMESTAMPTZ
);
CREATE INDEX IF NOT EXISTS "IX_ScreeningJobs_TenantId" ON "ScreeningJobs"("TenantId");
CREATE INDEX IF NOT EXISTS "IX_ScreeningJobs_CreatedByUserId" ON "ScreeningJobs"("CreatedByUserId");
CREATE INDEX IF NOT EXISTS "IX_ScreeningJobs_Status" ON "ScreeningJobs"("Status");

-- Screening Candidates
CREATE TABLE IF NOT EXISTS "ScreeningCandidates" (
    "Id"                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "ScreeningJobId"          UUID NOT NULL REFERENCES "ScreeningJobs"("Id") ON DELETE CASCADE,
    "FileName"                VARCHAR(500) NOT NULL,
    "StorageProviderType"     VARCHAR(50)  NOT NULL DEFAULT 'Local',
    "FileReference"           TEXT NOT NULL,
    "Status"                  "CandidateStatus" NOT NULL DEFAULT 'Queued',
    "ErrorMessage"            TEXT,
    "ExtractedText"           TEXT,
    "CandidateName"           VARCHAR(300),
    "Email"                   VARCHAR(300),
    "Phone"                   VARCHAR(100),
    "SemanticSimilarityScore" DECIMAL(5,4),
    "SkillsDepthScore"        DECIMAL(5,2),
    "LegitimacyScore"         DECIMAL(5,2),
    "OverallScore"            DECIMAL(5,2),
    "Recommendation"          VARCHAR(50),
    "ScoreSummary"            TEXT,
    "SkillsMatched"           JSONB,
    "SkillsGap"               JSONB,
    "RedFlags"                JSONB,
    "CreatedAt"               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ScoredAt"                TIMESTAMPTZ
);
CREATE INDEX IF NOT EXISTS "IX_ScreeningCandidates_JobId" ON "ScreeningCandidates"("ScreeningJobId");
CREATE INDEX IF NOT EXISTS "IX_ScreeningCandidates_JobId_Score" ON "ScreeningCandidates"("ScreeningJobId", "OverallScore" DESC NULLS LAST);
```

---

## 11. AI Scoring — 3-Layer Architecture (Updated)

### Layer 1: Semantic Similarity (OpenAI Embeddings)
```
JD embedding = embed(JdText)                    // text-embedding-3-small
Resume embedding = embed(resumeText[0..8000])   // truncated to token limit
score = cosineSimliarity(jdEmbedding, resumeEmbedding)  // 0.0–1.0
```
- Cached: JD embedding stored on `ScreeningJob` (JSONB) — computed once, reused for all candidates
- Captures **semantic alignment**: "software engineer" vs "developer" both match correctly

### Layer 2 + 3: Skills Depth + Legitimacy (GPT-4o-mini, single call)
```json
// Single GPT call combining Layer 2 and Layer 3
{
  "model": "gpt-4o-mini",
  "response_format": { "type": "json_object" },
  "messages": [
    {
      "role": "system",
      "content": "You are a neutral ATS validator. Respond ONLY with the JSON schema provided. No narrative."
    },
    {
      "role": "user",
      "content": "JD_SKILLS_REQUIRED: {jdSkills}\n\nRESUME_TEXT: {resumeText[0..6000]}\n\nEvaluate and return:\n{\n  \"candidateName\": string | null,\n  \"email\": string | null,\n  \"phone\": string | null,\n  \"skillsDepthScore\": 0-100,\n  \"skillsMatched\": [string],\n  \"skillsGap\": [string],\n  \"legitimacyScore\": 0-100,\n  \"redFlags\": [string],\n  \"scoreSummary\": \"2 sentences max\",\n  \"recommendation\": \"StrongFit|GoodFit|MaybeFit|NoFit\"\n}"
    }
  ]
}
```

### Weighted Overall Score
```
OverallScore = (SemanticSimilarity × 0.35) × 100 + SkillsDepthScore × 0.45 + LegitimacyScore × 0.20
```

### Recommendation Thresholds
| Range | Recommendation |
|---|---|
| ≥ 75 | StrongFit |
| 55–74 | GoodFit |
| 35–54 | MaybeFit |
| < 35 | NoFit |

---

## 12. Frontend: Cloud File Picker Architecture

### OneDrive/SharePoint Picker Component

```typescript
// OneDrivePickerModal.tsx
// Uses File Picker v8 via form POST + postMessage channel
// Required package: @azure/msal-browser

interface OneDrivePickerModalProps {
  open: boolean;
  onClose: () => void;
  onFilesSelected: (files: StorageFileRef[]) => void;
  tenantUrl: string;       // "https://contoso-my.sharepoint.com"
  multiSelect?: boolean;
  fileFilters?: string[];  // ["pdf", "docx"]
}
```

**Flow (MS File Picker v8):**
1. Component creates invisible `<iframe>` or opens popup
2. Forms a `POST` to `{tenantUrl}/_layouts/15/FilePicker.aspx?filePicker={options}`
3. Sets up `window.addEventListener('message')` for initialization
4. Establishes `MessagePort` for subsequent communication
5. Responds to `authenticate` commands by calling MSAL `acquireTokenSilent`
6. On `pick` command: maps selected items to `StorageFileRef[]`, calls `onFilesSelected`, closes picker

### S3 / Azure Blob Browser Component

```typescript
// S3BrowserModal.tsx / AzureBlobBrowserModal.tsx
// Calls backend /api/talent/storage/list to get file listing
// User navigates folder structure and multi-selects files

interface StorageBrowserModalProps {
  open: boolean;
  onClose: () => void;
  onFilesSelected: (files: StorageFileRef[]) => void;
  provider: 'S3' | 'AzureBlob';
  rootPath?: string;
}
```

### ResumeSourcePanel Component

Combines all input methods in a tabbed UI:

```
Tabs:
  [Upload Files]    — MUI drag-drop zone, multi-file, progress bars
  [OneDrive/SP]     — "Browse" button → opens OneDrivePickerModal
  [Amazon S3]       — Folder browser → S3BrowserModal
  [Azure Blob]      — Folder browser → AzureBlobBrowserModal
```

---

## 13. Required NuGet Packages

| Package | Purpose | Layer | License |
|---|---|---|---|
| `QuestPDF` | PDF resume generation | Infrastructure | MIT |
| `DocumentFormat.OpenXml` | Word .docx generation | Infrastructure | MIT (Microsoft) |
| `PdfPig` | PDF text extraction | Infrastructure | Apache-2.0 |
| `Azure.Storage.Blobs` | Azure Blob storage provider | Infrastructure | MIT (Microsoft) |
| `AWSSDK.S3` | AWS S3 storage provider | Infrastructure | Apache-2.0 |
| `Microsoft.Graph` | SharePoint/OneDrive Graph API | Infrastructure | MIT (Microsoft) |
| `Azure.Identity` | Managed Identity / credential chain | Infrastructure | MIT (Microsoft) |

### Install Commands (run in `backend/src/KnowHub.Infrastructure/`)
```powershell
dotnet add package QuestPDF --version 2024.12.5
dotnet add package DocumentFormat.OpenXml --version 3.4.0
dotnet add package PdfPig --version 0.1.9
dotnet add package Azure.Storage.Blobs --version 12.24.0
dotnet add package AWSSDK.S3 --version 3.7.403.2
dotnet add package Microsoft.Graph --version 5.73.0
dotnet add package Azure.Identity --version 1.13.2
```

---

## 14. Required Frontend npm Packages

| Package | Purpose |
|---|---|
| `@azure/msal-browser` | MSAL for OneDrive/SharePoint token acquisition |
| `react-dropzone` | Drag-and-drop file upload zone |

```bash
# Run from frontend/
npm install @azure/msal-browser react-dropzone
npm install --save-dev @types/react-dropzone
```

---

## 15. `appsettings.json` Additions

```json
{
  "Storage": {
    "DefaultProvider": "Local",
    "Local": {
      "UploadPath": "uploads/resumes"
    },
    "AzureBlob": {
      "ConnectionString": "SET_IN_ENV_OR_SECRETS",
      "ContainerName": "resumes"
    },
    "AwsS3": {
      "BucketName": "SET_IN_ENV_OR_SECRETS",
      "Region": "us-east-1"
    },
    "MicrosoftGraph": {
      "TenantId": "SET_IN_ENV_OR_SECRETS",
      "ClientId": "SET_IN_ENV_OR_SECRETS",
      "ClientSecret": "SET_IN_ENV_OR_SECRETS"
    }
  },
  "TalentModule": {
    "MaxFileSizeBytes": 10485760,
    "MaxFilesPerJob": 100,
    "MaxConcurrentScreening": 5,
    "AllowedFileExtensions": [".pdf", ".docx"],
    "AllowedSharePointDomains": ["contoso.sharepoint.com", "contoso-my.sharepoint.com"]
  }
}
```

---

## 16. Implementation Steps — Ordered Sequencing (45 Steps)

### Phase 1: Foundation (Steps 1–8)
1. `009_TalentModule.sql` — Write and test migration SQL
2. Apply migration to dev DB via Docker
3. `ScreeningJobStatus.cs`, `CandidateStatus.cs` — Domain enums
4. `ScreeningJob.cs`, `ScreeningCandidate.cs`, `ResumeProfile.cs` — Domain entities
5. `IStorageProvider.cs`, `StorageFileReference.cs`, `StorageFileItem.cs` — Application interfaces
6. `TalentConfigurations.cs` — EF Core entity type configurations (JSONB mappings, indexes)
7. Register entities in `KnowHubDbContext.cs`
8. `StorageConfiguration.cs`, `TalentModuleConfiguration.cs` — Config binding classes

### Phase 2: Storage Providers (Steps 9–14)
9. Install all 7 NuGet packages
10. `LocalStorageProvider.cs` — Disk-based provider
11. `AzureBlobStorageProvider.cs` — `Azure.Storage.Blobs` implementation
12. `AwsS3StorageProvider.cs` — `AWSSDK.S3` implementation
13. `SharePointOneDriveStorageProvider.cs` — Microsoft Graph implementation (delegated token)
14. `StorageProviderFactory.cs` — Resolves correct `IStorageProvider` by `providerType` string

### Phase 3: Resume Builder (Steps 15–22)
15. `TalentDtos.cs` — All application DTOs (ResumeProfileDto, all section types)
16. `IResumeBuilderService.cs` — Application interface
17. `ResumeValidators.cs` — FluentValidation for resume save request
18. `ResumeTextExtractor.cs` — PdfPig for PDF, OpenXml for DOCX text extraction
19. `ResumeGenerator.cs` — QuestPDF (3 templates: Professional, Modern, Minimal) + OpenXml Word
20. `ResumeBuilderService.cs` — Implements `IResumeBuilderService`, delegates to generator
21. `ResumeBuilderController.cs` — 4 endpoints (`GET/PUT /resume`, `GET /resume/pdf`, `GET /resume/word`)
22. Register `ResumeBuilderService` and `ResumeGenerator` in DI

### Phase 4: Screener Backend (Steps 23–33)
23. `IResumeScreenerService.cs`, `IScreeningJobQueue.cs` — Application interfaces
24. `ScreeningJobDtos.cs` — Create/list/detail/result DTOs
25. `ScreeningValidators.cs` — FluentValidation for create-job request
26. `ScreeningJobQueue.cs` — `Channel<Guid>` queue implementation
27. `ResumeScorer.cs` — 3-layer AI scoring (embeddings + GPT-4o-mini JSON mode)
28. `ResumeScreenerService.cs` — Orchestrates download → extract → score → persist → SignalR push
29. `BulkScreeningBackgroundService.cs` — `BackgroundService` that reads from `IScreeningJobQueue`
30. `StorageController.cs` — `/api/talent/storage/providers`, `/list`, `/verify-reference`
31. `ResumeScreenerController.cs` — All 9 screener endpoints
32. Register all screener services, queue, and background service in DI
33. Update `appsettings.json` and `appsettings.Development.json` with Storage + TalentModule config

### Phase 5: Frontend (Steps 34–44)
34. Install `@azure/msal-browser` and `react-dropzone`
35. `types.ts` — All TypeScript types (StorageFileRef, ScreeningJob, ScreeningCandidate, etc.)
36. `talentApi.ts` — All API calls for resume builder and screener
37. `storageApi.ts` — `listFiles()`, `verifyReference()`, `getProviders()`
38. `useOneDrivePicker.ts` — File Picker v8 hook (popup mode, MSAL token acquisition)
39. `useScreeningProgress.ts` — SignalR hook listening to `ReceiveScreeningProgress` and `ScreeningJobCompleted`
40. `OneDrivePickerModal.tsx` — Full implementation with postMessage channel
41. `S3BrowserModal.tsx` + `AzureBlobBrowserModal.tsx` — Backend-mediated file browser
42. `ResumeSourcePanel.tsx` — Tabbed: Upload | OneDrive | S3 | AzureBlob
43. `JdInputPanel.tsx` — Tabbed: Paste Text | Upload File | Pick from Storage
44. `CreateScreeningDialog.tsx` — Multi-step wizard: Step 1=JD, Step 2=Resumes, Step 3=Review+Start
45. `BulkProgressPanel.tsx` + `ScreeningDetailPage.tsx` + `CandidateResultCard.tsx` + results export

### Phase 6: AppLayout + Routes (included in Step 45)
- Add "Talent" section to AppLayout sidebar:
  ```
  Talent
    ├── Resume Builder    → /talent/resume-builder
    └── Resume Screener   → /talent/screening    [HR/Admin only]
  ```
- Add routes to `routes.tsx`

---

## 17. Test Coverage Plan

### Unit Tests (xUnit, FakeXxx pattern)
| Test Class | Covers |
|---|---|
| `StorageProviderTests.cs` | `LocalStorageProvider`: save/load/list with temp dirs |
| `ResumeScorerTests.cs` | Score weighting logic, recommendation thresholds |
| `ResumeTextExtractorTests.cs` | Magic byte validation, text extraction from sample files |
| `ScreeningJobQueueTests.cs` | Enqueue/dequeue ordering, cancellation |
| `ResumeBuilderServiceTests.cs` | Save/retrieve profile, section validation |
| `ResumeScreenerServiceTests.cs` | Job lifecycle state transitions |
| `ScreeningValidatorTests.cs` | JD min length, max files, required fields |

---

## 18. Data Flow Summary

```
HR Manager → CreateScreeningDialog (Step 1: JD)
  → Option A: Paste text → POST /api/talent/screening body.JdText
  → Option B: Upload file → POST /api/talent/screening body.JdFileRef (local temp)
  → Option C: Pick from OneDrive → MSAL token + File Picker v8 → body.JdFileRef (Graph ref)

HR Manager → CreateScreeningDialog (Step 2: Resumes)
  → Tab "Upload": react-dropzone → POST /api/talent/screening/{id}/upload (multipart)
  → Tab "OneDrive": OneDrivePickerModal → POST /api/talent/screening/{id}/add-from-storage
  → Tab "S3": S3BrowserModal (via GET /api/talent/storage/list?provider=S3) → POST add-from-storage
  → Tab "Azure": AzureBlobBrowserModal → POST add-from-storage

HR Manager → Start Screening
  → POST /api/talent/screening/{id}/start
  → Controller: enqueue jobId into IScreeningJobQueue
  → BulkScreeningBackgroundService picks up jobId
  → For each ScreeningCandidate:
      → IStorageProvider.DownloadFileAsync(fileRef)
      → ResumeTextExtractor.Extract(stream)
      → ResumeScorer.Score(jdText, resumeText)
      → Update ScreeningCandidate in DB
      → IHubContext.SendAsync("ReceiveScreeningProgress", ...)
  → Job complete → SendAsync("ScreeningJobCompleted", ...)

HR Manager ← BulkProgressPanel (SignalR)
  ← Real-time progress bar + latest-scored candidate card
  ← On complete: auto-navigate to results view
```

---

## 19. Configuration Guide (Per Storage Provider)

### SharePoint/OneDrive Setup
1. Register Azure AD app in Entra ID (formerly AAD)
2. Add delegated permissions: `Files.Read.All`, `Sites.Read.All`, `MyFiles.Read`
3. Add redirect URI: `http://localhost:5173` (dev), `https://{prod-domain}` (prod)
4. Set `Storage:MicrosoftGraph:TenantId` and `ClientId` in app settings
5. `ClientSecret` is **not required for delegated flow** — tokens from frontend MSAL

### AWS S3 Setup
1. Create S3 bucket for resumes, enable server-side encryption (SSE-S3 or SSE-KMS)
2. Create IAM role with `s3:GetObject` and `s3:ListBucket` on the specific bucket
3. In dev: set `AWS_ACCESS_KEY_ID` and `AWS_SECRET_ACCESS_KEY` in environment
4. In prod: use EC2/ECS IAM role (no keys in config files)

### Azure Blob Storage Setup
1. Create Storage Account + Container 
2. In dev: `Storage:AzureBlob:ConnectionString` (use connection string or SAS)
3. In prod: Use `Azure.Identity` `DefaultAzureCredential` (Managed Identity — no secrets in config)

---

## 20. Technical Decisions Summary

| Decision | Choice | Reasoning |
|---|---|---|
| PDF generation | QuestPDF | MIT, C#-native, fluent API, no Word required |
| Word generation | DocumentFormat.OpenXml 3.4 | MIT, Microsoft-maintained, no Word required |
| PDF parsing | PdfPig | Apache-2.0, pure .NET, no native deps |
| AI model | GPT-4o-mini (existing) | already configured, cost-effective, JSON mode |
| Embeddings | text-embedding-3-small (existing) | already configured, 1536-dim, fast |
| Storage pattern | `IStorageProvider` adapter | supports all 4 providers without coupling |
| SharePoint picker | File Picker v8 + MSAL | official Microsoft SDK, full M365 UI |
| S3/Blob browser | Backend-mediated list | credentials never exposed to frontend |
| Background jobs | `Channel<Guid>` + `BackgroundService` | zero deps, fits existing pattern |
| Progress push | Existing SignalR `NotificationHub` | no new hub, reuses established WS connection |
| Concurrency limit | `SemaphoreSlim(5)` | respects OpenAI rate limits |

---

*Plan Version: 2.0 | Created: March 19, 2026 | Workspace: c:\Webinar*
