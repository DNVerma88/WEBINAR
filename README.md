# KnowHub — Enterprise Knowledge-Sharing & Webinar Platform

> An internal platform that enables employees to propose, schedule, discover, attend, and archive knowledge sessions. It replaces ad-hoc knowledge sharing with a governed, searchable, and gamified system featuring AI-powered recommendations, multi-step approval workflows, and enterprise integrations.

---

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Tech Stack](#tech-stack)
4. [Project Structure](#project-structure)
5. [Backend](#backend)
6. [Frontend](#frontend)
7. [Database](#database)
8. [API Reference](#api-reference)
9. [Authentication & Roles](#authentication--roles)
10. [Configuration](#configuration)
11. [Setup & Running Locally](#setup--running-locally)
12. [Running with Docker](#running-with-docker)
13. [Testing](#testing)
14. [Feature Roadmap](#feature-roadmap)

---

## Overview

KnowHub is a multi-tenant enterprise knowledge platform built across three phases:

| Phase | Focus |
|-------|-------|
| **Phase 1** | Core platform: proposals, approvals, sessions, RBAC, notifications |
| **Phase 2** | Content, engagement & learning: XP, badges, leaderboards, learning paths, quizzes, communities, mentoring, knowledge assets |
| **Phase 3** | Intelligence, scale & governance: AI recommendations, analytics dashboards, enterprise integrations (Teams, Zoom, Slack, Outlook), speaker marketplace, moderation |

**Key capabilities:**
- Knowledge session proposal submission with a governed multi-step approval workflow
- Contributor profiles showcasing expertise, past sessions, ratings, and followers
- Session discovery: browse by category, tag, department, speaker; keyword search
- Participant registration, waitlist, and pre-reading materials
- Gamification: XP points, badges, leaderboards, learning streaks
- Learning Paths with milestones, cohort enrolment, and completion certificates
- Communities with wiki pages, posts, moderation, and discussion feeds
- AI-powered session summaries, content recommendations, and knowledge-gap detection
- Survey module for structured feedback and analytics
- Talent module for resume screening and AI assessments

---

## Architecture

The system follows **Clean Architecture** with strict dependency rules:

```
KnowHub.Domain          ← Entities, enums, domain rules (no external dependencies)
KnowHub.Application     ← Interfaces, DTOs, use-case services, CQRS-style dispatch
KnowHub.Infrastructure  ← EF Core, email, AI integrations, service implementations
KnowHub.Api             ← ASP.NET Core controllers, middleware, SignalR hubs, DI wiring
KnowHub.Tests           ← xUnit unit tests with in-memory database
```

**Dependency rule**: Inner layers never reference outer layers.
`Domain ← Application ← Infrastructure ← API`

### High-Level System Context

```
┌─────────────────────────────────────────────────────────────────┐
│                        KnowHub Platform                         │
│                                                                 │
│   ┌─────────────────┐   REST/SignalR   ┌──────────────────┐    │
│   │  React 18 SPA   │ ◄─────────────► │ ASP.NET Core API │    │
│   │  (Vite + TS)    │                 │   (.NET 10)       │    │
│   └─────────────────┘                 └────────┬─────────┘    │
│         :5173 (dev)                            │ EF Core       │
│         :80 (prod)                    ┌────────▼─────────┐    │
│                                       │  PostgreSQL 16    │    │
│                                       │  (knowhub_dev)    │    │
│                                       └──────────────────┘    │
└─────────────────────────────────────────────────────────────────┘

External: AWS SES (email) · OpenAI / Azure OpenAI / Gemini (AI)
          Redis (caching) · Teams / Zoom / Slack / Outlook (integrations)
```

---

## Tech Stack

### Backend
| Component | Technology |
|-----------|-----------|
| Runtime | .NET 10 / ASP.NET Core |
| ORM | Entity Framework Core 10 |
| Database driver | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Database | PostgreSQL 16 |
| Validation | FluentValidation |
| CQRS dispatch | MediatR |
| Real-time | SignalR |
| Caching | Redis 7 |
| Email | AWS SES (SMTP) |
| AI | OpenAI / Azure OpenAI / Google Gemini |
| Auth | JWT Bearer tokens with refresh token support |
| Testing | xUnit, EF Core InMemory |

### Frontend
| Component | Technology |
|-----------|-----------|
| Framework | React 19 + Vite 8 + TypeScript 5.9 |
| UI library | MUI (Material UI v7) |
| Routing | React Router v7 |
| HTTP client | Axios (centralised in `shared/api/`) |
| Forms | React Hook Form + Zod validation |
| Server state | TanStack Query (React Query v5) |
| Real-time | Microsoft SignalR client |
| Auth | MSAL Browser (Azure AD) |
| Charts | Recharts |
| Markdown | `@uiw/react-md-editor` |
| PWA | `vite-plugin-pwa` + Workbox |

### Infrastructure
| Component | Technology |
|-----------|-----------|
| Containerisation | Docker + Docker Compose |
| Web server (frontend) | Nginx |
| CI/Database migrations | Sequential SQL scripts (`database/sql/`) |

---

## Project Structure

```
KnowHub.slnx                        ← Solution file
docker-compose.yml                  ← Full-stack local orchestration
├── backend/
│   └── src/
│       ├── KnowHub.Api/            ← ASP.NET Core entry point
│       │   ├── Controllers/        ← REST API controllers (~45 controllers)
│       │   ├── Hubs/               ← SignalR real-time hubs
│       │   ├── Middleware/         ← Global exception handling
│       │   ├── Extensions/         ← Startup service registration
│       │   └── Program.cs
│       ├── KnowHub.Application/
│       │   ├── Contracts/          ← Service interfaces
│       │   ├── Models/             ← Request/response DTOs
│       │   ├── Validators/         ← FluentValidation validators
│       │   └── Utilities/          ← Shared helpers
│       ├── KnowHub.Domain/
│       │   ├── Entities/           ← EF Core entity classes
│       │   ├── Enums/              ← Domain enumerations
│       │   └── Exceptions/         ← Domain exception types
│       └── KnowHub.Infrastructure/
│           ├── AI/                 ← OpenAI / Gemini integrations
│           ├── BackgroundServices/ ← Hosted background workers
│           ├── Email/              ← AWS SES email service
│           ├── Extensions/         ← DI service registrations
│           └── Persistence/        ← EF Core DbContext + configurations
│   └── tests/
│       └── KnowHub.Tests/          ← xUnit unit tests
│           └── TestHelpers/        ← FakeXxx helpers, in-memory DB setup
├── frontend/
│   └── src/
│       ├── features/               ← Feature-scoped modules
│       │   ├── admin/
│       │   ├── analytics/
│       │   ├── assessment/
│       │   ├── auth/
│       │   ├── categories/
│       │   ├── communities/
│       │   ├── dashboard/
│       │   ├── feed/
│       │   ├── knowledge-assets/
│       │   ├── knowledge-bundles/
│       │   ├── knowledge-requests/
│       │   ├── leaderboards/
│       │   ├── learning-paths/
│       │   ├── mentoring/
│       │   ├── notifications/
│       │   ├── profile/
│       │   ├── proposals/
│       │   ├── sessions/
│       │   ├── speaker-marketplace/
│       │   ├── speakers/
│       │   ├── surveys/
│       │   ├── tags/
│       │   └── talent/
│       ├── shared/                 ← Shared components, API client, hooks
│       ├── App.tsx
│       ├── routes.tsx
│       └── main.tsx
└── database/
    └── sql/                        ← Ordered migration scripts
        ├── 001_Init.sql
        ├── 002_Seed.sql
        ├── 003_Phase2.sql
        └── ...018_PerformanceFixes.sql
```

---

## Backend

### Key Design Patterns

- **Clean Architecture** — strict layer separation; Domain has no external dependencies
- **CQRS via MediatR** — commands and queries dispatched through `IMediator`
- **Repository pattern** — data access abstracted behind contracts in `KnowHub.Application`
- **Global exception middleware** — all unhandled exceptions converted to RFC 7807 problem details
- **FluentValidation** — all input validated via DI-registered validators, never inline
- **Multi-tenancy** — every query filtered by `TenantId`; enforced at the EF Core query level
- **JWT + Refresh tokens** — short-lived access tokens (60 min) + long-lived refresh tokens (7 days)

### API Default Port

```
http://localhost:5200
```

### Proposal Status Lifecycle

```
Draft → Submitted → ManagerReview → (Manager Approved) →
  KnowledgeTeamReview → (KT Approved) → Published →
    (Session Created) → Scheduled → InProgress → Completed / Cancelled

Rejected (at any review stage)
RevisionRequested → (author revises) → Submitted
```

---

## Frontend

### Development Port

```
http://localhost:5173
```

### Architecture Conventions

- All HTTP calls go through the **Axios client** in `frontend/src/shared/api/`
- All forms use **React Hook Form** with **Zod** schema validation
- Server state is managed exclusively with **TanStack Query**
- UI components use **MUI only** — no mixing of UI libraries
- Permission checks use `isAdminOrAbove = isAdmin || isSuperAdmin` — never `isAdmin` alone
- Real-time updates via **SignalR** (`@microsoft/signalr`)

### Feature Modules

| Module | Description |
|--------|-------------|
| `auth` | Login, registration, password reset, Azure AD SSO |
| `dashboard` | Personalised activity feed and quick-access widgets |
| `proposals` | Submit, track, and manage knowledge session proposals |
| `sessions` | Browse, register, attend, and rate sessions |
| `speakers` | Contributor profiles, expertise, past sessions, endorsements |
| `speaker-marketplace` | AI-powered expert discovery and routing |
| `knowledge-assets` | Post-session recordings, slides, code, FAQs |
| `knowledge-bundles` | Curated themed collections of knowledge assets |
| `knowledge-requests` | Request sessions on a topic; XP bounty system |
| `learning-paths` | Ordered learning sequences with milestones and certificates |
| `communities` | Knowledge communities with posts, wiki, and moderation |
| `feed` | Real-time activity feed with discussion threads |
| `leaderboards` | XP-based monthly leaderboards by dimension |
| `mentoring` | Mentor/mentee pairing and session management |
| `surveys` | Survey builder, distribution, responses, and analytics |
| `assessment` | AI-powered employee skills assessments |
| `talent` | Resume builder and AI resume screening |
| `analytics` | Knowledge gap heatmaps, engagement, and retention reports |
| `admin` | User management, content moderation, categories, tags |
| `notifications` | In-app notifications with real-time SignalR updates |
| `profile` | User profile, skills, badges, XP history, learning streak |
| `categories` / `tags` | Taxonomy management |

---

## Database

### Engine

**PostgreSQL 16** — multi-tenant, all data isolated by `TenantId`.

### Connection Details (local / Docker)

| Setting | Value |
|---------|-------|
| Host | `localhost` (or `postgres` inside Docker) |
| Port | `5432` |
| Database | `knowhub_dev` |
| Username | `knowhub` |
| Password | `knowhub_secret` |

### Migrations

Migrations are plain SQL scripts applied in order at container startup via Docker Compose volume mounts:

| Script | Description |
|--------|-------------|
| `001_Init.sql` | Core schema — users, sessions, proposals, registrations |
| `002_Seed.sql` | Reference data — default tenant, categories, tags, admin user |
| `003_Phase2.sql` | Phase 2 additions — assets, ratings, badges, XP, learning paths |
| `004_Cohorts.sql` | Learning path cohorts |
| `005_AddPhase3Features.sql` | Phase 3 schema foundations |
| `006_AIAssessmentModule.sql` | AI assessment module tables |
| `007_WorkRoles.sql` | Work roles taxonomy |
| `008_GeneralizeAssessmentModule.sql` | Generalised assessment schema |
| `009_MissingIndexes.sql` | Performance indexes |
| `009_TalentModule.sql` | Talent / resume screening tables |
| `010_AddAchievements.sql` | Achievement and badge system |
| `011_SurveyModule.sql` | Survey module tables |
| `012_SurveyAnalytics.sql` | Survey analytics aggregation |
| `013_FixRatingConstraint.sql` | Rating constraint fix |
| `014_AddSurveyEndsAt.sql` | Survey end-date field |
| `015_AddPromptTemplate.sql` | AI prompt template storage |
| `016_DevCommunityEnhancement.sql` | Community feed enhancements |
| `017_Phase2_FeedAndModeration.sql` | Feed and moderation tables |
| `018_PerformanceFixes.sql` | Performance and index improvements |

### Key Entities

| Entity | Description |
|--------|-------------|
| `User` | Platform user with role flags, department, profile photo |
| `ContributorProfile` | Extended profile for knowledge contributors |
| `Session` | Scheduled knowledge session (webinar, workshop, demo, etc.) |
| `SessionProposal` | Proposal for a new session — goes through approval workflow |
| `ProposalApproval` | Individual approval step (Manager → KnowledgeTeam) |
| `KnowledgeAsset` | Post-session artifact (recording, slides, code, FAQ) |
| `LearningPath` | Curated ordered sequence of sessions and assets |
| `Community` | Knowledge community with members, posts, and wiki |
| `SessionQuiz` | Post-session knowledge-retention assessment |
| `UserXpEvent` | Append-only XP ledger (never updated or deleted) |
| `UserLearningStreak` | Current and longest streak tracking |
| `LeaderboardSnapshot` | Monthly JSONB snapshot of top-N users |
| `MentorMentee` | Mentor/mentee pairing lifecycle |
| `Survey` | Survey definition with questions and distribution |

---

## API Reference

The API is available at `http://localhost:5200`. All endpoints require a **Bearer JWT token** (except auth endpoints).

### Controller Modules

| Controller | Base Path | Description |
|------------|-----------|-------------|
| `AuthController` | `/api/auth` | Register, login, refresh token, password reset |
| `UsersController` | `/api/users` | Profile management, skills, followers |
| `SessionProposalsController` | `/api/proposals` | CRUD proposals, submit, approve, reject |
| `SessionsController` | `/api/sessions` | Session management, registration, materials |
| `SpeakersController` | `/api/speakers` | Contributor profiles, endorsements |
| `KnowledgeAssetsController` | `/api/knowledge-assets` | Post-session content repository |
| `KnowledgeBundlesController` | `/api/knowledge-bundles` | Curated asset bundles |
| `LearningPathsController` | `/api/learning-paths` | Learning path CRUD, enrolment, progress |
| `CommunitiesController` | `/api/communities` | Communities, members, wiki, moderation |
| `CommunityPostsController` | `/api/community-posts` | Community posts and discussions |
| `FeedController` | `/api/feed` | Personalised activity feed |
| `KnowledgeRequestsController` | `/api/knowledge-requests` | Topic requests, upvotes, bounties |
| `LeaderboardsController` | `/api/leaderboards` | XP leaderboards by dimension |
| `MentoringController` | `/api/mentoring` | Mentor/mentee pairings |
| `SurveysController` | `/api/surveys` | Survey management |
| `SurveyFormController` | `/api/survey-form` | Public survey response submission |
| `SurveyAnalyticsController` | `/api/survey-analytics` | Survey results and analytics |
| `NotificationsController` | `/api/notifications` | In-app notifications |
| `AnalyticsController` | `/api/analytics` | Platform-wide analytics and reporting |
| `AiController` | `/api/ai` | AI-powered recommendations and summaries |
| `AssessmentPeriodsController` | `/api/assessment-periods` | Assessment period management |
| `ResumeScreenerController` | `/api/resume-screener` | AI resume screening |
| `ResumeBuilderController` | `/api/resume-builder` | Resume builder |
| `SpeakerMarketplaceController` | `/api/speaker-marketplace` | Expert discovery |
| `CategoriesController` | `/api/categories` | Session categories |
| `TagsController` | `/api/tags` | Session/skill tags |
| `ModerationController` | `/api/moderation` | Content flagging and moderation |
| `StorageController` | `/api/storage` | File upload/download |

---

## Authentication & Roles

### JWT Configuration

- Access token validity: **60 minutes**
- Refresh token validity: **7 days**
- Roles stored as a **`[Flags]` integer** in the JWT `role` claim
- `MapInboundClaims = false` is required on `AddJwtBearer`

### User Roles

| Role | Value | Capabilities |
|------|-------|-------------|
| `Employee` | 1 | Browse sessions, register, rate, comment, request topics, follow contributors |
| `Contributor` | 2 | All Employee + submit proposals, deliver sessions, manage content, build profile |
| `Manager` | 4 | All Employee + first-tier proposal approval |
| `KnowledgeTeam` | 8 | All Employee + second-tier approval, manage categories/tags, moderate content |
| `Admin` | 16 | Full tenant management |
| `SuperAdmin` | 32 | All Admin + cross-tenant management |

> Users can hold multiple roles simultaneously (flags). A user with `Contributor | Manager` has value `6`.  
> `Admin` (16) and `SuperAdmin` (32) have identical feature-level permissions. Always check `isAdminOrAbove` — never `isAdmin` alone.

---

## Configuration

### Backend — `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=knowhub_dev;Username=knowhub;Password=knowhub_secret"
  },
  "Jwt": {
    "Key": "<min-32-char-secret>",
    "Issuer": "KnowHub",
    "Audience": "KnowHub",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  },
  "Cors": {
    "FrontendOrigin": "http://localhost:5173"
  },
  "AI": {
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "<your-openai-api-key>",
      "Model": "gpt-4o-mini",
      "EmbeddingModel": "text-embedding-3-small"
    }
  },
  "Email": {
    "SMTP": {
      "Host": "email-smtp.<region>.amazonaws.com",
      "Port": 587,
      "Username": "<ses-smtp-username>",
      "Password": "<ses-smtp-password>",
      "EnableSsl": true
    }
  }
}
```

### Frontend — Environment Variables

Create `frontend/.env.local`:

```env
VITE_API_BASE_URL=http://localhost:5200
VITE_SIGNALR_HUB_URL=http://localhost:5200/hubs
```

---

## Setup & Running Locally

### Prerequisites

| Tool | Minimum version |
|------|----------------|
| [.NET SDK](https://dotnet.microsoft.com/) | 10.0 |
| [Node.js](https://nodejs.org/) | 20 LTS |
| [PostgreSQL](https://www.postgresql.org/) | 16 (or use Docker) |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Latest (optional) |
| [Redis](https://redis.io/) | 7 (or use Docker) |

### 1. Clone the repository

```bash
git clone https://github.com/DNVerma88/WEBINAR.git
cd WEBINAR
```

### 2. Set up the database

Start PostgreSQL locally (or use Docker — see [Running with Docker](#running-with-docker)) and run the migration scripts in order:

```bash
psql -U knowhub -d knowhub_dev -f database/sql/001_Init.sql
psql -U knowhub -d knowhub_dev -f database/sql/002_Seed.sql
# ... continue through 018_PerformanceFixes.sql
```

Or simply use Docker Compose which applies all scripts automatically.

### 3. Configure the backend

Copy and edit the development settings:

```bash
cp backend/src/KnowHub.Api/appsettings.json backend/src/KnowHub.Api/appsettings.Development.json
```

Update `appsettings.Development.json` with your local database connection string and JWT secret.

### 4. Run the backend API

```powershell
cd backend/src/KnowHub.Api
dotnet run
# API available at http://localhost:5200
```

### 5. Install frontend dependencies

```bash
cd frontend
npm install
```

### 6. Configure the frontend

Create `frontend/.env.local`:

```env
VITE_API_BASE_URL=http://localhost:5200
VITE_SIGNALR_HUB_URL=http://localhost:5200/hubs
```

### 7. Run the frontend dev server

```bash
cd frontend
npm run dev
# App available at http://localhost:5173
```

### Default Admin Credentials (from seed data)

| Field | Value |
|-------|-------|
| Email | See `database/sql/002_Seed.sql` |
| Role | `SuperAdmin` |

---

## Running with Docker

The entire stack (PostgreSQL, Redis, API, Frontend) can be started with a single command:

```bash
docker compose up --build
```

| Service | Container | Port |
|---------|-----------|------|
| PostgreSQL 16 | `knowhub-postgres` | `5432` |
| Redis 7 | `knowhub-redis` | `6379` |
| ASP.NET Core API | `knowhub-api` | `5200 → 8080` |
| React Frontend | `knowhub-frontend` | `80 → 8080` |

All database migration scripts are applied automatically on first startup.

### Stop all services

```bash
docker compose down
```

### Stop and remove all data volumes

```bash
docker compose down -v
```

---

## Testing

```powershell
# Run all tests
dotnet test KnowHub.slnx

# Run tests with code coverage
dotnet test KnowHub.slnx --collect:"XPlat Code Coverage"

# Build the entire solution
dotnet build KnowHub.slnx
```

### Test Conventions

- Framework: **xUnit** only (no NUnit, no MSTest)
- Database: **EF Core InMemory** — no real database required
- Mocking: **hand-written `FakeXxx` classes** only (no Moq, no NSubstitute)
- Coverage: **coverlet** (collected with `--collect:"XPlat Code Coverage"`)
- Test project location: `backend/tests/KnowHub.Tests/`
- Test helpers location: `backend/tests/KnowHub.Tests/TestHelpers/`

---

## Feature Roadmap

### Phase 1 — Core Platform ✅
- [x] User registration and profile management
- [x] Knowledge session proposal and multi-step approval workflow
- [x] Session scheduling with meeting link and participant limit
- [x] Session discovery (browse, search, filter)
- [x] Participant registration and waitlist
- [x] Role-based access control (6 roles with flags support)
- [x] Basic notification system

### Phase 2 — Content, Engagement & Learning ✅
- [x] Knowledge Content Repository (recordings, slides, code, FAQs)
- [x] Session Chapters (timestamped in-video navigation)
- [x] Knowledge Bundles (curated asset collections)
- [x] After-Action Reviews
- [x] Q&A, discussion threads, comments, likes, bookmarks
- [x] Session and speaker ratings and feedback
- [x] Session Quizzes (MCQ/True-False, auto-graded)
- [x] XP Points System (append-only `UserXpEvent` ledger)
- [x] Typed badge categories and monthly leaderboards
- [x] Learning Paths with milestones and certificates
- [x] Learning Path Cohorts (department-assigned, mandatory)
- [x] Skill Endorsements (session-validated)
- [x] Community Wiki Pages
- [x] Knowledge Requests with XP bounty system
- [x] Weekly Digest Emails
- [x] Learning streak tracking
- [x] Mentor/Mentee pairing

### Phase 3 — Intelligence, Scale & Governance 🚧
- [x] AI session summaries and content recommendations
- [x] Survey module (builder, distribution, analytics)
- [x] Talent module (resume screening, AI assessments)
- [x] Speaker Marketplace
- [x] Feed and moderation system
- [ ] Knowledge Analytics Dashboard (Gap Heatmap, Skill Coverage, Learning Funnel)
- [ ] Full enterprise integrations (Teams, Zoom, Slack, Outlook Calendar)
- [ ] LMS/SCORM/xAPI content import
- [ ] HR system sync for org-chart and department data
- [ ] Mobile PWA with offline reading mode

---

## Contributing

1. Create a feature branch: `git checkout -b feature/your-feature-name`
2. Make your changes following the Clean Architecture conventions
3. Write xUnit tests for all new logic
4. Run `dotnet test KnowHub.slnx` to verify all tests pass
5. Run `npm run build` in `frontend/` to verify the frontend builds
6. Submit a pull request against `main`

---

## License

Internal enterprise project. All rights reserved.
