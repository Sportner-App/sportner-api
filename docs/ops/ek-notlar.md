# Ek notlar (sonraya bırakılanlar)

Bu dosya V1’de bilerek ertelenen / owner tarafında kalan işlerin kısa kaydıdır.
Kod tamamlandıktan sonra buradan takip edilir.

---

## 1) Supabase RLS + Data API

**Durum:** Şimdilik yapılmıyor. Backend Npgsql (postgres/service) ile çalışıyor; RLS zorunlu değil ta ki Data API / anon key client’a açılana kadar.

**Ne zaman:** Staging/prod’da Supabase Data API açık kalacaksa veya anon key sızma riski varsa.

**Nasıl:** [`supabase-rls.md`](supabase-rls.md) script’ini SQL Editor’de çalıştır; Data API’yi kapat veya grants’sız bırak.

**Kontrol:**

```sql
SELECT relname, relrowsecurity
FROM pg_class
WHERE relnamespace = 'public'::regnamespace
  AND relkind = 'r'
ORDER BY 1;
```

---

## 2) Client route senkronu

**Durum:** ✅ Kapandı — client `/api/user-profiles` kullanıyor.

API kanonik route aynı; eski `/api/profiles` yok.

---

## 3) Auth kararı (V1 güncel)

| Eski | Yeni |
| ---- | ---- |
| Phone + OTP + SMS | **Username + password** |
| 2FA | Yok (şimdilik) |

- `POST /api/auth/register` — username, password, firstName [, lastName]
- `POST /api/auth/login` — username, password
- Refresh / logout aynı

SMS / OTP kaldırıldı. Admin panel süreçleri yok; catalog **seed** ile ilerler.

---

## 4) Diğer ops backlog

| Madde | Not |
| ----- | --- |
| Real FCM/APNs | Push hâlâ `LoggingPushSender` |
| AdminUserIds | Spor CRUD admin policy; seed yeterliyse boş kalsın |
| PostGIS | Discover bbox day-1; exact radius sonra |
| Branch protection | CI workflow var; GitHub required check owner |
