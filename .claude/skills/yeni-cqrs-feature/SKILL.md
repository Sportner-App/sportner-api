---
name: yeni-cqrs-feature
description: Sportner API'de mevcut Features/<Grup>/<Feature>/<Feature>.cs kalıbını izleyerek yeni bir CQRS command veya query, gerekli Error/Dto dosyalarını, resx anahtarlarını ve controller action'ını iskelet olarak üretir. "yeni endpoint ekle", "yeni feature oluştur", "/yeni-cqrs-feature" gibi isteklerde kullan.
---

# Yeni CQRS Feature

Bu proje `AGENTS.md`'nin iddia ettiğinden çok daha ileride: `src/Application/Features/`
altında Identity, Catalog, Events, Messaging, Reviews, Social, Notifications, Gamification,
Moderation, Organizations, Albums, Quests, Explore, Locations, Feedback gruplarının çoğu zaten
implemente edilmiş durumda. Bu skill, o mevcut kalıbı birebir kopyalayarak yeni bir feature
iskeleti çıkarır — kalıbı yeniden icat etmez.

## Adım 0 — Girdileri topla

Kullanıcıdan veya argümanlardan şunları netleştir (eksikse sor):
- **Grup**: `src/Application/Features/<Grup>` (örn. `Events`, `Catalog`, yeni bir grup da olabilir)
- **Feature adı**: PascalCase, fiil+nesne (örn. `CreateEvent`, `ListMyAlbums`)
- **Tür**: Command (state değiştirir) mi, Query (okuma) mi
- **İş kuralı özeti**: ne yapıyor, hangi tablolara dokunuyor

## Adım 1 — Önce oku (atlama)

Sırayla:
1. `.cursor/rules/BACKEND_STANDARDS.mdc` — mimari/kodlama kuralları (en yüksek öncelik)
2. Dokunulacak her tablo için `docs/database/NN-<tablo>.md` — kolonlar, kısıtlar, iş kuralları
3. `docs/features/NN-<grup>.md` (varsa) — bu feature için request/response şekli, validasyon, iş kuralı
4. `src/Application/Features/<Grup>/` altında zaten var olan dosyalar — aynı gruptaki başka bir
   feature'ın tam olarak nasıl yazıldığına bak (en güvenilir referans kod, dokümandan önce gelir)

## Adım 2 — Grup seviyesi dosyalar var mı kontrol et

Her grup klasöründe (feature alt klasörlerinin dışında) genelde şunlar olur — yoksa oluştur:

- `<Grup>Errors.cs` — `internal static class <Grup>Errors` içinde `Error.NotFound/Validation/
  Conflict/Forbidden/Unauthorized(...)` static property'leri, her biri bir
  `ErrorMessagesResource.<Key>` resx anahtarına referans verir.
- `<Grup>Dtos.cs` — response record'ları (örn. `ReviewResponse`).
- Gerekirse `<Grup>Queries.cs` / projeksiyon yardımcıları (birden fazla feature aynı response
  şeklini üretiyorsa ortak `Project(...)` metodu).

Örnek (`src/Application/Features/Reviews/ReviewErrors.cs`):

```csharp
internal static class ReviewErrors
{
    internal static Error NotFound => Error.NotFound(
        "Review.NotFound",
        ErrorMessagesResource.Review_NotFound);
    // ...
}
```

## Adım 3 — Feature dosyasını oluştur

Tek dosya: `src/Application/Features/<Grup>/<FeatureName>/<FeatureName>.cs`. Üç parça aynı
dosyada: Command/Query record, Validator (sadece command'larda), Handler.

**Command şablonu** (bkz. `src/Application/Features/Reviews/CreateReview/CreateReview.cs`):

```csharp
namespace Sportner.Application.Features.<Grup>.<FeatureName>;

public sealed record <FeatureName>Command(/* parametreler */) : ICommand<TResponse>;

public sealed class <FeatureName>CommandValidator : AbstractValidator<<FeatureName>Command>
{
    public <FeatureName>CommandValidator()
    {
        RuleFor(command => command.X).NotEmpty();
    }
}

internal sealed class <FeatureName>CommandHandler : ICommandHandler<<FeatureName>Command, TResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public <FeatureName>CommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<TResponse>> Handle(<FeatureName>Command request, CancellationToken ct)
    {
        if (_currentUser.UserId is not { } userId)
            return Result<TResponse>.Failure(<Grup>Errors.NotAuthenticated);

        // ... iş kuralları, her ihlalde Result<TResponse>.Failure(<Grup>Errors.X) döndür

        await _dbContext.SaveChangesAsync(ct);
        return Result<TResponse>.Success(response);
    }
}
```

**Query şablonu** (bkz. `GetReviewById.cs`): Validator yok, `IQuery<TResponse>` /
`IQueryHandler<TQuery, TResponse>`, genelde `AsNoTracking()` ile okuma, yazma yok.

Önemli kurallar (BACKEND_STANDARDS.mdc'den):
- Handler sınıfı `internal sealed`, sadece ihtiyaç duyduğu abstraction'ları DI ile alır.
- `IApplicationDbContext` dışında repository yok — domain-özel sorgu gerekiyorsa bile önce bunu
  tercih et.
- Soft delete yok, UUID PK'ler `Guid.NewGuid()`, zaman alanları `DateTimeOffset`.
- Mapster (`Common/Mapping/MappingConfig.cs`) basit 1:1 entity→DTO eşlemede kullanılabilir;
  birden fazla tabloyu birleştiren response'larda bu projede manuel LINQ projeksiyonu tercih
  ediliyor (bkz. `ReviewQueries`/`ReviewDtos` örüntüsü) — tutarlılık için aynı yaklaşımı izle.

## Adım 4 — Yerelleştirme (atlarsan derleme patlar)

Her yeni `ErrorMessagesResource.X` veya `ValidationResource.X` anahtarı **üç** dosyada birden
tanımlanmalı, yoksa derleme zamanında kaybolmaz ama çalışma zamanında anahtar adının kendisi
dönebilir (bkz. AGENTS.md'deki `NotificationActor.Format` notu — aynı tuzak buraya da uygulanır):

- `src/Localization/Resources/ErrorMessagesResource.resx` (default)
- `src/Localization/Resources/ErrorMessagesResource.tr.resx`
- `src/Localization/Resources/ErrorMessagesResource.en.resx`

## Adım 5 — Controller action'ı ekle

`src/API/Controllers/<Grup>Controller.cs` yoksa oluştur, varsa yeni action ekle. Kalıp
(`ReviewsController.cs`):

```csharp
[Authorize]
public sealed class <Grup>Controller : ApiControllerBase
{
    [HttpPost]
    public async Task<IActionResult> <Action>(
        [FromBody] <FeatureName>Request request,
        CancellationToken cancellationToken)
    {
        var result = await Sender.Send(new <FeatureName>Command(request.X), cancellationToken);
        return result.ToActionResult(StatusCodes.Status201Created); // create'te 201, diğerlerinde parametresiz
    }

    public sealed record <FeatureName>Request(/* ... */);
}
```

Yetkilendirme gerekiyorsa `[Authorize(Policy = AuthorizationPolicies.X)]` ekle
(`src/API/Authorization/`).

## Adım 6 — Bittiğinde kullanıcıya hatırlat, kendin çalıştırma

- `dotnet build Sportner.slnx` ile derleme kontrolü — **sen çalıştırma, kullanıcıya komutu ver.**
- Yeni/değişen iş kuralı varsa ilgili `docs/database/NN-*.md` ve `docs/features/NN-*.md`
  dosyalarını da güncelle (bu proje için dokümanlar koddan ayrı bir kaynak — senkron kalmazlar).
- Test: `tests/Application.UnitTests` içinde aynı gruptaki mevcut testlere bak, aynı kalıpta bir
  test ekle; happy-path + en az bir Error dönen senaryo.
