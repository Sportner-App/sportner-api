# Post Likes

## Module

Social

## Aggregate Root

PostLike

---

# Purpose

The `post_likes` table stores user likes for social posts.

Each record represents a single user's like for a single post.

`PostLike` is an independent aggregate root that references a Post and a User.

The aggregate owns only the intrinsic state of the like itself.

Updating cached counters, notifications and feed ranking are coordinated by the Application layer.

---

# Responsibilities

- Store a user's like
- Prevent duplicate likes through database constraints
- Represent a lightweight engagement record

The aggregate is **not responsible** for:

- Notifications
- Feed ranking
- Statistics calculation
- Counter synchronization

---

# Aggregate Boundary

```text
PostLike
```

`PostLike` references:

- Post
- User

It does not belong to the Post aggregate.

The Post aggregate never loads all likes.

---

# Columns

| Column             | Type        | Nullable | Description            |
| ------------------ | ----------- | -------: | ---------------------- |
| id                 | UUID        |       No | Primary key            |
| post_id            | UUID        |       No | References `posts(id)` |
| user_id            | UUID        |       No | References `users(id)` |
| created_at         | TIMESTAMPTZ |       No | Creation timestamp     |
| updated_at         | TIMESTAMPTZ |      Yes | Last update timestamp  |
| created_by_user_id | UUID        |      Yes | Audit                  |
| updated_by_user_id | UUID        |      Yes | Audit                  |

---

# Indexes

- `PK(id)`
- `INDEX(post_id)`
- `INDEX(user_id)`
- `INDEX(user_id, created_at)`

---

# Unique Constraints

```text
UNIQUE(post_id, user_id)
```

A user may like the same post only once.

---

# Foreign Keys

| Column  | References | Delete Behavior |
| ------- | ---------- | --------------- |
| post_id | posts(id)  | Cascade         |
| user_id | users(id)  | Cascade         |

---

# Relationships

## References

- posts
- users

---

# Business Rules

- A like always belongs to exactly one post.
- A like always belongs to exactly one user.
- A user may like a post only once.
- Users cannot like their own posts.
- Deleted posts cannot receive new likes.
- Like records are immutable after creation.
- A like has no editable fields.
- Removing a like permanently deletes the record.
- Soft delete is not used.

---

# Lifecycle

## Create Like

Application flow:

1. Validate the user exists.
2. Validate the post exists.
3. Validate the post is available.
4. Validate the user is not the owner.
5. Validate no existing like exists.
6. Create PostLike.
7. Persist PostLike.
8. Increment `posts.like_count`.
9. Create notification when appropriate.

Only step **6** belongs to the Domain aggregate.

---

## Remove Like

Application flow:

1. Validate ownership.
2. Delete PostLike.
3. Decrement `posts.like_count`.
4. Remove or invalidate related notification if required.

---

# Cached Counter Contract

`PostLike` never updates `posts.like_count`.

The Application layer coordinates:

- PostLike persistence
- Cached counter synchronization
- Transaction handling

The cached counter must never become negative.

---

# Notification Contract

Creating a like may generate a notification.

Rules:

- Notify only the post owner.
- Do not notify for self-likes.
- Notification generation belongs to the Application layer.

The Domain aggregate has no dependency on Notification.

---

# Performance Notes

Like counts must never be calculated using:

```sql
COUNT(*)
```

during feed rendering.

Feed queries should always use the cached `posts.like_count`.

Common queries:

- Likes for a post
- Posts liked by a user
- Recent likes by a user

---

# Concurrency Notes

Concurrent likes should be protected by:

- `UNIQUE(post_id, user_id)`
- Transaction handling
- Atomic counter updates

Duplicate likes must be impossible.

---

# Deletion Policy

Likes are lightweight interaction records.

They are permanently removed when:

- The user removes the like.
- The parent post is deleted.

No soft-delete mechanism is required.

---

# Future Extensions

Possible future additions:

- Emoji reactions
- Multiple reaction types
- Friend likes
- Like history
- Analytics
- Reaction summaries

These features should extend the interaction model without changing the aggregate boundary.

---

# Notes

`PostLike` is an independent aggregate root.

It references a Post but is not owned by the Post aggregate.

The Application layer is responsible for:

- Duplicate validation across persistence
- Notification creation
- Counter synchronization
- Transaction management
- Feed recalculation

The aggregate itself only represents a valid "like" relationship between a user and a post.
