# P0-C — firma hostu ve alt alan adı erişimi — 23 Ekim 2026

## Kabul sonucu

Önceki 503/no-upstream bulgusu bu kontrolde tekrarlanmadı. Üç HTTPS hostu da doğrulanmış TLS bağlantısıyla API sağlık kontrolünde HTTP 200 ve `ready`, 118 migration / 0 bekleyen migration döndürdü:

| Host | DNS A kaydı | TLS doğrulaması | `/api/status` |
|---|---|---:|---:|
| `firma.envantertr.com` | `104.21.10.9`, `172.67.189.218` | geçti (`curl ssl_verify_result=0`) | 200, ready, 118/0 |
| `santa.envantertr.com` | `104.21.10.9`, `172.67.189.218` | geçti (`curl ssl_verify_result=0`) | 200, ready, 118/0 |
| `farma.envantertr.com` | `104.21.10.9`, `172.67.189.218` | geçti (`curl ssl_verify_result=0`) | 200, ready, 118/0 |

Ek olarak `https://firma.envantertr.com/` ve `/giris` HTTP 200, sayfa başlığı `EnvanterTR — Şirket Alanı` döndürdü.

## Kapsam sınırı

Bu, DNS/TLS/proxy/uygulama sağlık yönlendirmesi için canlı ve salt-okunur bir HTTP kabulüdür. Oturum açılmadı; tenant çözümlemesi, hesap erişimi, Santa/Farma veri izolasyonu veya müşteri satın alma provisioning'i bu kontrolle kanıtlanmış sayılmaz. Bunlar hedefteki P0-E / Bölüm 20 kapsamındadır. Bu kontrolde üretim verisi veya Coolify yapılandırması değiştirilmedi.
