# Domain Model Audit

Task: 019 — Audit Domain Model Against Database Documentation
Date: 2026-08-05
Mode: Audit only. No production code or documentation was modified.

## Executive Summary

The Domain model under `src/Domain` is in strong overall shape. All 26 documented tables have corresponding Domain types, all 16 enums match `docs/database/database-reference.md` exactly (names, numeric values, `short` underlying type), the Domain project has zero external dependencies, no entity calls `DateTimeOffset.UtcNow`, no public setters exist, and all collections are exposed read-only. The solution builds with 0 errors and 0 warnings.

The audit found **1 Critical**, **3 Major**, and **5 Minor** findings, plus a set of documentation contradictions and persistence-readiness notes.

The highest-priority issue: `src/Domain/Events/Event.cs` still implements `IsRecurring`, `RecurrenceRule`, and `UpdateRecurrence(...)`, while `docs/database/09-events.md` ("Removed Fields") explicitly excludes `is_recurring` and `recurrence_rule` from the schema. These properties cannot be persisted against the documented schema.

## Build Status

- Command: `dotnet build Sportner.slnx`
- Result: **Success**
- Errors: **0**
- Warnings: **0**
- Domain-related warnings: none. All eight projects (Domain, Localization, Application, Infrastructure, API, and the three test projects) compiled cleanly.

## Audit Scope

Compared all code under `src/Domain` against:

- `docs/database/README.md`, `database-erd.md`, `database-reference.md`
- Table documents `01-users.md` through `26-report-reasons.md`
- Project rules in `.cursor/rules/` (BACKEND_STANDARDS, DATABASE_STANDARDS, IMPLEMENTATION_WORKFLOW)

Audited areas: entity existence, inheritance, properties (name/type/nullability/mutability), audit fields, enums, constants, aggregate boundaries, behaviors and invariants, time handling, encapsulation, dependencies, namespaces/folders, documentation consistency, and persistence readiness.

Types audited: `BaseEntity`, `AuditableEntity`, `AggregateRoot`, `DomainException`, 16 enums, and 26 entities across Users, Sports, Events, Messaging, Reviews, Social, Notifications, Badges, and Moderation.

## Critical Findings

### C1 — `Event` contains recurrence fields removed from the documented schema

- Files: `src/Domain/Events/Event.cs`
  - Line 36: `public bool IsRecurring { get; private set; }`
  - Line 38: `public string? RecurrenceRule { get; private set; }`
  - Lines 58, 71, 86–87: `Create(...)` accepts `isRecurring` / `recurrenceRule` and assigns both properties
  - Lines 146–153: `UpdateRecurrence(bool, string?, DateTimeOffset)` behavior
  - Lines 598–618: `ValidateRecurrence(...)` and `NormalizeRecurrenceRule(...)` helpers
- Documentation: `docs/database/09-events.md`, section "# Removed Fields" states that `is_recurring` and `recurrence_rule` "are not part of the first version" and that recurring events must be implemented via a future `event_series` model, never as fields on `events`. The Columns table contains neither column.
- Impact: the entity carries persisted state with no documented columns. Any EF mapping would either fail against the documented schema or silently require undocumented columns — a schema incompatibility. The `UpdateRecurrence` behavior and both validation helpers are also undocumented business behavior.
- Severity rationale: schema incompatibility / impossible persistence mapping against the source-of-truth schema → Critical.

## Major Findings

### MJ1 — Documented constant classes `ReportReasonCodes` and `BadgeCodes` are missing

- Expected location: `src/Domain/Common/Constants/` — the folder exists but is **empty** (no files).
- Documentation: `docs/database/database-reference.md`, sections "# Report Reason Codes" (10 codes: SPAM, HARASSMENT, HATE_SPEECH, INAPPROPRIATE_CONTENT, VIOLENCE, NUDITY, FAKE_INFORMATION, IMPERSONATION, SCAM, OTHER) and "# Badge Codes" (8 codes: FIRST_EVENT, FIRST_POST, FIRST_FRIEND, FIRST_REVIEW, COMMUNITY_HELPER, SPORTS_EXPLORER, EVENT_MASTER, MARATHON_RUNNER). Also `docs/database/26-report-reasons.md` "# Default Report Reasons" and `docs/database/23-badges.md`.
- Current state: `ReportReason.Code` (`src/Domain/Moderation/ReportReason.cs`) and `Badge.Code` (`src/Domain/Badges/Badge.cs`) validate format and normalization but the documented permanent code values exist nowhere in the Domain layer. Seeding and business logic referencing these codes will have no typed source.
- Positive note: no enum was incorrectly used for these codes (they are correctly string-based in both entities).

### MJ2 — `Profile` maintains username-change state with no documented column

- File: `src/Domain/Users/Profile.cs`, line 8: `private DateTimeOffset? _usernameChangedAt;` — set in `Create(...)` (line 64) and in `ChangeUsername(...)` (lines 79–86), where it enforces the documented 30-day rule.
- Documentation: `docs/database/02-profiles.md` Columns table contains **no** `username_changed_at` column, while its Business Rules section requires "Username cannot be changed more than once every 30 days (backend rule)."
- Impact: the field is plain private state, not a mapped property. After rehydration from the database the field will be `null`, so the 30-day invariant is silently unenforced for loaded entities. Either the schema needs a documented column (with EF field mapping) or the rule must move to the Application layer with its own persistence strategy.
- Severity rationale: important invariant that will not actually hold at runtime + documentation mismatch → Major.

### MJ3 — Child-entity factories and mutators are public, allowing aggregate invariants to be bypassed

Only `PostMedia` follows the intended encapsulation pattern (`internal static PostMedia Create(...)`, `src/Domain/Social/PostMedia.cs` line 33, with `internal` mutators). All other child entities expose public factories and public mutation methods that let callers bypass the owning aggregate root:

- `src/Domain/Events/EventParticipant.cs`: public `CreateOrganizer` (line 27), `CreatePending` (line 46), `CreateApproved` (line 64); public `Approve` (line 83), `Reject` (line 101), `Cancel` (line 118), `ConfirmAttendance` (line 136), `MarkNoShow` (line 154). Calling `Approve` directly bypasses `Event` capacity and lifecycle checks.
- `src/Domain/Events/EventWaitlist.cs`: public `Create` (line 18).
- `src/Domain/Messaging/ConversationMember.cs`: public `CreateOwner` (line 23), `CreateMember` (line 31); public `PromoteToModerator` (line 39), `DemoteToMember` (line 57), `Leave` (line 75), `Rejoin` (line 91). Direct calls bypass `Conversation` membership and closed-conversation rules.
- `src/Domain/Users/UserSport.cs`: public `Create` (line 21), `MarkAsPrimary` (line 64), `RemovePrimaryStatus` (line 75). Direct `MarkAsPrimary` bypasses the primary-sport uniqueness invariant that `User` enforces.
- `src/Domain/Users/`: `Profile.Create` (line 40), `UserStatistics.Create` (line 34), `UserDevice.Create` (line 29), `UserSession.Create` (line 26), `UserSavedLocation.Create` (line 28) are all public.

Within-aggregate consistency (uniqueness, capacity, default-location rules) is enforced by the roots, but the compiler does not prevent Application code from mutating children directly. This is an aggregate-boundary weakness and an inconsistency with the pattern already established for `PostMedia` in Task 015.1.

## Minor Findings

### MN1 — Missing no-op guards (unnecessary `UpdatedAt` churn)

Several update methods always call `Touch(utcNow)` even when the value is unchanged, unlike `Post.UpdateContent`, `PostMedia`, `Notification`, `NotificationSetting`, `Badge`, and `ReportReason`, which guard against no-op writes:

- `src/Domain/Users/Profile.cs`: `UpdateDisplayName` (line 90), `UpdateBio` (line 97), `UpdateAvatar` (line 108), `UpdateIntroVideo` (line 114), `UpdateLocation` (line 120), `UpdatePersonalDetails` (line 131), `UpdateVisibility` (line 138)
- `src/Domain/Users/UserDevice.cs`: push-token and device-information update methods
- `src/Domain/Users/UserSavedLocation.cs`: rename/coordinate/address update methods
- `src/Domain/Sports/Sport.cs`: icon/detail update methods
- `src/Domain/Events/Event.cs`: detail/schedule/location update methods

Non-blocking; results in spurious `UpdatedAt` changes only.

### MN2 — `Domain/Badges` folder vs. documentation module name

- Code: `src/Domain/Badges/` with namespace `Sportner.Domain.Badges` (internally consistent between `Badge.cs` and `UserBadge.cs`).
- Documentation: `docs/database/README.md` groups `badges`/`user_badges` under a module named "System" (together with `reports`/`report_reasons`); the audit brief refers to the module as "Gamification". Neither document uses "Badges".
- Classification: **Harmless naming difference.** The namespace is consistent, unambiguous, and closer to the table names than either "System" or "Gamification". No rename performed or required; if the team standardizes on "Gamification" later, it is a mechanical rename.

### MN3 — Leftover empty folder `src/Domain/Feed/`

The folder is empty (its `.gitkeep` was removed when the social feed was implemented under `src/Domain/Social/`). It serves no purpose and should be deleted in a cleanup pass. `Domain/Common/ValueObjects/` is also empty but is an intentional placeholder per the foundation tasks.

### MN4 — Aggregate root name `NotificationSettings` (plural) in documentation

`docs/database/22-notification-settings.md`, section "## Aggregate Root", declares the root as `NotificationSettings`, while the implemented class is `NotificationSetting` (`src/Domain/Notifications/NotificationSetting.cs`). The singular class name is correct for a one-row-per-(user, type) entity; the document heading is the outlier.

### MN5 — `gender` documented as "Gender enum" but no Gender enum is defined

`docs/database/02-profiles.md` Columns table describes `gender SMALLINT` as "Gender enum", but `docs/database/database-reference.md` defines no Gender enum. The implementation uses `short? Gender` (`src/Domain/Users/Profile.cs` line 24), which is the only defensible mapping today. Non-blocking documentation ambiguity; if a Gender enum is intended it must be added to `database-reference.md` first.

## Documentation Contradictions

Reported only — none were resolved in this task.

1. **README Entity Ownership vs. per-table aggregate declarations** — `docs/database/README.md`, section "# Entity Ownership":
   - Lists `Conversation` as a child of the Event aggregate, contradicting `docs/database/12-conversations.md` ("## Aggregate Root: Conversation").
   - Lists `Message` as a child of the Conversation aggregate, contradicting `docs/database/14-messages.md` ("## Aggregate Root: Message", with the explicit domain-boundary note).
   - Lists `PostComment` and `PostLike` as children of the Post aggregate, contradicting `docs/database/19-post-likes.md` and `docs/database/20-post-comments.md`, which both declare independent aggregate roots.
   The implementation follows the newer per-table documents, which is correct; the README section is stale.
2. **Sport aggregate root** — `docs/database/03-sports.md`, "## Aggregate Root" says "None (Lookup Table)", but `Sport` is implemented as an `AggregateRoot` with a full lifecycle (per Task 010 instruction). One of the two must eventually be corrected.
3. **UserBadge aggregate root** — `docs/database/24-user-badges.md`, "## Aggregate Root" says "User", but `UserBadge` is implemented as an independent aggregate root (per Task 016 instruction and the boundary list in this audit's brief). The `User` aggregate contains no badge collection.
4. **User status naming** — `docs/database/README.md`, "## Soft Delete" example lists a status named "Blocked", while `docs/database/database-reference.md`, "# User Status" defines value 3 as "Banned". Code follows the reference (`UserStatus.Banned`).
5. **Recurring event fields** — `docs/database/09-events.md` "# Removed Fields" removes `is_recurring`/`recurrence_rule`; the implementation predates this and still contains them (see C1). This is a code-vs-doc contradiction rather than doc-vs-doc.
6. **NotificationSettings vs NotificationSetting** — see MN4.
7. **Gender enum** — see MN5.
8. **Confirmed non-contradictions** (verified explicitly):
   - `FriendshipStatus` **is present** in `docs/database/database-reference.md` ("# Friendship Status", values Pending=0, Accepted=1, Rejected=2, Blocked=3) and matches `src/Domain/Common/Enums/FriendshipStatus.cs` exactly.
   - `blocked_by_user_id` is documented in `docs/database/16-friendships.md` and implemented as `Friendship.BlockedByUserId`.
   - Capacity definition for Pending participants: `docs/database/09-events.md` "# Capacity Definition" (Pending/Approved/Attended/NoShow occupy capacity; Rejected/Cancelled do not) matches `EventParticipant.OccupiesCapacity()` after Task 011.1.
   - Report and Notification entity-type enums in the reference match the code exactly (6 values each; Report adds Review=4 and Message=5 where Notification has Conversation=4 and Badge=5 — intentional, per-module enums).
   - Delete-behavior wording (Restrict for historical references such as `events.organizer_user_id`, Cascade for owned children) is consistent across the modified table documents; no conflicting Cascade-vs-Restrict statements were found for the same relationship.

## Entity-by-Entity Matrix

| Entity | Document | Exists | Inheritance | Properties | Nullability | Behaviors | Aggregate Boundary | Status |
|---|---|---|---|---|---|---|---|---|
| BaseEntity | README (Audit Fields) | Yes | n/a (abstract) | `Id` Guid | OK | n/a | n/a | Pass |
| AuditableEntity | README (Audit Fields) | Yes | BaseEntity | CreatedAt, UpdatedAt?, CreatedByUserId?, UpdatedByUserId? | OK | n/a | n/a | Pass |
| AggregateRoot | README | Yes | AuditableEntity | none extra | OK | n/a | n/a | Pass |
| DomainException | Standards | Yes | Exception | n/a | n/a | n/a | n/a | Pass |
| User | 01-users.md | Yes | AggregateRoot | Match | Match | Status transitions, phone verification, child management all present | Root of Identity aggregate | Pass |
| Profile | 02-profiles.md | Yes | AuditableEntity | Match + undocumented `_usernameChangedAt` (MJ2) | Match | 30-day rule broken after rehydration (MJ2); no-op guards missing (MN1) | Child of User | Pass with Notes |
| UserStatistics | 05-user-statistics.md | Yes | AuditableEntity | Match | Match | Counter/attendance-rate updates non-negative | Child of User | Pass |
| UserSport | 04-user-sports.md | Yes | AuditableEntity | Match | Match | Skill/primary handling correct; public `MarkAsPrimary` (MJ3) | Child of User | Pass with Notes |
| UserSavedLocation | 08-user-saved-locations.md | Yes | AuditableEntity | Match | Match | Default uniqueness enforced by User; public factory (MJ3) | Child of User | Pass with Notes |
| UserDevice | 07-user-devices.md | Yes | AuditableEntity | Match | Match | Duplicate handling via User; public factory (MJ3); no-op guards (MN1) | Child of User | Pass with Notes |
| UserSession | 06-user-sessions.md | Yes | AuditableEntity | Match | Match | Revocation behavior present | Child of User | Pass with Notes |
| Sport | 03-sports.md | Yes | AggregateRoot | Match | Match | Lifecycle, name/slug/display-order validation present | Doc says "None (Lookup Table)" (Contradiction 2) | Pass with Notes |
| Event | 09-events.md | Yes | AggregateRoot | **`IsRecurring`/`RecurrenceRule` undocumented (C1)** | Otherwise match | Lifecycle, capacity (Pending occupies), waitlist, attendance correct; `UpdateRecurrence` undocumented (C1) | Owns EventParticipant + EventWaitlist | **Fail** |
| EventParticipant | 10-event-participants.md | Yes | AuditableEntity | Match | Match | Status transitions correct; public factories/mutators (MJ3) | Child of Event | Pass with Notes |
| EventWaitlist | 11-event-waitlist.md | Yes | AuditableEntity | Match | Match | Correct; public factory (MJ3) | Child of Event | Pass with Notes |
| Conversation | 12-conversations.md | Yes | AggregateRoot | Match | Match | Owner creation, closed behavior, membership, moderator rules present | Owns ConversationMember only; **no Message collection** | Pass |
| ConversationMember | 13-conversation-members.md | Yes | AuditableEntity | Match | Match | Role/leave/rejoin correct; public factories/mutators (MJ3) | Child of Conversation | Pass with Notes |
| Message | 14-messages.md | Yes | AggregateRoot | Match | Match | Intrinsic validation + redaction present | Independent aggregate root | Pass |
| Review | 15-reviews.md | Yes | AggregateRoot | Match | Match | Self-review prevention, 1–5 range, comment normalization, report flag | Independent aggregate root | Pass |
| Friendship | 16-friendships.md | Yes | AggregateRoot | Match incl. `BlockedByUserId` | Match | Self-request prevention, legal transitions, block direction, `IsBetween`/`InvolvesUser` | Independent aggregate root | Pass |
| Post | 17-posts.md | Yes | AggregateRoot | Match | Match | Content length, media count/order, publishability, non-negative counters | Owns PostMedia only; **no like/comment collections** | Pass |
| PostMedia | 18-post-media.md | Yes | AuditableEntity | Match | Match | Internal factory/mutators, immutable PostId/StoragePath, image/video rules, no-op guards | Child of Post (correctly encapsulated) | Pass |
| PostLike | 19-post-likes.md | Yes | AggregateRoot | Match | Match | Immutable PostId/UserId, no update behavior | Independent aggregate root | Pass |
| PostComment | 20-post-comments.md | Yes | AggregateRoot | Match | Match | Root/reply factory separation, content validation, no reply collection, non-negative counters | Independent aggregate root | Pass |
| Notification | 21-notifications.md | Yes | AggregateRoot | Match | Match | Unread initial state, read/unread transitions, immutable payload | Independent aggregate root | Pass |
| NotificationSetting | 22-notification-settings.md | Yes | AggregateRoot | Match | Match | Default matrix per documented table, per-channel toggles | Independent root; doc name plural (MN4); (user, type) uniqueness is DB/Application concern | Pass with Notes |
| Badge | 23-badges.md | Yes | AggregateRoot | Match | Match | Code normalization + immutability, lifecycle, XP validation | Independent aggregate root | Pass |
| UserBadge | 24-user-badges.md | Yes | AggregateRoot | Match | Match | Immutable ownership, `EarnedAt` not in future, no mutators | Doc declares root "User" (Contradiction 3) | Pass with Notes |
| Report | 25-reports.md | Yes | AggregateRoot | Match | Match | Pending/UnderReview/Resolved/Rejected lifecycle, moderator assignment, terminal-state protection | Independent aggregate root | Pass |
| ReportReason | 26-report-reasons.md | Yes | AggregateRoot | Match | Match | Code normalization + immutability, lifecycle, `IsSelectable` | Independent aggregate root | Pass |

No documented table lacks a Domain type; no undocumented entity exists (the only extra type surface is C1's recurrence members on `Event`).

## Enum and Constant Matrix

| Type | Documentation Match | Numeric/String Values | Underlying Type | Status | Notes |
|---|---|---|---|---|---|
| UserStatus | Yes | 0–4 exact (PendingVerification…Deleted) | short | Pass | README "Blocked" wording is the doc outlier (Contradiction 4) |
| SkillLevel | Yes | 0–4 exact | short | Pass | |
| DevicePlatform | Yes | iOS=0, Android=1 | short | Pass | |
| EventStatus | Yes | 0–4 exact | short | Pass | |
| ParticipantStatus | Yes | 0–5 exact | short | Pass | |
| ConversationType | Yes | 0–2 exact | short | Pass | |
| ConversationMemberRole | Yes | Member=0, Owner=1, Moderator=2 | short | Pass | |
| MessageType | Yes | 0–5 exact | short | Pass | |
| FriendshipStatus | Yes — present in database-reference.md | 0–3 exact | short | Pass | Explicitly verified: **not** missing from the reference |
| NotificationType | Yes | 0–12 exact (13 members) | short | Pass | |
| NotificationEntityType | Yes | 0–5 exact | short | Pass | |
| BadgeCategory | Yes | 0–6 exact | short | Pass | |
| BadgeRarity | Yes | 0–3 exact | short | Pass | |
| MediaType | Yes | Image=0, Video=1 | short | Pass | |
| ReportEntityType | Yes | 0–5 exact | short | Pass | |
| ReportStatus | Yes | 0–3 exact | short | Pass | |
| ReportReasonCodes (constants) | **No — missing from code** | 10 documented codes, none implemented | n/a (should be string constants) | **Fail** | MJ1; `src/Domain/Common/Constants/` is empty |
| BadgeCodes (constants) | **No — missing from code** | 8 documented codes, none implemented | n/a (should be string constants) | **Fail** | MJ1 |

No enum value was reordered, no undocumented member was added, and no enum was accidentally used where a string code is documented.

## Aggregate Boundary Review

Implemented boundaries match the expected model:

- `User` owns `Profile`, `UserStatistics`, `UserSport`, `UserSavedLocation`, `UserDevice`, `UserSession` (private `List<T>` fields, exposed via `IReadOnlyCollection<T>` at `src/Domain/Users/User.cs` lines 30–36).
- `Event` owns `EventParticipant` and `EventWaitlist` (`src/Domain/Events/Event.cs` lines 42–44).
- `Conversation` owns `ConversationMember` only (`src/Domain/Messaging/Conversation.cs` line 25). **No `Message` collection exists** — `Message` is an independent aggregate root referencing `ConversationId`.
- `Post` owns `PostMedia` only (`src/Domain/Social/Post.cs` line 28). **No `PostLike` or `PostComment` collection exists**, and `PostComment` has no child reply collection (reply counts are cached integers).
- Independent aggregate roots as expected: `PostLike`, `PostComment`, `Friendship`, `Review`, `Message`, `Notification`, `NotificationSetting`, `Badge`, `UserBadge`, `Report`, `ReportReason`, `Sport`.
- `PostMedia` lifecycle is fully controlled by `Post` (internal factory and mutators).
- No aggregate holds repository, service, or infrastructure dependencies; cross-aggregate references are `Guid` identifiers only.

Weakness: except for `PostMedia`, child factories and mutators are public (MJ3), so the boundary is enforced by convention rather than by the compiler.

## Persistence Readiness Notes

Advisory only — no EF Core code exists or was created.

- **Private backing collections:** `User`, `Event`, `Conversation`, `Post` use private `List<T>` fields; EF configurations will need `PropertyAccessMode.Field` (or field discovery by convention with matching names).
- **Private parameterless constructors:** present on every entity — compatible with EF materialization.
- **Internal factory (`PostMedia.Create`)**: fine for EF (materialization uses the private constructor), but Infrastructure seeding/tests outside the Domain assembly cannot construct `PostMedia` directly; `InternalsVisibleTo` or aggregate-mediated creation will be needed.
- **`Profile._usernameChangedAt` (MJ2):** either add a documented column with a field mapping, or move the rule to Application; otherwise the invariant is lost on rehydration.
- **Enum-to-SMALLINT:** 16 enums require `smallint` conversions (Npgsql maps `short`-backed enums naturally, but explicit `HasConversion`/column type is advisable).
- **Decimal precision:** `Profile.AverageRating` DECIMAL(3,2); `Event.Latitude`/`Longitude` DECIMAL(9,6); `UserSavedLocation` coordinates; `UserStatistics.AttendanceRate` DECIMAL(5,2) — all need explicit precision configuration.
- **`DateOnly` (`Profile.BirthDate`):** maps to PostgreSQL `date` via Npgsql; needs no converter but should be verified in configuration.
- **Composite unique indexes:** `user_sports(user_id, sport_id)`, `event_participants(event_id, user_id)`, `event_waitlist(event_id, user_id)`, `conversation_members(conversation_id, user_id)`, `post_likes(post_id, user_id)`, `user_badges(user_id, badge_id)`, `notification_settings(user_id, notification_type)`, and the friendship pair-uniqueness rule.
- **Self-referencing FK:** `post_comments.parent_comment_id` → `post_comments(id)` (implemented as `PostComment.ParentCommentId` Guid?, no navigation).
- **Multiple FKs to `users`:** `friendships` (requester, addressee, blocked_by), `reports` (reporter, reviewed_by), `reviews` (reviewer, reviewed user), `notifications` (recipient, actor), `messages` (sender) — each needs explicitly named FK constraints to avoid convention clashes.
- **Polymorphic references without FKs:** `reports.entity_id` and `notifications.entity_id` are plain `Guid`/`Guid?` with entity-type discriminators — configure as scalar columns, no relationships.
- **Cascade vs Restrict:** owned children (participants, waitlist, members, media) documented as cascade with their parent; historical references (`events.organizer_user_id`, `events.sport_id`, review/report references) documented as Restrict.
- **Cached counters:** `Post.LikeCount`/`CommentCount`, `PostComment.LikeCount`/`ReplyCount`, `Profile.AverageRating`/`ReviewCount`, `UserStatistics` counters — will need a concurrency strategy (atomic SQL updates or optimistic concurrency) in the Application/Infrastructure layers.
- **One-to-one relationships:** `users`↔`profiles` and `users`↔`user_statistics` (unique `user_id`), currently modeled as child references inside the `User` aggregate.

## Confirmed Correct Areas

- **Dependencies:** `src/Domain/Sportner.Domain.csproj` has zero package or project references. Every `using` in all 27 `.cs` entity/enum/base files resolves to `System.*` or `Sportner.Domain.*` only. No EF Core, ASP.NET Core, MediatR, FluentValidation, Mapster, Serilog, Supabase, or SignalR references.
- **Time handling:** zero occurrences of `DateTimeOffset.UtcNow`, `DateTime.UtcNow`, or `DateTime.Now` in `src/Domain`. All time-dependent methods take explicit `DateTimeOffset utcNow` parameters. Historical timestamps (`EarnedAt`, `JoinedAt`, `AttendedAt`, `ReviewedAt`, `RespondedAt`, `ReadAt`, `RedactedAt`) are separate properties from `CreatedAt`/`UpdatedAt`.
- **Encapsulation:** zero `{ get; set; }` public setters anywhere in `src/Domain`. All collections exposed as `IReadOnlyCollection<T>`. Immutable identifiers (`PostLike.PostId`/`UserId`, `UserBadge.UserId`/`BadgeId`, `PostMedia.PostId`/`StoragePath`, `Badge.Code`, `ReportReason.Code`) have no mutation methods. No arbitrary state-setting methods exist.
- **Audit fields:** provided solely by `AuditableEntity` (`Id` from `BaseEntity`); no entity redeclares them; setters are `protected` per convention.
- **All 16 enums** match the reference exactly (see matrix).
- **Namespaces:** every namespace matches its folder (`Sportner.Domain.Moderation`, `Sportner.Domain.Notifications`, `Sportner.Domain.Social` for all five social entities, `Sportner.Domain.Badges` internally consistent).
- **Key invariants verified present:** organizer auto-participation and Pending-occupies-capacity in `Event`; event-conversation owner creation and closed-conversation guards in `Conversation`; message redaction; review self-review prevention and 1–5 rating; friendship transition legality and block direction; post media count/order and publishability; notification unread lifecycle; notification-setting default matrix; badge/report-reason code normalization and immutability; report lifecycle with terminal-state protection.

## Recommended Fix Order

1. **C1** — Remove `IsRecurring`, `RecurrenceRule`, `UpdateRecurrence`, `ValidateRecurrence`, and `NormalizeRecurrenceRule` from `src/Domain/Events/Event.cs` (and the factory parameters) to match `09-events.md`. Highest risk: schema incompatibility.
2. **MJ1** — Add `ReportReasonCodes` and `BadgeCodes` constant classes under `src/Domain/Common/Constants/` synchronized with `database-reference.md`.
3. **MJ2** — Decide the persistence strategy for the 30-day username rule (documented column + field mapping, or Application-layer enforcement) and align `02-profiles.md` and `Profile.cs`.
4. **MJ3** — Tighten child-entity factory/mutator visibility to `internal` (matching the `PostMedia` pattern), starting with `EventParticipant` and `UserSport` where invariant bypass is most damaging.
5. **Documentation synchronization** — Fix `README.md` Entity Ownership (Conversation, Message, PostLike, PostComment), `03-sports.md` and `24-user-badges.md` aggregate-root declarations, README "Blocked" vs reference "Banned", `22-notification-settings.md` root name, and the Gender-enum ambiguity in `02-profiles.md`.
6. **Minor cleanup** — Add no-op guards (MN1), delete `src/Domain/Feed/` (MN3), and record a team decision on the Badges/Gamification module name (MN2).
7. **Persistence mapping considerations** — Address the readiness notes when EF configurations are introduced.

## Final Assessment

The Domain layer is architecturally sound and very close to the documented model: dependency isolation, time handling, encapsulation, enum fidelity, and aggregate boundaries are all correct. The model is **not yet persistence-ready** in two places — the stale recurrence fields on `Event` (C1) and the unmapped username-change state on `Profile` (MJ2) — and the documented constant catalog is missing (MJ1). All three are small, well-localized fixes. Once C1 and MJ1–MJ3 are resolved and the stale documentation sections are synchronized, the Domain model can proceed to EF Core configuration with no known blockers.

Overall verdict: **Pass with findings** — 1 Critical, 3 Major, 5 Minor, 0 build issues.
