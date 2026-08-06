# Reports

## Module

Moderation

## Aggregate Root

Report

---

# Purpose

The `reports` table stores user-generated reports for content and entities that violate community guidelines.

Reports allow users to flag inappropriate or abusive content for moderation.

The table supports multiple entity types through a polymorphic relationship, eliminating the need for separate report tables.

---

# Responsibilities

- Store user reports
- Prevent duplicate reports
- Support moderation workflows
- Track report status
- Support all reportable entities

---

# Columns

| Column              | Type        | Nullable | Description                     |
| ------------------- | ----------- | -------- | ------------------------------- |
| id                  | UUID        | No       | Primary Key                     |
| reporter_user_id    | UUID        | No       | User submitting the report      |
| entity_type         | SMALLINT    | No       | Reported entity type            |
| entity_id           | UUID        | No       | Reported entity identifier      |
| report_reason_id    | UUID        | No       | References report_reasons(id)   |
| description         | VARCHAR(2000) | Yes    | Optional additional information |
| status              | SMALLINT    | No       | Report status                   |
| reviewed_by_user_id | UUID        | Yes      | Moderator reviewing the report  |
| reviewed_at         | TIMESTAMPTZ | Yes      | Review date                     |
| resolution_note     | VARCHAR(2000) | Yes    | Moderator decision              |
| created_at          | TIMESTAMPTZ | No       | Created date                    |
| updated_at          | TIMESTAMPTZ | Yes      | Last updated date               |
| created_by_user_id  | UUID        | Yes      | Audit                           |
| updated_by_user_id  | UUID        | Yes      | Audit                           |

---

# Indexes

- PK(id)
- INDEX(entity_type, entity_id)
- INDEX(reporter_user_id)
- INDEX(status)
- INDEX(created_at)

---

# Unique Constraints

- UNIQUE(reporter_user_id, entity_type, entity_id)

---

# Foreign Keys

| Column              | References         |
| ------------------- | ------------------ |
| reporter_user_id    | users(id)          |
| report_reason_id    | report_reasons(id) |
| reviewed_by_user_id | users(id)          |

---

# Relationships

## Belongs To

- users (Reporter)
- users (Moderator)
- report_reasons

---

# Entity Types

| Value | Name    |
| ----- | ------- |
| 0     | User    |
| 1     | Event   |
| 2     | Post    |
| 3     | Comment |
| 4     | Review  |
| 5     | Message |

---

# Report Status

| Value | Name        |
| ----- | ----------- |
| 0     | Pending     |
| 1     | UnderReview |
| 2     | Resolved    |
| 3     | Rejected    |

---

# Business Rules

- A user may report the same entity only once.
- Users cannot report their own content.
- Every report must reference a valid report reason.
- Reports are immutable except for moderation fields.
- Only moderators may update report status.
- Reports remain in the system for audit purposes.
- Resolved reports cannot return to the Pending state.
- Multiple reports for the same entity may trigger automatic moderation actions.

---

# Lifecycle

## Create Report

- Validate the entity exists.
- Validate the user has not already reported the entity.
- Validate the selected report reason.
- Create the report.
- Queue the report for moderation.

---

## Review Report

- Assign moderator.
- Review the reported entity.
- Update report status.
- Record moderation notes.
- Apply moderation action if necessary.

---

## Resolve Report

Possible moderation actions include:

- No action
- Hide content
- Remove content
- Suspend user
- Ban user

These actions are executed by backend business logic and are not stored directly in this table.

---

# Performance Notes

Most moderation queries retrieve reports by:

- status
- entity_type
- created_at

Indexes should support moderation dashboards with large report volumes.

---

# Future Extensions

Possible future additions:

- AI moderation score
- Automatic spam detection
- Report priority
- Moderator assignment queue
- Evidence attachments
- Appeal workflow
- Bulk moderation
- Moderation history

---

# Notes

This table stores only report records.

The reported content is identified through the combination of:

- entity_type
- entity_id

This polymorphic design allows new reportable entities to be introduced without changing the database schema.

All moderation decisions should be audited through backend logging.

Reports should never be physically deleted except through administrative maintenance procedures.
