# User Saved Locations

## Module

Identity

## Aggregate Root

User

---

# Purpose

The `user_saved_locations` table stores user-defined favorite locations.

Saved locations allow users to quickly select frequently used places when creating events or searching for nearby activities.

Each location is private and only accessible by its owner.

---

# Responsibilities

- Store user favorite locations
- Support quick event creation
- Improve location selection experience
- Reduce repetitive map interactions

---

# Columns

| Column             | Type         | Nullable | Description                                                    |
| ------------------ | ------------ | -------- | -------------------------------------------------------------- |
| id                 | UUID         | No       | Primary Key                                                    |
| user_id            | UUID         | No       | References users(id)                                           |
| title              | VARCHAR(100) | No       | User-defined location name (e.g. Home, Office, Favorite Court) |
| latitude           | DECIMAL(9,6) | No       | Latitude coordinate                                            |
| longitude          | DECIMAL(9,6) | No       | Longitude coordinate                                           |
| address            | TEXT         | No       | Full formatted address                                         |
| city               | VARCHAR(100) | Yes      | City                                                           |
| district           | VARCHAR(100) | Yes      | District                                                       |
| is_default         | BOOLEAN      | No       | Default selected location                                      |
| created_at         | TIMESTAMPTZ  | No       | Created date                                                   |
| updated_at         | TIMESTAMPTZ  | Yes      | Updated date                                                   |
| created_by_user_id | UUID         | Yes      | Audit                                                          |
| updated_by_user_id | UUID         | Yes      | Audit                                                          |

---

# Indexes

- PK(id)
- INDEX(user_id)
- INDEX(city)

---

# Foreign Keys

| Column  | References |
| ------- | ---------- |
| user_id | users(id)  |

---

# Relationships

## Belongs To

- users

---

# Business Rules

- A user can save multiple locations.
- Each saved location belongs to exactly one user.
- Location names do not need to be unique.
- Only one location can be marked as the default.
- Users can update or remove their saved locations at any time.

---

# Lifecycle

### Create

- User selects a location from the map.
- User chooses to save the location.
- User provides a custom name.
- Location is stored.

### Update

- User can rename the location.
- User can update the default location.
- User can change the saved address.

### Delete

- User can remove any saved location.
- If the default location is removed, no location is marked as default.

---

# Performance Notes

Users are expected to store only a limited number of locations.

A simple index on `user_id` is sufficient for efficient retrieval.

---

# Future Extensions

Possible future additions:

- Place type (Home, Work, Sports Facility, Other)
- Favorite icon
- Color label
- Radius preference
- Last used timestamp

---

# Notes

Saved locations are private.

They are not shared with other users and are only used to improve the user's experience when creating or discovering events.
