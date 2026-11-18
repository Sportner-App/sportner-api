# Organizations

## Module

Organizations

## Aggregate Root

Organization

---

# Purpose

Stores clubs (university teams, venue groups, communities). The product calls them "clubs"; code and tables keep the `Organization` name.

A club is either public or private. All clubs (public and private) are listed and their limited detail and public events are visible to non-members. Public clubs can be joined without a code; private clubs restrict only membership entry and are joined only with the unique 8-character invite code.

A club event is either members-only (`events.is_organization_only`) or open; open club events appear in normal discovery.

---

# Columns

| Column             | Type         | Nullable | Description                          |
| ------------------ | ------------ | -------- | ------------------------------------ |
| id                 | UUID         | No       | Primary key                          |
| founder_user_id    | UUID         | No       | Creating user; cannot leave          |
| name               | VARCHAR(80)  | No       | Display name                         |
| description        | TEXT         | Yes      | Optional, max 1000                   |
| city_id            | UUID         | Yes      | References `Cities(Id)`              |
| invite_code        | VARCHAR(8)   | No       | Unique, case-insensitive             |
| is_public          | BOOLEAN      | No       | Default false; all clubs are listed; public clubs are joinable without a code, private ones only via invite code |
| image_url          | VARCHAR(2048)| Yes      | Club photo (`club-images` bucket public URL) |
| event_creation_policy | SMALLINT  | No       | Default 0. `0` = AllMembers (every approved member may create club events), `1` = ManagersOnly (founder/admins only) |
| deleted_at         | TIMESTAMPTZ  | Yes      | Soft delete marker; set when the founder deletes the club. A global EF query filter (`deleted_at IS NULL`) hides deleted clubs |
| created_at         | TIMESTAMPTZ  | No       | Creation timestamp                   |
| updated_at         | TIMESTAMPTZ  | Yes      | Last update                          |
| created_by_user_id | UUID         | Yes      | Audit                                |
| updated_by_user_id | UUID         | Yes      | Audit                                |

---

# Indexes

- `PK(id)`
- `INDEX(founder_user_id)`
- `INDEX(city_id)`
- `INDEX(is_public)`
- `UNIQUE(invite_code)`

---

# Foreign Keys

| Column          | References  | Delete Behavior |
| --------------- | ----------- | --------------- |
| founder_user_id | Users(Id)   | Restrict        |
| city_id         | Cities(Id)  | Restrict        |

---

# Business Rules

- Invite code is 8 characters from `23456789ABCDEFGHJKMNPQRSTUVWXYZ`.
- The founder and admins may rotate the invite code (`POST /api/organizations/{id}/invite-code/rotate`). Deleted clubs keep their invite code reserved (unique index), so allocation ignores the delete filter.
- Founder and admins may update name, description, city and visibility (`is_public`).
- All clubs, including private ones, are listed (`GET /api/organizations`; the `is_public` flag is in the response), searchable by name and city, ordered by approved member count; non-members see a limited detail (no invite code, no pending/blocked counts).
- Joining a public club needs no code (`POST /api/organizations/{id}/join`); private clubs reject it. Joining via invite code works for both.
- Only the founder or an admin may upload or remove the club photo (`PUT /api/organizations/{id}/image`).
- City, when set, must exist in the catalog.
- Members-only club events (`is_organization_only = true`) never appear on public / friends discovery; open club events do.
- Non-members of any club (public or private) see only its open events; members-only events (`is_organization_only`) stay hidden. A missing club returns not found.

## Event creation policy

`event_creation_policy` decides who may create events for the club: `0` AllMembers (any approved member, default) or `1` ManagersOnly (approved founder/admin). Founder and admins set it through `PATCH /api/organizations/{id}` (`eventCreationPolicy`, optional). The detail response exposes `eventCreationPolicy` and a policy-aware `canCreateEvents`; `GET /api/organizations/mine` items carry `canCreateEvents` too. Creating an event for a club the user may not create events for returns `Organization.CannotCreateEvents`.

## Club events: co-management, past events, count

Club events are the club's: approved Founder/Admin members co-manage them with the creator (see `09-events.md`, Club co-management); a deleted club grants no rights. The detail response (`GET /api/organizations/{id}`) carries `completedEventCount`, the number of club events with status Completed (a plain number, visibility is not filtered). `GET /api/organizations/{id}/events` takes an optional `scope`: `upcoming` (default, unchanged: non-cancelled events from 24 hours ago on, ascending, not paged) or `past` (Completed events and non-draft, non-cancelled events whose start has passed; newest first; paged with `page` (default 1) and `pageSize` (default 20, max 50) and returned as a plain list). The same visibility applies to both: members-only events are hidden from non-members. An unknown scope or out-of-range paging is a validation error.

## Founder transfer

`POST /api/organizations/{id}/transfer-founder` with `{ "userId": "<guid>" }`. Only the approved founder may call it. The target must be an approved member of the same club and not the caller (`Organization.TransferTargetNotApproved`, `Organization.CannotTransferToSelf`). In one save the target becomes Founder, the caller becomes Admin and `founder_user_id` points to the target. The new founder gets an `OrganizationRoleChanged` notification. The founder still cannot leave the club (`Organization.FounderCannotLeave`): transfer the role or delete the club first. `UpdateOrganizationMemberRole` still refuses to assign Founder.

## Deleting a club (soft delete)

`DELETE /api/organizations/{id}` (204) is allowed only for the approved founder (`Organization.NotFounder` otherwise). It sets `deleted_at` and, in the same save, cancels every club event in Draft/Published/Full status (same flow as `CancelEvent`: conversation closed, Pending/Approved participants notified, statistics reversed). Completed and already cancelled events are left as they are. Membership rows and events are not removed (FKs stay `Restrict`), so members-only past events stay visible to former approved members. After deletion every organization command/query returns `Organization.NotFound`; event lists/details show a null club name for the deleted club. Creating an event for a deleted club returns `Organization.NotFound`.
