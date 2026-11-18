# Organization Members

## Module

Organizations

## Entity

OrganizationMember

---

# Purpose

Stores membership of a user in an organization, including role and approval state.

---

# Columns

| Column             | Type        | Nullable | Description                |
| ------------------ | ----------- | -------- | -------------------------- |
| id                 | UUID        | No       | Primary key                |
| organization_id    | UUID        | No       | Parent organization        |
| user_id            | UUID        | No       | Member                     |
| role               | SMALLINT    | No       | Founder=0, Admin=1, Member=2 |
| status             | SMALLINT    | No       | Pending=0, Approved=1, Rejected=2, Left=3, Blocked=4 |
| responded_at       | TIMESTAMPTZ | Yes      | Approve / reject / leave   |
| created_at         | TIMESTAMPTZ | No       | Request or grant date      |
| updated_at         | TIMESTAMPTZ | Yes      | Last update                |
| created_by_user_id | UUID        | Yes      | Audit                      |
| updated_by_user_id | UUID        | Yes      | Audit                      |

---

# Indexes

- `PK(id)`
- `UNIQUE(organization_id, user_id)`
- `INDEX(user_id)`
- `INDEX(organization_id, status)`

---

# Foreign Keys

| Column          | References        | Delete Behavior |
| --------------- | ----------------- | --------------- |
| organization_id | Organizations(Id) | Restrict        |
| user_id         | Users(Id)         | Restrict        |

---

# Business Rules

- Join via invite code, or via the public join endpoint of a public club, creates an Approved + Member row directly (no approval step). Previously Left or Rejected users rejoin the same way; blocked users cannot.
- Founder is created as Approved + Founder.
- Founder and admins approve or reject pending requests. Pending rows are legacy: no current join path creates them, so these endpoints only matter for rows that already exist.
- Only the founder may grant or revoke Admin.
- Founder cannot leave. They must first transfer the founder role or delete the club. Other approved members may leave (status Left).
- The founder may transfer the founder role to any other approved member: the target becomes Founder and the previous founder becomes Admin (see 36-organizations.md, "Founder transfer").
- Founder and admins may remove an approved member (status Left). The person may rejoin with the invite code, or via the public join endpoint if the club is public.
- Founder and admins may block a member. Blocked users cannot rejoin until unblocked.
- Admins cannot remove, block, or change another admin. Nobody can target the founder.
- Unblocking sets status Left so the person can rejoin (invite code, or public join if the club is public).
- Rejected or Left members rejoin on the same row and become Approved directly. Blocked members cannot.
- Who may create organization events depends on `organizations.event_creation_policy`: AllMembers (0) lets any approved member create them; ManagersOnly (1) limits it to the approved founder and admins.
- Founder and admins co-manage organization events with the creator, including events they did not create: edit, publish, cancel, complete, approve/reject and assign/remove participants, waitlist, attendance and event albums (see `09-events.md`, Club co-management). A deleted club grants no such rights.
- Only Approved members may view or apply to members-only club events (`events.is_organization_only = true`). Public club events are open to everyone.
