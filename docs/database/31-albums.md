# Albums

## Module

Social / Media

## Aggregate Root

Album

---

# Purpose

Photo albums owned by a user profile or an event. Separate from posts — albums do not appear in the social feed automatically.

---

# Columns

| Column | Type | Nullable | Description |
| ------ | ---- | -------- | ----------- |
| id | UUID | No | PK |
| kind | SMALLINT | No | `AlbumKind` Profile=1 / Event=2 |
| owner_user_id | UUID | Yes | Required for Profile kind |
| event_id | UUID | Yes | Required for Event kind |
| title | VARCHAR(150) | No | |
| description | VARCHAR(1000) | Yes | |
| visibility | SMALLINT | No | `AlbumVisibility` |
| cover_media_id | UUID | Yes | Optional cover pointer (no FK day-1) |
| media_count | INT | No | Cached count |
| created_at | TIMESTAMPTZ | No | |
| updated_at | TIMESTAMPTZ | Yes | |
| created_by_user_id | UUID | Yes | Audit |
| updated_by_user_id | UUID | Yes | Audit |

XOR: Profile ⇒ `owner_user_id` set & `event_id` null; Event ⇒ reverse.

Limits (application): 20 albums/profile, 5 albums/event, 50 media/album. Image only day-1.

---

# Indexes

- PK(id)
- INDEX(owner_user_id)
- INDEX(event_id)
- INDEX(kind)
- INDEX(visibility)

---

# Foreign Keys

| Column | References | On delete |
| ------ | ---------- | --------- |
| owner_user_id | users(id) | Restrict |
| event_id | events(id) | Restrict |
