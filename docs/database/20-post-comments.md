# Post Comments

## Module

Social

## Aggregate Root

PostComment

---

# Purpose

The `post_comments` table stores user comments created on social posts.

Each comment belongs to exactly one post and one author.

Comments support threaded discussions through self-referencing replies while remaining an independent aggregate.

Replies reference another comment by identifier but are not loaded as child entities inside the aggregate.

---

# Responsibilities

- Store user comments
- Support replies
- Preserve discussion history
- Maintain intrinsic comment state

The aggregate is **not responsible** for:

- Notification generation
- Cached counter synchronization
- Feed ranking
- Recursive comment loading
- Post lifecycle management

---

# Aggregate Boundary

```text
PostComment
```

References:

- Post
- User
- Parent PostComment (optional)

The aggregate does **not** own child reply collections.

Replies are independent `PostComment` aggregates referencing their parent by identifier.

This prevents loading an unbounded reply tree into memory.

---

# Columns

| Column             | Type        | Nullable | Description                    |
| ------------------ | ----------- | -------: | ------------------------------ |
| id                 | UUID        |       No | Primary key                    |
| post_id            | UUID        |       No | References `posts(id)`         |
| user_id            | UUID        |       No | References `users(id)`         |
| parent_comment_id  | UUID        |      Yes | References `post_comments(id)` |
| content            | TEXT        |       No | Comment text                   |
| like_count         | INTEGER     |       No | Cached comment likes           |
| reply_count        | INTEGER     |       No | Cached direct replies          |
| created_at         | TIMESTAMPTZ |       No | Creation timestamp             |
| updated_at         | TIMESTAMPTZ |      Yes | Last update timestamp          |
| created_by_user_id | UUID        |      Yes | Audit                          |
| updated_by_user_id | UUID        |      Yes | Audit                          |

---

# Default Values

| Column      | Default |
| ----------- | ------: |
| like_count  |       0 |
| reply_count |       0 |

Counters must never become negative.

---

# Indexes

- `PK(id)`
- `INDEX(post_id)`
- `INDEX(parent_comment_id)`
- `INDEX(user_id)`
- `INDEX(created_at)`
- `INDEX(post_id, created_at)`

---

# Foreign Keys

| Column            | References        | Delete Behavior |
| ----------------- | ----------------- | --------------- |
| post_id           | posts(id)         | Cascade         |
| user_id           | users(id)         | Cascade         |
| parent_comment_id | post_comments(id) | Restrict        |

Recursive delete should be orchestrated by the Application layer rather than relying on cascading self-references.

---

# Relationships

## References

- posts
- users
- post_comments (parent)

---

# Business Rules

- Every comment belongs to one post.
- Every comment belongs to one author.
- A reply references one parent comment.
- Root comments have `parent_comment_id = NULL`.
- Reply comments must belong to the same post as their parent.
- Users may edit only their own comments.
- Blank comments are not allowed.
- Comment text must be trimmed.
- Maximum comment length: **1000** characters.
- A comment cannot reply to itself.
- Circular reply chains are forbidden.
- Replies are limited to **one nesting level** in version 1.
- Like and reply counters must never become negative.
- Comment ownership never changes after creation.

---

# Aggregate Rules

The aggregate controls only:

- Create comment
- Edit content
- Update intrinsic state

It does **not** load or manage child replies.

Each reply is another independent `PostComment` aggregate.

---

# Lifecycle

## Create Comment

Application flow:

1. Validate the post exists.
2. Validate the post accepts comments.
3. Validate the parent comment when provided.
4. Ensure parent belongs to the same post.
5. Ensure reply depth is valid.
6. Create PostComment.
7. Persist.
8. Increment `posts.comment_count`.
9. Increment parent `reply_count`.
10. Generate notifications.

Only step **6** belongs to the Domain aggregate.

---

## Edit Comment

Application flow:

1. Validate ownership.
2. Load comment.
3. Update content.
4. Persist.

Only intrinsic validation belongs to the Domain aggregate.

---

## Delete Comment

Application flow:

1. Validate ownership or moderation permission.
2. Delete descendant replies if required by business rules.
3. Delete comment.
4. Decrement parent reply counter.
5. Decrement post comment counter.
6. Remove related notifications if applicable.

The Domain aggregate does not recursively traverse reply trees.

---

# Cached Counter Contract

`reply_count` is maintained by the Application layer.

It is incremented after successful reply creation.

It is decremented after reply deletion.

`like_count` is maintained by the future `CommentLike` aggregate.

The Domain aggregate never updates cached counters directly.

---

# Notification Contract

Notifications may be created for:

- Post owner
- Parent comment owner

Rules:

- Never notify the acting user.
- Notification creation belongs entirely to the Application layer.

The aggregate has no Notification dependency.

---

# Performance Notes

Typical queries:

- Root comments for a post
- Replies for a comment
- User comments

Comments should be ordered by:

```text
created_at ASC
```

Replies should be loaded lazily.

The entire discussion tree must never be loaded into a single aggregate.

---

# Concurrency Notes

Cached counters should be updated atomically.

Application services must prevent lost updates during concurrent comment creation.

---

# Deletion Policy

Comments are physically deleted.

No soft-delete mechanism is introduced in version 1.

If moderation history becomes necessary, introduce an explicit moderation state rather than generic soft delete.

---

# Future Extensions

Possible additions:

- Comment likes
- Emoji reactions
- User mentions
- Hashtags
- Edited indicator
- AI moderation
- Toxic language detection
- Translation
- Attachments

These features should not change the aggregate boundary.

---

# Notes

`PostComment` is an independent aggregate root.

It references:

- Post
- User
- Parent Comment

The aggregate never owns child replies.

The Application layer is responsible for:

- Notification creation
- Cached counter synchronization
- Recursive deletion policies
- Authorization
- Transaction management
- Reply validation across aggregates
