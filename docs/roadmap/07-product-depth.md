# 07 — Ürün derinliği (badge / moderation / messaging)

**Amaç:** MVP üstü ürün kuralları.  
**Kritik:** Eşik ve yan etki matrisi **koddan önce** netleşir.

**Durum:** 7.1 + 7.2 Done (2026-08). 7.3 Direct/Group deferred. 7.4 Admin badges optional deferred.

---

## 7.1 İleri badge kuralları

### Karar kapısı (kilitli)

| Code | Kural | Tetik |
| ---- | ----- | ----- |
| `SPORTS_EXPLORER` | ≥ 3 distinct `UserSports` **veya** attended event’te ≥ 3 distinct sport | `AddSport` / `ConfirmAttendance` |
| `EVENT_MASTER` | ≥ 10 `ParticipantStatus.Attended` | `ConfirmAttendance` |
| `MARATHON_RUNNER` | 4 ISO hafta üst üste ≥1 attended / hafta | `ConfirmAttendance` + Events.Worker sweep |
| `COMMUNITY_HELPER` | ≥ 5 resolved report (reporter) **veya** ≥ 20 comments | `ResolveReport` / `CreateComment` / `CreateReply` |

Constants: `BadgeThresholds` + `docs/features/08-gamification.md`.

### Exit

- [x] 4 kuralın eşiği yazılı ve kodda aynı
- [x] Test: marathon streak helper (altı / eşik / gap)

---

## 7.2 Moderation yan etkileri

### Karar kapısı — aksiyon matrisi (kilitli)

| Entity | Create report | Resolve | Reject |
| ------ | ------------- | ------- | ------ |
| Review | `MarkAsReported` | flagged kalır | `ClearReportedStatus` |
| Post | — | `Hide` | `Unhide` |
| Comment | — | `Hide` | `Unhide` |
| User | — | — (Suspend ayrı Admin) | — |
| Message | — | `Redact` (tek yön) | — |
| Event | — | — (Cancel ayrı organizer) | — |

**Ban yok** otomatik. Suspend = ayrı admin command (domain method var; endpoint 7.4/sonra).

### Exit

- [x] Matris doldu ve kodlandı (`ApplyTargetSideEffectsAsync`)
- [x] Feed/list hidden filtreler
- [x] features/09 update

---

## 7.3 Messaging genişleme (Direct / Group)

### Karar kapısı (kilitli)

| Soru | Karar |
| ---- | ----- |
| Direct | Evet — accepted friends; idempotent existing DM |
| Group max members | **50** (`Conversation.MaxGroupMembers`) |
| Invite | Group only; owner/moderator + friend of inviter |
| SignalR | Aynı hub / `conversation:{id}` |

### Exit

- [x] Direct create + mesajlaş (mevcut message endpoints)
- [x] Group create / invite / leave
- [x] features/04 update


---

## 7.4 Admin: Badges & ReportReasons (opsiyonel)

Deferred — seed source of truth.

---

## Exit criteria (07 tamam)

- [x] 7.1 + 7.2 + 7.3 bitti
- [x] status.md
- [ ] 7.4 Admin badges/reasons (opsiyonel)

## Sonraki

→ [08-scale-and-platform.md](08-scale-and-platform.md)
