# 09 — Moderation (Reports)

Tables: `Reports`, `ReportReasons`.

Domain: `src/Domain/Moderation/*`. Specs: `docs/database/25`–`26`. Constants: `ReportReasonCodes`.

Depends on: seed reasons ([00-prerequisites.md](00-prerequisites.md)). Can start after Social/Reviews exist for real targets; reasons list can ship earlier.

---

## Progress

- [x] List active report reasons
- [x] Create report (user)
- [x] Moderator queue: start review / resolve / reject
- [x] Side effects on target entities (e.g. `Review.MarkAsReported`)

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `ReportsController` | `/api/reports` |
| `ReportReasonsController` | `/api/report-reasons` |

Moderator endpoints require the `Moderator` policy (`Authorization:ModeratorUserIds` allow-list until roles exist).

---

## Features

### Reasons

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `ListActiveReportReasons` | Query | `GET /api/report-reasons` | Active / selectable only. |

Admin CRUD optional; seed covers launch.

### User reports

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `CreateReport` | Command | `POST /api/reports` | `Report.Create` → Pending. Unique `(reporter, entityType, entityId)`. No self-report. Validate entity exists. Review targets get `MarkAsReported`. |
| [x] | `GetMyReports` | Query | `GET /api/reports/mine` | Offset page. |
| [x] | `UpdateReportDescription` | Command | `PUT /api/reports/{id}/description` | Only while Pending (reporter). |

`ReportEntityType`: User, Event, Post, Comment, Review, Message.

### Moderator workflow

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `ListReports` | Query | `GET /api/reports` (mod) | Optional status filter. |
| [x] | `StartReportReview` | Command | `POST /api/reports/{id}/start-review` | `StartReview` — assignee. |
| [x] | `ResolveReport` | Command | `POST /api/reports/{id}/resolve` | Resolution note required. |
| [x] | `RejectReport` | Command | `POST /api/reports/{id}/reject` | Clears `Review.IsReported`. |

### Target side effects

| Entity | Create report | Resolve | Reject |
| ------ | ------------- | ------- | ------ |
| Review | `MarkAsReported` | flagged | `ClearReportedStatus` |
| Post | — | `Hide` (`IsHidden`) | `Unhide` |
| Comment | — | `Hide` | `Unhide` |
| Message | — | `Redact` (one-way) | — |
| User / Event | — | no auto Suspend/Cancel | — |

Feed / list / get-by-id queries exclude hidden posts (author can still see own). Comment lists exclude hidden.

---

## Exit criteria

- [x] User can report with a seeded reason
- [x] Duplicate report → Conflict
- [x] Moderator can move Pending → UnderReview → Resolved/Rejected
