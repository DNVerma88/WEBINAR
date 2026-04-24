# Dev Community — Enhancement Plan v1.0
## Inspired by Dev.to · Medium · Hashnode · Stack Overflow · Reddit

> **Status**: Planning Complete — Ready for Implementation
> **Scope**: Full upgrade of the KnowHub Community module from a basic wiki+membership system to a full-featured developer community platform
> **Scalability Target**: 1M+ users, multi-tenant, sub-100ms API responses under load

---

## 1. Gap Analysis — Current vs Industry Leaders

### 1.1 What KnowHub Currently Has

| Area | Current State |
|---|---|
| Community entity | Name, slug, description, icon, cover image, member count, soft-delete |
| Roles | Member, Moderator, KnowledgeBroker, CoLeader |
| Content type | Wiki pages only (markdown, hierarchical, published/draft, viewCount) |
| Social | Join / Leave community |
| Search | Name + description text search |
| API | Community CRUD (Admin only), join/leave, wiki CRUD |
| Feed | Paginated list ordered by name |
| Notifications | None specific to communities |
| Analytics | None |
| Moderation | Role-based page edit restrictions only |

### 1.2 Feature Gaps vs Dev.to / Medium / Hashnode

#### 🔴 Critical Gaps (Core community experience is incomplete)

| Gap | Dev.to | Medium | Hashnode | Impact |
|---|---|---|---|---|
| **Community Posts / Discussions** — users can write articles, share links, ask questions inside a community | ✅ | ✅ | ✅ | Blocker — wiki alone is too rigid |
| **Post Reactions (multi-type)** — Like ❤️, Unicorn 🦄, Exploding head 🤯, etc. | ✅ | ✅ (claps) | ✅ | Engagement anchor |
| **Threaded Comments on posts** — nested replies with collapse | ✅ | ✅ | ✅ | Core engagement |
| **Post Tags / Topics** — tag posts for cross-community discovery | ✅ | ✅ | ✅ | Discoverability |
| **Personalized Feed** — ranked feed mixing posts from joined communities + followed tags | ✅ | ✅ | ✅ | Retention |
| **Follow Users** | ✅ | ✅ | ✅ | Social graph |

#### 🟠 High Impact Gaps

| Gap | Dev.to | Medium | Hashnode |
|---|---|---|---|
| **Post Series / Collections** — group related posts into a numbered series | ✅ | ✅ (series) | ✅ |
| **Reading List / Bookmarks** — save posts to read later | ✅ | ✅ | ✅ |
| **Author Post Analytics** — views, read-time, reactions per post | ✅ | ✅ | ✅ |
| **Pinned / Featured Posts** per community | ✅ | — | ✅ |
| **Community Announcements** (admin-broadcast sticky) | ✅ | — | ✅ |
| **Rich Editor** — WYSIWYG with code blocks + syntax highlighting | ✅ | ✅ | ✅ |
| **Embeds** — GitHub Gist, CodePen, CodeSandbox, YouTube, Twitter | ✅ | ✅ | ✅ |
| **@Mention notifications** | ✅ | — | ✅ |
| **Post scheduling** (publish at a future date/time) | — | ✅ | ✅ |
| **Canonical URL** (cross-posting with SEO protection) | ✅ | ✅ | ✅ |

#### 🟡 Medium Impact Gaps

| Gap | Dev.to | Hashnode |
|---|---|---|
| **Polls** — quick community polls inside posts | ✅ | ✅ |
| **Tag / Topic Following** — personalized content without joining community | ✅ | ✅ |
| **Trending Posts** feed (score = reactions + comments + recency) | ✅ | ✅ |
| **Moderation Queue** — flagged content review workflow | ✅ | ✅ |
| **Comment Flag / Report** | ✅ | ✅ |
| **Community Newsletter / Digest** — weekly email of top posts | ✅ | ✅ |
| **RSS Feeds** per community/author | ✅ | ✅ |
| **Top Contributors** leaderboard per community | ✅ | — |
| **Draft Auto-Save** (every 30 seconds, versioned) | ✅ | ✅ |

#### 🟢 Scalability Gaps (No business feature gap but will break at 1M users)

| Gap | Risk |
|---|---|
| ViewCount incremented synchronously per read | Race condition + DB write on every GET |
| MemberCount +/- updated synchronously in Join/Leave | Contention under burst joins |
| No Redis caching for feeds, hot posts, member counts | Full DB scan per request |
| No search index (Elasticsearch/OpenSearch) for community posts | Full-text queries will SEQSCAN at scale |
| No CDN strategy for community post cover images | High bandwidth, slow edge delivery |
| Community list query has no covering index on sort column | Slow at 10k+ communities |
| No event-driven notification fan-out | Broadcast to 100k followers = sync DB writes per user |
| Wiki/Post content stored as TEXT in OLTP DB | OLAP queries kill query performance |
| No connection pooling documented for community write bursts | PgBouncer not configured in docker-compose |
| Single-instance SignalR (not Redis-backed) | Real-time breaks when scaled to 2+ API pods |

---

## 2. Target Architecture

### 2.1 Conceptual Layer Map

```
┌─────────────────────────────────────────────────────────────────────┐
│  FRONTEND (React + Vite)                                            │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ │
│  │Feed Page │ │Post Write│ │Community │ │Analytics │ │Notif Hub │ │
│  │(personal)│ │(Rich Ed.)│ │Detail    │ │Dashboard │ │(SignalR) │ │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘ │
└──────────────────────┬──────────────────────────────────────────────┘
                       │ HTTP / WebSocket
┌──────────────────────▼──────────────────────────────────────────────┐
│  API LAYER (ASP.NET Core 10)                                        │
│  CommunityPostsController  FeedController  BookmarksController      │
│  PostReactionsController   PostSeriesController  ModerationCtrl     │
└──────────────────────┬──────────────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────────────┐
│  CACHE LAYER (Redis)                                                │
│  Feed cache (per user, 5 min TTL)  Hot post cache (1 min)          │
│  Member count (write-through)      Trending score (sorted set)      │
└──────────────────────┬──────────────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────────────┐
│  DATABASE LAYER (PostgreSQL 16)                                     │
│  CommunityPosts  PostReactions  PostComments  PostSeries            │
│  PostBookmarks   PostTags  Tags  UserFollows  ModerationReports     │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 Event-Driven Fan-Out (Scalability)

```
User Reacts to Post
      │
      ▼
PostReactionService (writes reaction to DB)
      │
      ├──► Redis INCR  post:{id}:reaction_count       (hot counter)
      │
      ├──► Background Channel ──► NotificationFanOutService
      │         └── Reads post author + @mentioned users
      │             └── Inserts Notification rows (batched, 500/tx)
      │             └── SignalR push to online users
      │
      └──► Background Channel ──► TrendingScoreService
                └── Updates Redis ZADD trending:{tenantId} score postId
                    (score = reactions*3 + comments*2 + view_rate*1)
```

### 2.3 Feed Algorithm (Personalized)

```
Feed Score = (ReactionCount * 0.4)
           + (CommentCount  * 0.3)
           + (ViewCount     * 0.1)
           + (FollowBoost   * 0.2)   // post from followed user/tag = 1, else 0
           * RecencyDecay            // exp(-hours_since_post / 48)
```
- Feed computed server-side, cached per user in Redis (5 min TTL, invalidated on new followed-community post)
- First page always served from Redis; subsequent pages hit DB with offset cursor

---

## 3. Database Schema — New Tables

### Migration: `016_DevCommunityEnhancement.sql`

```sql
-- ─────────────────────────────────────────────────────────────────
-- Tags (global, reusable across communities and posts)
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE "Tags" (
    "Id"            UUID         NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID         NOT NULL,
    "Name"          VARCHAR(100) NOT NULL,
    "Slug"          VARCHAR(100) NOT NULL,
    "Description"   TEXT,
    "PostCount"     INT          NOT NULL DEFAULT 0,   -- denormalized
    "IsOfficial"    BOOLEAN      NOT NULL DEFAULT FALSE,
    "CreatedDate"   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID         NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID         NOT NULL,
    "RecordVersion" INT          NOT NULL DEFAULT 1,
    CONSTRAINT "PK_Tags" PRIMARY KEY ("Id")
);
CREATE UNIQUE INDEX "IX_Tags_TenantId_Slug" ON "Tags" ("TenantId", "Slug");

-- ─────────────────────────────────────────────────────────────────
-- Community Posts  (articles, questions, discussions)
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE "CommunityPosts" (
    "Id"              UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"        UUID          NOT NULL,
    "CommunityId"     UUID          NOT NULL,
    "AuthorId"        UUID          NOT NULL,
    "SeriesId"        UUID,                            -- FK PostSeries (nullable)
    "SeriesOrder"     INT,
    "Title"           VARCHAR(300)  NOT NULL,
    "Slug"            VARCHAR(300)  NOT NULL,
    "ContentMarkdown" TEXT          NOT NULL,
    "ContentHtml"     TEXT          NOT NULL,          -- pre-rendered, sanitized
    "CoverImageUrl"   VARCHAR(500),
    "CanonicalUrl"    VARCHAR(1000),                   -- cross-posting SEO
    "PostType"        SMALLINT      NOT NULL DEFAULT 0, -- 0=Article,1=Discussion,2=Question,3=TIL
    "Status"          SMALLINT      NOT NULL DEFAULT 0, -- 0=Draft,1=Published,2=Pinned,3=Archived
    "ReadingTimeMinutes" INT        NOT NULL DEFAULT 1,
    "ReactionCount"   INT           NOT NULL DEFAULT 0, -- denormalized sum
    "CommentCount"    INT           NOT NULL DEFAULT 0, -- denormalized
    "ViewCount"       BIGINT        NOT NULL DEFAULT 0, -- denormalized (async updated)
    "BookmarkCount"   INT           NOT NULL DEFAULT 0, -- denormalized
    "PublishedAt"     TIMESTAMPTZ,
    "ScheduledAt"     TIMESTAMPTZ,                     -- future publish
    "IsFeatured"      BOOLEAN       NOT NULL DEFAULT FALSE,
    "LastDraftSavedAt" TIMESTAMPTZ,
    "CreatedDate"     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"       UUID          NOT NULL,
    "ModifiedOn"      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"      UUID          NOT NULL,
    "RecordVersion"   INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_CommunityPosts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CommunityPosts_Communities" FOREIGN KEY ("CommunityId")
        REFERENCES "Communities" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CommunityPosts_Users_Author" FOREIGN KEY ("AuthorId")
        REFERENCES "Users" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX "IX_CommunityPosts_TenantId_CommunityId_Slug" 
    ON "CommunityPosts" ("TenantId", "CommunityId", "Slug");
CREATE INDEX "IX_CommunityPosts_TenantId_CommunityId_Status_PublishedAt"
    ON "CommunityPosts" ("TenantId", "CommunityId", "Status", "PublishedAt" DESC);
CREATE INDEX "IX_CommunityPosts_TenantId_AuthorId"
    ON "CommunityPosts" ("TenantId", "AuthorId");
CREATE INDEX "IX_CommunityPosts_ScheduledAt" ON "CommunityPosts" ("ScheduledAt")
    WHERE "ScheduledAt" IS NOT NULL AND "Status" = 0;

-- ─────────────────────────────────────────────────────────────────
-- Post Tags  (M:N)
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE "CommunityPostTags" (
    "PostId"       UUID NOT NULL,
    "TagId"        UUID NOT NULL,
    "TenantId"     UUID NOT NULL,
    CONSTRAINT "PK_CommunityPostTags" PRIMARY KEY ("PostId", "TagId"),
    CONSTRAINT "FK_PostTags_Post" FOREIGN KEY ("PostId")
        REFERENCES "CommunityPosts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PostTags_Tag" FOREIGN KEY ("TagId")
        REFERENCES "Tags" ("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_CommunityPostTags_TagId" ON "CommunityPostTags" ("TagId");

-- ─────────────────────────────────────────────────────────────────
-- Post Reactions  (likes, unicorns, etc.)
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE "PostReactions" (
    "Id"            UUID        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID        NOT NULL,
    "PostId"        UUID        NOT NULL,
    "UserId"        UUID        NOT NULL,
    "ReactionType"  SMALLINT    NOT NULL DEFAULT 0,  -- 0=Like,1=Unicorn,2=Mind-Blown,3=Fire
    "CreatedDate"   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_PostReactions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PostReactions_Post" FOREIGN KEY ("PostId")
        REFERENCES "CommunityPosts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "UQ_PostReactions_User_Post_Type"
        UNIQUE ("TenantId", "PostId", "UserId", "ReactionType")
);
CREATE INDEX "IX_PostReactions_PostId" ON "PostReactions" ("PostId");

-- ─────────────────────────────────────────────────────────────────
-- Post Comments  (nested, max 2 levels)
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE "PostComments" (
    "Id"              UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"        UUID          NOT NULL,
    "PostId"          UUID          NOT NULL,
    "AuthorId"        UUID          NOT NULL,
    "ParentCommentId" UUID,
    "BodyMarkdown"    TEXT          NOT NULL,
    "IsDeleted"       BOOLEAN       NOT NULL DEFAULT FALSE,
    "LikeCount"       INT           NOT NULL DEFAULT 0,
    "CreatedDate"     TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedOn"      TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "RecordVersion"   INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_PostComments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PostComments_Post" FOREIGN KEY ("PostId")
        REFERENCES "CommunityPosts" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_PostComments_Parent" FOREIGN KEY ("ParentCommentId")
        REFERENCES "PostComments" ("Id") ON DELETE SET NULL
);
CREATE INDEX "IX_PostComments_PostId_CreatedDate"
    ON "PostComments" ("PostId", "CreatedDate");

-- ─────────────────────────────────────────────────────────────────
-- Post Bookmarks / Reading List
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE "PostBookmarks" (
    "Id"          UUID        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"    UUID        NOT NULL,
    "UserId"      UUID        NOT NULL,
    "PostId"      UUID        NOT NULL,
    "BookmarkedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_PostBookmarks" PRIMARY KEY ("Id"),
    CONSTRAINT "UQ_PostBookmarks_User_Post" UNIQUE ("TenantId", "UserId", "PostId"),
    CONSTRAINT "FK_PostBookmarks_Post" FOREIGN KEY ("PostId")
        REFERENCES "CommunityPosts" ("Id") ON DELETE CASCADE
);
CREATE INDEX "IX_PostBookmarks_UserId" ON "PostBookmarks" ("UserId");

-- ─────────────────────────────────────────────────────────────────
-- Post Series  (e.g. "React for Beginners" — 5 parts)
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE "PostSeries" (
    "Id"            UUID          NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"      UUID          NOT NULL,
    "CommunityId"   UUID          NOT NULL,
    "AuthorId"      UUID          NOT NULL,
    "Title"         VARCHAR(300)  NOT NULL,
    "Description"   TEXT,
    "Slug"          VARCHAR(300)  NOT NULL,
    "PostCount"     INT           NOT NULL DEFAULT 0,
    "CreatedDate"   TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "CreatedBy"     UUID          NOT NULL,
    "ModifiedOn"    TIMESTAMPTZ   NOT NULL DEFAULT NOW(),
    "ModifiedBy"    UUID          NOT NULL,
    "RecordVersion" INT           NOT NULL DEFAULT 1,
    CONSTRAINT "PK_PostSeries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PostSeries_Community" FOREIGN KEY ("CommunityId")
        REFERENCES "Communities" ("Id") ON DELETE CASCADE
);
ALTER TABLE "CommunityPosts"
    ADD CONSTRAINT "FK_CommunityPosts_PostSeries" FOREIGN KEY ("SeriesId")
        REFERENCES "PostSeries" ("Id") ON DELETE SET NULL;

-- ─────────────────────────────────────────────────────────────────
-- User Follows  (follow users AND tags)
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE "UserFollows" (
    "Id"               UUID        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"         UUID        NOT NULL,
    "FollowerId"       UUID        NOT NULL,   -- the user who pressed follow
    "FollowedUserId"   UUID,                   -- nullable: user follow
    "FollowedTagId"    UUID,                   -- nullable: tag follow
    "FollowedAt"       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_UserFollows" PRIMARY KEY ("Id"),
    CONSTRAINT "CHK_UserFollows_Target"
        CHECK ("FollowedUserId" IS NOT NULL OR "FollowedTagId" IS NOT NULL),
    CONSTRAINT "UQ_UserFollows_User" UNIQUE ("TenantId", "FollowerId", "FollowedUserId")
        WHERE "FollowedUserId" IS NOT NULL,
    CONSTRAINT "UQ_UserFollows_Tag"  UNIQUE ("TenantId", "FollowerId", "FollowedTagId")
        WHERE "FollowedTagId" IS NOT NULL
);
CREATE INDEX "IX_UserFollows_FollowerId" ON "UserFollows" ("FollowerId");

-- ─────────────────────────────────────────────────────────────────
-- Content Moderation  (flags, reports)
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE "ContentReports" (
    "Id"              UUID        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"        UUID        NOT NULL,
    "ReporterId"      UUID        NOT NULL,
    "TargetPostId"    UUID,
    "TargetCommentId" UUID,
    "ReasonCode"      SMALLINT    NOT NULL,   -- 0=Spam,1=Abuse,2=Misinformation,3=NSFW
    "Description"     TEXT,
    "Status"          SMALLINT    NOT NULL DEFAULT 0, -- 0=Open,1=Resolved,2=Dismissed
    "ResolvedBy"      UUID,
    "ResolvedAt"      TIMESTAMPTZ,
    "CreatedDate"     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT "PK_ContentReports" PRIMARY KEY ("Id")
);
CREATE INDEX "IX_ContentReports_TenantId_Status" ON "ContentReports" ("TenantId", "Status");

-- ─────────────────────────────────────────────────────────────────
-- Post View Events — append-only for async counter (no contention)
-- ─────────────────────────────────────────────────────────────────
CREATE TABLE "PostViewEvents" (
    "Id"          UUID        NOT NULL DEFAULT gen_random_uuid(),
    "TenantId"    UUID        NOT NULL,
    "PostId"      UUID        NOT NULL,
    "ViewerId"    UUID,                      -- NULL = anonymous
    "ViewedAt"    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "SessionKey"  VARCHAR(64)               -- deduplicate same session
);
-- Partitioned by month for large-scale analytics (manual partition creation)
CREATE INDEX "IX_PostViewEvents_PostId_ViewedAt" ON "PostViewEvents" ("PostId", "ViewedAt");
```

---

## 4. New Domain Entities

### `backend/src/KnowHub.Domain/Entities/Community/`

| Entity | Key Fields | Notes |
|---|---|---|
| `CommunityPost` | Id, CommunityId, AuthorId, SeriesId, Title, Slug, ContentMarkdown, ContentHtml, PostType, Status, ReadingTimeMinutes, reaction/comment/view/bookmark counts, ScheduledAt, PublishedAt, IsFeatured | Core content entity |
| `Tag` | Id, Name, Slug, PostCount, IsOfficial | Global tags per tenant |
| `CommunityPostTag` | PostId, TagId | Join table (no BaseEntity) |
| `PostReaction` | Id, PostId, UserId, ReactionType | Unique per user+post+type |
| `PostComment` | Id, PostId, AuthorId, ParentCommentId, BodyMarkdown, IsDeleted | Max 2-level nesting |
| `PostBookmark` | Id, UserId, PostId | Reading list |
| `PostSeries` | Id, CommunityId, AuthorId, Title, Slug, PostCount | Series of related posts |
| `UserFollow` | Id, FollowerId, FollowedUserId?, FollowedTagId? | User+Tag follow |
| `ContentReport` | Id, TargetPostId?, TargetCommentId?, ReasonCode, Status | Moderation |
| `PostViewEvent` | Id, PostId, ViewerId?, SessionKey | Append-only analytics |

### New Enums

```csharp
// PostType
Article = 0, Discussion = 1, Question = 2, TIL = 3, Showcase = 4

// PostStatus
Draft = 0, Published = 1, Pinned = 2, Archived = 3, Scheduled = 4

// ReactionType
Like = 0, Unicorn = 1, MindBlown = 2, Fire = 3, Clap = 4

// ReportReason
Spam = 0, Abuse = 1, Misinformation = 2, NSFW = 3, Copyright = 4

// ReportStatus
Open = 0, Resolved = 1, Dismissed = 2
```

---

## 5. New API Endpoints

### CommunityPostsController  `api/communities/{communityId}/posts`

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/` | Authorize | Paginated list — supports `?type=&tag=&status=&sortBy=latest\|trending\|top` |
| GET | `/{postId}` | Authorize | Get post by ID (increments view async) |
| POST | `/` | Authorize (member) | Create post (draft or publish) |
| PUT | `/{postId}` | Author/Moderator | Update post |
| DELETE | `/{postId}` | Author/Moderator/Admin | Delete post |
| POST | `/{postId}/pin` | Moderator/Admin | Pin / unpin |
| POST | `/{postId}/feature` | Admin | Feature / unfeature |
| POST | `/{postId}/reactions` | Authorize | Toggle reaction |
| GET | `/{postId}/reactions` | Authorize | Get reaction counts + user's reactions |
| GET | `/{postId}/comments` | Authorize | Paginated comments (20/page) |
| POST | `/{postId}/comments` | Authorize (member) | Add comment |
| DELETE | `/{postId}/comments/{commentId}` | Author/Moderator | Delete comment |
| POST | `/{postId}/bookmarks` | Authorize | Toggle bookmark |
| POST | `/{postId}/report` | Authorize | Report post |

### FeedController  `api/feed`

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/` | Authorize | Personalized feed (Redis cache + score algo) |
| GET | `/trending` | Authorize | Trending posts (last 7 days) |
| GET | `/latest` | Authorize | Latest published posts across joined communities |
| GET | `/bookmarks` | Authorize | User's bookmarked posts |

### TagsController  `api/tags`

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/` | Authorize | List all tags (search + pagination) |
| GET | `/{slug}/posts` | Authorize | Posts by tag |
| POST | `/{slug}/follow` | Authorize | Follow/unfollow tag |

### PostSeriesController  `api/communities/{communityId}/series`

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/` | Authorize | List series in community |
| POST | `/` | Authorize (member) | Create series |
| PUT | `/{seriesId}` | Author | Update series |
| GET | `/{seriesId}` | Authorize | Get series with ordered posts |

### UserFollowsController  `api/users/{userId}/follow`

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/` | Authorize | Follow / unfollow user |
| GET | `/{userId}/followers` | Authorize | Paginated follower list |
| GET | `/{userId}/following` | Authorize | Paginated following list |

### ModerationController  `api/moderation/reports`

| Method | Route | Auth | Description |
|---|---|---|---|
| GET | `/` | Admin/Moderator | Open reports queue |
| POST | `/{reportId}/resolve` | Admin/Moderator | Resolve + add action |
| POST | `/{reportId}/dismiss` | Admin/Moderator | Dismiss report |

---

## 6. Application Service Interfaces

```csharp
// ICommunityPostService
Task<PagedResult<CommunityPostSummaryDto>> GetPostsAsync(Guid communityId, GetPostsRequest q, CancellationToken ct);
Task<CommunityPostDetailDto> GetPostAsync(Guid communityId, Guid postId, CancellationToken ct);
Task<CommunityPostDetailDto> CreatePostAsync(Guid communityId, CreatePostRequest req, CancellationToken ct);
Task<CommunityPostDetailDto> UpdatePostAsync(Guid communityId, Guid postId, UpdatePostRequest req, CancellationToken ct);
Task DeletePostAsync(Guid communityId, Guid postId, CancellationToken ct);
Task TogglePinAsync(Guid communityId, Guid postId, CancellationToken ct);
Task SaveDraftAsync(Guid communityId, Guid postId, DraftPostRequest req, CancellationToken ct);

// IPostReactionService
Task<PostReactionResultDto> ToggleReactionAsync(Guid postId, ReactionType type, CancellationToken ct);
Task<List<ReactionCountDto>> GetReactionsAsync(Guid postId, CancellationToken ct);

// IPostCommentService
Task<PagedResult<PostCommentDto>> GetCommentsAsync(Guid postId, int page, int size, CancellationToken ct);
Task<PostCommentDto> AddCommentAsync(Guid postId, AddCommentRequest req, CancellationToken ct);
Task DeleteCommentAsync(Guid commentId, CancellationToken ct);

// IFeedService
Task<PagedResult<CommunityPostSummaryDto>> GetPersonalizedFeedAsync(FeedRequest req, CancellationToken ct);
Task<PagedResult<CommunityPostSummaryDto>> GetTrendingAsync(int page, int size, CancellationToken ct);

// ITagService
Task<PagedResult<TagDto>> GetTagsAsync(GetTagsRequest req, CancellationToken ct);
Task<PagedResult<CommunityPostSummaryDto>> GetPostsByTagAsync(string slug, int page, CancellationToken ct);
Task ToggleFollowTagAsync(string slug, CancellationToken ct);

// IUserFollowService
Task ToggleFollowUserAsync(Guid targetUserId, CancellationToken ct);
Task<PagedResult<UserSummaryDto>> GetFollowersAsync(Guid userId, int page, CancellationToken ct);
Task<PagedResult<UserSummaryDto>> GetFollowingAsync(Guid userId, int page, CancellationToken ct);

// IPostBookmarkService
Task ToggleBookmarkAsync(Guid postId, CancellationToken ct);
Task<PagedResult<CommunityPostSummaryDto>> GetBookmarksAsync(int page, int size, CancellationToken ct);

// IContentModerationService
Task ReportContentAsync(ReportContentRequest req, CancellationToken ct);
Task<PagedResult<ContentReportDto>> GetOpenReportsAsync(int page, CancellationToken ct);
Task ResolveReportAsync(Guid reportId, ResolveReportRequest req, CancellationToken ct);
```

---

## 7. Scalability Implementation Plan

### 7.1 Redis Integration

**Package:** `StackExchange.Redis` + `Microsoft.Extensions.Caching.StackExchangeRedis`

**Keys strategy:**
```
feed:{tenantId}:{userId}          ZSET  — sorted personal feed (score = algo rank, value = postId)  TTL=5min
trending:{tenantId}               ZSET  — trending posts (score = trending score)                   TTL=1min
post:{postId}:views               INT   — view counter buffer (flushed to DB every 60s)
post:{postId}:meta                HASH  — hot post metadata (reactions, comments)                  TTL=2min
community:{id}:member_count       INT   — member count write-through                               TTL=5min
user:{id}:feed_version            INT   — cache busting version (incr on new followed post)
```

**IDistributedCommunityCache interface (in Application layer):**
```csharp
public interface IDistributedCommunityCache
{
    Task<int?> GetMemberCountAsync(Guid communityId);
    Task SetMemberCountAsync(Guid communityId, int count);
    Task IncrViewAsync(Guid postId);
    Task<Dictionary<Guid, long>> FlushViewCountsAsync();  // called by background job
    Task InvalidateFeedAsync(Guid tenantId, Guid userId);
    Task AddToTrendingAsync(Guid tenantId, Guid postId, double score);
    Task<IReadOnlyList<Guid>> GetTrendingPostIdsAsync(Guid tenantId, int count);
}
```

### 7.2 Background Services

```
ViewCountFlushService   (every 60s):  reads Redis INCR values → bulk UPDATE CommunityPosts SET ViewCount
TrendingScorerService   (every 5min): recomputes trending scores → updates Redis ZSET  
PostSchedulerService    (every 1min): publishes ScheduledAt posts where ScheduledAt <= NOW()
FeedInvalidatorService  (event):      on new post in community → INCR feed_version for all members
DigestEmailService      (daily 9AM):  sends community digest emails to opted-in members
```

### 7.3 SignalR Scale-Out (Redis Backplane)

```csharp
// ServiceCollectionExtensions.cs addition
services.AddSignalR()
    .AddStackExchangeRedis(redisConnectionString, opts =>
    {
        opts.Configuration.ChannelPrefix = RedisChannel.Literal("knowhub");
    });
```
This enables multi-pod deployment without users losing real-time updates when load-balanced.

### 7.4 Full-Text Search (PostgreSQL approach — no extra infrastructure)

Use PostgreSQL `tsvector` + GIN index on `CommunityPosts` (zero additional infra cost):
```sql
ALTER TABLE "CommunityPosts"
    ADD COLUMN "SearchVector" TSVECTOR
        GENERATED ALWAYS AS (
            to_tsvector('english', "Title" || ' ' || coalesce("ContentMarkdown",''))
        ) STORED;
CREATE INDEX "IX_CommunityPosts_SearchVector" ON "CommunityPosts" USING GIN ("SearchVector");
```
Upgrade path to OpenSearch when volume exceeds 5M posts.

### 7.5 Database Connection Pooling

Add PgBouncer service to docker-compose for production deployment:
```yaml
# docker-compose.yml addition
pgbouncer:
  image: pgbouncer/pgbouncer:1.22
  environment:
    DATABASES_HOST: postgres
    DATABASES_PORT: 5432
    DATABASES_USER: knowhub
    DATABASES_PASSWORD: knowhub_secret
    POOL_MODE: transaction
    MAX_CLIENT_CONN: 1000
    DEFAULT_POOL_SIZE: 25
  ports:
    - "5433:5432"
```

### 7.6 Cursor-Based Pagination for Feed

Replace offset pagination with cursor for feeds (avoids `OFFSET N` full-table scans at large N):
```csharp
public record FeedCursor(DateTime LastPublishedAt, Guid LastPostId);

// Query: WHERE "PublishedAt" < @cursor_time 
//        OR ("PublishedAt" = @cursor_time AND "Id" < @cursor_id)
// ORDER BY "PublishedAt" DESC, "Id" DESC
// LIMIT 20
```

---

## 8. Frontend Plan

### 8.1 New Pages

| Page | Route | Description |
|---|---|---|
| `CommunityFeedPage` | `/community/:slug` | Community home — pinned posts + latest feed |
| `PostDetailPage` | `/community/:slug/posts/:postSlug` | Full post with reactions + threaded comments |
| `PostEditorPage` | `/community/:slug/editor` | Rich editor (article/discussion/question) |
| `PostEditorPage (edit)` | `/community/:slug/posts/:postSlug/edit` | Edit existing post |
| `FeedPage` | `/feed` | Personalized cross-community feed |
| `TrendingPage` | `/trending` | Trending posts (last 7 days) |
| `TagPage` | `/tags/:slug` | Posts by tag |
| `BookmarksPage` | `/bookmarks` | Reading list |
| `SeriesDetailPage` | `/community/:slug/series/:seriesSlug` | Series with ordered posts |
| `ModerationQueuePage` | `/admin/moderation` | Content reports queue |

### 8.2 New Components

| Component | Description |
|---|---|
| `PostCard` | Summary card — title, author avatar, tags, reactions bar, reading time |
| `RichEditor` | Markdown editor with live preview, toolbar, code block with language selector |
| `CodeBlock` | Syntax-highlighted code block (using `highlight.js` or `shiki`) |
| `PostReactionBar` | 5-reaction horizontal bar with count + toggle animation |
| `CommentThread` | Nested comment list (2 levels, collapse/expand) |
| `TagChip` | Clickable tag chip that navigates to tag feed |
| `PostSeriesBanner` | "Part 3 of 5 in series: React for Beginners" banner |
| `TrendingPostSidebar` | Sorted list of trending posts |
| `FollowButton` | Follow/unfollow user with immediate optimistic update |
| `ReportDialog` | Report post/comment modal with reason selection |
| `DraftAutoSave` | Debounced auto-save with "Saved X seconds ago" indicator |

### 8.3 API Layer Additions

```typescript
// frontend/src/features/community/api/communityPostsApi.ts
export const communityPostsApi = {
  list:     (communityId, params) => axiosClient.get(`.../posts`, { params }),
  get:      (communityId, postId) => axiosClient.get(`.../posts/${postId}`),
  create:   (communityId, data)   => axiosClient.post(`.../posts`, data),
  update:   (communityId, postId, data) => axiosClient.put(`.../posts/${postId}`, data),
  delete:   (communityId, postId) => axiosClient.delete(`.../posts/${postId}`),
  react:    (communityId, postId, type) => axiosClient.post(`.../posts/${postId}/reactions`, { type }),
  bookmark: (communityId, postId) => axiosClient.post(`.../posts/${postId}/bookmarks`),
  report:   (communityId, postId, data) => axiosClient.post(`.../posts/${postId}/report`, data),
};

// frontend/src/features/feed/api/feedApi.ts
export const feedApi = {
  personalized: (cursor?) => axiosClient.get('/feed', { params: { cursor } }),
  trending:     (page)    => axiosClient.get('/feed/trending', { params: { page } }),
  bookmarks:    (page)    => axiosClient.get('/feed/bookmarks', { params: { page } }),
};
```

---

## 9. Implementation Phases

### Phase 1 — Core Posts & Engagement (Sprint 1–2)
**DB Migration:** `016_DevCommunityEnhancement.sql` (CommunityPosts, PostReactions, PostComments, PostBookmarks, Tags, UserFollows)

**Backend:**
1. Domain entities: `CommunityPost`, `PostReaction`, `PostComment`, `PostBookmark`, `Tag`, `CommunityPostTag`, `UserFollow`
2. `ICommunityPostService` + `CommunityPostService` (CRUD, pin, status transitions)
3. `IPostReactionService` + `PostReactionService` (toggle reactions, denorm counter update)
4. `IPostCommentService` + `PostCommentService` (add, delete, nested fetch)
5. `IPostBookmarkService` + `PostBookmarkService` (toggle, user reading list)
6. `ITagService` + `TagService` (list, follow, posts-by-tag)
7. Controllers: `CommunityPostsController`, `TagsController`
8. Validators: `CreatePostRequestValidator`, `AddCommentRequestValidator`
9. `ViewCountFlushService` background job (Redis counter → DB)
10. EF Core configurations + DbSet registrations in `KnowHubDbContext`

**Frontend:**
1. `PostCard` component + `CommunityFeedPage` (latest posts)
2. `PostDetailPage` with `PostReactionBar` + `CommentThread`
3. `PostEditorPage` — basic markdown editor with preview
4. `TagChip` component + `TagPage`

**Deliverable:** Users can write posts, react, comment, bookmark, and browse by tag.

---

### Phase 2 — Personalized Feed + Follow Graph (Sprint 3)
1. `IUserFollowService` + `UserFollowService`
2. `IFeedService` + `FeedService` (score-based personalized feed, cursor pagination)
3. Redis integration (`IDistributedCommunityCache` + `RedisCommunityCache`)
4. `FeedController` (personalized, trending, latest, bookmarks)
5. `TrendingScorerService` background job
6. `FeedPage` + `TrendingPage` frontend pages
7. `FollowButton` component on user profiles
8. Feed cache invalidation on new posts in joined communities

**Deliverable:** Users see a ranked personalized feed. Trending posts visible without community membership.

---

### Phase 3 — Rich Editor + Series + Advanced Discovery (Sprint 4)
1. `IPostSeriesService` + backend endpoints
2. Frontend: `RichEditor` component with syntax-highlighted code blocks (add `highlight.js` / `@uiw/react-md-editor`)
3. Embed support (GitHub Gist `<iframe>`, YouTube `<iframe>`, CodePen) — server-side allowlist validation
4. `PostSeriesBanner` + `SeriesDetailPage`
5. Post scheduling backend (`PostSchedulerService`) + UI date-time picker
6. `CanonicalUrl` field on post editor + SEO `<link rel="canonical">` on post detail
7. Draft auto-save with debounce (2s) + "Saved X seconds ago" indicator

**Deliverable:** Content creation experience on par with Hashnode/Dev.to.

---

### Phase 4 — Moderation + Analytics + Notifications (Sprint 5)
1. `ContentReport` entity + `IContentModerationService`
2. `ModerationController` + `ModerationQueuePage` (admin)
3. `@mention` parsing in post/comment content → fan-out notification
4. Community digest email (`DigestEmailService` + email template)
5. Author analytics: per-post views/reactions/comments chart on `PostDetailPage`
6. Redis SignalR backplane for multi-pod scale-out
7. RSS feed endpoint per community (`/api/communities/{slug}/rss`)

**Deliverable:** Moderation pipeline, real-time @mentions, analytics, production-ready SignalR.

---

### Phase 5 — Scalability Hardening (Sprint 6)
1. PostgreSQL `tsvector` GIN index on `CommunityPosts` for full-text search
2. PgBouncer added to docker-compose (transaction-mode pooling)
3. Cursor-based pagination on feed and post-list endpoints
4. `PostViewEvent` append-only table + async view counting pipeline
5. CDN URL rewrite for `CoverImageUrl` (CloudFront / Azure CDN prefix config)
6. K6 load test: 10k concurrent users on feed API — baseline + after caching
7. DB query plan review for all new indexes (EXPLAIN ANALYZE on prod-like data)
8. Horizontal scale test: 3 API pods behind nginx, Redis backplane SignalR verified

**Deliverable:** Architecture validated at 1M user scale.

---

## 10. Non-Functional Requirements

| NFR | Target | Mechanism |
|---|---|---|
| Feed API p99 latency | < 100ms | Redis L1 cache, cursor pagination, covering indexes |
| Post detail API p99 | < 80ms | Redis hot post metadata cache (2min TTL) |
| ViewCount write | Async, < 1s flush | Redis INCR + background 60s flush |
| MemberCount accuracy | Eventually consistent | Redis write-through + periodic reconcile |
| Notification fan-out | < 5s to all followers | Bounded channel + batch DB inserts |
| Concurrent writers (reactions) | No race condition | Unique constraint + `ON CONFLICT DO NOTHING` |
| Multi-pod real-time | No missed events | Redis SignalR backplane |
| Search response time | < 200ms for tsvector | GIN index + query limit 20 |
| Post content storage | No SQL injection | Markdown sanitizer (existing `MarkdownSanitizer`) + HTML encode |
| GDPR / soft-delete | Posts anonymisable | `AuthorId → NULL` on user deletion (ON DELETE SET NULL) |

---

## 11. New NuGet Packages Required

| Package | Version | Purpose |
|---|---|---|
| `StackExchange.Redis` | 2.8.x | Redis client |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | 9.x | Redis IDistributedCache |
| `Microsoft.AspNetCore.SignalR.StackExchangeRedis` | 9.x | SignalR Redis backplane |
| `Markdig` | 0.40.x | Markdown → HTML rendering with extensions |

## 12. New NPM Packages Required

| Package | Purpose |
|---|---|
| `@uiw/react-md-editor` | Rich Markdown editor with preview |
| `highlight.js` | Syntax highlighting in code blocks |
| `react-timeago` | "3 minutes ago" relative timestamps |
| `rss-parser` (optional) | RSS feed reading if used in frontend |
