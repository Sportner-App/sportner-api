# 10 — Cross-cutting

Ongoing concerns that span modules. Not a gate that blocks starting Identity, but must stay consistent as features land.

See also: [IMPLEMENTATION_WORKFLOW.md](../../.cursor/rules/IMPLEMENTATION_WORKFLOW.md) Phases 8–10.

---

## Progress (backlog)

- [x] Authorization policies named and wired (`ActiveUser`, `CanCreateContent`, `Moderator`)
- [x] Cached counter matrix respected in handlers (incl. `DecreaseEventsJoined` on cancel)
- [x] Storage cleanup on deletes (commit-then-best-effort via `StorageCleanup`)
- [ ] Background jobs host
- [ ] SignalR realtime
- [~] Security hygiene (secrets out of tracked config → user-secrets; **rotate + RLS apply still pending**)

---

## Authorization policies

| Policy | Intent |
| ------ | ------ |
| `Authenticated` + `ActiveUser` | Default `[Authorize]` — DB check `User.CanAuthenticate()` |
| `CanCreateContent` | Blocks suspended/banned from posts/events/messages/reviews/friend requests |
| `Moderator` | Report queue + resolve — config: `Authorization:ModeratorUserIds` |
| `Admin` | Sports/Badges/ReportReasons mutations (not wired yet) |

Map roles/claims when admin model exists. Until then, Moderator/Admin can be a config allow-list of user ids (document clearly; replace before production scale).

---

## Cached counters

Never expose raw `COUNT(*)` on hot feed paths. Update in the same `SaveChangesAsync` as the mutation.

| Location | Fields | Updated by |
| -------- | ------ | ---------- |
| `Profiles` | `AverageRating`, `ReviewCount` | Reviews |
| `UserStatistics` | events_*, attendance_rate, average_rating, total_reviews, friends_count, posts_count, badges_count | Events, Reviews, Social, Badges |
| `Posts` | `LikeCount`, `CommentCount`, `MediaCount` | Likes, Comments, Media |
| `PostComments` | `LikeCount`, `ReplyCount` | Replies (comment likes future) |

Keep counters ≥ 0. Prefer domain helpers already on entities (`Increase*` / `Decrease*`).

Optional later job: reconcile counters from source tables.

---

## File storage cleanup

When deleting posts, media, messages, avatars:

1. Remove/update DB rows in the transaction.
2. Delete Supabase Storage objects **after** successful commit via `StorageCleanup.TryDelete*`.
3. Orphan retry remains a Phase 9 job.

Covered today: post delete/remove media, avatar/intro replace-or-clear, chat media redact.

---

## Background jobs (Phase 9)

| Job | Purpose |
| --- | ------- |
| Expired session cleanup | Revoked/expired sessions older than retention (~90 days) |
| OTP cleanup | Expire unused OTP records |
| Event reminders | `EventReminder` notifications before start |
| Badge rule sweeps | Non-realtime awards (`EVENT_MASTER`, streaks) |
| Counter reconciliation | Nightly integrity |
| Push/email dispatch | From notification outbox |
| Storage orphan GC | Unreferenced paths |

Host choice (Hangfire / Quartz / worker project) is an Infrastructure decision — keep Application contracts free of the host library.

---

## Realtime (Phase 8)

| Hub / feature | Notes |
| ------------- | ----- |
| Event chat | Push new messages after REST write |
| Typing indicator | Ephemeral |
| Online presence | Device/`LastSeen` optional |
| Notification push | In-app badge + mobile push |

REST messaging ([04-messaging.md](04-messaging.md)) ships first; SignalR must not rewrite domain rules.

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
