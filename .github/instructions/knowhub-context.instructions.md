---
description: "KnowHub project context — Use when planning, implementing, testing, or reviewing any feature, bug fix, or task in this project. Covers architecture, tech stack, conventions, build commands, testing patterns, and design guidelines."
---

# KnowHub Project Context

## Solution Overview

**Solution file**: `KnowHub.slnx` (root)
**Purpose**: An internal knowledge-sharing and webinar platform that enables employees to propose, schedule, discover, attend, and archive knowledge sessions. It replaces ad-hoc knowledge sharing with a governed, searchable, and gamified platform featuring AI-powered recommendations, multi-step approval workflows, and enterprise integrations (Teams, Zoom, Slack, Outlook).

**Business Context**:
- Employees can propose knowledge sessions (webinars, workshops, demos, panel discussions) and submit them through a governed approval workflow
- Subject matter experts (Contributors / Speakers) build a public profile showcasing their expertise, past sessions, ratings, and followers
- Knowledge Teams review and approve proposals; approved sessions become schedulable events with calendar and meeting-link integrations
- All sessions and their materials (slides, recordings, docs, code) are stored in a searchable Content Repository
- Gamification (badges, leaderboards), Communities, Knowledge Requests, and an AI Engine keep the platform engaging and continuously improving

---

## Phased Roadmap

### Phase 1 — Core Platform (current scope)
- User registration and profile management (basic + contributor profiles)
- Knowledge session proposal submission and multi-step approval workflow
- Approved session scheduling with meeting link and participant limit
- Session discovery: browse by category, tag, department, speaker; keyword search
- Participant registration, waitlist, and pre-reading materials
- Basic notification system (proposal status, session reminders)
- Role-based access control (Employee, Contributor, Manager, KnowledgeTeam, Admin, SuperAdmin)

### Phase 2 — Content, Engagement & Learning
- Knowledge Content Repository (post-session assets: recordings, slides, code, FAQs)
- Session Chapters (timestamped sections within recordings for in-video navigation)
- Knowledge Bundles (curated, themed collections of knowledge assets)
- After-Action Reviews (structured post-session speaker reflection: planned vs. actual, lessons learned)
- Q&A, discussion threads, comments, likes, bookmarks
- Session and speaker ratings and feedback
- Session Quizzes (post-session MCQ/True-False knowledge-retention assessments, auto-graded)
- Gamification expanded: XP Points System (`UserXpEvent` append-only ledger), typed badge categories, monthly leaderboards by dimension, individual performance graphs, learning streak tracking
- Learning Paths (curated ordered sequences of sessions/assets with milestones and completion certificates)
- Learning Path Cohorts (group enrolment; department-assigned or mandatory paths with deadlines)
- Skill Endorsements (session-validated: attendees endorse speaker skills after a session they attended)
- Communities enhanced: Wiki Pages (`CommunityWikiPage`), community-scoped search, SLATES-aligned subscription digest signals
- Knowledge Requests enhanced: XP bounty system, expert auto-suggest notifications, claim-to-draft-proposal flow
- Weekly Digest Emails (personalised: my communities, my tags, my XP progress, leaderboard position)
- Learning streak tracking and milestone notifications
- Mentor/Mentee pairing (skill-overlap matching; Phase 3 adds AI-assisted skill-gap analysis)

### Phase 3 — Intelligence, Scale & Governance
- AI features: transcript indexing and full-text search, AI-driven personalised learning path recommendations, auto-generated session summaries, knowledge-gap detection from quiz and engagement data
- Knowledge Analytics & Audit Dashboard: Knowledge Gap Heatmap (Category × Department), Skill Coverage Report, Content Freshness Report, Learning Funnel (discover → register → attend → rate → quiz), Cohort Completion Rates, Department Engagement Score, Knowledge Retention Score (quiz pass rates over time)
- Admin & Moderation panel: content flagging, user suspension, bulk session operations, full governance controls, ISO 9001-aligned KM reporting
- Enterprise integrations: Teams, Zoom, Slack, Google Meet, Outlook calendar sync, LMS/SCORM/xAPI content import, HR Systems for org-chart and department sync
- Peer Review for Knowledge Assets (community-nominated validators must approve before asset is marked "Verified")
- Advanced Mentor/Mentee Matching with AI-assisted skill-gap analysis and outcome tracking
- LeaderboardSnapshot archiving and historical trend analysis for engagement reporting
- Internal Speaker Marketplace with AI-powered expert routing and real-time availability matching
- Knowledge Request Fulfilment Marketplace (public bounty board with XP rewards, smart expert auto-routing)
- Mobile-responsive PWA with offline reading mode for knowledge assets

> **Architecture rule**: All entities, enums, and APIs must be designed so Phase 2 and Phase 3 are **additions, never refactors**. Phase 1 ships a subset of a complete domain model.

---

## Architecture — Clean Architecture (4 Layers)

```
KnowHub.Domain          ← Entities, enums, domain rules (no external deps)
KnowHub.Application     ← Interfaces, DTOs, use-case services, CQRS-style
KnowHub.Infrastructure  ← EF Core, email/notifications, integrations, service implementations
KnowHub.Api             ← ASP.NET Core controllers, middleware, global error handling, DI wiring
KnowHub.Tests           ← xUnit unit tests (no integration tests)
```

**Dependency rule**: Inner layers never reference outer layers. Domain ← Application ← Infrastructure ← API.

### Key Paths

| Component | Path |
|-----------|------|
| API project | `backend/src/KnowHub.Api/` |
| Global exception middleware | `backend/src/KnowHub.Api/Middleware/ExceptionHandlingMiddleware.cs` |
| Application contracts | `backend/src/KnowHub.Application/Contracts/` |
| Application models/DTOs | `backend/src/KnowHub.Application/Models/` |
| Application validators | `backend/src/KnowHub.Application/Validators/` |
| Domain entities | `backend/src/KnowHub.Domain/Entities/` |
| Domain enums | `backend/src/KnowHub.Domain/Enums/` |
| Domain exceptions | `backend/src/KnowHub.Domain/Exceptions/` |
| Infrastructure services | `backend/src/KnowHub.Infrastructure/Services/` |
| Infrastructure persistence | `backend/src/KnowHub.Infrastructure/Persistence/` |
| DI registration | `backend/src/KnowHub.Infrastructure/Extensions/ServiceCollectionExtensions.cs` |
| API controllers | `backend/src/KnowHub.Api/Controllers/` |
| Test project | `backend/tests/KnowHub.Tests/` |
| Test helpers | `backend/tests/KnowHub.Tests/TestHelpers/` |
| Frontend source | `frontend/src/` |
| Frontend features | `frontend/src/features/` |
| Frontend shared components | `frontend/src/shared/` |

---

## Tech Stack

### Backend
- **.NET 10** / **ASP.NET Core** (Minimal API-style controllers with attribute routing)
- **Entity Framework Core 10** with PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **PostgreSQL 16** Docker container: `knowhub-postgres`, port `5432`, DB `knowhub_dev`
- **FluentValidation** for input validation (registered via DI, not inline)
- **MediatR** for CQRS command/query dispatch (Phase 2+)
- Nullable reference types enabled, implicit usings enabled
- Default listening port: **5200** (`http://localhost:5200`)

### Frontend
- **React 18** + **Vite** + **TypeScript**
- **MUI (Material UI v5+)** for all UI components — no mixing of UI libraries
- **React Router v6** for routing
- **Axios** for all HTTP calls — centralised in `shared/api/`
- **React Hook Form** for all forms
- **TanStack Query (React Query)** for server-state caching
- Default dev port: **5173** (`http://localhost:5173`)

### Testing
- **xUnit** (v2.9+) — NO NUnit, NO MSTest
- **EF Core InMemory** for database tests (`Microsoft.EntityFrameworkCore.InMemory`)
- **No external mock libraries** — hand-written `FakeXxx` helper classes only (no Moq, no NSubstitute)
- **coverlet** for code coverage

---

## Build & Run Commands

```powershell
# Build entire solution
dotnet build KnowHub.slnx

# Run all tests
dotnet test KnowHub.slnx

# Run tests with coverage
dotnet test KnowHub.slnx --collect:"XPlat Code Coverage"

# Run API backend
cd backend/src/KnowHub.Api; dotnet run

# Frontend dev server
cd frontend; npm run dev

# Frontend production build
cd frontend; npm run build
```

---

## Domain Model

### Organisation Hierarchy
```
Tenant (enterprise customer or single-org deployment)
  └── Users (Employee / Contributor / Manager / KnowledgeTeam / Admin / SuperAdmin)
        └── ContributorProfile (optional extended profile for knowledge contributors)
        └── Communities (knowledge groups: AI, DevOps, QA, etc.)
              └── Sessions (proposed → approved → scheduled → completed)
```

### Core Entities (Phase 1 + future-ready)

| Entity | Description |
|--------|-------------|
| `Tenant` | A SaaS customer organisation. All data is isolated by TenantId. |
| `User` | Any person who logs in. Stores basic profile: name, email, department, role, designation, years of experience, location, profile photo. A user can hold multiple roles. |
| `ContributorProfile` | Extended profile for knowledge contributors: areas of expertise, technologies known, past sessions delivered, ratings average, follower count. |
| `UserSkill` | Many-to-many: skills/expertise tags associated with a user. |
| `UserFollower` | Follow/subscribe relationship between users (`FollowerId` → `FollowedId`). |
| `Category` | Top-level session categories (e.g., Engineering, QA, Architecture, Product, HR). Managed by Admin. |
| `Tag` | Reusable keyword tags for sessions and user skills (e.g., "React", "Kubernetes", "API Design"). |
| `SessionProposal` | A proposed knowledge session submitted by an employee. Contains all proposal form fields. |
| `ProposalApproval` | Each step in the approval workflow for a `SessionProposal` (Manager → KnowledgeTeam). Stores approver, decision, comment, and timestamp. |
| `Session` | A scheduled event created from an approved `SessionProposal`. Contains meeting link, date/time, participant limit, format, and status. |
| `SessionTag` | Many-to-many join between `Session` and `Tag`. |
| `SessionMaterial` | Attachments for a session proposal or session: slides, document links, demo links, recordings. |
| `SessionRegistration` | A participant's registration for a `Session`. Tracks waitlist position. |
| `KnowledgeAsset` | Post-session knowledge artifact stored in the Content Repository (recorded video, slides, code, FAQs, documentation). |
| `Comment` | A comment or Q&A entry on a session or knowledge asset. |
| `Like` | A user's like or bookmark on a session, asset, or comment. |
| `SessionRating` | A post-session rating: session score (1–5), speaker score (1–5), feedback text, suggestions. |
| `Badge` | Badge definition (name, description, icon, criteria). |
| `UserBadge` | An awarded badge: links `User` to `Badge` with awarded date and reason. |
| `Community` | A knowledge community (e.g., AI Community, DevOps Community). Has members and hosts sessions. |
| `CommunityMember` | Membership join: `UserId` → `CommunityId` with role (Member, Moderator). |
| `KnowledgeRequest` | An employee's request for a session on a specific topic. Can be upvoted, claimed, and bounty-rewarded (XP). |
| `Notification` | A system notification for a user (new session, approval update, comment, reminder, streak, badge unlock). |
| `UserXpEvent` | Append-only XP ledger: records every XP-earning action with type, amount, source entity reference, and timestamp. Never updated or deleted. |
| `LearningPath` | A curated, ordered sequence of sessions and/or knowledge assets with a stated objective, difficulty, estimated duration, and certificate on completion. |
| `LearningPathItem` | Ordered item within a `LearningPath`. Polymorphic: references either a `Session` or a `KnowledgeAsset`. Has `OrderSequence` and `IsRequired` flag. |
| `UserLearningPathEnrollment` | A user's enrolment in a `LearningPath`. Tracks progress %, completed item count, enrolment type (self/assigned/mandatory), and optional deadline. |
| `LearningPathCertificate` | Certificate issued on 100% path completion. Stores a unique certificate number (for external verification) and URL. |
| `SessionQuiz` | Optional post-session knowledge-retention quiz. Configures passing threshold %, max attempts, and auto-grading. One quiz per session maximum. |
| `QuizQuestion` | A single question in a `SessionQuiz`. Supports MCQ, True/False (auto-graded), and short-text (manual-graded). MCQ options stored as JSONB. |
| `UserQuizAttempt` | A user's answer submission for a quiz. Stores answers (JSONB), auto-computed score, pass/fail result, and attempt number. |
| `SkillEndorsement` | An attendee's skill endorsement for a speaker, scoped to the session they attended. Session-validated (only Attended participants may endorse). |
| `CommunityWikiPage` | A Markdown wiki page owned by a `Community`. Self-referencing `ParentPageId` enables hierarchical wiki trees. |
| `SessionChapter` | A timestamped chapter within a session recording. Enables in-video navigation without scrubbing. |
| `KnowledgeBundle` | A curated, themed package of `KnowledgeAsset`s grouped into a coherent learning collection by a contributor or the Knowledge Team. |
| `KnowledgeBundleItem` | Ordered join between a `KnowledgeBundle` and a `KnowledgeAsset`, with optional curator notes. |
| `AfterActionReview` | Structured post-session speaker reflection: what was planned, what happened, what went well, what to improve, key lessons learned. One per session. |
| `MentorMentee` | A mentoring pairing between a senior Contributor (mentor) and an Employee (mentee). Lifecycle: Pending → Active → Completed / Declined. |
| `UserLearningStreak` | One-row-per-user record (unique per tenant) tracking current streak days, longest streak, last activity date, and optional streak-freeze date. |
| `LeaderboardSnapshot` | Monthly snapshot of top-N users for a specific leaderboard dimension. Entries stored as JSONB for history integrity (decoupled from live user data). |

### Key Entity Relationships

> Use these as the authoritative reference when writing EF Core entity configurations, navigation properties, and DB migrations.

**User-centric**
```
User          (1)→(many)  UserXpEvent                  via UserId  [append-only ledger]
User          (1)→(1)     UserLearningStreak             unique per tenant
User          (1)→(many)  UserLearningPathEnrollment     via UserId
User          (1)→(many)  UserBadge                      via UserId
User          (1)→(many)  SessionRegistration            via ParticipantId
User          (1)→(many)  SessionProposal                via ProposerId
User          (many)↔(many) User                        via UserFollower (FollowerId, FollowedId)
User          (many)↔(many) Community                   via CommunityMember
User          (many)↔(many) User [MentorMentee]          via MentorId / MenteeId
ContributorProfile  (1)→(1) User                        via UserId [optional 1:1]
```

**Session-centric**
```
SessionProposal  (1)→(0..1)  Session                    one session per approved proposal
Session          (1)→(many)  SessionRegistration
Session          (1)→(many)  SessionMaterial
Session          (1)→(many)  SessionTag → Tag            many-to-many via join
Session          (1)→(many)  KnowledgeAsset
Session          (1)→(many)  SessionRating
Session          (1)→(0..1)  SessionQuiz                 unique FK
Session          (1)→(many)  SessionChapter
Session          (1)→(0..1)  AfterActionReview           unique FK
Session          (many)↔(many) LearningPath               via LearningPathItem
```

**Learning & Knowledge**
```
LearningPath     (1)→(many)  LearningPathItem            ordered; polymorphic Session|KnowledgeAsset
LearningPath     (1)→(many)  UserLearningPathEnrollment
LearningPath     (1)→(many)  LearningPathCertificate
KnowledgeAsset   (many)↔(many) KnowledgeBundle            via KnowledgeBundleItem
SessionQuiz      (1)→(many)  QuizQuestion
SessionQuiz      (1)→(many)  UserQuizAttempt
SkillEndorsement (many)→(1)  User [EndorseeId]           attendee→speaker endorsement per session+skill
```

**Community**
```
Community        (1)→(many)  CommunityMember → User
Community        (1)→(many)  CommunityWikiPage           self-referencing via ParentPageId
```

**Gamification**
```
Badge            (1)→(many)  UserBadge
LeaderboardSnapshot            no live FK; Entries stored as JSONB snapshot
UserXpEvent      (many)→(1)  User                        immutable; SUM(XpAmount) = total XP
```

---

### Proposal Status Lifecycle
```
Draft → Submitted → ManagerReview → (Approved by Manager) →
  KnowledgeTeamReview → (Approved by KnowledgeTeam) → Published →
    (Session Created) → Scheduled → InProgress → Completed / Cancelled

Rejected (at any review stage)
RevisionRequested → (author revises) → Submitted (resubmitted)
```

### User Roles
| Role | Value | Capabilities |
|------|-------|-------------|
| `Employee` | 1 | Browse sessions, register, rate, comment, request topics, follow contributors |
| `Contributor` | 2 | All Employee + submit proposals, deliver sessions, manage own content, build contributor profile |
| `Manager` | 4 | All Employee + first-tier proposal approval/rejection/revision request |
| `KnowledgeTeam` | 8 | All Employee + second-tier approval, manage categories and tags, moderate content |
| `Admin` | 16 | Full platform management for their tenant: users, sessions, proposals, categories, tags, communities, content |
| `SuperAdmin` | 32 | All Admin capabilities + cross-tenant management and system configuration |

> **IMPORTANT — Role implementation rules (never regress these):**
> - Roles are stored as a `[Flags]` enum integer in the DB. A user with only `SuperAdmin` has value `32` — it does NOT automatically include lower flag values.
> - **`Admin` (16) and `SuperAdmin` (32) have identical feature-level permissions** — both can do everything within a tenant. The only distinction is that SuperAdmin can also manage across tenants.
> - **Backend**: All permission checks must use `_currentUser.IsAdminOrAbove` (defined on `ICurrentUserAccessor`) rather than `IsInRole(UserRole.Admin)` alone. `IsAdminOrAbove` returns `true` for both Admin and SuperAdmin.
> - **Frontend**: All permission checks must use `isAdminOrAbove = isAdmin || isSuperAdmin` — never check `isAdmin` alone when the intent is "Admin or higher".
> - **JWT role claim**: Store role as numeric integer string `((int)user.Role).ToString()` — NOT `user.Role.ToString()` which produces a comma-separated flags string that the JWT library splits into multiple claims.
> - **JWT parsing**: `options.MapInboundClaims = false` must be set on `AddJwtBearer` to prevent ASP.NET Core remapping `"role"` to the long URI claim type.
> - Users can hold multiple roles simultaneously via flags (e.g., `Contributor | Manager` = 6).

> Users can hold multiple roles simultaneously — store as a flags enum or a `UserRole` join table.

### Session Format Types
```
Webinar | Workshop | Demo | PanelDiscussion | KnowledgeSharingTalk | OfficeHours
| HackSession | LearningSeriesEpisode | KnowledgeHarvestSession | ExpertInterview
```
> `KnowledgeHarvestSession` — structured format for capturing tacit knowledge from departing or transitioning subject-matter experts.
> `ExpertInterview` — a recorded Q&A-style session where the community interviews an expert.

### Session Difficulty Levels
```
Beginner | Intermediate | Advanced
```

### Material Types
```
Slides | Document | DemoLink | RecordingLink | CodeRepository | FAQ
```

### Knowledge Asset Types
```
Recording | Slides | Code | Documentation | FAQ | AfterActionReview | Certificate | Bundle
```

### XP Event Types
```
AttendSession | SubmitProposal | ProposalApproved | DeliverSession | FiveStarRating
| AnswerKnowledgeRequest | UploadAsset | CommentLiked | LearningPathCompleted
| BadgeAwarded | CompleteQuiz | StreakMilestone | ReferContributor
```

### Badge Categories
```
Contribution | Attendance | Mentoring | QualityRating | Knowledge | Trending | Streak
```

### Leaderboard Types
```
ByXp | ByContributions | ByAttendance | ByRating | ByMentoring | ByDepartment
```

### Quiz Question Types
```
MCQ | TrueFalse | ShortText
```

### Mentor/Mentee Status
```
Pending | Active | Completed | Declined
```

### Enrolment Types
```
SelfEnrolled | DepartmentAssigned | MandatoryAssigned
```

### Learning Path Item Types
```
Session | KnowledgeAsset
```

---

## Database Conventions — CRITICAL, follow exactly

### Standard Columns on EVERY Table
Every table (without exception) must include these columns in this order after the primary key:

```sql
Id            UUID            PRIMARY KEY
TenantId      UUID            NOT NULL  -- FK to Tenants, indexed, enforces SaaS data isolation
CreatedDate   TIMESTAMPTZ     NOT NULL DEFAULT NOW()
CreatedBy     UUID            NOT NULL  -- FK to Users
ModifiedOn    TIMESTAMPTZ     NOT NULL DEFAULT NOW()
ModifiedBy    UUID            NOT NULL  -- FK to Users
RecordVersion INT             NOT NULL DEFAULT 1  -- incremented on every UPDATE for optimistic concurrency
```

### Naming Convention
- **Column names**: PascalCase, NO underscores (e.g., `TenantId`, `SessionTitle`, `CreatedBy`) — **never** `snake_case`
- **Table names**: PascalCase plural (e.g., `SessionProposals`, `KnowledgeAssets`, `UserFollowers`)
- **Index names**: `IX_<TableName>_<Column(s)>` (e.g., `IX_SessionProposals_TenantId_Status`)
- **FK constraint names**: `FK_<ChildTable>_<ParentTable>_<Column>` (e.g., `FK_SessionProposals_Users_ProposerId`)
- **Primary key names**: `PK_<TableName>` (e.g., `PK_SessionProposals`)

### Key Tables and Columns

**Tenants**
```
Id, Name, Slug, IsActive, CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
*(Tenants table does NOT have TenantId — it IS the tenant)*

**Users**
```
Id, TenantId, FullName, Email, PasswordHash, Department, Designation, YearsOfExperience,
Location, ProfilePhotoUrl, Role (flags: Employee|Contributor|Manager|KnowledgeTeam|Admin|SuperAdmin),
IsActive, CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**ContributorProfiles**
```
Id, TenantId, UserId (FK→Users), AreasOfExpertise, TechnologiesKnown, Bio, AverageRating,
TotalSessionsDelivered, FollowerCount, EndorsementScore, IsKnowledgeBroker, AvailableForMentoring,
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> `EndorsementScore` — weighted count of session-validated `SkillEndorsement` records received. Computed by the system; never editable directly.
> `IsKnowledgeBroker` — set by Admin/KnowledgeTeam; marks this expert as a knowledge connector who routes questions within their domain.
> `AvailableForMentoring` — contributor opt-in flag for the Mentor/Mentee matching system.

**UserSkills** *(join table)*
```
Id, TenantId, UserId (FK→Users), TagId (FK→Tags),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**UserFollowers** *(join table)*
```
Id, TenantId, FollowerId (FK→Users), FollowedId (FK→Users),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**Categories**
```
Id, TenantId, Name, Description, IconName, SortOrder, IsActive,
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**Tags**
```
Id, TenantId, Name, Slug, UsageCount, IsActive,
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**SessionProposals**
```
Id, TenantId, ProposerId (FK→Users), Title, CategoryId (FK→Categories),
Topic, DepartmentRelevance, Description, ProblemStatement, LearningOutcomes,
TargetAudience, Format (enum), Duration, PreferredDate (nullable), PreferredTime (nullable),
DifficultyLevel (enum), RelatedProject, AllowRecording, Status (enum),
SubmittedAt (nullable), CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**ProposalApprovals**
```
Id, TenantId, ProposalId (FK→SessionProposals), ApproverId (FK→Users),
ApprovalStep (enum: ManagerReview|KnowledgeTeamReview), Decision (enum: Approved|Rejected|RevisionRequested),
Comment, DecidedAt,
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**Sessions**
```
Id, TenantId, ProposalId (FK→SessionProposals), SpeakerId (FK→Users),
Title, CategoryId (FK→Categories), Format (enum), DifficultyLevel (enum),
ScheduledAt, DurationMinutes, MeetingLink, MeetingPlatform (enum: Teams|Zoom|GoogleMeet|Other),
ParticipantLimit (nullable), Status (enum: Scheduled|InProgress|Completed|Cancelled),
IsPublic, RecordingUrl (nullable),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**SessionTags** *(join table)*
```
Id, TenantId, SessionId (FK→Sessions), TagId (FK→Tags),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**SessionMaterials**
```
Id, TenantId, SessionId (nullable FK→Sessions), ProposalId (nullable FK→SessionProposals),
MaterialType (enum), Title, Url, FileSizeBytes (nullable),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**SessionRegistrations**
```
Id, TenantId, SessionId (FK→Sessions), ParticipantId (FK→Users),
WaitlistPosition (nullable), RegisteredAt, AttendedAt (nullable), Status (enum: Registered|Waitlisted|Attended|Cancelled),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**KnowledgeAssets**
```
Id, TenantId, SessionId (nullable FK→Sessions),
AssetType (enum: Recording|Slides|Code|Documentation|FAQ|AfterActionReview|Certificate|Bundle),
Title, Url, Description, ViewCount, DownloadCount, IsPublic,
VersionNumber (int default 1), ExpiresAt (nullable TIMESTAMPTZ),
IsVerified (bool default false), VerifiedById (nullable FK→Users), VerifiedAt (nullable TIMESTAMPTZ),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> `VersionNumber` — incremented on content replacement. Phase 3 may add a `KnowledgeAssetVersion` history table.
> `ExpiresAt` — optional expiry; admin Knowledge Audit dashboard flags assets past expiry for review/retirement.
> `IsVerified` — set via community peer-review flow (Phase 3); `VerifiedById` records who verified it.

**SessionRatings**
```
Id, TenantId, SessionId (FK→Sessions), RaterId (FK→Users),
SessionScore (1-5), SpeakerScore (1-5), FeedbackText, NextSessionSuggestion,
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**Communities**
```
Id, TenantId, Name, Slug, Description, IconName, CoverImageUrl, MemberCount, IsActive,
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**CommunityMembers** *(join table)*
```
Id, TenantId, CommunityId (FK→Communities), UserId (FK→Users),
MemberRole (enum: Member|Moderator|KnowledgeBroker|CoLeader), JoinedAt,
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> `KnowledgeBroker` — community-designated connector; routes questions and knowledge to the right expert within the community.
> `CoLeader` — same permissions as Moderator plus can rename, archive, or configure the community.

**KnowledgeRequests**
```
Id, TenantId, RequesterId (FK→Users), Title, Description, CategoryId (nullable FK→Categories),
UpvoteCount, IsAddressed, AddressedBySessionId (nullable FK→Sessions),
Status (enum: Open|InProgress|Addressed|Closed),
BountyXp (int default 0), ClaimedByUserId (nullable FK→Users),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> `BountyXp` — optional XP reward offered by the requester. Deducted from their XP balance when the request is claimed and fulfilled. Defaults to 0 (no bounty).
> `ClaimedByUserId` — set when a Contributor claims the request via `POST /api/knowledge-requests/{id}/claim`; `Status` transitions to `InProgress`.

**Notifications**
```
Id, TenantId, UserId (FK→Users), NotificationType (enum), Title, Body,
RelatedEntityType (nullable), RelatedEntityId (nullable UUID), IsRead, ReadAt (nullable),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**Badges**
```
Id, TenantId, Name, Description, IconUrl, Criteria,
BadgeCategory (enum: Contribution|Attendance|Mentoring|QualityRating|Knowledge|Trending|Streak),
XpGranted (int default 0), IsActive,
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> `BadgeCategory` — groups badges on contributor profiles (displayed in category tabs) and enables category-scoped badge queries.
> `XpGranted` — XP automatically added to the recipient via a `UserXpEvent` when this badge is awarded.

**UserBadges**
```
Id, TenantId, UserId (FK→Users), BadgeId (FK→Badges), AwardedAt, AwardReason, XpGranted,
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> `XpGranted` — snapshot of XP awarded at grant time (copied from `Badges.XpGranted`). Preserved even if the badge XP definition changes later.

---

### New Entities — Phase 2 Tables

**UserXpEvents**
```
Id, TenantId, UserId (FK→Users),
EventType (enum: AttendSession|SubmitProposal|ProposalApproved|DeliverSession|FiveStarRating
  |AnswerKnowledgeRequest|UploadAsset|CommentLiked|LearningPathCompleted
  |BadgeAwarded|CompleteQuiz|StreakMilestone|ReferContributor),
XpAmount (int), RelatedEntityType (varchar 50 nullable), RelatedEntityId (UUID nullable),
EarnedAt (TIMESTAMPTZ),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> Append-only ledger — never updated or deleted. User total XP = `SUM(XpAmount)` per user.
> `RelatedEntityType` + `RelatedEntityId` — polymorphic audit reference to the source entity (e.g., `Session`, `KnowledgeRequest`).
> XP events are ONLY created by the server (service layer) — never accepted from client input (OWASP A04).

**LearningPaths**
```
Id, TenantId, Title, Slug (unique within tenant), Description, Objective,
CategoryId (nullable FK→Categories), DifficultyLevel (enum),
EstimatedDurationMinutes, IsPublished (bool default false), IsAssignable (bool default true), CoverImageUrl,
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**LearningPathItems** *(ordered items within a learning path)*
```
Id, TenantId, LearningPathId (FK→LearningPaths),
ItemType (enum: Session|KnowledgeAsset),
SessionId (nullable FK→Sessions), KnowledgeAssetId (nullable FK→KnowledgeAssets),
OrderSequence (int), IsRequired (bool default true),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> CHECK constraint: exactly one of `SessionId` / `KnowledgeAssetId` must be non-null. Enforced in DB migration AND validated in the service layer.

**UserLearningPathEnrollments**
```
Id, TenantId, UserId (FK→Users), LearningPathId (FK→LearningPaths),
EnrolmentType (enum: SelfEnrolled|DepartmentAssigned|MandatoryAssigned),
ProgressPercentage (decimal(5,2) default 0), CompletedItemCount (int default 0),
DeadlineAt (nullable TIMESTAMPTZ), StartedAt (nullable TIMESTAMPTZ), CompletedAt (nullable TIMESTAMPTZ),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> Unique constraint: `(TenantId, UserId, LearningPathId)` — one active enrolment per user per path.

**LearningPathCertificates**
```
Id, TenantId, UserId (FK→Users), LearningPathId (FK→LearningPaths),
CertificateNumber (varchar 64 unique), CertificateUrl, IssuedAt (TIMESTAMPTZ),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**SessionQuizzes**
```
Id, TenantId, SessionId (FK→Sessions, UNIQUE), Title, Description,
PassingThresholdPercent (int default 70), AllowRetry (bool default true),
MaxAttempts (int default 2), IsActive (bool default true),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> UNIQUE constraint on `(TenantId, SessionId)` — one quiz per session maximum.

**QuizQuestions**
```
Id, TenantId, QuizId (FK→SessionQuizzes),
QuestionText, QuestionType (enum: MCQ|TrueFalse|ShortText),
Options (JSONB nullable — array of option strings for MCQ),
CorrectAnswer (varchar 500 nullable — for MCQ and TrueFalse auto-grading),
OrderSequence (int), Points (int default 1),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> `Options` and `CorrectAnswer` must be validated server-side for allowed content types and max lengths (OWASP A03).

**UserQuizAttempts**
```
Id, TenantId, QuizId (FK→SessionQuizzes), UserId (FK→Users), AttemptNumber (int),
Answers (JSONB — [{questionId, answer}] pairs), Score (decimal(5,2) nullable),
IsPassed (bool nullable), SubmittedAt (TIMESTAMPTZ), GradedAt (nullable TIMESTAMPTZ),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> Unique constraint: `(TenantId, QuizId, UserId, AttemptNumber)` — no duplicate attempt numbers.
> MaxAttempts limit enforced in the service layer before creating a new attempt record.

**SkillEndorsements**
```
Id, TenantId, EndorserId (FK→Users), EndorseeId (FK→Users), TagId (FK→Tags),
SessionId (FK→Sessions), EndorsedAt (TIMESTAMPTZ),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> Unique constraint: `(TenantId, EndorserId, EndorseeId, TagId, SessionId)` — one endorsement per skill per session pairing.
> Endorser must have a `SessionRegistration` with `Status = Attended` for the given `SessionId` — enforced in the service layer before insert.

**CommunityWikiPages**
```
Id, TenantId, CommunityId (FK→Communities), AuthorId (FK→Users),
Title, Slug (unique within community), ContentMarkdown (TEXT),
ParentPageId (nullable FK→CommunityWikiPages — self-referencing for nested pages),
OrderSequence (int default 0), IsPublished (bool default false), ViewCount (int default 0),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> `ContentMarkdown` must be sanitised through a whitelist-based HTML/Markdown sanitizer before storage and before rendering (strip `<script>`, `<iframe>`, `javascript:` links — OWASP A03).

**SessionChapters**
```
Id, TenantId, SessionId (FK→Sessions), Title,
TimestampSeconds (int), OrderSequence (int),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**KnowledgeBundles**
```
Id, TenantId, Title, Description, CreatedByUserId (FK→Users),
CategoryId (nullable FK→Categories),
IsPublished (bool default false), CoverImageUrl,
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**KnowledgeBundleItems** *(join table)*
```
Id, TenantId, BundleId (FK→KnowledgeBundles), KnowledgeAssetId (FK→KnowledgeAssets),
OrderSequence (int), Notes (varchar 500 nullable),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**AfterActionReviews**
```
Id, TenantId, SessionId (FK→Sessions, UNIQUE), AuthorId (FK→Users),
WhatWasPlanned (TEXT), WhatHappened (TEXT), WhatWentWell (TEXT),
WhatToImprove (TEXT), KeyLessonsLearned (TEXT), IsPublished (bool default false),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**MentorMentees**
```
Id, TenantId, MentorId (FK→Users), MenteeId (FK→Users),
Status (enum: Pending|Active|Completed|Declined),
StartedAt (nullable TIMESTAMPTZ), EndedAt (nullable TIMESTAMPTZ),
GoalsText (TEXT nullable), MatchReason (varchar 500 nullable),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```

**UserLearningStreaks** *(one row per user per tenant)*
```
Id, TenantId, UserId (FK→Users),
CurrentStreakDays (int default 0), LongestStreakDays (int default 0),
LastActivityDate (DATE), StreakFrozenUntil (nullable DATE),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> Unique constraint: `(TenantId, UserId)` — exactly one streak record per user.

**LeaderboardSnapshots**
```
Id, TenantId, SnapshotMonth (int 1–12), SnapshotYear (int),
LeaderboardType (enum: ByXp|ByContributions|ByAttendance|ByRating|ByMentoring|ByDepartment),
Entries (JSONB — [{rank, userId, displayName, score, avatarUrl}]), GeneratedAt (TIMESTAMPTZ),
CreatedDate, CreatedBy, ModifiedOn, ModifiedBy, RecordVersion
```
> Unique constraint: `(TenantId, SnapshotMonth, SnapshotYear, LeaderboardType)` — one snapshot per type per month.
> `Entries` is a JSONB snapshot — intentionally decoupled from live user data for historical integrity.

### Mandatory Indexes
Every table must have at minimum:
- Primary key index on `Id`
- Index on `TenantId` (all queries are tenant-scoped)
- Composite index on `(TenantId, <main FK column>)` for frequently queried FKs
- Index on date/time columns used in range queries (`ScheduledAt`, `CreatedDate`, `DecidedAt`)
- Index on `Status` columns for filtered list queries

### Database Migrations
SQL migration files in `database/sql/` — numbered sequentially: `001_Init.sql`, `002_SeedCategories.sql`, etc.
File names use PascalCase to match the convention.

---

## API Design Standards

### REST Conventions
- Resource URLs: plural nouns, kebab-case: `/api/session-proposals`, `/api/knowledge-assets`, `/api/session-ratings`
- HTTP verbs: `GET` (read), `POST` (create), `PUT` (full update), `PATCH` (partial update), `DELETE`
- Response codes: `200 OK`, `201 Created` (with `Location` header), `204 No Content` (delete/update), `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict` (optimistic concurrency), `422 Unprocessable Entity` (validation), `500 Internal Server Error`
- Never return `200 OK` with an error payload — use correct HTTP status codes
- Paginated list responses must include: `{ data: [], totalCount, pageNumber, pageSize }`

### Phase 1 API Surface

| Endpoint | Method | Role | Description |
|----------|--------|------|-------------|
| `/api/auth/login` | POST | Public | Authenticate, return JWT |
| `/api/auth/refresh` | POST | Authenticated | Refresh JWT token |
| `/api/users` | GET | Admin | List users (paginated, filterable) |
| `/api/users/{id}` | GET | Authenticated | Get user profile |
| `/api/users/{id}` | PUT | Self/Admin | Update user profile |
| `/api/users/{id}/contributor-profile` | GET | Authenticated | Get contributor profile |
| `/api/users/{id}/contributor-profile` | PUT | Self/Admin | Update contributor profile |
| `/api/users/{id}/follow` | POST | Employee | Follow a contributor |
| `/api/users/{id}/follow` | DELETE | Employee | Unfollow a contributor |
| `/api/categories` | GET | Authenticated | List active categories |
| `/api/categories` | POST/PUT/DELETE | Admin | Manage categories |
| `/api/tags` | GET | Authenticated | List/search tags |
| `/api/tags` | POST/DELETE | Admin/KnowledgeTeam | Manage tags |
| `/api/session-proposals` | POST | Contributor | Submit a new session proposal |
| `/api/session-proposals` | GET | Authenticated | List proposals (role-filtered) |
| `/api/session-proposals/{id}` | GET | Authenticated | Get proposal details |
| `/api/session-proposals/{id}` | PUT | Contributor (owner) | Update a draft or revision-requested proposal |
| `/api/session-proposals/{id}/submit` | POST | Contributor (owner) | Submit draft for review |
| `/api/session-proposals/{id}/approve` | POST | Manager/KnowledgeTeam | Approve a proposal at current step |
| `/api/session-proposals/{id}/reject` | POST | Manager/KnowledgeTeam | Reject a proposal |
| `/api/session-proposals/{id}/request-revision` | POST | Manager/KnowledgeTeam | Request revisions |
| `/api/sessions` | GET | Authenticated | List/search sessions (with filters) |
| `/api/sessions/{id}` | GET | Authenticated | Get session details |
| `/api/sessions` | POST | Admin/KnowledgeTeam | Create session from approved proposal |
| `/api/sessions/{id}` | PUT | Admin/KnowledgeTeam/Speaker | Update session details |
| `/api/sessions/{id}/register` | POST | Employee | Register for a session |
| `/api/sessions/{id}/register` | DELETE | Employee | Cancel registration |
| `/api/sessions/{id}/materials` | GET | Authenticated | Get session materials |
| `/api/sessions/{id}/materials` | POST | Speaker/Admin | Add material to session |
| `/api/sessions/{id}/ratings` | POST | Attended Employee | Submit session rating |
| `/api/sessions/{id}/ratings` | GET | Authenticated | Get session ratings summary |
| `/api/communities` | GET | Authenticated | List communities |
| `/api/communities/{id}` | GET | Authenticated | Get community details |
| `/api/communities` | POST | Admin | Create community |
| `/api/communities/{id}/join` | POST | Employee | Join a community |
| `/api/communities/{id}/join` | DELETE | Employee | Leave a community |
| `/api/knowledge-requests` | GET | Authenticated | List knowledge requests |
| `/api/knowledge-requests` | POST | Employee | Submit a knowledge request |
| `/api/knowledge-requests/{id}/upvote` | POST | Employee | Upvote a request |
| `/api/notifications` | GET | Authenticated | Get own notifications |
| `/api/notifications/{id}/read` | PUT | Authenticated | Mark notification as read |
| `/api/notifications/read-all` | PUT | Authenticated | Mark all as read |
| `/api/speakers` | GET | Authenticated | Search internal expert speakers |
| `/health` | GET | Public | Health check |

### Phase 2 API Surface

| Endpoint | Method | Role | Description |
|----------|--------|------|-------------|
| `/api/learning-paths` | GET | Authenticated | List/search learning paths (filterable by category, difficulty, published) |
| `/api/learning-paths` | POST | Admin/KnowledgeTeam | Create a learning path |
| `/api/learning-paths/{id}` | GET | Authenticated | Get learning path details with ordered items |
| `/api/learning-paths/{id}` | PUT | Admin/KnowledgeTeam | Update learning path |
| `/api/learning-paths/{id}/enrol` | POST | Employee | Self-enrol in a learning path |
| `/api/learning-paths/{id}/enrol` | DELETE | Employee | Unenrol from a learning path |
| `/api/learning-paths/{id}/progress` | GET | Authenticated | Get own progress on a learning path |
| `/api/learning-paths/{id}/certificate` | GET | Authenticated | Download/view completion certificate |
| `/api/users/{id}/learning-paths` | GET | Authenticated | List a user's enrolled learning paths |
| `/api/sessions/{id}/quiz` | GET | Authenticated | Get the quiz for a session |
| `/api/sessions/{id}/quiz` | POST | Speaker/Admin | Create/attach a quiz to a session |
| `/api/sessions/{id}/quiz/attempt` | POST | Attended Employee | Submit a quiz attempt |
| `/api/sessions/{id}/quiz/attempts` | GET | Self/Admin | Get own quiz attempts for a session |
| `/api/users/{id}/xp` | GET | Authenticated | Get user XP total, recent events, and history |
| `/api/leaderboards` | GET | Authenticated | Get leaderboard (type and month/year filters) |
| `/api/communities/{id}/wiki` | GET | Authenticated | List wiki pages for a community |
| `/api/communities/{id}/wiki` | POST | Community Member | Create a wiki page |
| `/api/communities/{id}/wiki/{pageId}` | GET | Authenticated | Get a wiki page |
| `/api/communities/{id}/wiki/{pageId}` | PUT | Author/Moderator | Update a wiki page |
| `/api/communities/{id}/wiki/{pageId}` | DELETE | Author/Moderator/Admin | Delete a wiki page |
| `/api/sessions/{id}/endorsements` | POST | Attended Employee | Endorse a speaker skill post-session |
| `/api/users/{id}/endorsements` | GET | Authenticated | Get skill endorsements received by a user |
| `/api/sessions/{id}/chapters` | GET | Authenticated | List chapters/timestamps for a session recording |
| `/api/sessions/{id}/chapters` | POST | Speaker/Admin | Add chapter to a session recording |
| `/api/knowledge-bundles` | GET | Authenticated | List/search knowledge bundles |
| `/api/knowledge-bundles` | POST | Contributor/Admin | Create a knowledge bundle |
| `/api/knowledge-bundles/{id}` | GET | Authenticated | Get bundle details with ordered assets |
| `/api/knowledge-bundles/{id}` | PUT | Owner/Admin | Update a knowledge bundle |
| `/api/sessions/{id}/after-action-review` | POST | Speaker | Submit after-action review |
| `/api/sessions/{id}/after-action-review` | GET | Authenticated | Get after-action review for a session |
| `/api/knowledge-requests/{id}/claim` | POST | Contributor | Claim a knowledge request (moves to InProgress) |
| `/api/mentoring/requests` | POST | Employee | Request a mentor |
| `/api/mentoring/requests` | GET | Authenticated | List mentoring requests/pairings |
| `/api/mentoring/requests/{id}/accept` | POST | Mentor | Accept a mentoring request |
| `/api/mentoring/requests/{id}/decline` | POST | Mentor | Decline a mentoring request |
| `/api/users/{id}/streak` | GET | Authenticated | Get user learning streak stats |

### Global Error Handling — NO try/catch in Controllers
- A single `ExceptionHandlingMiddleware` in `KnowHub.Api/Middleware/` catches all unhandled exceptions
- Domain exceptions (`NotFoundException`, `ConflictException`, `ForbiddenException`, `ValidationException`, `BusinessRuleException`) are mapped to correct HTTP status codes
- Controllers contain **only** happy-path logic — no try/catch blocks
- Error response shape (RFC 7807 Problem Details):
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Session proposal with id 'abc-123' was not found.",
  "traceId": "00-abc..."
}
```

---

## Multi-Tenancy — SaaS Data Isolation

- Every authenticated request carries a JWT with `userId`, `tenantId`, and `role` claims
- `ICurrentUserAccessor` extracts and exposes these claims — injected into all services
- **Every single DB query** must be filtered by `TenantId` — no exceptions
- EF Core global query filters (`modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantId)`) applied on all tenant-scoped entities to enforce isolation automatically
- `TenantId` is populated automatically from the current user context on `INSERT` — never accepted from the client request body
- SuperAdmin access is scoped to their own tenant only unless explicitly elevated

---

## Code Quality Standards

### Naming Conventions
- Classes, interfaces, enums, methods, properties: **PascalCase**
- Local variables, parameters, private fields: **camelCase** (`_camelCase` for private fields)
- Constants: `PascalCase` in a `static class Constants`
- Async method suffix: `Async` (e.g., `GetSessionsAsync`)
- Interface prefix: `I` (e.g., `ISessionProposalService`)
- No abbreviations except universally understood ones (`Id`, `Dto`, `Url`, `Http`)
- **NEVER use phase/sprint/iteration numbers in file names** — files must be named after their domain concept, not the delivery iteration they were added in. This applies to every layer:
  - ❌ `Phase2EntityConfigurations.cs` → ✅ `LearningConfigurations.cs`, `GamificationConfigurations.cs`, `SocialConfigurations.cs`, `SessionEnrichmentConfigurations.cs`
  - ❌ `phase2.ts` → ✅ `learning-paths.ts`, `gamification.ts`, `mentoring.ts`, `wiki.ts`, etc.
  - ❌ `003_Phase2.sql` → ✅ `003_AddLearningAndGamification.sql`
  - EF Core config files: one file per domain group (e.g., `LearningConfigurations.cs` for all learning-path-related entity configs)
  - Frontend type files: one file per feature domain (e.g., `learning-paths.ts`, `endorsements.ts`)
  - If multiple entities belong to the same domain, group them in one domain-named file — never consolidate unrelated entities under a phase/release label

### SOLID Principles
- **Single Responsibility**: one class = one reason to change; services handle one domain concept
- **Open/Closed**: new session formats or approval steps added without modifying existing services
- **Liskov**: all service implementations fully honour their interface contracts
- **Interface Segregation**: small, focused interfaces in `Application/Contracts/` — never bloated with unrelated methods
- **Dependency Inversion**: all services and controllers depend on abstractions, never concrete implementations

### KISS — Keep It Simple
- No speculative abstractions — only introduce patterns that solve an immediate problem
- No over-engineering for hypothetical future requirements beyond what is explicitly phased
- Prefer clear, readable code over clever code
- No unnecessary inheritance hierarchies

### Complexity — Cognitive & Cyclomatic
- **Cyclomatic complexity max 10 per method**: if a method needs more than ~10 decision paths (if/else/switch/loops/catch), extract private helper methods or a dedicated class
- **Cognitive complexity max 15 per method**: deeply nested blocks, chained conditions, and mixed abstraction levels all raise cognitive load — flatten them
- **One level of abstraction per method**: a method either coordinates high-level steps OR does low-level work — never both in the same method
- **Method length target ≤ 30 lines**: if a method grows beyond this, it likely has multiple responsibilities; split it
- **Class length target ≤ 300 lines**: services that grow past this almost always need decomposition (e.g., extract a sub-service or a strategy class)
- **Switch/if-else chains on enums**: extract into a strategy map (`Dictionary<EnumType, IHandler>`) or the Strategy pattern rather than a long switch in the service
- **No deep nesting** (max 3 levels): replace `if (x) { if (y) { if (z) { ... } } }` with early-return guard clauses
  ```csharp
  // BAD
  if (isValid) { if (user != null) { if (user.IsActive) { /* work */ } } }
  // GOOD
  if (!isValid) throw new ValidationException(...);
  if (user is null) throw new NotFoundException(...);
  if (!user.IsActive) throw new BusinessRuleException(...);
  /* work */
  ```

### Async/Await
- All I/O operations must be async (`Task` / `Task<T>`)
- All public async methods accept `CancellationToken cancellationToken` as the last parameter
- Never use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`

### Request DTOs — Parameter Count Rule
- **If a method, service call, or API endpoint requires more than 3 input parameters, create a dedicated Request DTO** instead of passing individual arguments
- This applies at every layer: controller action parameters, service method signatures, and internal helper methods
- Request DTOs live in `KnowHub.Application/Contracts/` alongside their paired Response DTOs
- Naming convention: `<Action><Resource>Request` / `<Action><Resource>Response` (e.g., `SubmitSessionProposalRequest`, `CreateSessionQuizRequest`, `GetSessionsRequest`)
- Every Request DTO used in a POST/PUT/PATCH endpoint **must have a paired FluentValidation validator** in `KnowHub.Application/Validators/`
- List/filter endpoints with multiple optional query parameters also use a Request DTO (e.g., `GetSessionsRequest { CategoryId?, TagId?, DifficultyLevel?, SearchTerm?, PageNumber, PageSize }`) rather than a long method signature
  ```csharp
  // BAD — more than 3 parameters
  Task<PagedResult<SessionDto>> GetSessionsAsync(
      Guid? categoryId, string? searchTerm, DifficultyLevel? level,
      int pageNumber, int pageSize, CancellationToken cancellationToken);

  // GOOD — encapsulated in a Request DTO
  Task<PagedResult<SessionDto>> GetSessionsAsync(
      GetSessionsRequest request, CancellationToken cancellationToken);
  ```
- For UPDATE operations: the Request DTO includes `RecordVersion` (for optimistic concurrency) — never pass it as a separate parameter
- **Query filter DTOs** (for GET list endpoints) are decorated with `[FromQuery]` on the controller action — EF Core filtering logic reads from the DTO properties, never from raw query strings

### Validation
- Input validation via **FluentValidation** validators registered in DI
- Validators live in `KnowHub.Application/Validators/`
- Never do inline validation in controllers or services — validators are invoked by middleware/pipeline
- `ValidationException` (domain exception) is thrown on failure and caught by `ExceptionHandlingMiddleware`

### Optimistic Concurrency
- `RecordVersion` (int) is included in PUT/PATCH request DTOs
- On update, compare incoming `RecordVersion` against DB value — if mismatch, throw `ConflictException` (→ HTTP 409)
- Increment `RecordVersion` by 1 on every UPDATE in the service layer

### Reusability — Backend
- **Shared query extensions** in `KnowHub.Infrastructure/Persistence/Extensions/QueryableExtensions.cs`:
  - `.ApplyTenantFilter(tenantId)` — adds `WHERE TenantId = @tenantId`
  - `.ApplyPaging(pageNumber, pageSize)` — applies `.Skip().Take()` with validation
  - `.ApplySort(sortField, sortDir)` — dynamic column sorting via expression map
  - `.ApplySearch(term, selector)` — `ILIKE` full-text pre-filter on a projected column
- **Shared response/pagination models** in `KnowHub.Application/Models/`: `PagedResult<T>`, `ApiResponse<T>`, `SelectOptionDto` (for dropdown data)
- **Domain exceptions** cover all error cases — never add new exception types unless no existing one fits; reuse `NotFoundException`, `ConflictException`, `ForbiddenException`, `BusinessRuleException`, `ValidationException`
- **Extension methods** for repeated transformations (e.g., `ToDto()`, `ToSelectOption()`) in `KnowHub.Application/Extensions/`
- **No copy-paste service logic** — if two services share a workflow step, extract it into a shared domain service registered in DI

### No Database Calls in Loops — CRITICAL
- **Never call the database inside a `foreach`, `for`, or LINQ loop** — this is an N+1 query and will destroy performance at scale
- Load all required data **before** the loop with a single batched query, then operate on the in-memory collection:
  ```csharp
  // BAD — N+1: one DB call per item
  foreach (var proposalId in proposalIds)
  {
      var proposal = await _db.SessionProposals.FindAsync(proposalId);
      // ...
  }

  // GOOD — one DB call for all
  var proposals = await _db.SessionProposals
      .Where(p => proposalIds.Contains(p.Id) && p.TenantId == tenantId)
      .AsNoTracking()
      .ToListAsync(cancellationToken);
  foreach (var proposal in proposals) { /* ... */ }
  ```
- **Bulk inserts**: use `_db.AddRange(entities)` + single `SaveChangesAsync()` — never call `SaveChangesAsync()` inside a loop
- **Bulk XP events**: when awarding XP for multiple actions at once, build the list of `UserXpEvent` objects in memory then call `AddRange` once
- **Notification fan-out**: collect all `Notification` objects first, then persist with a single `AddRange` + `SaveChangesAsync()`

---

## Frontend Standards

> These rules apply to every React component and page in `frontend/src/`. Treat them as mandatory, not suggestions.

### MUI Usage
- Use **only MUI v5 components** for all UI — never mix with other UI libraries (Ant Design, Bootstrap, Chakra, etc.)
- Use the MUI theme (`theme.palette`, `theme.spacing`, `theme.typography`) for all colours, spacing, and typography — never hardcode hex values or `px` measurements inline
- The theme is defined in `frontend/src/shared/theme/theme.ts`; all colour and typography customisations live there — no per-component `sx={{ color: '#abc' }}` overrides that bypass the theme
- Use `sx` prop for one-off layout tweaks; use `styled()` only when a component is reused ≥ 3 times with the same customisation
- MUI `Grid2` (v5 container/item syntax) for all layouts — not `<div style={{display:'flex'}}>` inline
- `Typography` component for all text rendered to users — never raw `<p>`, `<h1>`, `<span>` etc.
- `Button`, `IconButton`, `LoadingButton` (MUI Lab) for all interactive buttons — never `<button>` HTML element directly

### Reusable Shared Components (`frontend/src/shared/components/`)
Before creating a component: check if one already exists in `shared/components/`. If the same MUI customisation is needed on 2+ pages, it **must** be extracted into a shared component. Never duplicate.

| Component | Purpose |
|-----------|--------|
| `PageHeader` | Page-level title + breadcrumb + optional action button slot |
| `DataTable<T>` | MUI `DataGrid`-wrapper with standard pagination, sorting, loading skeleton, and empty-state |
| `StatusChip` | Coloured MUI `Chip` driven by a status enum; colour mapping defined once inside the component |
| `ConfirmDialog` | Generic modal for destructive-action confirmation: title, message, confirm/cancel labels, `onConfirm` callback |
| `UnsavedChangesDialog` | **See rule below** — generic unsaved-changes guard used across the entire app |
| `FormSection` | Labelled card section wrapping a group of form fields with consistent heading and spacing |
| `RatingStars` | Displays a 1–5 star rating (read-only or interactive) |
| `TagChip` | MUI `Chip` displaying a skill/technology tag with optional click-to-filter behaviour |
| `XpBadge` | Displays a user's XP total/recent gain with icon and formatted number |
| `StreakIndicator` | Flame icon + current streak day count |
| `ProgressBar` | MUI `LinearProgress` wrapper showing labelled percentage for learning path progress |
| `EmptyState` | Centred illustration + heading + sub-text + optional CTA for empty list pages |
| `LoadingOverlay` | Full-panel skeleton / spinner for async loading states |
| `AvatarWithName` | MUI `Avatar` + name text side-by-side; handles missing photo gracefully with initials fallback |
| `SectionDivider` | Consistent horizontal divider with optional label |
| `SearchInput` | Debounced (300 ms) MUI `TextField` with search icon — emits `onSearch(term)` after debounce |

> **Rule**: if you write the same `sx={{...}}` block, the same `Chip` colour logic, or the same wrapper structure in more than one file — stop, extract a shared component instead.

### Unsaved Changes Guard — Generic Implementation
Every form page that modifies data **must** use the `useUnsavedChanges` hook. Never implement the unsaved-changes prompt ad-hoc per page.

**Hook**: `frontend/src/shared/hooks/useUnsavedChanges.ts`
```typescript
// Usage in any form page:
const { confirmNavigation } = useUnsavedChanges(isDirty);
// isDirty comes from React Hook Form's formState.isDirty
```

**Behaviour**:
- When `isDirty === true` and the user attempts to navigate away (React Router link, browser back, tab close), `UnsavedChangesDialog` is displayed automatically
- Dialog text: **"You have unsaved changes. If you leave now, your changes will be lost. Do you want to continue?"** with "Stay" (default focus) and "Leave" buttons
- Implemented once using React Router v6 `useBlocker` under the hood — **never** use `window.onbeforeunload` or per-route reimplementations
- The hook also registers a `beforeunload` listener for browser-tab-close / refresh scenarios
- `UnsavedChangesDialog` is rendered in the shared layout (or via a React portal) — not inline in each page component
- **Forms that must use this hook**: SessionProposal form, Session edit, CommunityWikiPage editor, Profile edit, LearningPath editor, Quiz editor, AfterActionReview form, KnowledgeBundle editor, MentorMentee request form, AdminCategory/Tag management

**File structure**:
```
frontend/src/shared/
  hooks/
    useUnsavedChanges.ts       — wraps useBlocker; returns { confirmNavigation, UnsavedChangesDialog }
  components/
    UnsavedChangesDialog.tsx   — MUI Dialog implementation
```

### Form Standards
- All forms use **React Hook Form** with `useForm<T>()` — never uncontrolled `useState` per field
- Form schemas validated with **Zod** (`zodResolver` from `@hookform/resolvers/zod`) — one `z.object({...})` schema per form in the same feature folder (e.g., `session-proposals/schemas/proposalFormSchema.ts`)
- `isDirty` from `formState` is the single source of truth for the unsaved-changes guard — never maintain a separate dirty flag
- On successful submit: reset the form with `reset(serverResponse)` to clear dirty state before navigating away
- Inline field-level error messages via `formState.errors` — displayed using MUI `FormHelperText` coloured `error`
- Submit buttons show MUI Lab `LoadingButton` with `loading={isSubmitting}` — never disable the button without visual feedback

### State Management
- **Server state**: TanStack Query only — no Redux/Zustand for API data
- **UI/local state**: `useState` / `useReducer` for component-local state
- **Cross-feature shared state** (auth context, tenant context, notification count): React Context in `shared/contexts/` — only for genuinely global state; do not use Context for API data
- TanStack Query `queryKey` convention: `[resource, tenantId, filters]` — always include `tenantId` to prevent cross-tenant cache bleed
- Invalidate query caches **after** all mutations using `queryClient.invalidateQueries({ queryKey: [...] })`

### Component Design Rules
- **Single responsibility**: one component = one visual/behavioural concern
- **Props over deep prop drilling**: if a value must pass through more than 2 component levels, use React Context or TanStack Query cache — not prop chains
- **No business logic in components**: API calls, permission checks, and data transformation belong in custom hooks (`useXxx`) — components only render and handle local interaction events
- **Conditional rendering clarity**: prefer early `if (!data) return <LoadingOverlay />;` guard clauses in component bodies over deeply nested ternaries
- **Key prop on lists**: always provide a stable, unique `key` on list-rendered elements — never use array index as key for data that can be reordered or paginated
- **Accessibility**: all interactive MUI components must have accessible labels (`aria-label`, `aria-labelledby`, or linked `<label>`); `IconButton` always has `aria-label`
- **No inline arrow functions in JSX for event handlers** on frequently rendered lists — extract to a named handler to avoid unnecessary re-renders

### Custom Hook Conventions (`frontend/src/shared/hooks/`)
Every non-trivial data-fetching or business logic pattern used in ≥ 2 components must be a custom hook:
- `useAuth()` — current user, roles, logout
- `useCurrentUser()` — full user profile from TanStack Query cache
- `useTenantId()` — extract tenantId from auth context
- `usePermission(role)` — returns `boolean`; avoids role-check duplication
- `useNotifications()` — notification list + unread count with 30s polling
- `useUnsavedChanges(isDirty)` — unsaved-changes blocker (see above)
- `useDebounce(value, delay)` — generic debounce for search inputs
- `useXp(userId)` — user XP total + recent events
- `useStreak(userId)` — current learning streak
- `useLeaderboard(type, month, year)` — leaderboard data
- Hook files export a single named hook; no default exports from hook files

### Routing Conventions (React Router v6)
- All routes defined in `frontend/src/app/routes.tsx` — no scattered `<Route>` definitions inside feature folders
- Route paths defined as constants in `frontend/src/shared/constants/routes.ts` — never hardcode path strings in `<Link to="...">` or `navigate("...")`
- Protected routes wrapped in `<RequireAuth>` component which checks `useAuth()` and redirects to `/login` if unauthenticated
- Role-gated routes wrapped in `<RequireRole roles={[...]}>`; renders `<ForbiddenPage>` if role check fails — never duplicate role checks inside page components
- All feature route subtrees are **lazy-loaded** (`React.lazy` + `Suspense`) from their folder's `index.tsx`

---

## Performance Guidelines

> Performance is a first-class requirement. Apply these rules on every feature, not as an afterthought.

### Backend — EF Core & Query Patterns
- **No N+1 queries**: always use `.Include()` with `.Select()` projection to a DTO; never call `.Include()` and then loop over the result to trigger lazy loads
- **`AsNoTracking()`**: all read-only queries (GET endpoints) must use `.AsNoTracking()` — never track entities that will not be updated
- **Project to DTOs in the query layer**: never return raw EF entity objects from services; use `.Select(e => new Dto { ... })` to limit SELECTed columns
- **Pagination is mandatory**: every list endpoint must accept `pageNumber`/`pageSize` (or cursor) and return `{ data, totalCount, pageNumber, pageSize }`. Hard limit: `pageSize` max 100.
- **Avoid SELECT \***: EF Core projections generate column-specific SQL automatically; never load full entity graphs for display-only endpoints
- **Soft deletes**: use `IsActive` flags, never hard-delete entities that are FK-referenced by other tables (Sessions, KnowledgeAssets, Tags, Categories)
- **Optimistic concurrency via `RecordVersion`**: prevents lost-update races without row-level locks
- **`CancellationToken` propagation**: every `async` method in every layer accepts and passes `CancellationToken cancellationToken` all the way to the EF Core call

### Backend — Caching
- Use `IMemoryCache` for reference data: `Categories`, `Tags` (10-minute sliding expiration; bust on Admin create/update/delete)
- Do **not** cache user-specific or tenant-specific lists — risk of cross-tenant data leakage
- Leaderboards are served from `LeaderboardSnapshot` (precomputed monthly) — never computed on-demand from raw XP events

### Backend — Background Jobs
- Long-running or non-latency-sensitive operations run via `IHostedService` (Phase 1/2) or Hangfire (Phase 3):
  - Weekly digest email generation
  - Monthly leaderboard snapshot generation
  - Streak recalculation (nightly)
  - XP aggregate denormalisation (if needed for performance)
  - `AfterActionReview` published notification fan-out

### Database — Indexing Strategy
- **Index every FK column** (in addition to primary key)
- **Composite indexes** for multi-column filter patterns (e.g., `(TenantId, Status)` on `SessionProposals`; `(TenantId, UserId, LearningPathId)` on `UserLearningPathEnrollments`)
- **Partial indexes** for hot filtered queries: `WHERE IsActive = true` on `Categories`/`Tags`; `WHERE Status = 'Open'` on `KnowledgeRequests`
- **JSONB columns** (`QuizQuestions.Options`, `UserQuizAttempts.Answers`, `LeaderboardSnapshots.Entries`) avoid over-normalisation for volatile semi-structured data; use `jsonb_path_exists` for indexed JSONB queries in Phase 3
- Analyze slow queries with `EXPLAIN ANALYZE` before adding new indexes; document all non-obvious indexes with a comment in the migration file
- Connection pooling via Npgsql's built-in pool (default 10; tune `MaxPoolSize` for production load)

### Frontend — Data Fetching
- **TanStack Query `staleTime` per data type**:
  - Reference data (categories, tags): `5 * 60 * 1000` (5 min)
  - Session lists, leaderboards: `2 * 60 * 1000` (2 min)
  - Notifications: `30 * 1000` (30 s) with background refetch
  - User XP/streak: `60 * 1000` (1 min)
- **Optimistic updates**: use TanStack Query's `onMutate`/`onError` rollback pattern for like, follow, register, quiz-submit actions — do not wait for server round-trip for UI feedback
- **Debounce search inputs**: 300 ms debounce before firing tag/speaker/session search API calls
- **Axios interceptors**: centralised auth token injection and 401 → token-refresh handling — never add `Authorization` headers per-request in individual API files

### Frontend — Rendering
- **Code-split by feature route**: `React.lazy` + `Suspense` for every `features/` subfolder — load only what the current page needs
- **Virtualise long lists**: MUI DataGrid with virtual scrolling for session catalogs, leaderboards, and notification lists exceeding 50 rows
- **Lazy-load images**: use `loading="lazy"` on all `<img>` tags; profile photos and cover images served from CDN with responsive size variants

---

## Testing Conventions

### File Location
- Tests mirror the namespace/path of the class under test
- `backend/tests/KnowHub.Tests/Services/` — for service layer tests
- `backend/tests/KnowHub.Tests/Validators/` — for FluentValidation validator tests
- `backend/tests/KnowHub.Tests/Security/` — for middleware and auth tests

### Test Naming
```csharp
// Pattern: MethodName_Scenario_ExpectedOutcome
SubmitProposalAsync_WhenUserIsNotContributor_ShouldThrowForbiddenException()
ApproveProposalAsync_WhenAlreadyApproved_ShouldThrowBusinessRuleException()
GetSessionsAsync_WhenNoSessionsExist_ShouldReturnEmptyPagedResult()
```

### Test Structure — Arrange / Act / Assert
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedOutcome()
{
    // Arrange
    var db = TestDbFactory.Create();
    // ... setup

    // Act
    var result = await sut.MethodAsync(input, CancellationToken.None);

    // Assert
    Assert.Equal(expected, result);
}
```

### Test Helpers (in `TestHelpers/`)
| Helper | Purpose |
|--------|---------|
| `TestDbFactory.Create()` | Creates fresh EF Core InMemory DB with tenant-scoped context |
| `FakeCurrentUserAccessor` | Fake `ICurrentUserAccessor` with configurable `UserId`, `TenantId`, `Role` |
| `FakeNotificationService` | Captures sent notifications for assertion |
| `FakeEmailService` | Captures sent emails for assertion |

### Creating New Fakes
When a new interface needs faking: create a `FakeXxx` sealed class in `TestHelpers/`, constructor-inject test data, implement interface minimally.

---

## Security & OWASP Top 10 Controls

> Map every new feature against this checklist. All controls are mandatory — not optional.

### OWASP A01 — Broken Access Control
- Every controller action enforces role-based authorisation via `[Authorize(Policy = "...")]` — no unannotated endpoints except `/health` and `/api/auth/login`
- `ICurrentUserAccessor` is the **only** source of truth for current user identity, tenant, and roles — never trust any value from the request body for identity
- All service methods validate that the requested resource's `TenantId` matches `ICurrentUserAccessor.TenantId` before any data access
- Resource ownership checks enforced in the service layer: only the proposal owner can edit a draft; only the session speaker can upload materials; only the quiz author can edit questions
- Skill endorsements: service validates the endorser has `SessionRegistration.Status = Attended` for the claimed session before inserting
- XP bounty deduction: service validates requester has sufficient XP balance before setting `BountyXp` on a `KnowledgeRequest`

### OWASP A02 — Cryptographic Failures
- Passwords hashed with **BCrypt** (cost factor ≥ 12) — never MD5, SHA1, or SHA256 for password storage
- JWT tokens signed with **RS256** (asymmetric key pair); private key stored in environment variables / Azure Key Vault — never in `appsettings.json` committed to source control
- **HTTPS enforced** in all non-development environments; HSTS header required in production
- DB connection strings, JWT secrets, integration API keys — stored in environment variables or a secrets manager; never hardcoded or committed
- TLS 1.2+ required for all PostgreSQL connections (Npgsql `SSL Mode=Require`)
- `CertificateNumber` in `LearningPathCertificates` generated with `Guid.NewGuid()` — not sequential integers

### OWASP A03 — Injection
- **All DB access via EF Core parameterized queries** — never string-concatenate user input into SQL; no raw `FromSql("")` with interpolation
- `CommunityWikiPage.ContentMarkdown` sanitised through a **whitelist-based Markdown sanitizer** (strip `<script>`, `<iframe>`, `javascript:` links, `data:` URIs) before storage and before client rendering
- Quiz question options (JSONB) validated with FluentValidation for max array length and max string length per option before persistence
- File/asset URLs validated against an **allowlist of trusted domains** (configurable per tenant) — no open redirect or SSRF via URL fields
- Tag names and slug fields validated for allowed character sets (alphanumeric + hyphen only) — enforced in FluentValidation validators

### OWASP A04 — Insecure Design
- **XP events are created server-side only** — `UserXpEvent` records are never accepted from client input; every XP-granting action is triggered by a corresponding domain event in the service layer
- `LeaderboardSnapshot` generated by a background job — not computed on-demand from unvalidated client parameters
- Quiz `MaxAttempts` limit enforced in the service layer (count existing attempts before creating) — not client-enforced
- `SkillEndorsement` uniqueness enforced at DB level (unique constraint) AND in the service layer to prevent double-endorsement
- `BountyXp` deducted from requester's XP only on claim confirmation — not on request creation; rollback if claim is rejected

### OWASP A05 — Security Misconfiguration
- **CORS**: allow only `http://localhost:5173` in development; explicit origin allowlist in production (no wildcard `*`)
- **Swagger UI disabled in production** (`if (!app.Environment.IsDevelopment())` guard)
- **HTTP security headers** required in production: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Content-Security-Policy` with explicit source allowlist
- PostgreSQL: use a dedicated application DB user with minimal privileges (no `SUPERUSER`, no `CREATEDB`); never use default `postgres` credentials in any environment
- EF Core migrations: **never auto-applied at startup in production** — run via explicit `dotnet ef database update` in CI/CD pipeline
- Remove all default ASP.NET Core error pages that expose stack traces; rely on `ExceptionHandlingMiddleware` + RFC 7807 Problem Details only

### OWASP A06 — Vulnerable & Outdated Components
- Dependabot enabled for both NuGet (`.csproj`) and npm (`package.json`) dependency updates
- Use only packages with active maintainers and < 1 year since last release
- Run `dotnet list package --vulnerable` and `npm audit` in CI pipeline; fail build on HIGH/CRITICAL CVEs

### OWASP A07 — Identification & Authentication Failures
- JWT access token expiry: **15 minutes**; refresh token expiry: **7 days** (stored server-side as a hashed value; delivered in `httpOnly`, `Secure`, `SameSite=Strict` cookie)
- **Refresh token rotation**: every use of a refresh token invalidates the old one and issues a new one
- **Invalidate all tokens on password change** or account suspension
- **Account lockout**: 5 consecutive failed login attempts → 15-minute lockout window; lockout event logged at WARNING level
- SSO flows (Microsoft Entra ID, Google OAuth): validate all claims from the identity provider before issuing KnowHub's own JWT — never pass through the IdP token directly to the frontend
- Email verification required before a new account can submit proposals or access content beyond discovery

### OWASP A08 — Software & Data Integrity Failures
- `RecordVersion` (optimistic concurrency) on every entity prevents silent lost-update attacks
- JWT signature verification on every request — unsigned or algorithm-swapped tokens rejected via `ValidateIssuerSigningKey = true`
- Do **not** deserialise JSONB content from the DB into executable objects — treat stored JSONB as data only, validate schema on read in the service layer
- Background job payloads (Hangfire Phase 3) signed with HMAC to prevent job-injection attacks

### OWASP A09 — Security Logging & Monitoring Failures
- **Structured logging via Serilog** with JSON output: log authentication events (success/failure/lockout), authorisation failures (403), proposal status transitions, admin operations
- **Never log PII**: use anonymised user IDs (`UserId` GUID) in all log entries — no emails, full names, or session content in logs
- Built-in audit trail: every entity has `CreatedBy`, `ModifiedBy`, `CreatedDate`, `ModifiedOn`; never modify these fields post-insert
- Failed access attempts (403, 401) and validation errors (422) emit at WARNING level in structured logs
- In production: logs shipped to a centralised SIEM/log aggregator with alerting on anomalous patterns (e.g., > 10 consecutive 401s from same IP)

### OWASP A10 — Server-Side Request Forgery (SSRF)
- Meeting links, asset URLs, recording URLs, and material links are **validated against a configurable per-tenant domain allowlist** before persistence — no server-side fetching of user-supplied URLs except through a dedicated `ExternalUrlValidationService`
- `ExternalUrlValidationService` checks: scheme must be `https://`, hostname must be in allowlist, no IP addresses, no localhost/internal ranges
- Phase 3 integration webhooks (Teams, Zoom callbacks) validated with **HMAC-SHA256 signature verification** before processing — reject any webhook payload that fails signature check
- No server-side proxying of user-supplied resource URLs without domain allowlist enforcement

### Additional Security Rules
- JWT auth required on **all** endpoints except `/health` and `/api/auth/login`
- `TenantId` is **never** trusted from the request body or query string — always sourced from the validated JWT via `ICurrentUserAccessor`
- Private sessions are accessible only to registered participants, the speaker, and Admins/KnowledgeTeam
- Learning path certificates: `CertificateUrl` must be a signed URL with expiry (pre-signed storage URL) — not a publicly guessable path
- SSO: Microsoft Entra ID, Google OAuth — handled at the auth layer; the platform always issues its own short-lived JWT after SSO validation

---

## Frontend Structure

```
frontend/src/
  features/
    auth/                  — Login page, SSO, JWT storage, auth context
    dashboard/             — Home feed, trending sessions, recommendations, XP widget
    session-proposals/     — Proposal form, submission, approval status tracking
    sessions/              — Session list, detail, registration, materials, chapters
    communities/           — Community list, community page, membership, wiki pages
    knowledge-requests/    — Browse, submit, upvote, claim topic requests
    contributor-profile/   — Speaker profile: sessions, ratings, badges, endorsements
    notifications/         — Notification centre, mark-read, toast alerts
    content-repository/    — Knowledge assets browser, bundles, after-action reviews (Phase 2)
    learning-paths/        — Browse paths, enrol, track progress, certificates (Phase 2)
    quizzes/               — Quiz attempt UI, results, pass/fail feedback (Phase 2)
    gamification/          — XP history, badges, leaderboards, streaks (Phase 2)
    mentoring/             — Mentor/mentee matching, pairing management (Phase 2)
    admin/                 — Admin panel: users, categories, tags, moderation
    analytics/             — Analytics dashboard, heatmaps, reports (Phase 3)
  shared/
    api/                   — Axios API client functions (one file per domain resource)
    components/            — Reusable MUI components (PageHeader, DataTable, StatusChip,
                               RatingStars, TagChip, XpBadge, StreakIndicator, ProgressBar, etc.)
    hooks/                 — Custom React hooks (useAuth, useCurrentUser, useTenantId,
                               useNotifications, useXp, useLeaderboard, useStreak, etc.)
    constants/             — App-wide constants (Roles, ProposalStatus, SessionFormat,
                               DifficultyLevel, XpEventType, BadgeCategory, LeaderboardType)
    utils/                 — Pure utility functions (date formatting, slug generation,
                               permission checks, xp formatting, certificate URL helpers)
  entities/
    models.ts              — TypeScript interfaces mirroring all C# DTOs exactly
```
