# User Sports

## Module

Identity

## Aggregate Root

User

---

# Purpose

The `user_sports` table stores the sports a user participates in along with their self-declared skill level.

Each user can associate with multiple sports, but each sport can only be added once per user.

---

# Responsibilities

- Store user's sports
- Store user's skill level for each sport
- Provide skill information during event participation

---

# Columns

| Column             | Type        | Nullable | Description                        |
| ------------------ | ----------- | -------- | ---------------------------------- |
| id                 | UUID        | No       | Primary Key                        |
| user_id            | UUID        | No       | References users(id)               |
| sport_id           | UUID        | No       | References sports(id)              |
| skill_level        | SMALLINT    | No       | Skill level enum                   |
| is_primary         | BOOLEAN     | No       | Indicates the user's primary sport |
| created_at         | TIMESTAMPTZ | No       | Created date                       |
| updated_at         | TIMESTAMPTZ | Yes      | Updated date                       |
| created_by_user_id | UUID        | Yes      | Audit                              |
| updated_by_user_id | UUID        | Yes      | Audit                              |

---

# Indexes

- PK(id)
- INDEX(user_id)
- INDEX(sport_id)
- INDEX(skill_level)

---

# Unique Constraints

UNIQUE(user_id, sport_id)

---

# Relationships

## Belongs To

- users
- sports

---

# Skill Levels

| Value | Name         |
| ----- | ------------ |
| 0     | Beginner     |
| 1     | Intermediate |
| 2     | Advanced     |
| 3     | Expert       |
| 4     | Professional |

---

# Business Rules

- A user can have multiple sports.
- A sport can only be added once per user.
- Only one sport can be marked as the primary sport.
- Skill level can be updated at any time.
- Removing a sport does not affect event history.

---

# Future Extensions

Possible future additions:

- Years of experience
- Preferred playing position
- Dominant hand
- Certifications
- Coach flag
- Verified skill level

---

# Notes

Skill level is self-declared.

Future versions may introduce verified skill levels based on event participation, community reviews or platform verification.
