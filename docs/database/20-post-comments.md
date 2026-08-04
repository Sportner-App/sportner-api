# Post Comments

## Module

Social

## Aggregate Root

Post

---

# Purpose

The `post_comments` table stores comments made by users on social posts.

Comments encourage community interaction and support threaded conversations through nested replies.

Each comment belongs to exactly one post and one author.

---

# Responsibilities

- Store post comments
- Support nested replies
- Trigger notifications
- Support moderation
- Maintain engagement

---

# Columns

| Column             | Type        | Nullable | Description                  |
| ------------------ | ----------- | -------- | ---------------------------- |
| id                 | UUID        | No       | Primary Key                  |
| post_id            | UUID        | No       | References posts(id)         |
| user_id            | UUID        | No       | References users(id)         |
| parent_comment_id  | UUID        | Yes      | References post_comments(id) |
| content            | TEXT        | No       | Comment content              |
| like_count         | INTEGER     | No       | Cached like count            |
| reply_count        | INTEGER     | No       | Cached reply count           |
| created_at         | TIMESTAMPTZ | No       | Created date                 |
| updated_at         | TIMESTAMPTZ | Yes      | Updated date                 |
| created_by_user_id | UUID        | Yes      | Audit                        |
| updated_by_user_id | UUID        | Yes      | Audit                        |

---

# Indexes

- PK(id)
- INDEX(post_id)
- INDEX(user_id)
- INDEX(parent_comment_id)
- INDEX(created_at)

---

# Foreign Keys

| Column            | References        |
| ----------------- | ----------------- |
| post_id           | posts(id)         |
| user_id           | users(id)         |
| parent_comment_id | post_comments(id) |

---

# Relationships

## Belongs To

- posts
- users

## Self Reference

- parent_comment_id → post_comments(id)

---

# Business Rules

- Every comment belongs to exactly one post.
- Every comment has exactly one author.
- Comments may optionally reply to another comment.
- Reply comments must belong to the same post as their parent comment.
- Users may edit only their own comments.
- Comments are permanently deleted when removed.
- Deleting a comment decrements the parent post's `comment_count`.
- Creating a reply increments the parent comment's `reply_count`.
- Creating a comment generates a notification for the post owner.
- Replying to another user's comment generates a notification for that user.
- Notifications are never generated for actions performed on a user's own content.

---

# Lifecycle

## Create Comment

- Validate the post exists.
- Validate the post is available.
- Validate the parent comment (if provided).
- Create the comment.
- Increment `posts.comment_count`.
- Increment `reply_count` when replying.
- Generate notifications.

---

## Edit Comment

- Validate ownership.
- Update comment content.

---

## Delete Comment

- Validate ownership.
- Delete child replies recursively.
- Delete the comment.
- Decrement `posts.comment_count`.
- Update parent `reply_count` if applicable.

---

# Performance Notes

Most queries retrieve comments by:

- post_id
- parent_comment_id
- created_at

Comments should always be ordered chronologically.

Nested replies should be loaded lazily to avoid unnecessarily large payloads.

Cached counters should always be updated by backend business logic.

---

# Future Extensions

Possible future additions:

- Comment likes
- Emoji reactions
- User mentions
- Hashtags
- Pinned comments
- AI moderation
- Toxic language detection
- Comment translation
- Edited indicator

---

# Notes

Comments are first-class social entities.

Replies are implemented through self-referencing relationships.

The backend is responsible for maintaining:

- posts.comment_count
- post_comments.reply_count

Notifications are generated automatically after successful comment creation.

Reports for inappropriate comments are handled through the shared `reports` module.
