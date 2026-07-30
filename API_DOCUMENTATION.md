# Sportner API — Frontend Entegrasyon Dokümanı

## Genel Bilgiler

- Lokal base URL: `http://localhost:5139`
- Swagger: `http://localhost:5139/swagger`
- Health: `GET /health/live`, `GET /health/ready`
- Content-Type: `application/json`
- Tarihler ISO 8601 formatındadır: `2026-08-15T18:30:00Z`
- UUID alanları string olarak gönderilir.
- Localization: `Accept-Language: tr-TR` veya `en-US` (varsayılan `en-US`, TMS ile aynı)
- Auth login/register rate limit: dakikada 20 istek
- Korumalı endpoint'lerde JWT şu header ile gönderilmelidir:

```http
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
Accept-Language: tr-TR
```

`🔒` işaretli endpoint'ler JWT gerektirir.

Genel hata yanıtı (GlobalExceptionHandler):

```json
{
  "message": "Hata açıklaması"
}
```

Olası HTTP durumları:

- `200 OK`: İşlem başarılı
- `201 Created`: Kayıt oluşturuldu
- `400 Bad Request`: Validasyon veya iş kuralı hatası
- `401 Unauthorized`: Token yok, geçersiz veya süresi dolmuş
- `403 Forbidden`: Kullanıcının işlem yetkisi yok
- `404 Not Found`: Kayıt bulunamadı
- `429 Too Many Requests`: Auth rate limit
- `500 Internal Server Error`: Beklenmeyen hata

---

## 1. Auth

### Kullanıcı Kaydı

`POST /api/Auth/register`

Request:

```json
{
  "email": "user@example.com",
  "password": "Guvenli123!",
  "fullName": "Yagiz Erdenler"
}
```

Kurallar:

- `email` geçerli bir e-posta olmalıdır.
- `password` en az 6 karakter olmalıdır.
- Aynı e-posta daha önce API şifresiyle kaydedilmişse `400` döner.

Response — `200 OK`:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userId": "8f5ca5ee-b8c0-4e3a-bd72-4ea88bc71f83",
  "email": "user@example.com",
  "fullName": "Yagiz Erdenler"
}
```

### Giriş

`POST /api/Auth/login`

Request:

```json
{
  "email": "user@example.com",
  "password": "Guvenli123!"
}
```

Response — `200 OK`:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userId": "8f5ca5ee-b8c0-4e3a-bd72-4ea88bc71f83",
  "email": "user@example.com",
  "fullName": "Yagiz Erdenler"
}
```

Hatalı bilgiler — `400 Bad Request`:

```json
{
  "message": "E-posta veya şifre hatalı"
}
```

### Expo Push Token Güncelleme 🔒

`POST /api/Auth/update-push-token`

Request:

```json
{
  "pushToken": "ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]"
}
```

Response — `200 OK`:

```json
{
  "message": "Push token güncellendi."
}
```

Bu endpoint login sonrasında ve Expo push token değiştiğinde çağrılmalıdır.

---

## 2. Profiles

### Kendi Profilini Getir 🔒

`GET /api/Profiles/me`

Response — `200 OK`:

```json
{
  "userId": "8f5ca5ee-b8c0-4e3a-bd72-4ea88bc71f83",
  "email": "user@example.com",
  "fullName": "Yagiz Erdenler",
  "avatarUrl": "https://example.com/avatar.jpg",
  "bio": "Futbol ve tenis oyuncusu",
  "sports": ["football", "tennis"],
  "introVideoUrl": "https://example.com/intro.mp4",
  "isOnboarded": true,
  "birthDate": "1998-06-12T00:00:00Z",
  "skillLevels": {
    "football": "intermediate",
    "tennis": "beginner"
  },
  "avgRating": 4.7,
  "reviewCount": 12,
  "pushToken": "ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]",
  "updatedAt": "2026-07-27T01:25:00Z"
}
```

Frontend login sonrası bu endpoint'i çağırmalı; `isOnboarded` değeri `false` ise onboarding ekranına yönlendirmelidir.

> `skillLevels` bir JSON nesnesi olarak döner (`JSON.parse` gerekmez). Değer yoksa `null` gelir.

### Kendi Profilini Güncelle 🔒

`PUT /api/Profiles/me`

Tüm alanlar opsiyoneldir. Yalnızca gönderilen alanlar güncellenir.

Request:

```json
{
  "fullName": "Yagiz Erdenler",
  "avatarUrl": "https://example.com/avatar.jpg",
  "bio": "Futbol ve tenis oyuncusu",
  "sports": ["football", "tennis"],
  "introVideoUrl": "https://example.com/intro.mp4",
  "birthDate": "1998-06-12T00:00:00Z",
  "isOnboarded": true,
  "skillLevels": {
    "football": "intermediate",
    "tennis": "beginner"
  }
}
```

`skillLevels` JSON nesnesi olarak gönderilir. Geriye dönük uyumluluk için JSON string (`"{\"football\":\"advanced\"}"`) de kabul edilir.

`birthDate` için `"1999-03-28"` veya `"1999-03-28T00:00:00Z"` kullanılabilir.

Response — `200 OK`:

```json
{
  "userId": "8f5ca5ee-b8c0-4e3a-bd72-4ea88bc71f83",
  "email": "user@example.com",
  "fullName": "Yagiz Erdenler",
  "avatarUrl": "https://example.com/avatar.jpg",
  "bio": "Futbol ve tenis oyuncusu",
  "sports": ["football", "tennis"],
  "introVideoUrl": "https://example.com/intro.mp4",
  "isOnboarded": true,
  "birthDate": "1998-06-12T00:00:00Z",
  "skillLevels": {
    "football": "intermediate",
    "tennis": "beginner"
  },
  "avgRating": 4.7,
  "reviewCount": 12,
  "pushToken": "ExponentPushToken[xxxxxxxxxxxxxxxxxxxxxx]",
  "updatedAt": "2026-07-27T01:25:00Z"
}
```

### Avatar Yukle 🔒

`POST /api/Profiles/me/avatar`

`multipart/form-data` ile gorsel gonderilir. Dosya Supabase Storage `avatars` bucket'ina yuklenir ve `profiles.avatar_url` guncellenir.

Kurallar:

- Form field adi: `file`
- Izin verilen tipler: `image/jpeg`, `image/png`, `image/webp`
- Maksimum boyut: `5 MB`
- Path formati: `{userId}/avatar.{ext}` (upsert = true)

Ornek (Expo / React Native - fetch):

```ts
const formData = new FormData();
formData.append("file", {
  uri: imageUri,          // file://... veya content://...
  name: "avatar.jpg",
  type: "image/jpeg",
} as any);

const response = await fetch(`${API_URL}/api/Profiles/me/avatar`, {
  method: "POST",
  headers: {
    Authorization: `Bearer ${token}`,
    // ONEMLI: Content-Type EKLEME. Boundary otomatik eklenir.
    // Accept / global axios Content-Type: application/json kullanma.
  },
  body: formData,
});
```

Yanlis ornek (415 verir):

```http
Content-Type: application/json
{"file":{"uri":"...","name":"avatar.jpg"}}
```

Dogru curl testi:

```bash
curl 'http://localhost:5139/api/Profiles/me/avatar' \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Accept: application/json" \
  -F "file=@/path/to/avatar.jpg;type=image/jpeg"
```

Response — `200 OK`:

```json
{
  "avatarUrl": "https://unrumlwcfmbvuhqdujmr.supabase.co/storage/v1/object/public/avatars/{userId}/avatar.jpg?t=1720000000"
}
```

Not: `appsettings.json` icinde `Supabase:ServiceRoleKey` degeri doldurulmalidir. Bucket public olmali veya public URL erisilebilir olmali.

### Kullanıcı Profilini Getir 🔒

`GET /api/Profiles/{userId}`

Örnek:

```http
GET /api/Profiles/8f5ca5ee-b8c0-4e3a-bd72-4ea88bc71f83
```

Response — `200 OK`: `GET /api/Profiles/me` ile aynı `UserProfileDto` yapısı.

> Gizlilik: diğer kullanıcı profillerinde `pushToken` her zaman `null` döner. Push token yalnızca `GET /api/Profiles/me` yanıtında gelir.

---

## 3. Events

### Etkinlikleri Listele

`GET /api/Events`

Opsiyonel query parametreleri:

- `timeframe`: `upcoming` (varsayılan) | `past` | `all`
  - `upcoming`: `eventDate >= şimdi`
  - `past`: `eventDate < şimdi` (en yeniden eskiye)
  - `all`: tüm etkinlikler
- `sportType`: Spor türüne göre filtre
- `search`: Başlık, açıklama ve adres içinde arama
- `latitude`: Kullanıcı enlemi
- `longitude`: Kullanıcı boylamı
- `radiusKm`: Opsiyonel maksimum mesafe (km). Yalnızca `latitude` ve
  `longitude` ile kullanılabilir. Gönderilmezse mesafe filtresi uygulanmaz.

`latitude` ve `longitude` birlikte gönderildiğinde sonuçlar varsayılan olarak
yakından uzağa sıralanır. `radiusKm` gönderilmediğinde etkinlik ne kadar uzakta
olursa olsun sonuçlara dahil edilir.

Geçmiş / gelecek ayrımı ayrı bir kolon veya tablo ile yapılmaz. `event_date` ile okuma anında hesaplanır; kullanıcı hiçbir şey yapmadan etkinlik saati geçince otomatik “past” sayılır.

Örnekler:

```http
GET /api/Events?sportType=football&search=halı%20saha&latitude=41.0082&longitude=28.9784&radiusKm=10
GET /api/Events?timeframe=past
GET /api/Events?timeframe=all&sportType=tennis
```

Response — `200 OK`:

```json
[
  {
    "id": "24801432-1ae7-4b2c-9f52-852a883df63f",
    "title": "Akşam Halı Saha",
    "description": "Eksik oyuncu arıyoruz",
    "sportType": "football",
    "eventDate": "2026-08-15T18:30:00Z",
    "maxPlayers": 10,
    "addressText": "Kadıköy, İstanbul",
    "latitude": 40.9909,
    "longitude": 29.0289,
    "participantsCount": 4,
    "createdBy": "8f5ca5ee-b8c0-4e3a-bd72-4ea88bc71f83",
    "organizerName": "Yagiz Erdenler",
    "organizerAvatarUrl": "https://example.com/avatar.jpg",
    "createdAt": "2026-07-27T01:00:00Z"
  }
]
```

### Kullanıcının Geçmiş Etkinlikleri 🔒

`GET /api/Events/me/past`

Giriş yapmış kullanıcının **oluşturduğu** veya **approved katılımcısı olduğu** ve `eventDate < şimdi` olan etkinlikleri döner (en yeniden eskiye).

```http
GET /api/Events/me/past
Authorization: Bearer <JWT_TOKEN>
```

Response — `200 OK`: `EventDto` listesi (yukarıdaki ile aynı yapı).

### Performans (Supabase — opsiyonel index)

Yeni tablo / kolon gerekmez. İstersen şu index’leri ekle:

```sql
CREATE INDEX IF NOT EXISTS idx_events_event_date ON events (event_date);
CREATE INDEX IF NOT EXISTS idx_event_participants_user_status
  ON event_participants (user_id, status);
```

### Etkinlik Detayı

`GET /api/Events/{id}`

Response — `200 OK`:

```json
{
  "id": "24801432-1ae7-4b2c-9f52-852a883df63f",
  "title": "Akşam Halı Saha",
  "description": "Eksik oyuncu arıyoruz",
  "sportType": "football",
  "eventDate": "2026-08-15T18:30:00Z",
  "maxPlayers": 10,
  "addressText": "Kadıköy, İstanbul",
  "latitude": 40.9909,
  "longitude": 29.0289,
  "participantsCount": 4,
  "createdBy": "8f5ca5ee-b8c0-4e3a-bd72-4ea88bc71f83",
  "organizerName": "Yagiz Erdenler",
  "organizerAvatarUrl": "https://example.com/avatar.jpg",
  "createdAt": "2026-07-27T01:00:00Z",
  "approvedParticipantsCount": 4,
  "participants": [
    {
      "userId": "9469c642-6535-481f-82da-d840ee9608b3",
      "fullName": "Ali Veli",
      "avatarUrl": "https://example.com/ali.jpg",
      "skillLevel": "intermediate",
      "status": "approved"
    }
  ]
}
```

### Etkinlik Oluştur 🔒

`POST /api/Events`

Request:

```json
{
  "title": "Akşam Halı Saha",
  "description": "Eksik oyuncu arıyoruz",
  "sportType": "football",
  "eventDate": "2026-08-15T18:30:00Z",
  "maxPlayers": 10,
  "addressText": "Kadıköy, İstanbul",
  "latitude": 40.9909,
  "longitude": 29.0289
}
```

Kurallar:

- `maxPlayers`: `2–100`
- `createdBy`, `createdAt` ve `participantsCount` backend tarafından belirlenir.

Response — `201 Created`:

```json
{
  "id": "24801432-1ae7-4b2c-9f52-852a883df63f",
  "title": "Akşam Halı Saha",
  "description": "Eksik oyuncu arıyoruz",
  "sportType": "football",
  "eventDate": "2026-08-15T18:30:00Z",
  "maxPlayers": 10,
  "addressText": "Kadıköy, İstanbul",
  "latitude": 40.9909,
  "longitude": 29.0289,
  "participantsCount": 0,
  "createdBy": "8f5ca5ee-b8c0-4e3a-bd72-4ea88bc71f83",
  "organizerName": "Yagiz Erdenler",
  "organizerAvatarUrl": "https://example.com/avatar.jpg",
  "createdAt": "2026-07-27T01:00:00Z"
}
```

### Etkinlik Sil 🔒

`DELETE /api/Events/{id}`

Request body yoktur. Yalnızca etkinliği oluşturan kullanıcı (`createdBy`) silebilir.

Örnek:

```http
DELETE /api/Events/24801432-1ae7-4b2c-9f52-852a883df63f
Authorization: Bearer <JWT_TOKEN>
```

Response — `200 OK`:

```json
{
  "message": "Etkinlik silindi."
}
```

Başkasının etkinliğini silmeye çalışırsan `403 Forbidden`, bulunamazsa `404 Not Found` döner. İlişkili katılımcılar, mesajlar ve değerlendirmeler cascade ile silinir.

### Etkinliğe Katılım İsteği Gönder 🔒

`POST /api/Events/{id}/join`

Request body yoktur.

Response — `200 OK`:

```json
{
  "userId": "9469c642-6535-481f-82da-d840ee9608b3",
  "fullName": "Ali Veli",
  "avatarUrl": "https://example.com/ali.jpg",
  "skillLevel": "intermediate",
  "status": "pending"
}
```

Kullanıcı kendi etkinliğine istek gönderemez. Aynı etkinlik için mevcut `pending` veya `approved` kayıt varsa `400` döner. Etkinlik sahibine push bildirimi gönderilir.

### Katılımcıları Listele

`GET /api/Events/{id}/participants`

Response — `200 OK`:

```json
[
  {
    "userId": "9469c642-6535-481f-82da-d840ee9608b3",
    "fullName": "Ali Veli",
    "avatarUrl": "https://example.com/ali.jpg",
    "skillLevel": "intermediate",
    "status": "pending"
  }
]
```

`status`: `pending`, `approved` veya `rejected`.

### Katılım İsteğini Onayla/Reddet 🔒

`PATCH /api/Events/{id}/participants/{userId}`

Bu işlemi yalnızca etkinlik sahibi yapabilir.

Request:

```json
{
  "status": "approved"
}
```

Alternatif:

```json
{
  "status": "rejected"
}
```

Response — `200 OK`:

```json
{
  "userId": "9469c642-6535-481f-82da-d840ee9608b3",
  "fullName": "Ali Veli",
  "avatarUrl": "https://example.com/ali.jpg",
  "skillLevel": "intermediate",
  "status": "approved"
}
```

Onay/red sonucunda katılımcıya push bildirimi gönderilir.

---

## 4. Event Chat

Chat endpoint'lerini yalnızca etkinlik sahibi ve `approved` katılımcılar kullanabilir.

### Mesajları Getir 🔒

`GET /api/Events/{eventId}/messages`

Response — `200 OK`:

```json
[
  {
    "id": "418448cf-c42b-48fa-837d-d25c5f22ecf6",
    "eventId": "24801432-1ae7-4b2c-9f52-852a883df63f",
    "userId": "9469c642-6535-481f-82da-d840ee9608b3",
    "userFullName": "Ali Veli",
    "userAvatarUrl": "https://example.com/ali.jpg",
    "content": "Saat 18:30'da görüşürüz.",
    "createdAt": "2026-08-15T12:00:00Z"
  }
]
```

### Mesaj Gönder 🔒

`POST /api/Events/{eventId}/messages`

Request:

```json
{
  "content": "Saat 18:30'da görüşürüz."
}
```

`content` uzunluğu `1–2000` karakter olmalıdır.

Response — `201 Created`:

```json
{
  "id": "418448cf-c42b-48fa-837d-d25c5f22ecf6",
  "eventId": "24801432-1ae7-4b2c-9f52-852a883df63f",
  "userId": "9469c642-6535-481f-82da-d840ee9608b3",
  "userFullName": "Ali Veli",
  "userAvatarUrl": "https://example.com/ali.jpg",
  "content": "Saat 18:30'da görüşürüz.",
  "createdAt": "2026-08-15T12:00:00Z"
}
```

---

## 5. Reviews

### Değerlendirme Oluştur 🔒

`POST /api/Reviews`

Request:

```json
{
  "eventId": "24801432-1ae7-4b2c-9f52-852a883df63f",
  "reviewedId": "9469c642-6535-481f-82da-d840ee9608b3",
  "rating": 5,
  "comment": "Takım oyununa çok uyumluydu."
}
```

Kurallar:

- `rating`: `1–5`
- `comment`: en fazla 1000 karakter
- Kullanıcı kendisini değerlendiremez.
- Her iki kullanıcı da etkinlik sahibi veya onaylı katılımcı olmalıdır.
- Aynı etkinlikte aynı kullanıcıya yalnızca bir değerlendirme yapılabilir.

Response — `201 Created`:

```json
{
  "id": "f993b300-239c-49ed-9ed3-998d355235b1",
  "eventId": "24801432-1ae7-4b2c-9f52-852a883df63f",
  "reviewerId": "8f5ca5ee-b8c0-4e3a-bd72-4ea88bc71f83",
  "reviewerFullName": "Yagiz Erdenler",
  "reviewerAvatarUrl": "https://example.com/avatar.jpg",
  "reviewedId": "9469c642-6535-481f-82da-d840ee9608b3",
  "rating": 5,
  "comment": "Takım oyununa çok uyumluydu.",
  "createdAt": "2026-08-15T22:00:00Z"
}
```

### Kullanıcının Değerlendirmelerini Getir

`GET /api/Reviews/user/{userId}`

Response — `200 OK`:

```json
[
  {
    "id": "f993b300-239c-49ed-9ed3-998d355235b1",
    "eventId": "24801432-1ae7-4b2c-9f52-852a883df63f",
    "reviewerId": "8f5ca5ee-b8c0-4e3a-bd72-4ea88bc71f83",
    "reviewerFullName": "Yagiz Erdenler",
    "reviewerAvatarUrl": "https://example.com/avatar.jpg",
    "reviewedId": "9469c642-6535-481f-82da-d840ee9608b3",
    "rating": 5,
    "comment": "Takım oyununa çok uyumluydu.",
    "createdAt": "2026-08-15T22:00:00Z"
  }
]
```

---

## 6. Sports

### Spor Branşlarını Listele

`GET /api/Sports`

Response — `200 OK`:

```json
[
  {
    "id": "football",
    "name": "Futbol",
    "iconName": "football",
    "category": "team"
  },
  {
    "id": "tennis",
    "name": "Tenis",
    "iconName": "tennis",
    "category": "racket"
  }
]
```

---

## Önerilen Frontend Akışı

1. `POST /api/Auth/login` veya `register`
2. JWT token'ı güvenli storage'da sakla.
3. `GET /api/Profiles/me` çağır.
4. `isOnboarded === false` ise onboarding'i göster.
5. Onboarding bitince `PUT /api/Profiles/me` ile `isOnboarded: true` gönder.
6. Expo token alındığında `POST /api/Auth/update-push-token` çağır.
7. Korumalı tüm isteklerde `Authorization: Bearer <token>` gönder.

## TypeScript İstek Yardımcısı

```ts
const API_URL = "http://localhost:5139";

export async function apiRequest<T>(
  path: string,
  options: RequestInit = {},
  token?: string,
): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  });

  const body = await response.json().catch(() => null);

  if (!response.ok) {
    throw new Error(body?.message ?? `HTTP ${response.status}`);
  }

  return body as T;
}
```

> Expo uygulaması fiziksel cihazda çalışıyorsa `localhost` telefona işaret eder. API URL olarak bilgisayarın yerel ağ IP adresi kullanılmalıdır (ör. `http://192.168.1.50:5139`).
