# Badges

## Module

Gamification

## Aggregate Root

Badge

---

# Purpose

The `badges` table stores all badge definitions available in the application.

A badge represents an achievement that users can earn by completing specific actions or reaching predefined milestones.

This table contains only badge metadata. User ownership is managed separately by the `user_badges` table.

---

# Responsibilities

- Define available badges
- Store badge metadata
- Support gamification
- Enable achievement system

---

# Columns

| Column             | Type         | Nullable | Description                           |
| ------------------ | ------------ | -------- | ------------------------------------- |
| id                 | UUID         | No       | Primary Key                           |
| code               | VARCHAR(100) | No       | Unique badge identifier               |
| name               | VARCHAR(100) | No       | Badge name                            |
| description        | VARCHAR(1000) | No     | Badge description                     |
| icon_path          | VARCHAR(500)  | No     | Badge icon stored in Supabase Storage |
| category           | SMALLINT     | No       | Badge category                        |
| rarity             | SMALLINT     | No       | Badge rarity                          |
| experience_points  | INTEGER      | No       | XP awarded when earned                |
| display_order      | SMALLINT     | No       | UI display order                      |
| is_active          | BOOLEAN      | No       | Badge availability                    |
| created_at         | TIMESTAMPTZ  | No       | Created date                          |
| updated_at         | TIMESTAMPTZ  | Yes      | Updated date                          |
| created_by_user_id | UUID         | Yes      | Audit                                 |
| updated_by_user_id | UUID         | Yes      | Audit                                 |

---

# Indexes

- PK(id)
- UNIQUE(code)
- INDEX(category)
- INDEX(rarity)
- INDEX(is_active)

---

# Relationships

## Referenced By

- user_badges

---

# Badge Categories

| Value | Name        |
| ----- | ----------- |
| 0     | Sports      |
| 1     | Events      |
| 2     | Social      |
| 3     | Community   |
| 4     | Streak      |
| 5     | Achievement |
| 6     | Special     |

---

# Badge Rarity

| Value | Name      |
| ----- | --------- |
| 0     | Common    |
| 1     | Rare      |
| 2     | Epic      |
| 3     | Legendary |

---

# Business Rules

- Badge codes must be unique.
- Badge definitions are managed only by administrators.
- Badges can be deactivated without deleting existing user achievements.
- Badge icons are stored in Supabase Storage.
- XP is awarded only once when a badge is earned.
- Badge metadata can be updated without affecting earned badges.

---

# Lifecycle

## Create Badge

- Create badge definition.
- Upload badge icon.
- Publish badge.

---

## Update Badge

- Update metadata.
- Replace icon if necessary.
- Keep existing user achievements unchanged.

---

## Deactivate Badge

- Set `is_active = false`.
- Existing earned badges remain visible.
- Badge can no longer be earned.

---

# Performance Notes

Most queries retrieve badges by:

- category
- rarity
- display_order

Badge definitions are relatively static and should be cached.

---

# Future Extensions

Possible future additions:

- Seasonal badges
- Hidden badges
- Secret achievements
- Badge levels
- Localized badge names
- Badge expiration
- Event-exclusive badges
- AI-generated achievements

---

# Notes

This table contains only badge definitions.

Ownership information is stored in the `user_badges` table.

Badge icons are stored exclusively in Supabase Storage.

The `code` field is used internally by the backend to identify badge rules.

Examples:

- FIRST_EVENT
- FIRST_POST
- FIRST_FRIEND
- FIRST_REVIEW
- SPORTS_EXPLORER
- COMMUNITY_HELPER
- EVENT_MASTER
- MARATHON_RUNNER
