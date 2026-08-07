# User Badges

## Module

Gamification

## Aggregate Root

User

---

# Purpose

The `user_badges` table stores the badges earned by users.

Each record represents a single badge awarded to a specific user.

Badge definitions are stored in the `badges` table, while this table only records ownership and the date the badge was earned.

---

# Responsibilities

- Store earned badges
- Prevent duplicate badge awards
- Track achievement history
- Support user user_profiles
- Support gamification

---

# Columns

| Column             | Type        | Nullable | Description           |
| ------------------ | ----------- | -------- | --------------------- |
| id                 | UUID        | No       | Primary Key           |
| user_id            | UUID        | No       | References users(id)  |
| badge_id           | UUID        | No       | References badges(id) |
| earned_at          | TIMESTAMPTZ | No       | Badge earned date     |
| created_at         | TIMESTAMPTZ | No       | Created date          |
| updated_at         | TIMESTAMPTZ | Yes      | Updated date          |
| created_by_user_id | UUID        | Yes      | Audit                 |
| updated_by_user_id | UUID        | Yes      | Audit                 |

---

# Indexes

- PK(id)
- INDEX(user_id)
- INDEX(badge_id)
- INDEX(earned_at)

---

# Unique Constraints

- UNIQUE(user_id, badge_id)

---

# Foreign Keys

| Column   | References |
| -------- | ---------- |
| user_id  | users(id)  |
| badge_id | badges(id) |

---

# Relationships

## Belongs To

- users
- badges

---

# Business Rules

- A user may earn the same badge only once.
- Badge ownership is permanent.
- Badge definitions are managed through the `badges` table.
- Earning a badge automatically awards the badge's experience points.
- Earning a badge generates a notification for the user.
- Badge assignment is performed only through backend business logic.
- Badge ownership cannot be created manually by clients.

---

# Lifecycle

## Earn Badge

- Validate badge exists.
- Validate badge is active.
- Verify the user does not already own the badge.
- Create the ownership record.
- Award experience points.
- Update user statistics if applicable.
- Generate a badge notification.

---

## View Profile

- Retrieve earned badges.
- Join badge metadata from the `badges` table.
- Display badges ordered by rarity and earned date.

---

# Performance Notes

Most queries retrieve badges by:

- user_id
- earned_at

Badge definitions should be joined with cached metadata.

Profile pages should order badges by:

1. Rarity
2. Display Order
3. Earned Date

---

# Future Extensions

Possible future additions:

- Featured badges
- Badge showcase
- Badge visibility
- Badge progress tracking
- Seasonal achievements
- Hidden achievements
- Badge sharing
- XP history
- User levels

---

# Notes

This table stores only badge ownership.

Badge metadata is always loaded from the `badges` table.

Experience points are awarded only once when the badge is first earned.

Notifications are generated through the `notifications` module.

Badge ownership contributes to the user's overall reputation and progression within the platform.

Removing a badge should only be possible through administrative actions and should be logged for auditing purposes.
