# Album Media

## Module

Social / Media

## Aggregate Root

Album

---

# Purpose

Image items belonging to an album. Stored in Supabase bucket `albums`.

---

# Columns

| Column | Type | Nullable | Description |
| ------ | ---- | -------- | ----------- |
| id | UUID | No | PK |
| album_id | UUID | No | FK albums |
| storage_path | VARCHAR(500) | No | Object path in `albums` bucket |
| file_name | VARCHAR(255) | No | Original file name |
| mime_type | VARCHAR(100) | No | image/jpeg\|png\|webp |
| file_size | BIGINT | No | Bytes |
| width | INT | Yes | |
| height | INT | Yes | |
| display_order | SMALLINT | No | 1-based order within album |
| uploaded_by_user_id | UUID | No | Uploader |
| created_at | TIMESTAMPTZ | No | |
| updated_at | TIMESTAMPTZ | Yes | |
| created_by_user_id | UUID | Yes | Audit |
| updated_by_user_id | UUID | Yes | Audit |

---

# Indexes

- PK(id)
- UNIQUE(album_id, display_order)
- INDEX(album_id)
- INDEX(uploaded_by_user_id)

---

# Foreign Keys

| Column | References | On delete |
| ------ | ---------- | --------- |
| album_id | albums(id) | Cascade |
| uploaded_by_user_id | users(id) | Restrict |
