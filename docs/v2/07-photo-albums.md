# 07 — Photo albums

**Amaç:** Post feed’den bağımsız fotoğraf albümleri (profil + etkinlik).  
**Durum:** **Done** (2026-08-14).

---

## Shipped

- `Albums` + `AlbumMedia`, bucket `albums`, image-only
- Profile CRUD + Event albums (organizer creates; Approved/Attended upload)
- Visibility + block checks; Report `Album=6`
- Migration `AddAlbumsAndAlbumMedia`

Docs: `docs/database/31-albums.md`, `32-album-media.md`, `docs/features/11-albums.md`.

---

## Exit criteria

- [x] Profile album CRUD + media limits
- [x] Event album + participant upload matrix tested
- [x] Visibility / block
- [x] Storage cleanup best-effort
- [x] DB specs + features doc
- [x] status.md → 07 Done

## V2 kapanış

Planlanan V2 ürün listesi tamam.
