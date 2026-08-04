# User Sessions

## Module

Identity

## Aggregate Root

User

---

# Purpose

The `user_sessions` table manages authenticated user sessions and refresh token lifecycle.

Each successful login creates a new session.

The application supports multiple concurrent sessions across different devices.

This table is the source of truth for refresh token validation and session revocation.

Refresh tokens are never stored in the `users` table.

---

# Responsibilities

- Store refresh token sessions
- Support multiple active devices
- Handle refresh token rotation
- Support session revocation
- Track login metadata
- Improve account security

---

# Columns

| Column             | Type        | Nullable | Description                        |
| ------------------ | ----------- | -------- | ---------------------------------- |
| id                 | UUID        | No       | Primary Key                        |
| user_id            | UUID        | No       | References users(id)               |
| device_id          | UUID        | Yes      | References user_devices(id)        |
| refresh_token_hash | TEXT        | No       | Hashed refresh token               |
| ip_address         | VARCHAR(45) | Yes      | IPv4 / IPv6 address                |
| user_agent         | TEXT        | Yes      | Browser or application information |
| expires_at         | TIMESTAMPTZ | No       | Refresh token expiration date      |
| revoked_at         | TIMESTAMPTZ | Yes      | Session revocation date            |
| created_at         | TIMESTAMPTZ | No       | Session creation date              |
| updated_at         | TIMESTAMPTZ | Yes      | Last update date                   |
| created_by_user_id | UUID        | Yes      | Audit                              |
| updated_by_user_id | UUID        | Yes      | Audit                              |

---

# Indexes

- PK(id)
- INDEX(user_id)
- INDEX(device_id)
- INDEX(expires_at)
- INDEX(revoked_at)

---

# Foreign Keys

| Column    | References       |
| --------- | ---------------- |
| user_id   | users(id)        |
| device_id | user_devices(id) |

---

# Relationships

## Belongs To

- users
- user_devices (optional)

---

# Business Rules

- A user can have multiple active sessions.
- Every login creates a new session.
- Refresh tokens must always be stored as hashes.
- Revoked sessions cannot be reused.
- Expired sessions are considered invalid.
- Refresh token rotation replaces the previous token.
- Logging out revokes only the current session.
- "Logout from all devices" revokes all active sessions.

---

# Security Rules

- Never store plain refresh tokens.
- Never expose refresh tokens in API responses.
- Every refresh request must validate:
  - User status
  - Session status
  - Expiration date
  - Token hash
- Blocked or deleted users cannot refresh sessions.

---

# Lifecycle

### Login

- Create new session
- Generate Refresh Token
- Hash Refresh Token
- Save session

### Refresh Token

- Validate current session
- Rotate refresh token
- Update expiration
- Replace stored hash

### Logout

- Set revoked_at
- Session becomes invalid

### Logout All Devices

- Revoke all active sessions for the user

---

# Performance Notes

This table will grow continuously.

Old expired sessions should be cleaned periodically by a scheduled background job.

Recommended retention period:

- 90 days

---

# Future Extensions

Possible future additions:

- Device fingerprint
- Country
- City
- Last activity timestamp
- MFA status
- Risk score
- Login method

---

# Notes

This table is the authentication session store.

It should never contain business data.

Only refresh token metadata and session information belong here.
