# 09 — Moderation (Reports)

Tables: `Reports`, `ReportReasons`.

Domain: `src/Domain/Moderation/*`. Specs: `docs/database/25`–`26`. Constants: `ReportReasonCodes`.

Depends on: seed reasons ([00-prerequisites.md](00-prerequisites.md)). Can start after Social/Reviews exist for real targets; reasons list can ship earlier.

---

## Progress

- [ ] List active report reasons
- [ ] Create report (user)
- [ ] Moderator queue: start review / resolve / reject
- [ ] Side effects on target entities (e.g. `Review.MarkAsReported`)

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `ReportsController` | `/api/reports` |
| `ReportReasonsController` | `/api/report-reasons` |

Moderator endpoints require an admin/moderator authorization policy (define in [10-cross-cutting.md](10-cross-cutting.md)).

---

## Features

### Reasons

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `ListActiveReportReasons` | Query | `GET /api/report-reasons` | `IsSelectable` / active only. |

Admin CRUD optional; seed covers launch.

### User reports

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `CreateReport` | Command | `POST /api/reports` | `Report.Create` → Pending. Unique `(reporter, entityType, entityId)`. No self-report. Validate entity exists. |
| [ ] | `GetMyReports` | Query | `GET /api/reports/mine` | Optional. |

`ReportEntityType`: User, Event, Post, Comment, Review, Message.

### Moderator workflow

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `ListPendingReports` | Query | `GET /api/reports` (mod) | Filter by status. |
| [ ] | `StartReportReview` | Command | `POST /api/reports/{id}/start-review` | `StartReview` — assignee. |
| [ ] | `ResolveReport` | Command | `POST /api/reports/{id}/resolve` | Resolution note required. |
| [ ] | `RejectReport` | Command | `POST /api/reports/{id}/reject` | |
| [ ] | `UpdateReportDescription` | Command | `PUT /api/reports/{id}/description` | Only while Pending (reporter). |

### Target side effects

| Entity | On create / resolve |
| ------ | ------------------- |
| Review | `MarkAsReported` / `ClearReportedStatus` when rejected |
| Post / Comment / User / Message | Product rules (hide, suspend) — implement deliberately; do not auto-ban without policy |

---

## Exit criteria

- [ ] User can report with a seeded reason
- [ ] Duplicate report → Conflict
- [ ] Moderator can move Pending → UnderReview → Resolved/Rejected
