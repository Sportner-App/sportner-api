# 01 — Identity

Tables: `Users`, `Profiles`, `UserSports`, `UserStatistics`, `UserSessions`, `UserDevices`, `UserSavedLocations`, plus side-effect `NotificationSettings`.

Domain: `src/Domain/Users/*`. Specs: `docs/database/01`–`08`.

Depends on: [00-prerequisites.md](00-prerequisites.md).

---

## Progress

- [ ] Auth (OTP + JWT + refresh + logout)
- [ ] Devices
- [ ] Sessions management
- [ ] Profile (create / me / public / updates)
- [ ] User sports
- [ ] Saved locations
- [ ] Notification settings seed on user create

---

## Controllers

| Controller | Base route (suggested) |
| ---------- | ---------------------- |
| `AuthController` | `/api/auth` |
| `ProfilesController` | `/api/profiles` |
| `UserSportsController` | `/api/me/sports` or `/api/profiles/me/sports` |
| `DevicesController` | `/api/me/devices` |
| `SessionsController` | `/api/me/sessions` |
| `SavedLocationsController` | `/api/me/saved-locations` |

All except `RequestOtp` / `VerifyOtp` / `Refresh` require `[Authorize]` unless noted.

---

## Features

### Auth

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `RequestOtp` | Command | `POST /api/auth/request-otp` | Normalize phone; send OTP via `IOtpService`. Do not create user yet *or* create `PendingVerification` user — pick one approach and keep it consistent (recommend: upsert pending user on request or on verify; prefer create-on-verify to avoid orphan phones). |
| [ ] | `VerifyOtp` | Command | `POST /api/auth/verify-otp` | Verify OTP → `User.Create` if missing → `VerifyPhoneNumber` → `Activate` → issue JWT + refresh → `CreateSession` (+ optional `RegisterDevice`). Seed `NotificationSetting.CreateDefault` for all `NotificationType` values. |
| [ ] | `RefreshToken` | Command | `POST /api/auth/refresh` | Validate hash, user `CanAuthenticate`, session not revoked/expired → rotate refresh (`RotateRefreshToken`) → new access token. |
| [ ] | `Logout` | Command | `POST /api/auth/logout` | `RevokeSession` for current refresh/session. |
| [ ] | `LogoutAll` | Command | `POST /api/auth/logout-all` | `RevokeAllSessions`. |

Never log OTP, JWT, or refresh token plaintext.

### Devices

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `RegisterDevice` | Command | `POST /api/me/devices` | `User.RegisterDevice` (upsert by `deviceIdentifier`). |
| [ ] | `UpdateDevicePushToken` | Command | `PUT /api/me/devices/{id}/push-token` | `UserDevice.UpdatePushToken`. |
| [ ] | `ListMyDevices` | Query | `GET /api/me/devices` | Projection; current user only. |
| [ ] | `RemoveDevice` | Command | `DELETE /api/me/devices/{id}` | `User.RemoveDevice` (revokes related sessions, clears push). |

### Sessions

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `ListMySessions` | Query | `GET /api/me/sessions` | Active/non-revoked metadata only (no token hashes). |
| [ ] | `RevokeSession` | Command | `DELETE /api/me/sessions/{id}` | `User.RevokeSession`. |

### Profile

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `CreateProfile` | Command | `POST /api/profiles/me` | `Profile.Create` + `User.AttachProfile`. Once per user. |
| [ ] | `GetMyProfile` | Query | `GET /api/profiles/me` | Include sports summary / stats as needed. |
| [ ] | `GetPublicProfile` | Query | `GET /api/profiles/{userId}` or by username | Respect `IsProfilePublic` / block rules later. |
| [ ] | `UpdateUsername` | Command | `PUT /api/profiles/me/username` | `Profile.UpdateUsername` — 30-day rule via `UsernameChangedAt`. |
| [ ] | `UpdateDisplayName` | Command | `PUT /api/profiles/me/display-name` | |
| [ ] | `UpdateBio` | Command | `PUT /api/profiles/me/bio` | ≤500. |
| [ ] | `UpdateAvatar` | Command | `PUT /api/profiles/me/avatar` | Upload via `IFileStorage`, store path. |
| [ ] | `UpdateIntroVideo` | Command | `PUT /api/profiles/me/intro-video` | Same storage pattern. |
| [ ] | `UpdateLocation` | Command | `PUT /api/profiles/me/location` | City / coords on profile. |
| [ ] | `UpdatePersonalDetails` | Command | `PUT /api/profiles/me/personal-details` | Birthdate, gender, etc. per spec. |
| [ ] | `UpdateVisibility` | Command | `PUT /api/profiles/me/visibility` | `IsProfilePublic`. |

`UserStatistics` is **read-only** to clients. Created with `User.Create`; mutated by other modules.

### User sports

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `ListMySports` | Query | `GET /api/me/sports` | |
| [ ] | `AddSport` | Command | `POST /api/me/sports` | `User.AddSport`; sport must be active. |
| [ ] | `ChangeSportSkillLevel` | Command | `PUT /api/me/sports/{sportId}` | |
| [ ] | `SetPrimarySport` | Command | `PUT /api/me/sports/{sportId}/primary` | |
| [ ] | `RemoveSport` | Command | `DELETE /api/me/sports/{sportId}` | |

### Saved locations

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [ ] | `ListSavedLocations` | Query | `GET /api/me/saved-locations` | |
| [ ] | `AddSavedLocation` | Command | `POST /api/me/saved-locations` | `User.AddSavedLocation`. |
| [ ] | `UpdateSavedLocation` | Command | `PUT /api/me/saved-locations/{id}` | Rename / coords / address. |
| [ ] | `SetDefaultSavedLocation` | Command | `PUT /api/me/saved-locations/{id}/default` | |
| [ ] | `RemoveSavedLocation` | Command | `DELETE /api/me/saved-locations/{id}` | |

---

## Side effects on first successful verify / activate

1. Ensure `UserStatistics` exists (domain `User.Create` already attaches).
2. Insert default `NotificationSetting` rows for every `NotificationType` (0–12) via `NotificationSetting.CreateDefault`.
3. Optional: register device if client sent device payload on verify.

---

## Authorization notes

- Suspended / Banned / Deleted: `CanAuthenticate` false → refresh and protected endpoints fail.
- Suspended: cannot create content later (`CanCreateContent`); enforce in Events / Social commands.
- Admin ban/suspend endpoints can wait; document as future admin module if needed.

---

## Exit criteria

- [ ] Phone OTP login issues access + refresh
- [ ] Refresh rotation works; logout revokes
- [ ] Profile CRUD for current user
- [ ] Sports and saved locations CRUD
- [ ] Devices register/remove
- [ ] Default notification settings exist for new users
