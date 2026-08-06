# Report Reasons

## Module

Moderation

## Aggregate Root

ReportReason

---

# Purpose

The `report_reasons` table stores all predefined report reasons that users can select when reporting content or users.

Report reasons provide a standardized moderation workflow and allow the platform to categorize reported content consistently.

This table acts as a master data table and is managed only by administrators.

---

# Responsibilities

- Store predefined report reasons
- Standardize moderation categories
- Support localization
- Enable moderation analytics

---

# Columns

| Column             | Type         | Nullable | Description                                   |
| ------------------ | ------------ | -------- | --------------------------------------------- |
| id                 | UUID         | No       | Primary Key                                   |
| code               | VARCHAR(100) | No       | Unique internal identifier                    |
| name               | VARCHAR(100) | No       | Display name                                  |
| description        | VARCHAR(1000) | Yes    | Additional explanation shown to users         |
| display_order      | SMALLINT     | No       | Display order in the UI                       |
| is_active          | BOOLEAN      | No       | Determines whether the reason can be selected |
| created_at         | TIMESTAMPTZ  | No       | Created date                                  |
| updated_at         | TIMESTAMPTZ  | Yes      | Last updated date                             |
| created_by_user_id | UUID         | Yes      | Audit                                         |
| updated_by_user_id | UUID         | Yes      | Audit                                         |

---

# Indexes

- PK(id)
- UNIQUE(code)
- INDEX(display_order)
- INDEX(is_active)

---

# Relationships

## Referenced By

- reports

---

# Business Rules

- Report reasons are predefined by the platform.
- Users can only select active report reasons.
- Report reasons cannot be deleted if referenced by existing reports.
- Inactive report reasons remain available for historical reports.
- Backend business logic should reference reasons by `code`, not by `name`.

---

# Lifecycle

## Create Report Reason

- Create report reason.
- Assign unique code.
- Set display order.
- Activate.

---

## Update Report Reason

- Update display name.
- Update description.
- Update display order.
- Existing reports remain unchanged.

---

## Deactivate Report Reason

- Set `is_active = false`.
- New reports cannot use this reason.
- Historical reports continue to reference it.

---

# Performance Notes

This table is expected to contain a small number of records.

Report reasons should be cached by the application to minimize database access.

---

# Default Report Reasons

| Code                  | Name                  |
| --------------------- | --------------------- |
| SPAM                  | Spam                  |
| HARASSMENT            | Harassment            |
| HATE_SPEECH           | Hate Speech           |
| INAPPROPRIATE_CONTENT | Inappropriate Content |
| VIOLENCE              | Violence              |
| NUDITY                | Nudity                |
| FAKE_INFORMATION      | Fake Information      |
| IMPERSONATION         | Impersonation         |
| SCAM                  | Scam or Fraud         |
| OTHER                 | Other                 |

---

# Future Extensions

Possible future additions:

- Entity-specific report reasons
- Severity levels
- AI moderation mapping
- Localization support
- Automatic moderation thresholds
- Parent / Child reason hierarchy

---

# Notes

This table contains only master data.

The `code` field is the permanent identifier used by the backend and must never change.

The `name` and `description` fields may be localized without affecting business logic.

All report reasons should be seeded during application initialization.

This table should remain relatively static throughout the lifetime of the application.
