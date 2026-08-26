# 10 — Cross-cutting

Ongoing concerns that span modules. Not a gate that blocks starting Identity, but must stay consistent as features land.

See also: [IMPLEMENTATION_WORKFLOW.md](../../.cursor/rules/IMPLEMENTATION_WORKFLOW.md) Phases 8–10.

---

## Progress (backlog)

- [x] Authorization policies named and wired (`ActiveUser`, `CanCreateContent`, `Moderator`, `Admin`)
- [x] Cached counter matrix respected in handlers (incl. `DecreaseEventsJoined` on cancel / cancel-event)
- [x] Storage cleanup on deletes (commit-then-best-effort via `StorageCleanup`)
- [x] Background jobs hosts (`Identity.Worker`, `Events.Worker` + Application cleaners)
- [x] SignalR realtime (event chat push; typing/presence later)
- [ ] Security hygiene (secrets out of tracked config) — **owner: appsettings temizleme yok**; RLS apply ayrı

---

## Authorization policies

| Policy | Intent |
| ------ | ------ |
| `Authenticated` + `ActiveUser` | Default `[Authorize]` — DB check `User.CanAuthenticate()` |
| `CanCreateContent` | Blocks suspended/banned from posts/events/messages/reviews/friend requests |
| `Moderator` | Report queue + resolve — config: `Authorization:ModeratorUserIds` |
| `Admin` | Sports mutations — config: `Authorization:AdminUserIds` (Badges/ReportReasons later) |

Map roles/claims when admin model exists. Until then, Moderator/Admin can be a config allow-list of user ids (document clearly; replace before production scale).

---

## Cached counters

Never expose raw `COUNT(*)` on hot feed paths. Update in the same `SaveChangesAsync` as the mutation.

| Location | Fields | Updated by |
| -------- | ------ | ---------- |
| `UserProfiles` | `AverageRating`, `ReviewCount` | Reviews (`ReviewRatingSync`) |
| `UserStatistics` | events_*, attendance_rate, average_rating, total_reviews, friends_count, posts_count, badges_count | Events, Reviews, Social, Badges |
| `Posts` | `LikeCount`, `CommentCount`, `MediaCount` | Likes, Comments, Media |
| `PostComments` | `LikeCount`, `ReplyCount` | Replies (comment likes future) |

Keep counters ≥ 0. Prefer domain helpers already on entities (`Increase*` / `Decrease*`).

Audit checklist: [docs/roadmap/artifacts/counter-matrix.md](../roadmap/artifacts/counter-matrix.md).

Optional later job: reconcile counters from source tables.

---

## File storage cleanup

When deleting posts, media, messages, avatars, sport covers:

1. Remove/update DB rows in the transaction.
2. Delete Supabase Storage objects **after** successful commit via `StorageCleanup.TryDelete*`.
3. Orphan retry remains a Phase 9 job.

Covered today: post delete/remove media, avatar/intro replace-or-clear, sport cover replace-or-clear, chat media redact.

---

## Background jobs (Phase 9)

Hosts: **`Sportner.Identity.Worker`**, **`Sportner.Events.Worker`**, **`Sportner.Notifications.Worker`** (separate deployable processes; Cronos via `Sportner.Workers.Hosting`). Contracts in Application.

| Job | Purpose | Status |
| --- | ------- | ------ |
| Expired session cleanup | Revoked/expired sessions older than retention (~90 days) | Live |
| OTP cleanup | Expired challenges in `IOtpChallengeStore` | Live |
| Event auto-complete | Published/Full → Completed after `eventDate + duration` | Live |
| Event reminders | `EventReminder` 24h + 1h; idempotent via `EventReminderDispatches` | Live |
| Push delivery | `NotificationDeliveryOutbox` → `IPushSender` (`LoggingPushSender` day-1) | Live |
| Badge rule sweeps | `MARATHON_RUNNER` daily via Events.Worker | Live |
| Counter reconciliation | Nightly integrity | Later |
| Email dispatch | Outbox Email channel | Later |
| Storage orphan GC | Unreferenced paths | Later |

---

## Realtime (Phase 8)

| Hub / feature | Notes |
| ------------- | ----- |
| Event chat | **Done** — `ConversationHub` at `/hubs/event-chat`; REST write + push `MessageCreated`/`MessageEdited`/`MessageRedacted` |
| Typing indicator | Ephemeral — later |
| Online presence | Device/`LastSeen` optional — later |
| Notification push | In-app + mobile push via outbox (`LoggingPushSender`; FCM/APNs later) |

REST messaging ([04-messaging.md](04-messaging.md)) remains the write path; SignalR does not rewrite domain rules.
Group key `conversation:{id}` is DM-ready when Direct conversations ship.

---

## Security hygiene

- Move DB/JWT/Supabase secrets to user-secrets / env ([docs/configuration.md](../configuration.md)); rotate anything that was committed.
- Never log OTP, JWT, refresh tokens, passwords.
- Keep Supabase Data API locked down (RLS / revoke / disable).
- `service_role` key only on server.

Tracked `appsettings.*.json` files currently hold local development secrets for convenience.
Before production, move them to user-secrets / env and rotate.
---

## Testing expectations (as modules land)

| Level | Focus |
| ----- | ----- |
| Domain unit | Aggregate invariants (already started) |
| Application unit | Handlers with mocked `IApplicationDbContext` / Testcontainers later |
| API integration | Auth + one vertical slice per module |
| Phase 10 | Performance, security, load |

Each feature DoD in [README.md](README.md) still applies.

---

## Explicit non-goals for early MVP

- Tournament / team / payments modules
- Direct & Group DM
- PostGIS-heavy discovery
- Full admin backoffice UI
