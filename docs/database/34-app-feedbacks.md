# App Feedbacks

## Module

Support

## Aggregate Root

AppFeedback

---

# Purpose

The `AppFeedbacks` table stores in-app product suggestions submitted by authenticated users.

There is no email or moderation workflow. Rows are reviewed directly in the database.

---

# Responsibilities

- Persist a single free-text suggestion per submission
- Attribute the suggestion to the submitting user
- Support later review by `CreatedAt`

---

# Columns

| Column             | Type          | Nullable | Description              |
| ------------------ | ------------- | -------- | ------------------------ |
| id                 | UUID          | No       | Primary Key              |
| user_id            | UUID          | No       | Submitting user          |
| content            | VARCHAR(2000) | No       | Suggestion text          |
| created_at         | TIMESTAMPTZ   | No       | Created date             |
| updated_at         | TIMESTAMPTZ   | Yes      | Audit                    |
| created_by_user_id | UUID          | Yes      | Audit                    |
| updated_by_user_id | UUID          | Yes      | Audit                    |

---

# Indexes

- PK(id)
- INDEX(user_id)
- INDEX(created_at)
- INDEX(user_id, created_at)

---

# Foreign Keys

| Column  | References |
| ------- | ---------- |
| user_id | users(id)  |

Delete behavior: Restrict.

---

# Business Rules

- Content is required, trimmed, 10–2000 characters.
- A user may submit more than one suggestion.
- The same user cannot submit again within 2 minutes.
- Suggestions are append-only. No update or delete API.
- No email, notification, or admin inbox is generated.

---

# Notes

Review submissions with:

```sql
SELECT "CreatedAt", "UserId", "Content"
FROM "AppFeedbacks"
ORDER BY "CreatedAt" DESC;
```
