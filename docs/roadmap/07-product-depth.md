# 07 — Ürün derinliği (badge / moderation / messaging)

**Amaç:** MVP üstü ürün kuralları.  
**Kritik:** Eşik ve yan etki matrisi **koddan önce** netleşir.

---

## 7.1 İleri badge kuralları

### Karar kapısı (zorunlu)

| Code | Önerilen kural (onayla) | Tetik |
| ---- | ----------------------- | ----- |
| `SPORTS_EXPLORER` | ≥ 3 distinct `UserSports` **veya** attended event’te ≥ 3 distinct sport | UserSport add / ConfirmAttendance |
| `EVENT_MASTER` | ≥ 10 `ParticipantStatus.Attended` | ConfirmAttendance |
| `MARATHON_RUNNER` | 4 hafta üst üste ≥1 attended / hafta | Job sweep (04) |
| `COMMUNITY_HELPER` | ≥ 5 resolved report where reporter = user **veya** ≥ 20 comments | Moderation resolve / CreateComment |

### Nasıl

1. Kararları tabloya kilitle (bu dosyayı güncelle).
2. `IBadgeAwarder.TryAwardAsync` çağrıları hook veya job’da.
3. Idempotent kalır.
4. Seed’de badge satırları zaten var.

### Exit

- [ ] 4 kuralın eşiği yazılı ve kodda aynı
- [ ] Test: eşik altı ödül yok; eşikte bir kez

---

## 7.2 Moderation yan etkileri

### Karar kapısı — aksiyon matrisi

| Entity | Create report | Resolve | Reject |
| ------ | ------------- | ------- | ------ |
| Review | `MarkAsReported` (var) | flagged kalır | `ClearReportedStatus` (var) |
| Post | ? hide from feed | ? | ? |
| Comment | ? | ? | ? |
| User | ? | ? Suspend | ? |
| Message | ? | ? | ? |
| Event | ? | ? | ? |

**Öneri (konuşulacak):** Resolve → hedefi “hidden” flag; Suspend sadece User + ayrı admin onayı.

### Nasıl (karar sonrası)

1. Domain’e minimum flag method’ları (`Hide`, `Suspend` — spec’e uygun).
2. Resolve/Reject handler’da `ApplyReviewSideEffects` benzeri switch.
3. Feed/query’ler hidden’ı filtreler.
4. Asla otomatik Ban (Banned status) — ayrı admin command.

### Exit

- [ ] Matris doldu ve kodlandı
- [ ] features/09 update

---

## 7.3 Messaging genişleme (Direct / Group)

### Karar kapısı

| Soru | Varsayılan |
| ---- | ---------- |
| Direct day-1 bu fazda mı? | Ürün isterse evet; teknik olarak Event’ten sonra |
| Group max members | 50? |

### Nasıl

1. Domain: `Conversation` factory Direct/Group (schema reserved — method ekle).
2. Commands: `CreateDirectConversation`, `CreateGroupConversation`, `InviteMember`.
3. REST controllers; SignalR group aynı pattern.
4. `MessageType.Location` ayrı mini-slice (factory + validation).

### Exit

- [ ] Direct create + mesajlaş
- [ ] features/04 deferred kutuları update

---

## 7.4 Admin: Badges & ReportReasons (opsiyonel)

02’deki Admin policy üzerine:

- Badge activate/deactivate/reorder
- ReportReason activate/deactivate

Seed hâlâ source of truth olabilir; admin override.

---

## Exit criteria (07 tamam)

- [ ] Konuşulan alt-başlıklar (7.1 / 7.2 / 7.3) seçilip bitti
- [ ] status.md

## Sonraki

→ [08-scale-and-platform.md](08-scale-and-platform.md)
