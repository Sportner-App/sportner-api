# 01 — Identity

Tables: `Users`, `UserProfiles`, `UserSports`, `UserStatistics`, `UserSessions`, `UserDevices`, `UserSavedLocations`, plus side-effect `NotificationSettings`.

Domain: `src/Domain/Users/*`. Specs: `docs/database/01`–`08`.

Depends on: [00-prerequisites.md](00-prerequisites.md).

---

## Progress

- [x] Auth (username/password + JWT + refresh + logout)
- [x] Devices
- [x] Sessions management
- [x] UserProfile (create / me / public / updates)
- [x] Onboarding completion
- [x] User sports
- [x] Saved locations
- [x] Notification settings seed on user create

---

## Controllers

| Controller | Base route (suggested) |
| ---------- | ---------------------- |
| `AuthController` | `/api/auth` |
| `UserProfilesController` | `/api/user-profiles` |
| `OnboardingController` | `/api/me/onboarding` |
| `UserSportsController` | `/api/me/sports` |
| `DevicesController` | `/api/me/devices` |
| `SessionsController` | `/api/me/sessions` |
| `SavedLocationsController` | `/api/me/saved-locations` |

All except `Register` / `Login` / `Refresh` require `[Authorize]` unless noted.

---

## Features

### Auth

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `Register` | Command | `POST /api/auth/register` | Username + password + firstName → `User.RegisterWithPassword` + `UserProfile` + session. Seeds notification settings. |
| [x] | `Login` | Command | `POST /api/auth/login` | Lookup profile username → verify password hash → JWT + refresh. |
| [x] | `RefreshToken` | Command | `POST /api/auth/refresh` | Validate hash, user `CanAuthenticate`, session active → `RotateRefreshToken` → new access token. |
| [x] | `Logout` | Command | `POST /api/auth/logout` | Revokes the session for the given refresh token (idempotent). |
| [x] | `LogoutAll` | Command | `POST /api/auth/logout-all` | `RevokeAllSessions` for current user. |

`Register` / `Login` / `RefreshToken` return `isOnboardingCompleted` (sport still required for full onboarding).

OTP/SMS removed from V1. Never log password, JWT, or refresh token plaintext.

### Devices

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `RegisterDevice` | Command | `POST /api/me/devices` | `User.RegisterDevice` (upsert by `deviceIdentifier`); loads the aggregate with its devices. |
| [x] | `UpdateDevicePushToken` | Command | `PUT /api/me/devices/{deviceId}/push-token` | `UserDevice.UpdatePushToken`; null body clears the token. |
| [x] | `ListMyDevices` | Query | `GET /api/me/devices` | No-tracking projection; current user only. Exposes `hasPushToken`, never the token itself. |
| [x] | `RemoveDevice` | Command | `DELETE /api/me/devices/{deviceId}` | `User.RemoveDevice` (revokes device sessions, clears push); loads devices + sessions. |

### Sessions

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `ListMySessions` | Query | `GET /api/me/sessions` | Active (non-revoked, non-expired) metadata only; never returns the refresh token hash. |
| [x] | `RevokeSession` | Command | `DELETE /api/me/sessions/{sessionId}` | Scoped to the caller; `UserSession.Revoke` is idempotent. |

### UserProfile

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `CreateProfile` | Command | `POST /api/user-profiles/me` | `UserProfile.Create` + `User.AttachUserProfile`. Once per user → 409. Username stored lowercase so the unique index is effectively case-insensitive. |
| [x] | `GetMyProfile` | Query | `GET /api/user-profiles/me` | Includes sports summary and read-only `UserStatistics`. |
| [x] | `GetPublicProfile` | Query | `GET /api/user-profiles/{userId}` | Anonymous allowed. Private profile → 403, banned/deleted owner → 404. Never exposes phone or birth date. |
| [x] | `GetProfileByUsername` | Query | `GET /api/user-profiles/by-username/{username}` | Same projection and visibility rules. |
| [x] | `UpdateUsername` | Command | `PUT /api/user-profiles/me/username` | 30-day cooldown and uniqueness checked in the handler → 409 instead of a domain exception. |
| [x] | `UpdateDisplayName` | Command | `PUT /api/user-profiles/me/display-name` | |
| [x] | `UpdateBio` | Command | `PUT /api/user-profiles/me/bio` | ≤500. |
| [x] | `UpdateAvatar` | Command | `PUT /api/user-profiles/me/avatar` | Multipart upload to the `avatars` bucket via `IFileStorage`; stores the path. Empty body clears it. |
| [x] | `UpdateIntroVideo` | Command | `PUT /api/user-profiles/me/intro-video` | Same pattern against the `intro-videos` bucket. |
| [x] | `UpdateLocation` | Command | `PUT /api/user-profiles/me/location` | City on profile. |
| [x] | `UpdatePersonalDetails` | Command | `PUT /api/user-profiles/me/personal-details` | Gender code + birth date; 13–120 age range enforced by the validator. |
| [x] | `UpdateVisibility` | Command | `PUT /api/user-profiles/me/visibility` | `IsProfilePublic`. |

`UserStatistics` is **read-only** to clients. Created with `User.Create`; mutated by other modules.

### Onboarding

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `CompleteOnboarding` | Command | `POST /api/me/onboarding/complete` | `User.CompleteOnboarding` stamps `OnboardingCompletedAt`. Requires an existing profile (username + first name) → 409 `Onboarding.ProfileRequired`, and at least one sport with a skill level → 409 `Onboarding.SportRequired`. Idempotent: already-completed users get 204 and keep their original date. |

Onboarding is the second step after login: the client collects profile and sport data through the
existing profile / sports endpoints (including optional `PUT .../avatar` and `PUT .../intro-video`)
and calls this endpoint last to close the flow. Media uploads store the public URL so clients can
render the file without a second lookup.

### User sports

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `ListMySports` | Query | `GET /api/me/sports` | No-tracking join; primary first, then sport display order. |
| [x] | `AddSport` | Command | `POST /api/me/sports` | Sport must exist (404) and be active (400); duplicate → 409. `User.AddSport` keeps the single-primary invariant. Returns the refreshed list. |
| [x] | `ChangeSportSkillLevel` | Command | `PUT /api/me/sports/{sportId}` | Returns the refreshed list. |
| [x] | `SetPrimarySport` | Command | `PUT /api/me/sports/{sportId}/primary` | Clears the previous primary; returns the refreshed list. |
| [x] | `RemoveSport` | Command | `DELETE /api/me/sports/{sportId}` | Returns the refreshed list. |

### Saved locations

| Status | Use case | Type | Endpoint | Domain / notes |
| ------ | -------- | ---- | -------- | -------------- |
| [x] | `ListSavedLocations` | Query | `GET /api/me/saved-locations` | No-tracking; default first, then title. |
| [x] | `AddSavedLocation` | Command | `POST /api/me/saved-locations` | `User.AddSavedLocation`; keeps the single-default invariant. Returns the created location. |
| [x] | `UpdateSavedLocation` | Command | `PUT /api/me/saved-locations/{locationId}` | Rename + coords + address in one call; scoped to the caller. |
| [x] | `SetDefaultSavedLocation` | Command | `PUT /api/me/saved-locations/{locationId}/default` | Clears the previous default; returns the refreshed list. |
| [x] | `RemoveSavedLocation` | Command | `DELETE /api/me/saved-locations/{locationId}` | |

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

- [x] Phone OTP login issues access + refresh
- [x] Refresh rotation works; logout revokes
- [x] UserProfile CRUD for current user
- [x] Onboarding completion is stored and returned on authentication
- [x] Sports and saved locations CRUD
- [x] Devices register/remove
- [x] Default notification settings exist for new users
