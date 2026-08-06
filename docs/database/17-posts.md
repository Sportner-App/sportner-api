# Posts

## Module

Social

## Aggregate Root

Post

---

# Purpose

The `posts` table stores user-generated social posts.

A post represents the primary content shared by a user and acts as the aggregate root for its media attachments.

Likes, comments and reports reference the post but belong to separate aggregate boundaries because their collections may grow without limit.

Media files are managed through the `post_media` table, allowing posts to contain multiple images and videos while keeping the relational model normalized.

---

# Responsibilities

- Store post content
- Own post media
- Maintain cached engagement counters
- Support the Explore feed
- Support user profile feeds
- Control post content and media lifecycle

The Post aggregate is not responsible for storing or loading complete like, comment or report collections.

---

# Columns

| Column             | Type        | Nullable | Description                        |
| ------------------ | ----------- | -------: | ---------------------------------- |
| id                 | UUID        |       No | Primary key                        |
| user_id            | UUID        |       No | References `users(id)`             |
| content            | VARCHAR(2200) |    Yes | Post caption or description        |
| like_count         | INTEGER     |       No | Cached total number of likes       |
| comment_count      | INTEGER     |       No | Cached total number of comments    |
| media_count        | SMALLINT    |       No | Cached total number of media items |
| created_at         | TIMESTAMPTZ |       No | Creation timestamp                 |
| updated_at         | TIMESTAMPTZ |      Yes | Last update timestamp              |
| created_by_user_id | UUID        |      Yes | Audit field                        |
| updated_by_user_id | UUID        |      Yes | Audit field                        |

---

# Default Values

| Column        | Default |
| ------------- | ------: |
| like_count    |       0 |
| comment_count |       0 |
| media_count   |       0 |

Cached counters must never become negative.

---

# Indexes

- `PK(id)`
- `INDEX(user_id)`
- `INDEX(created_at)`
- `INDEX(user_id, created_at)`

The composite index supports paginated user-profile post queries.

---

# Foreign Keys

| Column  | References | Delete Behavior |
| ------- | ---------- | --------------- |
| user_id | users(id)  | Restrict        |

Published user content must not be removed automatically when a user account changes lifecycle status.

Post deletion must be performed explicitly through application business logic.

---

# Relationships

## Belongs To

- `users`

## Owns

- `post_media`

## Referenced By Independent Aggregates

- `post_likes`
- `post_comments`
- `reports`
- `notifications`

---

# Aggregate Boundary

The Post aggregate contains:

```text
Post
└── PostMedia
The following entities are not held as Post collections:

PostLike
PostComment
Report

They reference PostId but are independent aggregate roots.

This prevents the Post aggregate from loading unbounded collections.

Business Rules
Every post belongs to exactly one user.
A post may contain text only, media only, or both.
A post must contain at least one of:
Non-empty content
One or more media items
Blank content must normalize to null.
Non-empty content must be trimmed.
Only the post owner may edit or delete the post.
Media is managed through post_media.
A post may contain multiple images and videos.
Media ordering is controlled through post_media.display_order.
Cached counters are updated only through controlled domain or application behavior.
Cached counters must never be modified directly by clients.
The Post aggregate must not expose mutable media collections.
The Post aggregate must not contain like, comment or report collections.
Reported posts may be hidden or moderated through the Moderation module.
Moderation state must not be inferred only from the existence of reports.
A post cannot remain valid with both empty content and zero media items.
Cached Counter Rules
Like Count

like_count is updated when:

A PostLike is created.
A PostLike is removed.

Rules:

Increment only after successful like creation.
Decrement only after successful like removal.
Never allow the value to become negative.
Comment Count

comment_count represents the total number of comments belonging to the post, including replies.

It is updated when:

A PostComment is created.
A PostComment and its child replies are removed.

Rules:

Increment only after successful comment creation.
Decrement by the number of removed comments.
Never allow the value to become negative.
Media Count

media_count is controlled directly by the Post aggregate.

It is updated when:

Media is added.
Media is removed.

The value must always equal the number of active PostMedia entities owned by the Post aggregate.

Lifecycle
Create Post

Application flow:

Validate the user exists and is allowed to create content.
Upload media files to Supabase Storage when provided.
Create the Post aggregate.
Add media metadata through Post aggregate behavior.
Validate that content or media exists.
Persist the Post and its media atomically.
Initialize all cached counters.

The Domain layer must not upload files.

Edit Post

Allowed operations:

Update content.
Add media.
Remove media.
Reorder media.

Rules:

Ownership is validated by the Application layer.
The Post aggregate validates its resulting intrinsic state.
Editing must not leave the post without both content and media.
Media changes must keep media_count synchronized.
Removing media requires later storage cleanup orchestration by the Application layer.
Delete Post

Post deletion is orchestrated by the Application layer.

The operation must:

Validate ownership or moderation permission.
Remove related PostLike records.
Remove related PostComment records.
Remove related Report records or preserve them according to moderation retention policy.
Remove related notifications where appropriate.
Delete media objects from Supabase Storage.
Delete post_media records.
Delete the post.

Database cascading must not be relied on for external storage cleanup.

The operation should execute database changes transactionally where possible.

Content Rules
Content is optional when at least one media item exists.
Blank content normalizes to null.
HTML and rich text are not supported in the first version.
Content must be treated as plain text.
A maximum content length must be enforced consistently in Domain, validation and database configuration.
Use 2200 characters unless another final product requirement is documented.
Media Rules
Binary files are stored only in Supabase Storage.
PostgreSQL stores media metadata and storage paths.
Media is managed through post_media.
A Post owns its media lifecycle inside the Domain model.
File upload and deletion are Application or Infrastructure responsibilities.
The Domain aggregate only manages media metadata and ordering.
At least one media item may exist without text content.
Media limits must be enforced before persistence.
Use a maximum of 10 media items per post for the first version.
display_order values must remain unique and sequential within the post.
Feed Rules

The Explore feed may rank posts using:

Recency
Like count
Comment count
User interests
Friend activity
Location relevance
User reputation
Sport preferences

Feed-ranking logic does not belong inside the Post aggregate.

Feed queries must use projections and cursor-based pagination.

The complete Post aggregate must not be loaded for read-only feed queries.

Performance Notes

Common queries include:

Latest posts
Posts created by a user
Posts referenced by notifications
Posts with media previews
Ranked Explore feed posts

Recommended ordering:

created_at DESC
id DESC

Cursor pagination should use a stable pair such as:

created_at + id

Like, comment and media counts must be read from cached columns instead of recalculated during feed requests.

Concurrency Notes

Cached engagement counters may be updated concurrently.

Infrastructure implementation must use a concurrency-safe strategy such as:

Atomic database updates
Appropriate transaction isolation
Concurrency tokens where justified

The Domain model must prevent negative values, but database updates must also avoid lost-update problems.

Deletion Policy

Posts are physically deleted when removed by their owner unless moderation or legal-retention requirements require preservation.

Do not introduce:

deleted_at
is_deleted

Moderation hiding and owner deletion are different concepts.

If hidden-content history becomes necessary, introduce an explicit moderation state rather than a generic soft-delete column.

Future Extensions

Possible future additions:

Tagged users
Tagged events
Hashtags
Saved posts
Shared posts
Poll posts
Story posts
Scheduled publishing
Visibility settings
Location tagging
Sport tagging
AI-generated captions
AI content moderation
Post editing history
Post pinning

These features should be introduced without placing unbounded collections inside the Post aggregate.

Notes

The posts table stores post metadata and cached counters.

The Post aggregate owns only its bounded PostMedia collection.

PostLike, PostComment and Report are separate aggregate roots that reference the post by identifier.

This aggregate boundary keeps writes focused and prevents the Post aggregate from growing with unlimited social interactions.

The Application layer is responsible for coordinating:

User authorization
Storage operations
Like and comment persistence
Notification creation
Report creation
Cached counter synchronization across aggregates
Transaction management
```
