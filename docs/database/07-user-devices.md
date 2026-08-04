# User Devices

## Module

Identity

## Aggregate Root

User

---

# Purpose

The `user_devices` table stores the devices used by users to access the application.

It enables push notifications, device management, session tracking and future security features such as trusted devices and suspicious login detection.

Each physical device should have its own record.

---

# Responsibilities

- Store user devices
- Store push notification tokens
- Track device activity
- Support trusted device management
- Support multi-device authentication

---

# Columns

| Column             | Type         | Nullable | Description                    |
| ------------------ | ------------ | -------- | ------------------------------ |
| id                 | UUID         | No       | Primary Key                    |
| user_id            | UUID         | No       | References users(id)           |
| platform           | SMALLINT     | No       | Device platform (iOS, Android) |
| device_name        | VARCHAR(100) | Yes      | User-friendly device name      |
| device_identifier  | VARCHAR(255) | No       | Unique device identifier       |
| app_version        | VARCHAR(30)  | Yes      | Installed application version  |
| os_version         | VARCHAR(30)  | Yes      | Operating system version       |
| push_token         | TEXT         | Yes      | Push notification token        |
| last_seen_at       | TIMESTAMPTZ  | Yes      | Last activity date             |
| created_at         | TIMESTAMPTZ  | No       | Created date                   |
| updated_at         | TIMESTAMPTZ  | Yes      | Updated date                   |
| created_by_user_id | UUID         | Yes      | Audit                          |
| updated_by_user_id | UUID         | Yes      | Audit                          |

---

# Indexes

- PK(id)
- INDEX(user_id)
- INDEX(device_identifier)
- INDEX(last_seen_at)

---

# Unique Constraints

- UNIQUE(device_identifier)

---

# Foreign Keys

| Column  | References |
| ------- | ---------- |
| user_id | users(id)  |

---

# Relationships

## Belongs To

- users

## Referenced By

- user_sessions (optional)

---

# Platform Enum

| Value | Name    |
| ----- | ------- |
| 0     | iOS     |
| 1     | Android |

---

# Business Rules

- A user can register multiple devices.
- Each physical device should have only one record.
- Push token may change and should be updated.
- LastSeenAt should be updated whenever the device communicates with the backend.
- Removing a device should revoke all active sessions associated with that device.

---

# Lifecycle

### First Login

- Register device
- Save platform information
- Save application version
- Save operating system version
- Save push notification token

### Subsequent Login

- Update push token if changed
- Update app version
- Update OS version
- Update last_seen_at

### Logout

- Device remains registered.
- Active session is revoked.

### Device Removal

- Delete device record.
- Revoke all related sessions.
- Remove push notification token.

---

# Performance Notes

Users typically own only a few devices.

This table is expected to remain relatively small.

Indexes on `user_id` and `device_identifier` are sufficient.

---

# Future Extensions

Possible future additions:

- Device model
- Manufacturer
- Trusted device flag
- Jailbreak / Root detection
- Last IP address
- Last login location
- Biometric authentication support

---

# Notes

This table represents physical devices, not login sessions.

Authentication sessions are managed separately in the `user_sessions` table.

A single device can have multiple sessions over time, but only one active push notification token.
