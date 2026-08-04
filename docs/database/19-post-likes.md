# Post Likes

## Module

Social

## Aggregate Root

Post

---

# Purpose

The `post_likes` table stores user likes for social posts.

Each user can like a post only once.

Likes contribute to engagement metrics, Explore feed ranking and notification generation.

---

# Responsibilities

- Store post likes
- Prevent duplicate likes
- Trigger notifications
- Maintain engagement statistics

---

# Columns

| Column             | Type        | Nullable | Description          |
| ------------------ | ----------- | -------- | -------------------- |
| id                 | UUID        | No       | Primary Key          |
| post_id            | UUID        | No       | References posts(id) |
| user_id            | UUID        | No       | References users(id) |
| created_at         | TIMESTAMPTZ | No       | Created date         |
| updated_at         | TIMESTAMPTZ | Yes      | Updated date         |
| created_by_user_id | UUID        | Yes      | Audit                |
| updated_by_user_id | UUID        | Yes      | Audit                |

---

# Indexes

- PK(id)
- INDEX(post_id)
- INDEX(user_id)

---

# Unique Constraints

- UNIQUE(post_id, user_id)

---

# Foreign Keys

| Column  | References |
| ------- | ---------- |
| post_id | posts(id)  |
| user_id | users(id)  |

---

# Relationships

## Belongs To

- posts
- users

---

# Business Rules

- A user can like a post only once.
- Users cannot like their own posts.
- Liking a post increases the post's `like_count`.
- Removing a like decreases the post's `like_count`.
- Creating a like generates a notification for the post owner.
- Notifications are not generated when users interact with their own posts.
- Deleted posts cannot receive new likes.
- Likes are permanently deleted when removed by the user.
- Deleting a post automatically removes all associated likes.

---

# Lifecycle

## Like Post

- Validate the post exists.
- Validate the post is available.
- Validate the user has not already liked the post.
- Create the like.
- Increment `posts.like_count`.
- Create a notification for the post owner.

---

## Unlike Post

- Validate ownership.
- Delete the like.
- Decrement `posts.like_count`.

---

## Delete Post

- Delete all related likes.
- Update cached counters.
- Delete associated notifications.

---

# Performance Notes

Most queries retrieve likes by:

- post_id
- user_id

Like counts should never be calculated using COUNT(\*) during feed requests.

The cached `posts.like_count` field is the source of truth for displaying engagement.

---

# Future Extensions

Possible future additions:

- Double tap support
- Reaction types (❤️ 👍 🔥 👏 😂)
- Friend likes
- Like history
- Like analytics

---

# Notes

Each user may like a post only once.

Likes are lightweight interaction records and should be physically deleted when removed.

Notifications generated from likes are managed through the `notifications` module.

The backend is responsible for keeping `posts.like_count` synchronized at all times.
