# Quests

## Module

Gamification

## Aggregate Root

Quest

---

# Purpose

Catalog of evergreen quest definitions. A quest tracks a single metric toward a target and
awards a badge on auto-complete. Separate from `Badges` / `UserBadges` so progress UI and
award idempotency stay clean.

---

# Columns

| Column | Type | Nullable | Description |
| ------ | ---- | -------- | ----------- |
| id | UUID | No | Primary Key |
| code | VARCHAR(100) | No | Immutable unique code |
| title | VARCHAR(150) | No | Display title (Turkish, default) |
| title_en | VARCHAR(150) | Yes | English display title |
| description | VARCHAR(1000) | No | Display description (Turkish, default) |
| description_en | VARCHAR(1000) | Yes | English display description |
| metric_code | VARCHAR(100) | No | Application metric key (`QuestMetrics`) |
| target_value | INT | No | Required progress |
| reward_badge_id | UUID | No | FK Badges — awarded on complete |
| sort_order | SMALLINT | No | Catalog order |
| is_active | BOOLEAN | No | Soft catalog flag (no soft-delete row) |
| created_at | TIMESTAMPTZ | No | |
| updated_at | TIMESTAMPTZ | Yes | |
| created_by_user_id | UUID | Yes | Audit |
| updated_by_user_id | UUID | Yes | Audit |

Day-1: no `starts_at` / `ends_at` (evergreen). V2.1 optional.

---

# Indexes

- PK(id)
- UNIQUE(code)
- INDEX(is_active)
- INDEX(metric_code)
- INDEX(sort_order)
- INDEX(reward_badge_id)

---

# Foreign Keys

| Column | References | On delete |
| ------ | ---------- | --------- |
| reward_badge_id | badges(id) | Restrict |

---

# Business Rules

- Code immutable after create.
- TargetValue > 0.
- MetricCode must match Application `QuestMetrics` dictionary.
- Inactive quests are hidden from list APIs; in-flight UserQuests are left as-is.
- `title_en`/`description_en` are optional; read paths resolve to the caller's negotiated UI culture (`Accept-Language`) via `CatalogLocalization`, falling back to the Turkish text when missing.
