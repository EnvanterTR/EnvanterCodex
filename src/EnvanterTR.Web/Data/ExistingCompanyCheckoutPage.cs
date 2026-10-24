using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnvanterTR.Web.Data;

public static partial class CheckoutEndpoints
{
    private static void MapExistingCompanyCheckoutPage(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/subscription/checkout", RenderCompanyCheckoutPageAsync)
            .RequireAuthorization("company.member");
        endpoints.MapPost("/subscription/checkout", SubmitCompanyCheckoutPageAsync)
            .RequireAuthorization("company.member").RequireRateLimiting("checkout")
            .WithMetadata(new RequestSizeLimitAttribute(16384), new ExistingCompanyCheckoutMetadata());
    }

    private static async Task<IResult> RenderCompanyCheckoutPageAsync(HttpContext context, AppDbContext db,
        UserManager<EnvanterUser> users, IAntiforgery antiforgery, TenantProvisioningService provisioning,
        IOptions<IyzipayOptions> options, IConfiguration configuration, CancellationToken cancellationToken) =>
        await CompanyCheckoutPageAsync(context, db, users, antiforgery, provisioning, options.Value, configuration, cancellationToken);

    private static async Task<IResult> SubmitCompanyCheckoutPageAsync(HttpContext context, AppDbContext db,
        UserManager<EnvanterUser> users, SignInManager<EnvanterUser> signIn, IAntiforgery antiforgery,
        TenantProvisioningService provisioning, IyzipayCheckoutClient iyzipay, IOptions<IyzipayOptions> options,
        IDataProtectionProvider protection, IConfiguration configuration, CancellationToken cancellationToken)
    {
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException)
        {
            return await CompanyCheckoutPageAsync(context, db, users, antiforgery, provisioning, options.Value,
                configuration, cancellationToken, errorStatus: 400);
        }
        var form = await context.Request.ReadFormAsync(cancellationToken);
        string Field(string name) => form[name].ToString();
        Guid.TryParse(Field("checkoutId"), out var checkoutId);
        Guid.TryParse(Field("planId"), out var planId);
        var request = new InitializeCompanyCheckoutRequest(checkoutId, "", "", null,
            Field("firstName"), Field("lastName"), "", Field("phone"), Field("identityNumber"),
            Field("billingAddress"), Field("city"), Field("country"), Field("zipCode"), planId, "annual",
            Field("acknowledged") == "true", CheckoutNoticeVersion, Field("couponCode"),
            Field("currentPassword"), Field("twoFactorCode"));
        var result = await InitializeAsync(request, context, antiforgery, db, iyzipay, options, protection,
            users, signIn, provisioning, cancellationToken);
        if (result is IValueHttpResult { Value: CompanyCheckoutInitialized initialized })
            return Results.Redirect(initialized.CheckoutUrl);
        var status = (result as IStatusCodeHttpResult)?.StatusCode ?? 403;
        if (status is 401 or 403) return result;
        return await CompanyCheckoutPageAsync(context, db, users, antiforgery, provisioning, options.Value,
            configuration, cancellationToken, form, status);
    }

    private static async Task<IResult> CompanyCheckoutPageAsync(HttpContext context, AppDbContext db,
        UserManager<EnvanterUser> users, IAntiforgery antiforgery, TenantProvisioningService provisioning,
        IyzipayOptions options, IConfiguration configuration, CancellationToken ct,
        IFormCollection? submitted = null, int? errorStatus = null)
    {
        var owner = await ResolveExistingCheckoutOwnerAsync(context.User, db, users, ct);
        if (owner is null) return Results.Forbid();
        var language = submitted?["lang"].ToString() ?? context.Request.Query["lang"].ToString();
        if (!new[] { "tr", "en", "mk", "ru", "uk" }.Contains(language))
        {
            language = context.Request.Headers.AcceptLanguage.ToString().Split(',', ';')[0].Split('-')[0].ToLowerInvariant();
            if (!new[] { "tr", "en", "mk", "ru", "uk" }.Contains(language)) language = "tr";
        }
        var theme = submitted?["theme"].ToString() ?? context.Request.Query["theme"].ToString();
        if (theme is not ("light" or "dark")) theme = "system";
        var culture = CultureInfo.GetCultureInfo(language);
        string Text(string key) => CheckoutPageText(key, language);
        static string E(string? value) => WebUtility.HtmlEncode(value ?? "");
        string Money(decimal amount, string currency) => E(amount.ToString("N2", culture) + " " + currency);
        var now = DateTimeOffset.UtcNow;
        var orders = await db.CompanyCheckoutOrders.AsNoTracking().Where(order => order.TargetCompanyId == owner.Company.Id &&
                order.RequestedByUserId == owner.Account.Id).OrderByDescending(order => order.CreatedAt).Take(5).ToListAsync(ct);
        var pending = orders.FirstOrDefault(order => order.Status is "Pending" or "PaymentStarted" or "PaymentReview" && order.ExpiresAt > now);
        var purchaseId = Guid.TryParse(submitted?["checkoutId"].ToString(), out var submittedId) && submittedId != Guid.Empty
            ? submittedId : pending?.Id ?? Guid.NewGuid();
        var selectedPlan = Guid.TryParse(submitted?["planId"].ToString(), out var submittedPlan) ? submittedPlan : pending?.PlanId;
        var plans = await db.SubscriptionPlans.AsNoTracking().Where(plan => plan.IsActive && plan.IsPublic &&
            plan.DeploymentKind == "cloud" && plan.AnnualPrice > 0).OrderBy(plan => plan.SortOrder).ThenBy(plan => plan.AnnualPrice).ToListAsync(ct);
        plans = plans.Where(plan => SupportedCurrencies.Contains(plan.Currency.ToUpperInvariant())).ToList();
        var planMarkup = new StringBuilder();
        var eligibleCount = 0;
        foreach (var plan in plans)
        {
            var eligible = await provisioning.CanPurchaseAnnualPlanAsync(owner.Company.Id, plan, ct);
            if (eligible && selectedPlan is null) selectedPlan = plan.Id;
            if (eligible) eligibleCount++;
            var usersLimit = plan.MaxLoginUsers?.ToString(culture) ?? Text("unlimited");
            var assetsLimit = plan.MaxAssets?.ToString(culture) ?? Text("unlimited");
            planMarkup.Append($"""
                <label class="plan {(eligible ? "" : "unavailable")}"><input type="radio" name="planId" value="{plan.Id}" {(selectedPlan == plan.Id ? "checked" : "")} {(eligible ? "required" : "disabled")}>
                <span><strong>{E(plan.Name)}</strong><b>{Money(plan.AnnualPrice, plan.Currency)} <small>/ {E(Text("year"))}</small></b>
                <small>{Money(plan.AnnualPrice / 12m, plan.Currency)} · {E(Text("equivalent"))}</small>
                <small>{E(usersLimit)} {E(Text("users"))} · {E(assetsLimit)} {E(Text("assets"))}</small>
                {(eligible ? "" : $"<small class=\"warning\">{E(Text("ineligible"))}</small>")}</span></label>
                """);
        }
        var ready = GetConfigurationReadiness(options).Available;
        var canPay = ready && eligibleCount > 0 && owner.Account.TwoFactorEnabled && pending?.Status != "PaymentReview";
        var securityToken = antiforgery.GetAndStoreTokens(context);
        var nameParts = (pending?.OwnerName ?? owner.Account.DisplayName ?? "").Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        string Value(string name, string fallback = "") => E(submitted?[name].ToString() ?? fallback);
        string Input(string key, string name, string type = "text", int max = 100, string fallback = "", string? autocomplete = null) =>
            $"<label>{E(Text(key))}<input name=\"{name}\" type=\"{type}\" maxlength=\"{max}\" value=\"{Value(name, fallback)}\" required{(autocomplete is null ? "" : $" autocomplete=\"{autocomplete}\"")}></label>";
        var languages = string.Join(" ", new[] { "tr", "en", "mk", "ru", "uk" }.Select(lang =>
            $"<a href=\"?lang={lang}&amp;theme={theme}\" lang=\"{lang}\"{(lang == language ? " aria-current=\"true\"" : "")}>{lang.ToUpperInvariant()}</a>"));
        var themes = string.Join(" ", new[] { "light", "dark", "system" }.Select(mode =>
            $"<a href=\"?lang={language}&amp;theme={mode}\"{(mode == theme ? " aria-current=\"true\"" : "")}>{E(Text(mode))}</a>"));
        var portal = TenantHostResolver.BuildPortalUrl(owner.Company.Slug!, configuration["TenantRouting:RootDomain"]);
        var error = errorStatus is null ? "" : $"<div class=\"notice error\" role=\"alert\">{E(Text(errorStatus switch { 428 => "reauth", 409 => "conflict", 503 => "providerError", _ => "invalid" }))}</div>";
        var stateNotice = !ready ? Text("providerOff") : eligibleCount == 0 ? Text("noPlans") :
            !owner.Account.TwoFactorEnabled ? Text("setupMfa") : pending?.Status == "PaymentReview" ? Text("review") : null;
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        var history = string.Join("", orders.Select(order => $"<tr><td>{E(TimeZoneInfo.ConvertTime(order.CreatedAt, zone).ToString("g", culture))}</td>" +
            $"<td>{Money(order.Amount, order.Currency)}</td><td>{E(Text(order.Status))}</td></tr>"));
        if (history.Length == 0) history = $"<tr><td colspan=\"3\">{E(Text("noOrders"))}</td></tr>";
        var html = $$"""
            <!doctype html><html lang="{{language}}" data-theme="{{theme}}"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <meta name="robots" content="noindex,nofollow"><title>{{E(Text("title"))}} · EnvanterTR</title>
            <style>
            :root{color-scheme:light;--bg:#f4f7fb;--panel:#fff;--text:#19263b;--muted:#526278;--line:#cbd5e1;--accent:#1769e0;font:16px Inter,"Segoe UI",Arial,sans-serif}
            :root[data-theme=dark]{color-scheme:dark;--bg:#0c1422;--panel:#162234;--text:#eff5ff;--muted:#b4c3d8;--line:#3a4d67;--accent:#71acff}
            @media(prefers-color-scheme:dark){:root[data-theme=system]{color-scheme:dark;--bg:#0c1422;--panel:#162234;--text:#eff5ff;--muted:#b4c3d8;--line:#3a4d67;--accent:#71acff} }
            *{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text)}main{max-width:1100px;margin:36px auto;padding:0 20px 48px}header,nav{display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:14px}a{color:var(--accent);text-underline-offset:3px}nav a{padding:6px}a[aria-current=true]{font-weight:800}h1{font-size:clamp(1.8rem,4vw,2.5rem);letter-spacing:-.04em;margin-bottom:12px}h2{font-size:1.12rem}p,small{color:var(--muted);line-height:1.6}small{display:block}section,fieldset{background:var(--panel);border:1px solid var(--line);border-radius:16px;padding:24px;margin:20px 0}fieldset{margin:0;min-width:0}legend{padding:0 10px;font-weight:800}label{display:grid;gap:8px;font-size:.9rem}input,textarea{width:100%;max-width:100%;min-width:0;padding:12px;border:1px solid var(--line);border-radius:9px;background:var(--bg);color:var(--text);font:inherit}textarea{resize:vertical;min-height:90px}input:focus,textarea:focus{outline:2px solid var(--accent);outline-offset:2px}.grid,.plans{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px}.wide{grid-column:1/-1}.plan{display:flex;align-items:flex-start;padding:18px;border:1px solid var(--line);border-radius:12px}.plan:has(input:checked){border-color:var(--accent);box-shadow:0 0 0 1px var(--accent)}.plan input,.consent input{width:20px;flex:none;accent-color:var(--accent)}.plan strong,.plan b{display:block;margin-bottom:8px}.plan b{font-size:1.3rem}.plan b small{display:inline;font-size:.8rem}.unavailable{opacity:.65}.consent{display:flex;align-items:flex-start;gap:10px;line-height:1.5}.notice{padding:16px;border:1px solid var(--line);border-radius:10px;margin:16px 0;line-height:1.6}.error{border-color:#c14646}.warning{color:var(--muted)}button{border:0;border-radius:10px;padding:15px 22px;background:#1769e0;color:white;font:inherit;font-weight:750;cursor:pointer}button:disabled{opacity:.55;cursor:not-allowed}table{width:100%;border-collapse:collapse;font-size:.86rem}th,td{text-align:left;padding:12px 8px;border-bottom:1px solid var(--line);overflow-wrap:anywhere}.scroll{overflow-x:auto}.identity{overflow-wrap:anywhere}.identity strong{display:block}.identity a{display:inline-block;margin-top:8px}footer{margin-top:20px;font-size:.85rem;color:var(--muted)}@media(max-width:640px){main{margin-top:20px;padding:0 12px 30px}.grid,.plans{grid-template-columns:1fr}section,fieldset{padding:16px}button{width:100%}header nav{width:100%} }
            </style></head><body><main><header><a href="/giris">← {{E(Text("back"))}}</a><nav aria-label="{{E(Text("language"))}}">{{languages}}</nav><nav aria-label="{{E(Text("appearance"))}}">{{themes}}</nav></header>
            <h1>{{E(Text("title"))}}</h1><p>{{E(Text("intro"))}}</p><section class="identity"><strong>{{E(owner.Company.Name)}}</strong><span>{{E(owner.Account.Email)}}</span><br><a href="{{E(portal)}}">{{E(portal)}}</a><p>{{E(Text("preserved"))}}</p></section>
            {{error}}{{(stateNotice is null ? "" : $"<div class=\"notice\" role=\"status\">{E(stateNotice)}</div>")}}
            <form method="post" action="/subscription/checkout" autocomplete="on">
            <input type="hidden" name="{{E(securityToken.FormFieldName)}}" value="{{E(securityToken.RequestToken)}}">
            <input type="hidden" name="checkoutId" value="{{purchaseId}}"><input type="hidden" name="lang" value="{{language}}"><input type="hidden" name="theme" value="{{theme}}">
            <fieldset {{(canPay ? "" : "disabled")}}><legend>{{E(Text("annual"))}}</legend><p>{{E(Text("annualNote"))}}</p>
            <div class="plans">{{planMarkup}}</div><h2>{{E(Text("billing"))}}</h2><div class="grid">
            {{Input("firstName", "firstName", max:80, fallback:nameParts.FirstOrDefault() ?? "", autocomplete:"given-name")}}
            {{Input("lastName", "lastName", max:80, fallback:nameParts.Skip(1).FirstOrDefault() ?? "", autocomplete:"family-name")}}
            {{Input("phone", "phone", "tel", 24, autocomplete:"tel")}}{{Input("identity", "identityNumber", max:11)}}
            {{Input("city", "city", autocomplete:"address-level2")}}{{Input("country", "country", autocomplete:"country-name")}}
            <label>{{E(Text("zip"))}}<input name="zipCode" maxlength="24" value="{{Value("zipCode")}}" autocomplete="postal-code"></label>
            <label>{{E(Text("coupon"))}}<input name="couponCode" maxlength="64" value="{{Value("couponCode", pending?.CouponCode ?? "")}}" autocomplete="off"></label>
            <label class="wide">{{E(Text("address"))}}<textarea name="billingAddress" maxlength="500" required autocomplete="street-address">{{Value("billingAddress")}}</textarea></label>
            </div><h2>{{E(Text("security"))}}</h2><p>{{E(Text("securityNote"))}}</p><div class="grid">
            <label>{{E(Text("password"))}}<input name="currentPassword" type="password" maxlength="1024" required autocomplete="current-password"></label>
            <label>{{E(Text("code"))}}<input name="twoFactorCode" maxlength="32" required autocomplete="one-time-code" spellcheck="false" autocapitalize="off"></label></div>
            <p>{{E(Text("notice"))}}</p><label class="consent"><input name="acknowledged" type="checkbox" value="true" required>{{E(Text("consent"))}}</label>
            <p><button type="submit" id="annualCheckoutSubmit">{{E(Text(pending is null ? "pay" : "resume"))}}</button></p></fieldset></form>
            <section><h2>{{E(Text("history"))}}</h2><p>{{E(Text("historyNote"))}}</p><div class="scroll"><table><thead><tr><th>{{E(Text("date"))}}</th><th>{{E(Text("amount"))}}</th><th>{{E(Text("status"))}}</th></tr></thead><tbody>{{history}}</tbody></table></div><p><a href="?lang={{language}}&amp;theme={{theme}}">{{E(Text("refresh"))}}</a></p></section>
            <footer>{{E(Text("footer"))}}</footer></main></body></html>
            """;
        return Results.Content(html, "text/html; charset=utf-8", Encoding.UTF8, errorStatus ?? 200);
    }

    private static string CheckoutPageText(string key, string language)
    {
        var index = language switch { "en" => 1, "mk" => 2, "ru" => 3, "uk" => 4, _ => 0 };
        string[] values = key switch
        {
            "title" => ["Yıllık şirket aboneliği", "Annual company subscription", "Годишна претплата на компанијата", "Годовая подписка компании", "Річна підписка компанії"],
            "intro" => ["Yıllık paketinizi seçin ve mevcut şirket alanınızda devam edin.", "Choose an annual plan and continue in your existing company workspace.", "Изберете годишен пакет и продолжете во постојниот простор на компанијата.", "Выберите годовой план и продолжайте работу в существующем пространстве компании.", "Оберіть річний план і продовжуйте роботу в наявному просторі компанії."],
            "preserved" => ["Portal adresiniz, kullanıcılarınız ve envanter kayıtlarınız korunur.", "Your portal address, users and inventory records are preserved.", "Адресата на порталот, корисниците и евиденцијата остануваат зачувани.", "Адрес портала, пользователи и инвентарные записи сохраняются.", "Адреса порталу, користувачі та інвентарні записи зберігаються."],
            "back" => ["Şirket portalına dön", "Back to company portal", "Назад кон порталот", "Вернуться в портал", "Повернутися до порталу"],
            "language" => ["Dil", "Language", "Јазик", "Язык", "Мова"],
            "appearance" => ["Görünüm", "Appearance", "Изглед", "Оформление", "Вигляд"],
            "light" => ["Açık", "Light", "Светло", "Светлая", "Світла"],
            "dark" => ["Koyu", "Dark", "Темно", "Тёмная", "Темна"],
            "system" => ["Sistem", "System", "Систем", "Системная", "Системна"],
            "annual" => ["12 aylık paket", "12-month plan", "Пакет за 12 месеци", "План на 12 месяцев", "План на 12 місяців"],
            "annualNote" => ["Yıllık tek ödeme · Aylık tahsilat yapılmaz · Fiyatlar KDV hariçtir. Devam eden ücretli dönemde aynı paket yenilenebilir; paket değişikliği denemeden geçişte veya dönem sonunda yapılır.", "One annual payment · No monthly charges · VAT excluded. Renew the same plan during a prepaid period; change plans when leaving a trial or after the paid period ends.", "Една годишна уплата · Без месечна наплата · Без ДДВ. Во платениот период се обновува истиот пакет; промена по пробниот период или по истекот.", "Один годовой платёж · Без ежемесячных списаний · Без НДС. В оплаченном периоде можно продлить тот же план; сменить план — после пробного периода или окончания оплаты.", "Один річний платіж · Без щомісячних списань · Без ПДВ. Протягом оплаченого періоду можна продовжити той самий план; змінити — після пробного або оплаченого періоду."],
            "year" => ["yıl", "year", "година", "год", "рік"],
            "equivalent" => ["aylık karşılığı", "monthly equivalent", "месечен еквивалент", "в месяц в пересчёте", "місячний еквівалент"],
            "users" => ["kullanıcı", "users", "корисници", "пользователей", "користувачів"],
            "assets" => ["varlık", "assets", "средства", "активов", "активів"],
            "unlimited" => ["Sınırsız", "Unlimited", "Неограничено", "Без ограничений", "Без обмежень"],
            "ineligible" => ["Mevcut kapasite veya ücretli dönem için uygun değil.", "Not eligible for current capacity or prepaid period.", "Не одговара на капацитетот или платениот период.", "Не соответствует текущей ёмкости или оплаченному периоду.", "Не відповідає поточній місткості або оплаченому періоду."],
            "billing" => ["Fatura bilgileri", "Billing details", "Податоци за фактура", "Платёжные данные", "Платіжні дані"],
            "firstName" => ["Yetkili adı", "Contact first name", "Име на контакт", "Имя контактного лица", "Ім’я контактної особи"],
            "lastName" => ["Yetkili soyadı", "Contact last name", "Презиме на контакт", "Фамилия контактного лица", "Прізвище контактної особи"],
            "phone" => ["Telefon (+ülke kodu)", "Phone (+country code)", "Телефон (+код на држава)", "Телефон (+код страны)", "Телефон (+код країни)"],
            "identity" => ["Fatura kimlik numarası", "Billing identity number", "Идентификациски број", "Идентификационный номер", "Ідентифікаційний номер"],
            "city" => ["Şehir", "City", "Град", "Город", "Місто"],
            "country" => ["Ülke", "Country", "Држава", "Страна", "Країна"],
            "zip" => ["Posta kodu (isteğe bağlı)", "Postal code (optional)", "Поштенски код (незадолжително)", "Почтовый индекс (необязательно)", "Поштовий індекс (необов’язково)"],
            "coupon" => ["Kupon (isteğe bağlı)", "Coupon (optional)", "Купон (незадолжително)", "Купон (необязательно)", "Купон (необов’язково)"],
            "address" => ["Fatura adresi", "Billing address", "Адреса за фактура", "Платёжный адрес", "Платіжна адреса"],
            "security" => ["Hesap doğrulaması", "Account verification", "Потврда на сметка", "Проверка аккаунта", "Перевірка облікового запису"],
            "securityNote" => ["Mevcut parolanız ve Authenticator kodunuz veya tek kullanımlık kurtarma kodunuz gerekir. Kart bilgileri yalnız iyzico ekranında girilir.", "Use your current password and Authenticator code or one-use recovery code. Enter card details only on iyzico.", "Внесете ја тековната лозинка и Authenticator или еднократен код за враќање. Картичката се внесува само на iyzico.", "Введите текущий пароль и код Authenticator или одноразовый код восстановления. Данные карты вводятся только на iyzico.", "Введіть поточний пароль і код Authenticator або одноразовий код відновлення. Дані картки вводяться лише на iyzico."],
            "password" => ["Mevcut parola", "Current password", "Тековна лозинка", "Текущий пароль", "Поточний пароль"],
            "code" => ["Authenticator / kurtarma kodu", "Authenticator / recovery code", "Authenticator / код за враќање", "Authenticator / код восстановления", "Authenticator / код відновлення"],
            "notice" => ["İletişim ve fatura bilgileriniz ödeme oturumu için iyzico’ya iletilir. Parolanız ve doğrulama kodunuz sağlayıcıya gönderilmez. Ödeme doğrulanınca mevcut şirket aboneliğiniz etkinleştirilir; sipariş ve ödeme referansı saklanır.", "Contact and billing details are sent to iyzico for payment. Your password and verification code are not sent to the provider. Verified payment activates your existing company subscription; order and payment references are retained.", "Контактните и фактурните податоци се испраќаат до iyzico. Лозинката и кодот не се испраќаат до давателот. Потврдената уплата ја активира постојната претплата; нарачката и референцата се зачувуваат.", "Контактные и платёжные данные передаются iyzico. Пароль и код не передаются провайдеру. Подтверждённый платёж активирует подписку существующей компании; заказ и платёжная ссылка сохраняются.", "Контактні та платіжні дані передаються iyzico. Пароль і код не передаються провайдеру. Підтверджений платіж активує підписку наявної компанії; замовлення та платіжне посилання зберігаються."],
            "consent" => ["Ödeme ve mevcut şirket aboneliği için veri işleme bilgilendirmesini okudum.", "I have read the data notice for payment and the existing company subscription.", "Го прочитав известувањето за податоци за уплата и претплата.", "Я прочитал(а) уведомление об обработке данных для оплаты и подписки компании.", "Я прочитав(ла) повідомлення про обробку даних для оплати й підписки компанії."],
            "pay" => ["Yıllık ödemeye geç", "Continue to annual payment", "Кон годишна уплата", "Перейти к годовой оплате", "Перейти до річної оплати"],
            "resume" => ["Mevcut ödemeye devam et", "Resume existing payment", "Продолжи со постојната уплата", "Продолжить текущую оплату", "Продовжити поточну оплату"],
            "history" => ["Son ödeme işlemleriniz", "Your recent payment attempts", "Последни обиди за уплата", "Ваши последние платежи", "Ваші останні платежі"],
            "historyNote" => ["Yalnız bu şirkette sizin başlattığınız son beş işlem. Saat dilimi: Europe/Istanbul. Durum sağlayıcı doğrulamasından sonra güncellenir.", "Your latest five attempts for this company only. Time zone: Europe/Istanbul. Status updates after provider verification.", "Само последните пет обиди за оваа компанија. Зона: Europe/Istanbul. Статус по потврда од давателот.", "Только пять последних операций этой компании, начатых вами. Часовой пояс: Europe/Istanbul. Статус меняется после проверки провайдера.", "Лише п’ять останніх операцій цієї компанії, розпочатих вами. Часовий пояс: Europe/Istanbul. Статус змінюється після перевірки провайдера."],
            "date" => ["Tarih", "Date", "Датум", "Дата", "Дата"],
            "amount" => ["Tutar", "Amount", "Износ", "Сумма", "Сума"],
            "status" => ["Durum", "Status", "Статус", "Статус", "Стан"],
            "refresh" => ["Durumu yenile", "Refresh status", "Освежи статус", "Обновить статус", "Оновити стан"],
            "noOrders" => ["Henüz ödeme işleminiz yok.", "No payment attempts yet.", "Сè уште нема обиди за уплата.", "Платежей пока нет.", "Платежів поки немає."],
            "Pending" or "PaymentStarted" => ["Ödeme bekleniyor", "Awaiting payment", "Се чека уплата", "Ожидание оплаты", "Очікування оплати"],
            "PaymentReview" or "review" => ["Ödeme güvenlik incelemesinde. Sonucu bekleyin; yeni ödeme başlatmayın.", "Payment is under review. Wait for its result; do not start another payment.", "Уплатата е на проверка. Почекајте; не почнувајте нова уплата.", "Платёж проверяется. Дождитесь результата; не начинайте новый платёж.", "Платіж перевіряється. Дочекайтеся результату; не починайте новий платіж."],
            "Provisioned" => ["Abonelik etkin", "Subscription active", "Претплатата е активна", "Подписка активна", "Підписка активна"],
            "ProvisioningFailed" => ["Ödeme doğrulandı; destek gerekli", "Payment verified; support required", "Уплатата е потврдена; потребна е поддршка", "Платёж подтверждён; нужна поддержка", "Платіж підтверджено; потрібна підтримка"],
            "PaymentFailed" => ["Ödeme tamamlanmadı", "Payment not completed", "Уплатата не е завршена", "Платёж не завершён", "Платіж не завершено"],
            "Expired" => ["Ödeme oturumu sona erdi", "Payment session expired", "Сесијата за уплата истече", "Сессия оплаты истекла", "Сесія оплати завершилася"],
            "reauth" => ["Parolanızı ve Authenticator/kurtarma kodunuzu doğrulayın. MFA kapalıysa şirket portalında profil → Güvenlik ayarları bölümünden etkinleştirin.", "Verify your password and Authenticator/recovery code. If MFA is off, enable it in portal profile → Security settings.", "Потврдете лозинка и Authenticator/код. Ако MFA е исклучена, активирајте во профил → Безбедност.", "Проверьте пароль и код Authenticator/восстановления. Если MFA выключена, включите её в профиле → Настройки безопасности.", "Перевірте пароль і код Authenticator/відновлення. Якщо MFA вимкнено, увімкніть у профілі → Налаштування безпеки."],
            "conflict" => ["Paket kapasitesini ve bekleyen ödeme işlemini kontrol edin. Tamamlanmış işlemler için durumu yenileyin.", "Check plan capacity and the pending payment. Refresh status for completed attempts.", "Проверете капацитет и уплата во тек. Освежете за завршени обиди.", "Проверьте ёмкость плана и ожидающий платёж. Для завершённых операций обновите статус.", "Перевірте місткість плану й очікуваний платіж. Для завершених операцій оновіть стан."],
            "providerError" => ["Ödeme sağlayıcısına ulaşılamadı. İşlem durumunu kontrol edip tekrar deneyin.", "Payment provider unavailable. Check the attempt status before retrying.", "Давателот е недостапен. Проверете статус пред повторен обид.", "Провайдер оплаты недоступен. Перед повтором проверьте статус операции.", "Провайдер оплати недоступний. Перед повтором перевірте стан операції."],
            "invalid" => ["Güvenlik doğrulaması veya form bilgileri geçersiz. Alanları kontrol edin; parolanızı ve kodunuzu yeniden girin.", "Security verification or form details are invalid. Check the fields and re-enter your password and code.", "Неважечка проверка или податоци. Проверете ги полињата и повторно внесете лозинка и код.", "Проверка или данные формы неверны. Проверьте поля, повторно введите пароль и код.", "Перевірка або дані форми неправильні. Перевірте поля, повторно введіть пароль і код."],
            "providerOff" => ["Çevrimiçi ödeme henüz kullanılamıyor. Bilgileriniz gönderilmez; platform yöneticinizle iletişime geçin.", "Online payment is unavailable. Your details are not submitted; contact the platform administrator.", "Онлајн уплатата е недостапна. Податоците не се испраќаат; контактирајте го администраторот.", "Онлайн-оплата недоступна. Данные не отправляются; свяжитесь с администратором платформы.", "Онлайн-оплата недоступна. Дані не надсилаються; зверніться до адміністратора платформи."],
            "noPlans" => ["Bu şirket için satın alınabilir uygun yıllık paket bulunmuyor. Platform yöneticinizle iletişime geçin.", "No eligible annual plan is available for this company. Contact the platform administrator.", "Нема соодветен годишен пакет. Контактирајте го администраторот.", "Для компании нет подходящего годового плана. Свяжитесь с администратором.", "Для компанії немає відповідного річного плану. Зверніться до адміністратора."],
            "setupMfa" => ["Ödeme için MFA gereklidir. Şirket portalında profil → Güvenlik ayarları bölümünden Authenticator kurulumunu tamamlayın.", "Payment requires MFA. Complete Authenticator setup in portal profile → Security settings.", "Потребна е MFA. Поставете Authenticator во профил → Безбедност.", "Для оплаты нужна MFA. Настройте Authenticator в профиле → Настройки безопасности.", "Для оплати потрібна MFA. Налаштуйте Authenticator у профілі → Налаштування безпеки."],
            "footer" => ["EnvanterTR Cloud · Yıllık tek ödeme · Kart bilgileri iyzico üzerinde işlenir.", "EnvanterTR Cloud · One annual payment · Card details processed by iyzico.", "EnvanterTR Cloud · Една годишна уплата · Картичката се обработува на iyzico.", "EnvanterTR Cloud · Один годовой платёж · Данные карты обрабатывает iyzico.", "EnvanterTR Cloud · Один річний платіж · Дані картки обробляє iyzico."],
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown checkout text")
        };
        return values[index];
    }
}
