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

- Join via invite code creates Pending + Member.
- Founder is created as Approved + Founder.
- Founder and admins approve or reject pending requests.
- Only the founder may grant or revoke Admin.
- Founder cannot leave. Other approved members may leave (status Left).
- Founder and admins may remove an approved member (status Left). The person may rejoin with the invite code.
- Founder and admins may block a member. Blocked users cannot rejoin until unblocked.
- Admins cannot remove, block, or change another admin. Nobody can target the founder.
- Unblocking sets status Left so the person can request again with the invite code.
- Rejected or Left members may request again on the same row. Blocked members cannot.
- Any approved member may create organization events.
- Founder and admins may cancel organization events they did not create.
- Only Approved members may view org events or apply to them.
