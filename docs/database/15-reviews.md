# Reviews

## Module

Reviews

## Aggregate Root

Review

---

# Purpose

The `reviews` table stores ratings and comments exchanged between participants after completing an event.

Reviews contribute to each user's reputation and help build trust within the community.

Only users who actually attended the same event are allowed to review one another.

---

# Responsibilities

- Store participant ratings
- Store written reviews
- Build user reputation
- Prevent duplicate reviews
- Support community moderation

---

# Columns

| Column             | Type        | Nullable | Description                                    |
| ------------------ | ----------- | -------- | ---------------------------------------------- |
| id                 | UUID        | No       | Primary Key                                    |
| event_id           | UUID        | No       | References events(id)                          |
| reviewer_user_id   | UUID        | No       | User who submitted the review                  |
| reviewed_user_id   | UUID        | No       | User being reviewed                            |
| rating             | SMALLINT    | No       | Rating (1-5)                                   |
| comment            | TEXT        | Yes      | Optional review comment                        |
| is_reported        | BOOLEAN     | No       | Indicates whether the review has been reported |
| created_at         | TIMESTAMPTZ | No       | Created date                                   |
| updated_at         | TIMESTAMPTZ | Yes      | Updated date                                   |
| created_by_user_id | UUID        | Yes      | Audit                                          |
| updated_by_user_id | UUID        | Yes      | Audit                                          |

---

# Indexes

- PK(id)
- INDEX(event_id)
- INDEX(reviewer_user_id)
- INDEX(reviewed_user_id)
- INDEX(rating)

---

# Unique Constraints

- UNIQUE(event_id, reviewer_user_id, reviewed_user_id)

---

# Check Constraints

- rating BETWEEN 1 AND 5
- reviewer_user_id <> reviewed_user_id

---

# Foreign Keys

| Column           | References |
| ---------------- | ---------- |
| event_id         | events(id) |
| reviewer_user_id | users(id)  |
| reviewed_user_id | users(id)  |

---

# Relationships

## Belongs To

- events
- users (Reviewer)
- users (Reviewed)

---

# Business Rules

- Reviews are allowed only after the event is completed.
- Both users must have attended the same event.
- Each participant may review another participant only once per event.
- Users cannot review themselves.
- Organizer and participants can review each other equally.
- Ratings must be between 1 and 5.
- Comments are optional.
- Reported reviews are hidden until moderation is completed.
- Updating a review recalculates the reviewed user's statistics.

---

# Lifecycle

### Event Completed

↓

Organizer confirms attendance

↓

Review period becomes available

↓

Participant submits review

↓

User reputation is recalculated

↓

Review becomes publicly visible

---

# Performance Notes

Most queries retrieve reviews by:

- reviewed_user_id
- event_id

Average rating and review count should not be calculated on every request.

They should be stored in `user_statistics` and updated whenever a review is created, updated or removed.

---

# Future Extensions

Possible future additions:

- Review categories (Sportsmanship, Skill, Communication, Punctuality)
- Anonymous reviews
- Review likes
- Review replies
- Review editing window
- AI moderation
- Toxic language detection

---

# Notes

Reviews represent long-term reputation and should never be physically deleted under normal circumstances.

If a review violates community guidelines, it should be hidden through the moderation system rather than removed from the database.

User profile ratings, review count and average score are derived from this table and synchronized with `user_statistics`.

Review authorization must always be validated by backend business logic using the `event_participants` table.
