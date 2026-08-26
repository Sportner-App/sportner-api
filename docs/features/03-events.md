# 03 — Events

Tables: `Events`, `EventParticipants`, `EventWaitlist`. Related: event `Conversations` created on publish.

Domain: `src/Domain/Events/*`. Specs: `docs/database/09`–`11`.

Depends on: [01-identity.md](01-identity.md), [02-catalog.md](02-catalog.md).

---

## Progress

- [x] Organizer create / update / publish / cancel / complete
- [x] Discovery queries
- [x] Apply / approve / reject / cancel participation
- [x] Waitlist join / promote
- [x] Attendance (Attended / NoShow)
- [x] Publish → event conversation orchestration

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `EventsController` | `/api/events` |

Nested actions stay on the same controller for v1.

---

## Features

### Organizer lifecycle

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `CreateEvent` | Command | `POST /api/events` | Draft + auto organizer participant. Requires `CanCreateContent`. |
| [x] | `UpdateEventDetails` | Command | `PUT /api/events/{id}` | Draft/Published/Full. |
| [x] | `UpdateEventSchedule` | Command | `PUT /api/events/{id}/schedule` | Future date + duration > 0 (domain). |
| [x] | `UpdateEventLocation` | Command | `PUT /api/events/{id}/location` | |
| [x] | `UpdateEventCapacity` | Command | `PUT /api/events/{id}/capacity` | Cannot shrink below occupied count. |
| [x] | `PublishEvent` | Command | `POST /api/events/{id}/publish` | Creates event conversation + owner member if missing; bumps `EventsOrganized` once. |
| [x] | `CancelEvent` | Command | `POST /api/events/{id}/cancel` | Closes conversation; notifies approved/pending participants (`EventCancelled`); bumps cancelled counter. |
| [x] | `CompleteEvent` | Command | `POST /api/events/{id}/complete` | Manual fallback. Auto-complete runs when `eventDate + duration` elapses (`EventCompletionDispatcher` + lazy on `GetEventById`). Closes conversation. |
| [x] | `GetEventById` | Query | `GET /api/events/{id}` | Sport (`sportName`, `sportSlug`, `sportCoverImageUrl`), organizer snippet, counts, my participation / waitlist, conversation id. |
| [x] | `ListMyOrganizedEvents` | Query | `GET /api/events/mine/organized` | Offset pagination. |
| [x] | `ListMyParticipatingEvents` | Query | `GET /api/events/mine/participating?scope=` | Excludes self-organized; skips rejected/cancelled. Optional `scope=upcoming|past` (by start time). |
| [x] | `DiscoverEvents` | Query | `GET /api/events` | Published/Full, future dates; optional `sportId` + address city substring (V1 compat). List items include `sportCoverImageUrl`. |
| [x] | `ExploreEvents` | Query | `GET /api/explore/events` | Ranked discover (V2); auth required; optional geo/sport/city; `limit`. Same sport cover field as list/detail. |

### Participation

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `ApplyToEvent` | Command | `POST /api/events/{id}/apply` | Pending participant or waitlist. Organizer blocked. Cancelled users may re-apply (same row). |
| [x] | `ApproveParticipant` | Command | `POST /api/events/{id}/participants/{userId}/approve` | Adds conversation member; `EventsJoined`++; `EventRequestApproved` notification. |
| [x] | `RejectParticipant` | Command | `POST /api/events/{id}/participants/{userId}/reject` | `EventRequestRejected` notification. |
| [x] | `CancelParticipation` | Command | `POST /api/events/{id}/participants/me/cancel` | Removes conversation membership when present. Blocked after scheduled end / completed / cancelled. |
| [x] | `ListParticipants` | Query | `GET /api/events/{id}/participants` | Current participants only (excludes cancelled/rejected). Includes `id`, `kind`, `isGuest`, nullable `userId`. Organizer sees pending (for approval); others see approved/attended/no-show only. Pending applicants are not roster members until approved. |
| [x] | `AssignEventParticipants` | Command | `POST /api/events/{id}/participants/assign` | Organizer-only. Draft/Published/Full. Body: `guests[{firstName,lastName}]` + `friendUserIds`. Guests occupy capacity as Approved. Friends must be accepted friends and are added as Approved. |
| [x] | `RemoveAssignedParticipant` | Command | `DELETE /api/events/{id}/participants/{participantId}` | Organizer-only. Cancels a guest or assigned/applied participant (not the organizer) and frees capacity. |

### Waitlist

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `ListWaitlist` | Query | `GET /api/events/{id}/waitlist` | Organizer only. |
| [x] | `PromoteFromWaitlist` | Command | `POST /api/events/{id}/waitlist/{userId}/promote` | Approved participant + conversation member + notification. |

### Attendance (after Complete)

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `ConfirmAttendance` | Command | `POST /api/events/{id}/participants/{userId}/attended` | `EventsCompleted`++; refreshes attendance rate. |
| [x] | `MarkNoShow` | Command | `POST /api/events/{id}/participants/{userId}/no-show` | Refreshes attendance rate. |

---

## Application orchestration

- Mutating commands load the **Event aggregate** with participants + waitlist via `EventAccess.LoadAggregateAsync`.
- `INotificationPublisher` (in-app only for v1) is used for approve / reject / cancel / promote. Push/email stay deferred.
- Conversation helpers live in `EventAccess` (ensure on publish, add/remove member, close on cancel/complete).

---

## Exit criteria

- [x] Full Draft → Publish → Apply → Approve → Complete → Attendance path works (domain + handlers wired)
- [x] Capacity / Full / waitlist behaviors match Domain
- [x] Event conversation exists after publish
- [x] Discovery query paginated and filtered
