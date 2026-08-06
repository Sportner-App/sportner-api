# 03 — Events

Tables: `Events`, `EventParticipants`, `EventWaitlist`. Related: event `Conversations` created on publish.

Domain: `src/Domain/Events/*`. Specs: `docs/database/09`–`11`.

Depends on: [01-identity.md](01-identity.md), [02-catalog.md](02-catalog.md).

---

## Progress

- [ ] Organizer create / update / publish / cancel / complete
- [ ] Discovery queries
- [ ] Apply / approve / reject / cancel participation
- [ ] Waitlist join / promote
- [ ] Attendance (Attended / NoShow)
- [ ] Publish → event conversation orchestration

---

## Controllers

| Controller | Base route |
| ---------- | ---------- |
| `EventsController` | `/api/events` |

Nested actions stay on the same controller or thin nested controllers — prefer one `EventsController` with clear routes for v1.

---

## Features

### Organizer lifecycle

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `CreateEvent` | Command | `POST /api/events` | `Event.Create` (Draft); auto organizer participant. Require `CanCreateContent`. |
| [ ] | `UpdateEventDetails` | Command | `PUT /api/events/{id}` | `UpdateDetails` — Draft/Published/Full. |
| [ ] | `UpdateEventSchedule` | Command | `PUT /api/events/{id}/schedule` | Date must be in future when changing; duration > 0. |
| [ ] | `UpdateEventLocation` | Command | `PUT /api/events/{id}/location` | |
| [ ] | `UpdateEventCapacity` | Command | `PUT /api/events/{id}/capacity` | Cannot shrink below occupied count. |
| [ ] | `PublishEvent` | Command | `POST /api/events/{id}/publish` | `Publish` → ensure one `Conversation.CreateEventConversation` + owner member. Idempotent if conversation exists (`U(event_id)`). |
| [ ] | `CancelEvent` | Command | `POST /api/events/{id}/cancel` | Not from Completed. Side effect: notify participants; close conversation (Messaging). |
| [ ] | `CompleteEvent` | Command | `POST /api/events/{id}/complete` | Only after scheduled end; Published/Full. Unlocks attendance + reviews. |
| [ ] | `GetEventById` | Query | `GET /api/events/{id}` | Projection: sport, organizer profile snippet, counts, my participation state. |
| [ ] | `ListMyOrganizedEvents` | Query | `GET /api/events/mine/organized` | Paginated. |
| [ ] | `ListMyParticipatingEvents` | Query | `GET /api/events/mine/participating` | Paginated. |
| [ ] | `DiscoverEvents` | Query | `GET /api/events` | `Published`/`Full`, `event_date > now`, filters (sport, city), paginated. Geo bbox optional later. |

### Participation

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `ApplyToEvent` | Command | `POST /api/events/{id}/apply` | `Event.Apply` → participant or waitlist. Organizer cannot apply. |
| [ ] | `ApproveParticipant` | Command | `POST /api/events/{id}/participants/{userId}/approve` | Organizer only. Sync conversation membership (add member). Notify `EventRequestApproved`. |
| [ ] | `RejectParticipant` | Command | `POST /api/events/{id}/participants/{userId}/reject` | Notify `EventRequestRejected`. |
| [ ] | `CancelParticipation` | Command | `POST /api/events/{id}/participants/me/cancel` | Free capacity; may move status Published↔Full; remove conversation member if present. |
| [ ] | `ListParticipants` | Query | `GET /api/events/{id}/participants` | Organizer sees all; others may see approved only (product rule — document in handler). |

### Waitlist

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `ListWaitlist` | Query | `GET /api/events/{id}/waitlist` | Organizer. |
| [ ] | `PromoteFromWaitlist` | Command | `POST /api/events/{id}/waitlist/{userId}/promote` | `PromoteFromWaitlist`; add conversation member. |

### Attendance (after Complete)

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `ConfirmAttendance` | Command | `POST /api/events/{id}/participants/{userId}/attended` | Organizer. Updates stats counters. |
| [ ] | `MarkNoShow` | Command | `POST /api/events/{id}/participants/{userId}/no-show` | Organizer. Affects attendance rate. |

---

## Application orchestration (critical)

Load **Event aggregate** with participants + waitlist for mutating commands.

On **Publish**:

1. Domain `Publish`.
2. Create event conversation if missing; add organizer as Owner.
3. Persist once via `SaveChangesAsync`.

On **Approve / Promote**:

1. Domain transition.
2. `Conversation.AddMember` for the event conversation.
3. Notification create (respect settings) — can be same UoW or outbox later.

On **Cancel event / remove participant**:

1. Domain transition.
2. Membership leave/remove; optionally `Conversation.Close` when event cancelled/completed.

Update `UserStatistics` event counters in the same transaction where attendance/completion rules require it (see [10-cross-cutting.md](10-cross-cutting.md)).

---

## Exit criteria

- [ ] Full Draft → Publish → Apply → Approve → Complete → Attendance path works
- [ ] Capacity / Full / waitlist behaviors match Domain
- [ ] Event conversation exists after publish
- [ ] Discovery query paginated and filtered
