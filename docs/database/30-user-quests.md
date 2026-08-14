# User Quests

## Module

Gamification

## Aggregate Root

User (progress owned via UserQuest)

---

# Purpose

Per-user progress for a quest. Non-repeatable: unique `(user_id, quest_id)`.
Auto-completes when `current_value >= quest.target_value` — no claim endpoint.

---

# Columns

| Column | Type | Nullable | Description |
| ------ | ---- | -------- | ----------- |
| id | UUID | No | Primary Key |
| user_id | UUID | No | FK users |
| quest_id | UUID | No | FK quests |
| status | SMALLINT | No | `QuestStatus` (Active=0, Completed=1, Expired=2, Abandoned=3) |
| current_value | INT | No | Progress toward target |
| completed_at | TIMESTAMPTZ | Yes | Set on complete |
| created_at | TIMESTAMPTZ | No | |
| updated_at | TIMESTAMPTZ | Yes | |
| created_by_user_id | UUID | Yes | Audit |
| updated_by_user_id | UUID | Yes | Audit |

---

# Indexes

- PK(id)
- UNIQUE(user_id, quest_id)
- INDEX(user_id)
- INDEX(quest_id)
- INDEX(status)
- INDEX(user_id, status)

---

# Foreign Keys

| Column | References | On delete |
| ------ | ---------- | --------- |
| user_id | users(id) | Restrict |
| quest_id | quests(id) | Restrict |

---

# Business Rules

- Day-1 statuses used: Active, Completed (Expired/Abandoned reserved).
- CurrentValue never decreases; increments via `IQuestProgressTracker`.
- On complete: set Completed + CompletedAt, then `IBadgeAwarder.TryAwardAsync` for reward badge.
- Completing twice is a no-op (status already Completed).
