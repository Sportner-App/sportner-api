# Event Questions

## Module

Events

## Aggregate Root

EventQuestion

---

# Purpose

`EventQuestions` stores public Q&A on an event detail page.

Users can ask the organizer before joining. Replies stay one level under the question (Instagram-style flatten).

---

# Columns

| Column             | Type          | Nullable | Description                         |
| ------------------ | ------------- | -------- | ----------------------------------- |
| id                 | UUID          | No       | Primary key                         |
| event_id           | UUID          | No       | References events(id)               |
| author_user_id     | UUID          | No       | Author                              |
| parent_id          | UUID          | Yes      | Root question id; null = question   |
| reply_to_user_id   | UUID          | Yes      | Flattened reply-to-reply mention    |
| content            | VARCHAR(1000) | No       | Question or reply text              |
| reply_count        | INTEGER       | No       | Cached replies on the root only     |
| created_at         | TIMESTAMPTZ   | No       | Created                             |
| updated_at         | TIMESTAMPTZ   | Yes      | Audit                               |
| created_by_user_id | UUID          | Yes      | Audit                               |
| updated_by_user_id | UUID          | Yes      | Audit                               |

---

# Indexes

- PK(id)
- INDEX(event_id, created_at)
- INDEX(event_id, parent_id)
- INDEX(parent_id)
- INDEX(author_user_id)

---

# Foreign Keys

| Column           | References          | Delete   |
| ---------------- | ------------------- | -------- |
| event_id         | events(id)          | Restrict |
| author_user_id   | users(id)           | Restrict |
| parent_id        | event_questions(id) | Restrict |
| reply_to_user_id | users(id)           | Restrict |

---

# Business Rules

- Root rows (`parent_id` is null) are questions. Replies always attach to a root.
- Reply-to-reply flattens under the root and sets `reply_to_user_id`.
- Organizer cannot create a root question on their own event.
- Writes are allowed only while the event has not ended (`HasEnded`).
- Either-way block with the organizer or the reply target is forbidden.
- History stays readable after the event ends.
