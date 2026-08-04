# User Statistics

## Module

Identity

## Aggregate Root

User

---

# Purpose

The `user_statistics` table stores precomputed user statistics to improve application performance.

Instead of calculating values from multiple tables on every request, statistics are updated by the backend whenever related actions occur.

This table acts as a cache for user profile statistics.

---

# Responsibilities

- Store participation statistics
- Store organizer statistics
- Store review statistics
- Store social statistics
- Store cancellation statistics

---

# Columns

| Column | Type | Nullable | Description |
|----------|------|----------|-------------|
| id | UUID | No | Primary Key |
| user_id | UUID | No | References users(id) |
| events_joined | INTEGER | No | Total joined events |
| events_organized | INTEGER | No | Total organized events |
| events_completed | INTEGER | No | Successfully completed events |
| events_cancelled | INTEGER | No | Cancelled events |
| attendance_rate | DECIMAL(5,2) | No | Attendance percentage |
| average_rating | DECIMAL(3,2) | No | Average review score |
| total_reviews | INTEGER | No | Total received reviews |
| friends_count | INTEGER | No | Total friends |
| posts_count | INTEGER | No | Total posts |
| badges_count | INTEGER | No | Total earned badges |
| created_at | TIMESTAMPTZ | No | Created date |
| updated_at | TIMESTAMPTZ | Yes | Updated date |
| created_by_user_id | UUID | Yes | Audit |
| updated_by_user_id | UUID | Yes | Audit |

---

# Indexes

- PK(id)
- UNIQUE(user_id)

---

# Relationships

## Belongs To

- users

---

# Business Rules

- Every user has exactly one statistics record.
- Statistics are updated only by the backend.
- Clients cannot modify statistics directly.
- Statistics should always reflect completed business operations.

---

# Update Triggers

The backend updates this table when:

- User joins an event
- User completes an event
- User cancels an event
- User organizes an event
- User receives a review
- User creates a post
- User earns a badge
- Friendship is created or removed

---

# Future Extensions

Possible future additions:

- Current streak
- Longest streak
- Favorite sport
- Total play time
- Total distance
- Achievement points
- Reputation score
- No-show count

---

# Notes

This table is a performance optimization table.

Values are derived from other business tables and should never be treated as the source of truth.

If necessary, statistics can always be recalculated from the underlying data.