# Profiles

## Module

Identity

## Aggregate Root

User

---

# Purpose

The `profiles` table stores all public user information displayed throughout the application.

This table is intentionally separated from `users` to keep authentication and profile data isolated.

---

# Responsibilities

- Public profile information
- Personal details
- Profile media
- Visibility settings

Authentication data must never be stored here.

---

# Columns

| Column             | Type         | Nullable | Description           |
| ------------------ | ------------ | -------- | --------------------- |
| id                 | UUID         | No       | Primary Key           |
| user_id            | UUID         | No       | References users(id)  |
| username           | VARCHAR(30)  | No       | Unique username       |
| first_name         | VARCHAR(50)  | No       | First name            |
| last_name          | VARCHAR(50)  | Yes      | Last name             |
| bio                | VARCHAR(500) | Yes      | User biography        |
| gender             | SMALLINT     | Yes      | Gender enum           |
| birth_date         | DATE         | Yes      | Birth date            |
| city               | VARCHAR(100) | Yes      | City                  |
| profile_image_url  | TEXT         | Yes      | Profile image path    |
| intro_video_url    | TEXT         | Yes      | Intro video path      |
| average_rating     | DECIMAL(3,2) | No       | Cached average rating |
| review_count       | INTEGER      | No       | Cached review count   |
| is_profile_public  | BOOLEAN      | No       | Profile visibility    |
| created_at         | TIMESTAMPTZ  | No       | Created date          |
| updated_at         | TIMESTAMPTZ  | Yes      | Updated date          |
| created_by_user_id | UUID         | Yes      | Audit                 |
| updated_by_user_id | UUID         | Yes      | Audit                 |

---

# Indexes

- PK(id)
- UNIQUE(user_id)
- UNIQUE(username)
- INDEX(city)
- INDEX(average_rating)

---

# Relationships

## Belongs To

- users

## Referenced By

- None

---

# Business Rules

- Every profile belongs to exactly one user.
- Username must be unique.
- Username cannot be changed more than once every 30 days (backend rule).
- Intro video is optional.
- Profile image is optional.
- AverageRating and ReviewCount are maintained by the backend.

---

# Future Extensions

Possible future additions:

- Instagram
- X (Twitter)
- YouTube
- Website
- Favorite sports
- Cover image
- Verification badge

---

# Notes

Only publicly visible profile information should exist in this table.

Sport skills, statistics, badges and sessions belong to their own tables.
