# 02 — DM depth

**Amaç:** V1 Direct/Group mesajlaşmayı ürün kalitesine taşımak (receipt, mute, search, realtime polish).  
**Bağımlılık:** V1 Messaging (`docs/features/04-messaging.md`), SignalR hub (`ConversationHub`).  
**Durum:** Done (2026-08-14).

---

## V1 baseline

Zaten var:

- Direct (accepted friends, idempotent)
- Group (max 50, invite/leave)
- Text/media messages, edit/redact
- Event conversation
- SignalR group `conversation:{id}`

V2 transport’u yeniden yazmaz.

---

## V2 kapsamı

| Madde | Açıklama |
| ----- | -------- |
| Read receipts | Üye bazlı `LastReadMessageId` / `LastReadAt` |
| Typing indicator | SignalR event (persist yok) |
| Mute conversation | Üye bazlı mute; push/inbox sessiz |
| Conversation search | Kendi konuşmalarında title/peer arama |
| Message search | Konuşma içinde text search (ILIKE / trigram sonra) |
| Unread badge | List conversations’ta unread count |

### Bilinçli defer

- Disappearing messages
- Voice / location message types (schema reserved olsa bile)
- E2E encryption
- Message-request “Accept/Decline” inbox UI ayrımı (V2.1 — day-1 open DM + block yeterli)

---

## Karar kapısı

### Kilitli

| Soru | Karar |
| ---- | ----- |
| Kim kime DM açar? | **Stranger DM açık** (owner 2026-08-13): accepted friendship **zorunlu değil** |
| Koruma | Block either-way → 403; banned/deleted → yok; self → yok |
| Idempotent | Aynı iki kullanıcı için tek Direct conversation (V1 aynı) |
| Group create | Hâlâ arkadaşlık ister (stranger sadece **Direct**) |
| Read receipt | ConversationMember üzerinde `LastReadAt` + `LastReadMessageId` |
| Receipt privacy | Herkes görür; ayar yok day-1 |
| Typing | SignalR only; DB yok; timeout client 3s |
| Mute | `ConversationMember.MutedUntil` nullable |
| Unread | `messages.created_at > member.last_read_at` count (cap 99) |
| Search | Auth user’ın member olduğu konuşmalar |
| List ayrımı | `isFriend` flag on conversation list — client “İstekler / Tanımadığın” filtresi yapabilir |
| B1 Group seen-by | **V2.1** — day-1 last-read yeterli |
| B2 Mute → push | **Evet** — mute süresince tüm conversation push kesilir |
| B3 Edit → receipt | Receipt **değişmez** |
| B4 Stranger mesaj limiti | **Yok** — block + report + API rate limit |

**V1 farkı:** `CreateDirectConversation` friendship check kalkar (block check kalır).  
`docs/features/04-messaging.md` buna göre güncellenecek.

---

## Domain / DB plan

**Muhtemel migration** (`ConversationMember`):

| Column | Type | Notes |
| ------ | ---- | ----- |
| `LastReadMessageId` | uuid? | FK Messages optional |
| `LastReadAt` | timestamptz? | |
| `MutedUntil` | timestamptz? | |

Spec: `docs/database/13-conversation-members.md` güncelle.

**Domain methods (taslak):**

- `ConversationMember.MarkRead(messageId, at)`
- `ConversationMember.Mute(until)` / `Unmute()`

---

## Application / API plan

| Use case | Type | Endpoint / hub |
| -------- | ---- | -------------- |
| `MarkConversationRead` | Command | `POST /api/conversations/{id}/read` body: `messageId` |
| `MuteConversation` | Command | `POST /api/conversations/{id}/mute` |
| `UnmuteConversation` | Command | `POST /api/conversations/{id}/unmute` |
| `SearchMyConversations` | Query | `GET /api/conversations/search?q=` |
| `SearchMessages` | Query | `GET /api/conversations/{id}/messages/search?q=` |
| `ListMyConversations` enrich | Query | unreadCount, muted, peer summary |
| Hub `Typing` | SignalR | `typing(conversationId)` → others |

Push: mute aktifken delivery dispatcher skip (B2 kararına göre).

---

## Dokunulacak alanlar

- Domain `ConversationMember`
- `Application/Features/Messaging/*`
- `API/Controllers` + `ConversationHub`
- Notifications worker / outbox filter
- `docs/features/04-messaging.md`, database 13

---

## Uygulama sırası

1. Migration + domain methods.
3. MarkRead + list unread.
4. Mute/Unmute + push filter.
5. Search endpoints.
6. Typing hub event + client contract doc.
7. Tests.

---

## Exit criteria

- [x] Read + unread count doğru (multi-device: last write wins)
- [x] Mute push’u keser (karar B2)
- [x] Search yetkisiz conversation’da 403
- [x] Typing persist etmez
- [x] features/04 + db/13 güncellendi
- [x] status.md → 02 Done

## Sonraki

→ [03-recommendation-engine.md](03-recommendation-engine.md)
