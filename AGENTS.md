# Sportner API ÔÇö Agent Guide

> This file is intentionally short. The authoritative engineering rules live in
> [`.cursor/rules/`](.cursor/rules) and the per-table specs live in
> [`docs/database/`](docs/database). Read those before implementing anything.
> If anything here ever conflicts with `.cursor/rules/`, the rules win.

## Source of truth (read these first)

- [`.cursor/rules/BACKEND_STANDARDS.mdc`](.cursor/rules/BACKEND_STANDARDS.mdc) ÔÇö architecture & coding standards (highest priority)
- [`.cursor/rules/DATABASE_STANDARDS.md`](.cursor/rules/DATABASE_STANDARDS.md) ÔÇö database & EF Core mapping standards
- [`.cursor/rules/IMPLEMENTATION_WORKFLOW.md`](.cursor/rules/IMPLEMENTATION_WORKFLOW.md) ÔÇö phase/order workflow
- [`docs/database/*.md`](docs/database) ÔÇö one spec per table (columns, defaults, indexes, FKs, business rules)
- [`docs/database/database-reference.md`](docs/database/database-reference.md) & [`database-erd.md`](docs/database/database-erd.md)
- [`docs/features/`](docs/features) ÔÇö CQRS features & controllers roadmap (implement in listed order)

## Fixed architecture (from the rules ÔÇö do not replace)

Clean Architecture ┬À **CQRS + MediatR** ┬À **Mapster** ┬À **FluentValidation** ┬À **Result Pattern** ┬À
Global Exception Middleware ┬À **EF Core convention-first persistence** ┬À PostgreSQL/Supabase ┬À
UUID PKs (`Guid.NewGuid()`) ┬À `DateTimeOffset`/TIMESTAMPTZ ┬À SMALLINT enums ┬À no soft delete ┬À
`IApplicationDbContext` persistence boundary ┬À no separate Unit of Work ┬À no generic repository
(repositories only for justified domain-specific queries).

Layers: `API ÔåÆ Application ÔåÆ Domain` and `Infrastructure ÔåÆ Application ÔåÆ Domain`. Domain has no external deps.

## Current build state (2026-08)

- **Domain layer:** complete for all 26 tables and audited (see [`docs/audits/019-domain-model-audit.md`](docs/audits/019-domain-model-audit.md)).
- **Architecture foundation:** CQRS contracts, typed Result, localized global exception handling, current-user abstraction, auditing interceptor and Serilog are wired.
- **Persistence:** all 26 entities are registered convention-first. Add explicit Fluent API only when a documented invariant cannot be represented by conventions.
- **Migration:** `InitialCreate` is generated and reviewed; apply to the target DB and harden Supabase RLS before feature work if not already done (see [`docs/audits/020-architecture-and-persistence-readiness.md`](docs/audits/020-architecture-and-persistence-readiness.md) and [`docs/features/00-prerequisites.md`](docs/features/00-prerequisites.md)).
- **Application (CQRS features), Controllers:** not started yet. Follow the step-by-step backlog in [`docs/features/README.md`](docs/features/README.md) ÔÇö prerequisites ÔåÆ Identity ÔåÆ Catalog ÔåÆ Events ÔåÆ Messaging ÔåÆ Reviews ÔåÆ Social ÔåÆ Notifications ÔåÆ Gamification ÔåÆ Moderation.

## Run

```bash
dotnet build Sportner.slnx
dotnet run --project src/API/Sportner.API.csproj
dotnet run --project src/Workers/Identity.Worker/Sportner.Identity.Worker.csproj
dotnet run --project src/Workers/Events.Worker/Sportner.Events.Worker.csproj
dotnet run --project src/Workers/Notifications.Worker/Sportner.Notifications.Worker.csproj
```

Workers are purpose-split deployable hosts (Atmaca-style). Shared cron helpers: `Sportner.Workers.Hosting`.  
Job logic stays in Application (`BackgroundJobs:*` config).  
Config shape matches API for every host: `appsettings.json` + `Development` + `Production` (see [`docs/configuration.md`](docs/configuration.md)).

## Veritabanı erişimi (Supabase)

Postgres Supabase'de barınıyor ama Supabase Data API (PostgREST) **kullanılmıyor**;
uygulamaya tek giriş bu API, `postgres` rolüyle Supavisor üzerinden.

29 Eyl 2026'da `public` şeması Data API'ye kapatıldı (migration:
`lock_down_public_schema_from_data_api`):

- `anon` ve `authenticated` rollerindeki tüm tablo/sequence/function yetkileri geri alındı
- Aynı roller için `ALTER DEFAULT PRIVILEGES` revoke edildi — yeni EF Core
  tabloları otomatik grant almaz
- 42 tabloda RLS açıldı, policy yok (`postgres` ve `service_role` BYPASSRLS
  taşıdığı için API etkilenmez)

Yeni tablo eklerken policy yazmaya gerek yok; `rls_enabled_no_policy` advisor
uyarısı bu tasarımın beklenen sonucudur. Data API ileride gerçekten gerekirse
grant + policy birlikte tanımlanmalı.

Şemada kullanılmayan Supabase-native kalıntılar duruyor (`handle_new_user`,
`get_nearby_events`, `auto_join_event_creator`, `update_event_participants_count`).

## Dosya yüklemede içerik doğrulaması

`IFileStorage` üç katman: `ValidatingFileStorage` → `ResizingFileStorage` →
`SupabaseFileStorage` (DI: `Infrastructure/DependencyInjection.cs`).

En dıştaki katman dosyanın ilk baytlarını okuyup gerçek türünü doğrular.
İstemcinin gönderdiği `Content-Type` tek başına güvenilmez: `image/jpeg`
etiketiyle HTML yüklenip herkese açık depodan servis edilmesi saklanmış XSS'e
kapı açıyordu.

Sıra önemli — küçültme katmanı çözemediği içeriği olduğu gibi geçirdiği için
imza kontrolü ondan önce çalışmalı. Yeni bir içerik türüne izin verilecekse
`ValidatingFileStorage.Signatures` tablosuna da eklenmeli, yoksa doğrulama o
türü sessizce atlar.

## Loglama

`RedactQueryStringSecretsEnricher` istek yolundaki `access_token`, `token`,
`api_key` gibi query parametrelerini maskeler. SignalR WebSocket upgrade'i
token'ı query string'de taşımak zorunda (WebSocket API header ekleyemiyor) ve
Serilog yolu ham hedeften okuduğu için JWT'ler düz metin loglanıyordu.

## Güvenlik başlıkları

`SecurityHeadersMiddleware` pipeline'ın başında (hata yanıtları da kapsansın
diye) her yanıta `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`,
`Referrer-Policy: no-referrer` ve `X-Permitted-Cross-Domain-Policies: none`
ekler. CSP `default-src 'none'` — servis JSON döndürüyor, hiçbir kaynak
yüklemiyor. Swagger UI kendi inline script'lerini çalıştırdığı için CSP'den
muaf.

HSTS yalnızca Development dışında (`app.UseHsts()`). `UseForwardedHeaders`
X-Forwarded-Proto'yu çözdüğü için proxy arkasında da isteğin HTTPS olduğu
görülüyor; aksi halde başlık hiç gönderilmezdi.

Not: yüklenen medya Supabase Storage'dan servis ediliyor, bu middleware'den
geçmiyor. Depodaki dosyaların başlıkları Supabase tarafının sorumluluğunda.

## AllowedHosts ve CORS

`appsettings.Production.json > AllowedHosts` artık `*` değil; host header
saldırılarına karşı izin verilen liste:

- `api-production-9004e.up.railway.app` — Railway public domain
- `healthcheck.railway.app` — Railway'in `/health/live` sağlık kontrolü
- `sportner-api.railway.internal` — private network endpoint
- `localhost`

**Yeni bir alan adı eklerken bu listeyi güncellemek şart**, aksi halde o host
üzerinden gelen her istek 400 döner. Sağlık kontrolü host'u listeden çıkarsa
Railway deploy'u başarısız sayıp geri alır. Acil durumda Railway'den
`AllowedHosts=*` env'i ile geçici olarak eski davranışa dönülebilir.

CORS politikası `AllowCredentials()` kullandığı için `Cors:AllowedOrigins`
içinde `*` bulunması Development dışında başlangıçta hata verir — o kombinasyon
her siteye kimlik bilgisiyle erişim demek olurdu. Doğrulama `AddCors`'un
dışında: policy lambda'sı ilk isteğe kadar çalışmadığından, içine konan bir
hata başlangıçta değil trafik başladıktan sonra patlardı.

Production'da izinli origin listesinin boş olması normaldir: mobil istemci
CORS'a tabi değil.

## Bellek / GC

API projesinde `ServerGarbageCollection=false`. Web SDK'sı bunu varsayılan
olarak açıyor; Server GC konteynerin gördüğü her çekirdek için ayrı heap açıp
belleği tutuyor ve 2 vCPU / 1 GB'lik konteynerde ~330 MB'a çıkıyordu — servisin
ortalama CPU kullanımı 0.004 vCPU iken.

Worker'lar `Microsoft.NET.Sdk.Worker` kullandığı için zaten Workstation GC'de
(83–155 MB), orada değiştirilecek bir şey yok.

Trafik artıp throughput darboğaz olursa Server GC geri açılabilir — önce
Railway metriklerinden CPU'nun gerçekten doyduğunu doğrula.

## Apple ile giriş: ad/soyad yalnızca bir kez gelir

Apple `fullName` alanını **sadece ilk yetkilendirmede** döndürür. Kullanıcı
uygulamaya daha önce Apple ile giriş yaptıysa sonraki denemelerde ad ve soyad
`null` gelir — bu bir hata değil, Apple'ın tasarımı. Doğum tarihi ise hiçbir
zaman gelmez, o yüzden onboarding'de kullanıcıdan istenir.

Test ederken "ilk giriş" durumunu yeniden yaratmak için: iPhone Ayarlar →
Apple Kimliği → Şifre ve Güvenlik → Apple ile Oturum Açma → Sportner →
Apple Kimliğini Kullanmayı Bırak.

Bu yüzden `ExternalAuthQueries.SuggestUsernameAsync` ad/soyad boş geldiğinde
e-postanın yerel bölümüne düşer. Apple "Hide My Email" adresleri
(`@privaterelay.appleid.com`) rastgele yerel bölüm taşıdığı için elenir;
geriye yalnızca o kalırsa `sportner<id>` biçimine düşülür.

## Organizasyon üyelik onayı: altyapı var, akış bağlı değil

`OrganizationMember.CreatePending` hiçbir yerden çağrılmıyor. `JoinOrganization`
davet koduyla gelen herkesi `CreateApproved` ile doğrudan üye yapıyor, `Rejoin`
de `Approved` durumuna geçiriyor — yani üyelik hiçbir zaman `Pending` olmuyor.

Sonuçları:

- `ApproveOrganizationMember` ve `RejectOrganizationMember` uçları pratikte
  tetiklenemiyor: `Approve()` yalnızca `Pending` üyelikte çalışıyor.
- `OrganizationJoinRequested` bildirim türü hiç kullanılmıyor.
  `OrganizationJoinApproved` / `OrganizationJoinRejected` de öyle.
- Yöneticiye giden `OrganizationMemberJoined` bildirimi **doğru** — kişi
  gerçekten katılmış oluyor.

Bu bir hata değil, yarım kalmış bir özellik. Onay akışı istenirse
organizasyona "katılım onay gerektirir" ayarı eklenip `JoinOrganization`'ın o
durumda `CreatePending` kullanması yeterli; geri kalan parçalar hazır.
(29 Eyl 2026'da bilinçli olarak olduğu gibi bırakıldı.)

## Bildirim metinleri ve eksik çeviri anahtarları

Bildirim metinleri resx anahtarlarına kod içinde üretilen isimlerle ulaşıyor
(ör. `$"EventUpdated_Field_{change}"`), yani eksik bir çeviri derlemede
yakalanmıyor.

`NotificationActor.Format` eskiden eksik anahtarda `null` şablonla
`string.Format` çağırıp `ArgumentNullException` atıyordu. Bu istisna çağıran
işlemin içinde patlıyordu: bir çeviri eksiği yüzünden kullanıcı etkinliğini
güncelleyemiyordu. Artık eksik anahtarda anahtarın kendisi, uyuşmayan
argümanlarda ham şablon dönüyor — bildirim ikincil bir iş, ana işlemi
düşürmemeli.

Bu da eksikliği sessizleştirdiği için `NotificationResourceTests` boşluğu
kapatıyor: `EventUpdateField` enum'unun her değeri için her dilde etiket
bulunduğunu doğruluyor. Bildirim metinlerine yeni bir dinamik anahtar ailesi
eklerken oraya da bir kontrol ekle.
