# 11 — Albums (Photo galleries)

Tables: `Albums`, `AlbumMedia`. Specs: `docs/database/31`–`32`.

Depends on: Identity, Events, Storage (`albums` bucket). Not part of social feed.

---

## Progress

- [x] Profile album CRUD + media (image only)
- [x] Event album + participant upload (Approved/Attended)
- [x] Visibility / block checks
- [x] ReportEntityType.Album
- [x] Storage cleanup best-effort on delete
- [ ] Share album as post (V2.1)
- [ ] VIDEO media (V2.1)
- [ ] PHOTO_READY badge hook (optional)

---

## Controllers

| Controller | Routes |
| ---------- | ------ |
| `AlbumsController` | `/api/albums` |
| `UserAlbumsController` | `/api/users/{userId}/albums` |
| `EventsController` | `/api/events/{eventId}/albums` |

---

## Features

| Status | Use case | Endpoint |
| ------ | -------- | -------- |
| [x] | CreateProfileAlbum | `POST /api/albums` |
| [x] | CreateEventAlbum | `POST /api/events/{eventId}/albums` |
| [x] | UpdateAlbum | `PUT /api/albums/{id}` |
| [x] | DeleteAlbum | `DELETE /api/albums/{id}` |
| [x] | AddAlbumMedia | `POST /api/albums/{id}/media` |
| [x] | RemoveAlbumMedia | `DELETE /api/albums/{id}/media/{mediaId}` |
| [x] | ReorderAlbumMedia | `PUT /api/albums/{id}/media/order` |
| [x] | SetAlbumCover | `PUT /api/albums/{id}/cover` |
| [x] | ListMyAlbums | `GET /api/albums/me` |
| [x] | ListUserAlbums | `GET /api/users/{userId}/albums` |
| [x] | ListEventAlbums | `GET /api/events/{eventId}/albums` |
| [x] | GetAlbumById | `GET /api/albums/{id}` |

Limits: 20 profile albums, 5 event albums, 50 media/album. Event default visibility = EventParticipants.

Upload matrix (event): organizer **or** participant `Approved`/`Attended`.
