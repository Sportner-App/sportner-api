# 00 — V2 execution rules

[`docs/roadmap/00-execution-rules.md`](../roadmap/00-execution-rules.md) aynen geçerli.
Bu dosya yalnızca **V2’ye özgü** ek kuralları listeler.

---

## 1. Plan → kod kapısı

1. İlgili `docs/v2/0X-….md` okunur.
2. “Karar kapısı”nda açık sorular varsa **önce konuş**, sonra kod.
3. Onay cümlesi: “`v2/0X` ile başla”.
4. Yeni tablo / enum / storage bucket varsa önce `docs/database/*` spec taslağı,
   sonra migration — tersi yok.

## 2. V1’i bozma

- Mevcut endpoint contract’larını kırmadan genişlet (additive query param / yeni route).
- Breaking change gerekirse aynı playbook’ta “migration note + client etki” yaz.
- V1 award hook’ları ve friendship state machine’i yeniden yazılmaz; üzerine eklenir.

## 3. Ortak motor kuralı

Explore / friend suggestions / event ranking **aynı sinyal sözlüğünü** kullanır
(`03-recommendation-engine`). Her yüzey kendi SQL’ini kopyalayıp farklı skor
üretmez.

## 4. Quest ≠ Badge

- `Badge` = kazanılan kalıcı rozet tanımı + `UserBadge`.
- `Quest` = ilerleme ölçen görev; tamamlanınca badge (veya XP) ödülü tetikleyebilir.
- Quest progress’i badge aggregate’ine gömme; ayrı tablolar (06).

## 5. Medya sahipliği

| Tür | V1 | V2 |
| --- | -- | -- |
| Post media | `PostMedia` | Değişmez |
| Message media | `Message` path | Değişmez |
| Profile avatar / intro | UserProfile | Değişmez |
| Albüm | — | `Album` + `AlbumMedia` (07) |

Albüm, post’un yerine geçmez; feed’e otomatik düşmez (karar 07’de kilitlenir).

## 6. Performans varsayımları (V2)

- Ranking: önce SQL + in-process skor; Redis/feature-store yok.
- Cursor pagination zorunlu (offset yok).
- Cached counters (`UserStatistics`, post like/comment counts) okunur; feed’de aggregate load yok.
- Geo: day-1 bbox / Haversine; PostGIS ayrı ops kararı.

## 7. Konuşmadan kodlanmayacak V2 konuları

Karar turu 1+2 ile **kilitlendi** (2026-08-13). Özet: [`status.md`](status.md).

Yeni konu çıkarsa bu bölüme eklenir; aksi halde faz playbook’undaki kilitli tablo yeter.

## 8. DoD (V2 playbook)

- [ ] Exit criteria tamam
- [ ] Build + ilgili testler yeşil
- [ ] `docs/features/*` ve gerekirse `docs/database/*` güncellendi
- [ ] `docs/v2/status.md` güncellendi
- [ ] Bilinçli defer’ler playbook’ta işaretli
