# Posts

## Module

Social

## Aggregate Root

Post

---

# Purpose

The `posts` table stores user-generated social posts.

A post represents the main content shared by a user and serves as the aggregate root for media attachments, likes, comments and reports.

Media files are managed separately through the `post_media` table, allowing posts to contain multiple images and videos while keeping the data model normalized and scalable.

---

# Responsibilities

- Store post content
- Own post media
- Build the Explore feed
- Support user profiles
- Enable community engagement
- Maintain cached counters for high-performance queries

---

# Columns

| Column             | Type        | Nullable | Description                 |
| ------------------ | ----------- | -------- | --------------------------- |
| id                 | UUID        | No       | Primary Key                 |
| user_id            | UUID        | No       | References users(id)        |
| content            | TEXT        | Yes      | Post caption or description |
| like_count         | INTEGER     | No       | Cached total likes          |
| comment_count      | INTEGER     | No       | Cached total comments       |
| media_count        | SMALLINT    | No       | Cached total media items    |
| created_at         | TIMESTAMPTZ | No       | Created date                |
| updated_at         | TIMESTAMPTZ | Yes      | Last updated date           |
| created_by_user_id | UUID        | Yes      | Audit                       |
| updated_by_user_id | UUID        | Yes      | Audit                       |

---

# Indexes

- PK(id)
- INDEX(user_id)
- INDEX(created_at)

---

# Foreign Keys

| Column  | References |
| ------- | ---------- |
| user_id | users(id)  |

---

# Relationships

## Belongs To

- users

## Owns

- post_media

## Referenced By

- post_likes
- post_comments
- reports

---

# Business Rules

- Every post belongs to exactly one user.
- A post may contain text only, media only, or both.
- Media is stored separately in the `post_media` table.
- A post may contain multiple images and videos.
- At least one of `content` or `post_media` must exist.
- Only the owner can edit or delete the post.
- Cached counters (`like_count`, `comment_count`, `media_count`) are maintained automatically by backend business logic.
- Posts reported by the community may become hidden until moderation is completed.
- Deleting a post permanently removes the post together with all related media, likes, comments and reports.

---

# Lifecycle

## Create Post

- Validate user.
- Create the post.
- Upload media files to Supabase Storage.
- Create `post_media` records.
- Calculate `media_count`.
- Initialize cached counters.
- Publish the post.

---

## Edit Post

- Validate ownership.
- Update post content.
- Add new media.
- Remove existing media.
- Reorder media.
- Recalculate `media_count`.

---

## Delete Post

- Validate ownership.
- Delete media files from Supabase Storage.
- Delete all `post_media` records.
- Delete all likes.
- Delete all comments.
- Delete all reports.
- Delete the post.

---

# Performance Notes

Most queries retrieve posts by:

- created_at
- user_id

The Explore feed should use cursor-based pagination.

Like, comment and media counts are cached to avoid expensive aggregation queries.

Feed queries should always project only the required fields.

---

# Future Extensions

Possible future additions:

- Tagged users
- Tagged events
- Hashtags
- Saved posts
- Shared posts
- Poll posts
- Story posts
- AI-generated captions
- AI content moderation
- Scheduled publishing
- Visibility settings (Public / Friends / Private)

---

# Notes

This table stores only post metadata.

Media files are managed through the `post_media` table.

Binary files are stored exclusively in Supabase Storage.

The Explore feed ranking should combine:

- Recency
- Like count
- Comment count
- User interests
- Friend activity
- Location relevance
- User reputation

The `posts` table acts as the aggregate root for all post-related entities.

The cached counters (`like_count`, `comment_count`, `media_count`) must always be synchronized by backend business logic and should never be updated directly by clients.
