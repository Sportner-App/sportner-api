# Sports

## Module

System

## Aggregate Root

None (Lookup Table)

---

# Purpose

The `sports` table stores all supported sports available on the platform.

Users cannot create custom sports. Every sport used throughout the application must exist in this table.

This table acts as a shared reference for user skills and events.

---

# Responsibilities

- Define supported sports
- Ensure data consistency
- Provide a single source of truth for sports

---

# Columns

| Column             | Type         | Nullable | Description                       |
| ------------------ | ------------ | -------- | --------------------------------- |
| id                 | UUID         | No       | Primary Key                       |
| name               | VARCHAR(100) | No       | Sport name                        |
| slug               | VARCHAR(100) | No       | URL-friendly identifier           |
| icon_url           | TEXT         | Yes      | Sport icon path                   |
| display_order      | INTEGER      | No       | Display order                     |
| is_active          | BOOLEAN      | No       | Whether the sport can be selected |
| created_at         | TIMESTAMPTZ  | No       | Created date                      |
| updated_at         | TIMESTAMPTZ  | Yes      | Updated date                      |
| created_by_user_id | UUID         | Yes      | Audit                             |
| updated_by_user_id | UUID         | Yes      | Audit                             |

---

# Indexes

- PK(id)
- UNIQUE(name)
- UNIQUE(slug)
- INDEX(display_order)
- INDEX(is_active)

---

# Relationships

## One To Many

- user_sports
- events

---

# Business Rules

- Sport names must be unique.
- Slugs must be unique.
- Users can only select active sports.
- Events can only be created using active sports.
- Sports are managed only by administrators.

---

# Initial Seed Data

- Basketball
- Football
- Volleyball
- Tennis
- Table Tennis
- Running
- Cycling
- Swimming
- Fitness
- Hiking
- Boxing
- Pilates
- Yoga
- CrossFit
- Badminton

Additional sports can be added without schema changes.

---

# Future Extensions

Possible future additions:

- Category
- Olympic sport flag
- Team/Individual type
- Indoor/Outdoor type
- Required player count
- Color
- Emoji/Icon pack

---

# Notes

This is a lookup table.

Sports should never be deleted.

If a sport is no longer available, set `is_active = false`.
