# Kurumsal Giriş (Entra ID / LDAP) Tasarımı

Durum: TASARIM. Kod henüz yazılmadı. Yeni EF migration (129) gerektirir; sahibin onayı ile uygulanır.

## Hedef
Şirket yöneticisi, çalışanların Microsoft Entra ID (Microsoft 365) veya şirket içi Active Directory (LDAP) hesabıyla giriş yapmasını açabilir. Parola EnvanterTR'de saklanmaz.

## İlke
- Yeni kullanıcı otomatik oluşturulmaz (JIT yok). Giriş yalnız şirkette zaten üyeliği olan kullanıcıya açılır; eşleşme e-posta ile yapılır.
- Dizin doğrulaması sonrası oturum, mevcut `/api/session/login` ile aynı kurallardan geçer: aktif şirket, üyelik, şirket MFA şartı, oturum süresi, denetim kaydı.
- Platform yöneticisi girişi SSO'ya bağlanmaz.
- Sırlar (istemci gizli anahtarı, LDAP bind parolası) ASP.NET DataProtection ile şifrelenerek saklanır; API yanıtında asla dönmez.

## Veri modeli (migration 129)
`CompanyIdentityProviders`: Id, CompanyId (unique), Kind (`entra`|`ldap`), Enabled, AllowedEmailDomains, EntraTenantId, EntraClientId, SecretProtected, LdapHost, LdapPort, LdapUseTls, LdapBaseDn, LdapBindDn, LdapUserFilter, CreatedAt, UpdatedAt.

## Entra ID akışı (OIDC, kod + PKCE)
1. `GET /api/session/sso/start` → state, nonce, code_verifier çerezde (HttpOnly, kısa ömür); Microsoft yetkilendirme adresine yönlendirme.
2. `GET /api/session/sso/callback` → state doğrula, kodu token uç noktasında değiş (sunucudan sunucuya TLS), `iss`, `aud`, `tid`, `nonce`, `exp` kontrol et, e-postayı (`preferred_username`/`email`) al.
3. E-posta üyelikle eşleşirse ortak "oturumu tamamla" yardımcısı çağrılır.

Şirket yöneticisinin yapacağı: Entra'da App Registration oluşturup yönlendirme adresini `https://<firma>.envantertr.com/api/session/sso/callback` olarak tanımlamak; Tenant ID, Client ID ve gizli anahtarı ayarlara girmek.

## LDAP / Active Directory akışı
- Yalnız LDAPS (veya StartTLS); düz LDAP reddedilir.
- Servis hesabı ile bağlan, kullanıcıyı filtreyle bul (`userPrincipalName`/`mail`), bulunan DN ile kullanıcı parolasını doğrula. Filtre girdisi LDAP için kaçışlanır.
- Şirket içi (on-premise) ve Ubuntu sürümlerinde sunucu dizine erişebilmelidir; bulutta dizin internetten erişilebilir olmalıdır (VPN/ağ geçidi müşteri sorumluluğu).
- Bağımlılık: `System.DirectoryServices.Protocols` (Windows/Linux). Üçüncü taraf listesine eklenir.

## Gerekli refaktör
`/api/session/login` içindeki "üyelik + MFA + oturum + claim" bölümü ortak bir yardımcıya taşınır; parola girişi ve SSO aynı kodu kullanır. Mevcut güvenlik testleri bu refaktörün regresyon korumasıdır.

## Test
- Entra: sahte OIDC sağlayıcı ile birim/uyum testi (state, nonce, yanlış tenant, üyeliksiz e-posta, süresi dolmuş token).
- LDAP: kaçış (injection) testleri; gerçek doğrulama yerel bir OpenLDAP kapsayıcısı ile.
- Tarayıcı: giriş sayfasında "Microsoft ile giriş" düğmesi yalnız şirket SSO'yu açtıysa görünür.
