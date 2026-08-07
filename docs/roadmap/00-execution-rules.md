# 00 — Execution rules (nasıl çalışacağız)

Her playbook uygulamasında bu kurallar geçerli.

---

## 1. Başlama protokolü

1. İlgili `0X-….md` dosyasını baştan sona oku.
2. “Karar kapısı” bölümünde cevap bekleyen maddeler varsa **önce konuş**, sonra kod.
3. Onay: “`0X` ile başla” / “devam”.
4. Uygulama sırasında playbook dışına taşma; scope creep olursa yeni madde olarak roadmap’e yaz.

## 2. Slice disiplini (kod)

Her teknik iş için:

1. Domain invariant var mı? Yoksa önce Domain.
2. Application: Command/Query + Validator + Handler + Result errors.
3. API: thin controller, MediatR only.
4. Tests: happy + kritik fail.
5. Docs: ilgili `docs/features/*` checkbox / bu playbook exit criteria.

Asla: controller’da iş kuralı, generic repository, feature’a özel `SaveChanges` kaçırma (UoW handler’da).

## 3. Mimari sabitler (değiştirme)

- Clean Architecture yönü: API → Application → Domain; Infrastructure → Application.
- CQRS + MediatR, FluentValidation, Result, Mapster (Application).
- Persistence: `IApplicationDbContext`, convention-first EF.
- UUID PK, `DateTimeOffset`, no soft delete.
- Messaging v1: Event conversation; SignalR domain’i yeniden yazmaz.

## 4. Migration kuralları

- Mevcut DB varsa: **Rename / Alter**; Drop+Create veri siler → review zorunlu.
- `dotnet ef migrations add` çıktısını oku; data-loss uyarısı varsa elle düzelt.
- Apply:  
  `dotnet ef database update --project src/Infrastructure --startup-project src/API`

## 5. Config / secret kuralları

- Tracked `appsettings.*.json` içine yeni secret koyma.
- Local: user-secrets. Prod/CI: env (`ConnectionStrings__SupabaseConnection`, …).
- Commit edilmiş secret değiştiyse **rotate** şart (history silinmez).

## 6. Definition of Done (faz)

Bir playbook bitmiş sayılır ancak:

- [ ] Exit criteria listedeki maddeler tamam
- [ ] `dotnet build` + ilgili testler yeşil
- [ ] `docs/features` / `docs/roadmap/status.md` güncellendi
- [ ] Bilinçli defer’ler playbook’ta işaretli

## 7. Konuşmadan ilerlemeyeceğimiz konular

Aşağıdakiler koddan **önce** kısa karar ister:

- Job host seçimi (Hangfire vs Quartz vs worker process)
- Admin/Moderator’ın kalıcı modelı (allow-list vs role claim)
- Badge eşik sayıları
- Moderation yan etki matrisi (hide/suspend/ban kim, ne zaman)
- SMS provider seçimi
- SignalR auth modeli (JWT query string vs header)

## 8. Dosya dokunma sınırı

Playbook’ta “Dokunulacak alanlar” listesi dışına çıkılacaksa önce söyle.  
Özellikle: `appsettings` secret temizliği, RLS SQL, client repo — ayrı onay.
