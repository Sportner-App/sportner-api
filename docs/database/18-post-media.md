# Post Media

## Module

Social

## Aggregate Root

Post

---

# Purpose

The `post_media` table stores media metadata owned by the Post aggregate.

Each media record represents a single image or video attached to a post.

Binary files are stored exclusively in Supabase Storage, while PostgreSQL stores only media metadata required by the application.

`PostMedia` is not an independent aggregate. Its lifecycle is fully controlled by the owning `Post` aggregate.

---

# Responsibilities

- Store media metadata
- Preserve media ordering
- Support images and videos
- Maintain media lifecycle together with the owning Post
- Support efficient feed rendering

---

# Aggregate Boundary

```text
Post
└── PostMedia
```

`PostMedia` cannot exist without a `Post`.

All creation, update and deletion operations must occur through Post aggregate behavior.

The Application layer must never manipulate `PostMedia` independently.

---

# Columns

| Column             | Type         | Nullable | Description                   |
| ------------------ | ------------ | -------: | ----------------------------- |
| id                 | UUID         |       No | Primary key                   |
| post_id            | UUID         |       No | References `posts(id)`        |
| media_type         | SMALLINT     |       No | Image or Video                |
| storage_path       | TEXT         |       No | Supabase Storage path         |
| file_name          | VARCHAR(255) |       No | Original uploaded filename    |
| mime_type          | VARCHAR(100) |       No | MIME type                     |
| file_size          | BIGINT       |       No | File size in bytes            |
| width              | INTEGER      |      Yes | Image or video width          |
| height             | INTEGER      |      Yes | Image or video height         |
| duration_seconds   | INTEGER      |      Yes | Video duration                |
| display_order      | SMALLINT     |       No | Display order inside the post |
| created_at         | TIMESTAMPTZ  |       No | Creation timestamp            |
| updated_at         | TIMESTAMPTZ  |      Yes | Last update timestamp         |
| created_by_user_id | UUID         |      Yes | Audit                         |
| updated_by_user_id | UUID         |      Yes | Audit                         |

---

# Default Values

No database defaults are required.

All values are assigned through domain behavior.

---

# Indexes

- `PK(id)`
- `INDEX(post_id)`
- `INDEX(post_id, display_order)`

---

# Unique Constraints

```text
UNIQUE(post_id, display_order)
```

Only one media item may occupy a display position inside a post.

---

# Foreign Keys

| Column  | References | Delete Behavior |
| ------- | ---------- | --------------- |
| post_id | posts(id)  | Cascade         |

Cascade deletion removes media metadata together with the owning post.

Actual storage cleanup remains an Application responsibility.

---

# Relationships

## Belongs To

- `posts`

---

# Media Types

| Value | Name  |
| ----: | ----- |
|     0 | Image |
|     1 | Video |

Only documented media types are allowed.

Future media types must be introduced through explicit enum changes.

---

# Business Rules

- Every media item belongs to exactly one post.
- A media item cannot exist without its owning post.
- Images and videos may coexist.
- A post may contain a maximum of **10** media items.
- `display_order` starts at **1**.
- Display order must always remain sequential.
- Gaps are not allowed.
- Duplicate display positions are not allowed.
- `storage_path` cannot be empty.
- `storage_path` is immutable after creation.
- Replacing a file requires creating a new PostMedia entity.
- `file_size` must be greater than zero.
- `mime_type` must not be blank.
- Width and height should exist whenever known.
- Video duration applies only to video media.
- Image media must not contain duration metadata.
- Binary content must never be stored in PostgreSQL.

---

# Aggregate Rules

The Post aggregate controls:

- Add media
- Remove media
- Reorder media
- Validate media limits
- Maintain `media_count`

`PostMedia` must never update `media_count` directly.

---

# Lifecycle

## Create Post

Application flow:

1. Upload media to Supabase Storage.
2. Collect media metadata.
3. Create Post aggregate.
4. Add PostMedia entities.
5. Persist aggregate.

The Domain layer never uploads files.

---

## Edit Post

Allowed operations:

- Add media
- Remove media
- Reorder media

Rules:

- Reordering must produce sequential `display_order` values.
- Media count must remain synchronized.
- Replacing an existing file creates a new PostMedia entity.
- Storage cleanup is performed after successful persistence.

---

## Delete Post

Application flow:

1. Delete media files from Supabase Storage.
2. Delete Post aggregate.
3. Cascade removes PostMedia metadata.

The Domain layer manages metadata only.

---

# Ordering Rules

Media must always be returned using:

```text
display_order ASC
```

The ordering must be deterministic.

Duplicate positions are invalid.

---

# Performance Notes

Media is typically loaded together with its parent Post.

No independent pagination is required.

Projection queries should retrieve only metadata required by the client.

Binary files must always be loaded from Supabase Storage.

---

# Storage Rules

PostgreSQL stores only:

- metadata
- storage path

Supabase Storage stores:

- binary image files
- binary video files

The database is never the source of binary media.

---

# Future Extensions

Possible additions:

- BlurHash
- Image thumbnails
- Video thumbnails
- Compression metadata
- EXIF metadata
- AI-generated captions
- AI moderation
- HDR support
- Original upload metadata

These additions should not change the aggregate boundary.

---

# Notes

`PostMedia` is a child entity owned by the `Post` aggregate.

It has no independent lifecycle.

The Application layer is responsible for:

- Uploading files
- Deleting files
- Storage retries
- Virus scanning
- Image optimization
- Video transcoding

The Domain layer manages only media metadata and ordering.
