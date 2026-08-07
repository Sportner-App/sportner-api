# 05 — SignalR realtime

**Amaç:** Event chat’te REST yazımından sonra anlık mesaj iletimi.  
**Kritik kural:** Domain / iş kuralları REST handler’da kalır; hub sadece push + ephemeral sinyal.

Kaynak: [04-messaging](../features/04-messaging.md) · [10-cross-cutting](../features/10-cross-cutting.md)

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

`EventChatHub` (isim serbest) — group key: `conversation:{id}`.

### Nasıl

1. `MapHub<EventChatHub>("/hubs/event-chat")`
2. Connect olunca: kullanıcıyı conversation member mı diye doğrula (`MessagingAccess` benzeri).
3. `Groups.AddToGroupAsync(Context.ConnectionId, groupName)`
4. Yetkisiz → abort.

### Dokunulacak

- `src/API/Hubs/*`
- `Program.cs` — `AddSignalR` + MapHub
- Auth: JWT bearer events `OnMessageReceived` query token

### Exit

- [ ] Member join oluyor; non-member reject

---

## 5.2 REST → hub publish

### Ne

`SendTextMessage` / `SendMediaMessage` başarılı `SaveChanges` sonrası gruba event.

### Nasıl

1. Application abstraction: `IChatRealtimeNotifier.NotifyMessageCreatedAsync(...)`  
   (Application → interface; Infrastructure/API impl hub context).
2. Handler sonunda (SaveChanges sonrası) çağır — **fail olursa HTTP 500 yapma**; log + best-effort (mesaj DB’de durur).
3. Payload: mevcut `MessageResponse` ile aynı shape (client tek model kullansın).

### Yapma

- Hub içinde DbContext ile mesaj create etmek (çift yol yasak).

### Exit

- [ ] İki client: A REST gönderir, B hub’dan alır
- [ ] REST contract değişmedi

---

## 5.3 Edit / Redact push

### Ne

`MessageEdited` / `MessageRedacted` event’leri aynı group’a.

### Nasıl

Aynı notifier; event type discriminator.

### Exit

- [ ] Edit/redact diğer client’ta görünür

---

## 5.4 (Sonra) Typing / presence

Ayrı checklist — day-1 değil:

- Typing: ephemeral, DB yok
- Presence: `LastSeenAt` throttle update

---

## Exit criteria (05 tamam)

- [ ] Message created/edited/redacted realtime
- [ ] Auth güvenli
- [ ] features/04 + 10 docs update
- [ ] status.md

## Sonraki

→ [06-push-email-delivery.md](06-push-email-delivery.md)
