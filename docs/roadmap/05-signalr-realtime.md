# 05 — SignalR realtime

**Amaç:** Event chat’te REST yazımından sonra anlık mesaj iletimi.  
**Kritik kural:** Domain / iş kuralları REST handler’da kalır; hub sadece push + ephemeral sinyal.

Kaynak: [04-messaging](../features/04-messaging.md) · [10-cross-cutting](../features/10-cross-cutting.md)

**Durum:** Done (2026-08)

---

## Karar kapısı

| # | Soru | Varsayılan |
| - | ---- | ---------- |
| 1 | Scope day-1 | **Sadece event conversation message push** |
| 2 | Typing / presence day-1? | **Hayır** — ayrı mini-faz |
| 3 | JWT SignalR’a nasıl? | Access token query `access_token` (mobil client) + auth middleware |
| 4 | Scale-out | Tek instance şimdilik; Redis backplane sonra |

---

## 5.1 Hub iskeleti

### Ne

`ConversationHub` — group key: `conversation:{id}` (Direct/Group için de aynı transport).

### Nasıl

1. `MapHub<ConversationHub>("/hubs/event-chat")`
2. `JoinConversation(conversationId)`: aktif `ConversationMembers` kontrolü
3. `Groups.AddToGroupAsync(Context.ConnectionId, groupName)`
4. Yetkisiz → `HubException`

### Dokunulacak

- `src/API/Hubs/ConversationHub.cs`
- `Program.cs` — `AddSignalR` + MapHub
- Auth: JWT bearer events `OnMessageReceived` query token

### Exit

- [x] Member join oluyor; non-member reject

---

## 5.2 REST → hub publish

### Ne

`SendTextMessage` / `SendMediaMessage` başarılı `SaveChanges` sonrası gruba event.

### Nasıl

1. Application abstraction: `IChatRealtimeNotifier` (+ `NullChatRealtimeNotifier` workers/tests)
2. API: `SignalRChatRealtimeNotifier` — **fail olursa HTTP 500 yapma**; log + best-effort
3. Payload: mevcut `MessageResponse`

### Yapma

- Hub içinde DbContext ile mesaj create etmek (çift yol yasak).

### Exit

- [x] REST SaveChanges sonrası `MessageCreated` push
- [x] REST contract değişmedi

---

## 5.3 Edit / Redact push

### Ne

`MessageEdited` / `MessageRedacted` event’leri aynı group’a.

### Exit

- [x] Edit/redact handler’ları notifier çağırıyor

---

## 5.4 (Sonra) Typing / presence

Ayrı checklist — day-1 değil:

- Typing: ephemeral, DB yok
- Presence: `LastSeenAt` throttle update

---

## Client smoke

1. JWT al → `GET /api/events/{eventId}/conversation`
2. Hub: `/hubs/event-chat?access_token={jwt}`
3. Invoke `JoinConversation(conversationId)`
4. Başka client’tan `POST .../messages` → dinle `MessageCreated`

---

## Exit criteria (05 tamam)

- [x] Message created/edited/redacted realtime
- [x] Auth güvenli (JWT + membership)
- [x] features/04 + 10 docs update
- [x] status.md

## Sonraki

→ [06-push-email-delivery.md](06-push-email-delivery.md)
