# Post Media

## Module

Social

## Aggregate Root

Post

---

# Purpose

The `post_media` table stores media files attached to social posts.

Each post may contain multiple media items such as images or videos.

Binary files are stored in Supabase Storage, while PostgreSQL stores only media metadata.

The order of media items is preserved to ensure a consistent user experience.

---

# Responsibilities

- Store post media
- Support multiple images
- Support multiple videos
- Preserve media ordering
- Store media metadata

---

# Columns

| Column             | Type         | Nullable | Description                   |
| ------------------ | ------------ | -------- | ----------------------------- |
| id                 | UUID         | No       | Primary Key                   |
| post_id            | UUID         | No       | References posts(id)          |
| media_type         | SMALLINT     | No       | Media type                    |
| storage_path       | TEXT         | No       | Supabase Storage file path    |
| file_name          | VARCHAR(255) | No       | Original file name            |
| mime_type          | VARCHAR(100) | No       | MIME type                     |
| file_size          | BIGINT       | No       | File size in bytes            |
| width              | INTEGER      | Yes      | Media width                   |
| height             | INTEGER      | Yes      | Media height                  |
| duration_seconds   | INTEGER      | Yes      | Video duration                |
| display_order      | SMALLINT     | No       | Display order inside the post |
| created_at         | TIMESTAMPTZ  | No       | Created date                  |
| updated_at         | TIMESTAMPTZ  | Yes      | Updated date                  |
| created_by_user_id | UUID         | Yes      | Audit                         |
| updated_by_user_id | UUID         | Yes      | Audit                         |

---

# Indexes

- PK(id)
- INDEX(post_id)
- INDEX(display_order)

---

# Unique Constraints

- UNIQUE(post_id, display_order)

---

# Foreign Keys

| Column  | References |
| ------- | ---------- |
| post_id | posts(id)  |

---

# Relationships

## Belongs To

- posts

---

# Media Types

| Value | Name  |
| ----- | ----- |
| 0     | Image |
| 1     | Video |

---

# Business Rules

- Every media item belongs to exactly one post.
- A post may contain one or more media items.
- Media order is determined by `display_order`.
- Files are uploaded to Supabase Storage before the database record is created.
- Only media metadata is stored in PostgreSQL.
- Deleting a post removes all associated media.
- Images and videos may coexist within the same post.

---

# Lifecycle

### Create Post

- Upload all media files.
- Store metadata for each media item.
- Assign display order.
- Associate media with the post.

### Update Post

- Add new media.
- Remove existing media.
- Reorder media.
- Synchronize display order.

### Delete Post

- Remove all files from Supabase Storage.
- Delete related media records.
- Delete the post.

---

# Performance Notes

Media is almost always loaded together with its parent post.

Queries should retrieve media ordered by:

- display_order ASC

No additional pagination is required.

---

# Future Extensions

Possible future additions:

- Image thumbnails
- Video thumbnails
- BlurHash
- Compression metadata
- EXIF metadata
- AI-generated captions
- AI content detection
- Media visibility

---

# Notes

This table stores only metadata.

Binary media files must never be stored inside PostgreSQL.

Supabase Storage is the single source of truth for uploaded media.

The client should always display media according to `display_order`.
