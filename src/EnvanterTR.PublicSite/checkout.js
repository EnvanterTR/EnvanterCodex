(() => {
  'use strict';

  const noticeVersion = 'cloud-checkout-data-v1';
  const translations = {
    tr: {
      secure: 'Güvenli ödeme adımı', back: '← Paketlere dön', eyebrow: 'ENVANTERTR CLOUD · SATIN ALMA',
      title: 'Şirket alanınızı oluşturun', intro: 'Planınızı seçin, firma ve fatura bilgilerini girin. Ödeme sağlayıcısında doğrulandıktan sonra şirket hesabınız açılır.',
      loading: 'Plan ve ödeme durumu kontrol ediliyor…', summary: 'Sipariş özeti', monthly: 'Aylık', annual: 'Yıllık',
      users: 'Cloud kullanıcı sayısı', unlimited: 'Sınırsız', paymentNote: 'Tutar etkin plan fiyatından sunucuda hesaplanır. Kart bilgileri iyzico’nun güvenli ödeme ekranında girilir.',
      companyDetails: 'Şirket ve fatura bilgileri', formIntro: 'Şirket hesabının sahibi bu e-posta ile davet edilir. Burada parola veya kart bilgisi istenmez.',
      companyName: 'Şirket adı', slug: 'Şirket web adresi', slugHelp: 'Şirket adından önerilir: Farma A.Ş. → farma.envantertr.com. Adresi değiştirebilirsiniz.', firstName: 'Yetkili adı', lastName: 'Yetkili soyadı', email: 'Hesap e-postası',
      phone: 'Telefon · ülke koduyla', identity: 'T.C. kimlik numarası', zip: 'Posta kodu', address: 'Fatura adresi', city: 'Şehir', country: 'Ülke',
      noticeTitle: 'Ödeme ve hesap açılışı için veri kullanımı',
      noticeBody: 'Ödeme oturumunu oluşturmak için bu formdaki yetkili ve fatura bilgileri iyzico’ya iletilir. Ödeme kartı bilgilerini bu uygulama almaz. Ödeme sağlayıcı tarafından doğrulanınca şirket alanı, sahibi ve abonelik oluşturulur; sipariş durumu ve ödeme referansı sistemde tutulur.',
      consent: 'Ödeme ve şirket hesabı açılışı için yukarıdaki veri işleme bilgilendirmesini okudum.', newAttempt: 'Yeni ödeme denemesi oluştur',
      demo: 'Önce demoyu incele', pay: 'Güvenli ödemeye geç', submitting: 'Ödeme oturumu hazırlanıyor…',
      footer: 'Güvenli bağlantı · Ödeme sonucu sunucuda sağlayıcıdan doğrulanır · Şirket hesabı doğrulanmış ödemeden önce açılmaz',
      unavailable: 'Çevrimiçi ödeme şu anda kullanılamıyor. Bilgileriniz gönderilmeden önce ödeme yapılandırmasının tamamlanması gerekiyor.',
      invalidPlan: 'Bu paket satın alınabilir Cloud planı olarak yayımlanmamış veya seçilen dönem için fiyatı yok.',
      loadError: 'Paket bilgileri yüklenemedi. Biraz sonra yeniden deneyin.', missingPlan: 'Satın alma bağlantısında geçerli bir Cloud paket seçilmemiş.',
      period: 'Faturalama dönemi', monthlyPeriod: 'Aylık abonelik', annualPeriod: 'Yıllık abonelik',
      sameAttempt: 'Aynı ödeme isteği hazırlanıyor. Birkaç saniye sonra aynı düğmeyle yeniden deneyin.',
      freshAttemptReady: 'Önceki deneme kapandı. Aynı bilgilerle yeni bir güvenli ödeme denemesi başlatabilirsiniz.',
      unsafeUrl: 'Ödeme sağlayıcısından güvenli bir bağlantı alınamadı. Kart bilgileriniz gönderilmedi.',
      fillFields: 'Lütfen işaretli alanları ve bilgilendirme onayını kontrol edin.',
      demoLink: 'Örnek şirketle ürünü deneyin', priceUnavailable: 'Bu plan için seçilen dönem fiyatı yayımlanmamış.'
    },
    en: {
      secure: 'Secure payment step', back: '← Back to plans', eyebrow: 'ENVANTERTR CLOUD · CHECKOUT',
      title: 'Create your company workspace', intro: 'Choose a plan and enter company and billing details. Your workspace opens after the payment provider verifies the payment.',
      loading: 'Checking plan and payment availability…', summary: 'Order summary', monthly: 'Monthly', annual: 'Annual',
      users: 'Cloud users', unlimited: 'Unlimited', paymentNote: 'The server calculates the amount from the active plan. Card details are entered on iyzico’s secure payment page.',
      companyDetails: 'Company and billing details', formIntro: 'The workspace owner will receive an invitation at this email. No password or card details are collected here.',
      companyName: 'Company name', slug: 'Workspace web address', slugHelp: 'Suggested from the company name: Farma A.Ş. → farma.envantertr.com. You can edit the address.', firstName: 'Contact first name', lastName: 'Contact last name', email: 'Account email',
      phone: 'Phone · include country code', identity: 'National identity number', zip: 'Postal code', address: 'Billing address', city: 'City', country: 'Country',
      noticeTitle: 'Data use for payment and workspace setup',
      noticeBody: 'The contact and billing details in this form are sent to iyzico to create a payment session. This app does not collect payment card details. After the provider verifies payment, the company workspace, owner account and subscription are created; order status and payment reference are retained.',
      consent: 'I have read the data use notice above for payment and company account setup.', newAttempt: 'Start a new payment attempt',
      demo: 'Explore the demo first', pay: 'Continue to secure payment', submitting: 'Preparing payment…',
      footer: 'Secure connection · Payment is verified with the provider on the server · The workspace is not opened before verified payment',
      unavailable: 'Online payment is unavailable right now. Payment setup must be completed before you submit any details.',
      invalidPlan: 'This plan is not a published Cloud plan for purchase or has no price for the selected term.',
      loadError: 'Plan information could not be loaded. Please try again shortly.', missingPlan: 'Choose a valid Cloud plan from the purchase link.',
      period: 'Billing period', monthlyPeriod: 'Monthly subscription', annualPeriod: 'Annual subscription',
      sameAttempt: 'This payment request is still being prepared. Wait a few seconds, then retry with the same button.',
      freshAttemptReady: 'The previous attempt has ended. You can start a new secure payment attempt with the same details.',
      unsafeUrl: 'A secure payment link was not returned. Your card details were not submitted.',
      fillFields: 'Check the required fields and acknowledge the data use notice.',
      demoLink: 'Explore the sample workspace', priceUnavailable: 'A price for this billing term has not been published.'
    },
    ru: {
      secure: 'Безопасный шаг оплаты', back: '← Назад к планам', eyebrow: 'ENVANTERTR CLOUD · ПОКУПКА',
      title: 'Создайте рабочее пространство компании', intro: 'Выберите план и заполните сведения о компании и оплате. Пространство откроется после подтверждения платежа провайдером.',
      loading: 'Проверка плана и доступности оплаты…', summary: 'Сводка заказа', monthly: 'Месяц', annual: 'Год',
      users: 'Пользователи Cloud', unlimited: 'Без ограничений', paymentNote: 'Сумма рассчитывается сервером по действующему плану. Данные карты вводятся на защищённой странице iyzico.',
      companyDetails: 'Данные компании и оплаты', formIntro: 'Владелец компании получит приглашение на этот адрес. Пароль и данные карты здесь не запрашиваются.',
      companyName: 'Название компании', slug: 'Веб-адрес компании', slugHelp: 'Предлагается по названию компании: Farma A.Ş. → farma.envantertr.com. Адрес можно изменить.', firstName: 'Имя контактного лица', lastName: 'Фамилия контактного лица', email: 'Электронная почта аккаунта',
      phone: 'Телефон · с кодом страны', identity: 'Идентификационный номер', zip: 'Почтовый индекс', address: 'Адрес для оплаты', city: 'Город', country: 'Страна',
      noticeTitle: 'Использование данных для оплаты и создания аккаунта',
      noticeBody: 'Контактные и платёжные сведения из формы передаются iyzico для создания платёжной сессии. Приложение не получает данные платёжной карты. После подтверждения оплаты создаются компания, владелец и подписка; статус заказа и ссылка на платёж сохраняются.',
      consent: 'Я ознакомился(лась) с уведомлением об использовании данных для оплаты и создания аккаунта.', newAttempt: 'Начать новую попытку оплаты',
      demo: 'Сначала посмотреть демо', pay: 'Перейти к безопасной оплате', submitting: 'Подготовка оплаты…',
      footer: 'Защищённое соединение · Платёж подтверждается провайдером на сервере · Компания не открывается до подтверждения оплаты',
      unavailable: 'Онлайн-оплата сейчас недоступна. Настройка оплаты должна быть завершена до отправки сведений.',
      invalidPlan: 'Этот план не опубликован для покупки в Cloud или для выбранного периода нет цены.',
      loadError: 'Не удалось загрузить план. Повторите попытку позже.', missingPlan: 'Выберите действующий план Cloud по ссылке покупки.',
      period: 'Период оплаты', monthlyPeriod: 'Подписка на месяц', annualPeriod: 'Подписка на год',
      sameAttempt: 'Платёжный запрос готовится. Подождите несколько секунд и повторите попытку.',
      freshAttemptReady: 'Предыдущая попытка завершена. Можно начать новую безопасную оплату с теми же данными.',
      unsafeUrl: 'Не получена защищённая ссылка оплаты. Данные карты не отправлены.',
      fillFields: 'Проверьте обязательные поля и подтверждение ознакомления с уведомлением.',
      demoLink: 'Открыть демонстрационную компанию', priceUnavailable: 'Цена для этого периода не опубликована.'
    },
    uk: {
      secure: 'Безпечний етап оплати', back: '← Повернутися до планів', eyebrow: 'ENVANTERTR CLOUD · ПРИДБАННЯ',
      title: 'Створіть простір компанії', intro: 'Оберіть план і введіть дані компанії та оплати. Простір відкриється після підтвердження платежу провайдером.',
      loading: 'Перевіряємо план і доступність оплати…', summary: 'Підсумок замовлення', monthly: 'Місяць', annual: 'Рік',
      users: 'Користувачі Cloud', unlimited: 'Без обмежень', paymentNote: 'Суму сервер обчислює за активним планом. Дані картки вводяться на захищеній сторінці iyzico.',
      companyDetails: 'Дані компанії та оплати', formIntro: 'Власник компанії отримає запрошення на цю адресу. Пароль і дані картки тут не збираються.',
      companyName: 'Назва компанії', slug: 'Вебадреса компанії', slugHelp: 'Пропонується за назвою компанії: Farma A.Ş. → farma.envantertr.com. Адресу можна змінити.', firstName: 'Ім’я контактної особи', lastName: 'Прізвище контактної особи', email: 'Електронна пошта облікового запису',
      phone: 'Телефон · із кодом країни', identity: 'Ідентифікаційний номер', zip: 'Поштовий індекс', address: 'Платіжна адреса', city: 'Місто', country: 'Країна',
      noticeTitle: 'Використання даних для оплати та створення облікового запису',
      noticeBody: 'Контактні та платіжні дані з цієї форми передаються iyzico для створення платіжної сесії. Застосунок не отримує дані платіжної картки. Після підтвердження платежу створюються компанія, власник і підписка; статус замовлення та платіжний номер зберігаються.',
      consent: 'Я ознайомився/ознайомилася з повідомленням про використання даних для оплати та створення облікового запису.', newAttempt: 'Почати нову спробу оплати',
      demo: 'Спочатку переглянути демо', pay: 'Перейти до безпечної оплати', submitting: 'Готуємо оплату…',
      footer: 'Захищене з’єднання · Платіж перевіряється провайдером на сервері · Компанію не буде відкрито до перевірки платежу',
      unavailable: 'Онлайн-оплата зараз недоступна. Перш ніж надсилати дані, потрібно налаштувати оплату.',
      invalidPlan: 'Цей план не опубліковано для придбання Cloud або для вибраного періоду немає ціни.',
      loadError: 'Не вдалося завантажити план. Спробуйте трохи пізніше.', missingPlan: 'Виберіть дійсний план Cloud за посиланням придбання.',
      period: 'Період оплати', monthlyPeriod: 'Місячна підписка', annualPeriod: 'Річна підписка',
      sameAttempt: 'Платіжний запит ще готується. Зачекайте кілька секунд і спробуйте ще раз.',
      freshAttemptReady: 'Попередню спробу завершено. Можна розпочати нову безпечну оплату з тими самими даними.',
      unsafeUrl: 'Не отримано безпечне посилання оплати. Дані картки не надіслано.',
      fillFields: 'Перевірте обов’язкові поля та підтвердження ознайомлення з повідомленням.',
      demoLink: 'Переглянути демонстраційну компанію', priceUnavailable: 'Ціну для цього періоду не опубліковано.'
    },
    mk: {
      secure: 'Безбеден чекор за плаќање', back: '← Назад кон пакетите', eyebrow: 'ENVANTERTR CLOUD · КУПУВАЊЕ',
      title: 'Создадете простор за вашата компанија', intro: 'Изберете пакет и внесете податоци за компанијата и наплатата. Просторот се отвора откако давателот ќе го потврди плаќањето.',
      loading: 'Се проверуваат пакетот и плаќањето…', summary: 'Преглед на нарачката', monthly: 'Месечно', annual: 'Годишно',
      users: 'Cloud корисници', unlimited: 'Неограничено', paymentNote: 'Серверот ја пресметува сумата од активниот пакет. Податоците за картичката се внесуваат на безбедната страница на iyzico.',
      companyDetails: 'Податоци за компанијата и наплатата', formIntro: 'Сопственикот ќе добие покана на оваа е-пошта. Тука не се бара лозинка или број на картичка.',
      companyName: 'Име на компанијата', slug: 'Веб-адреса на компанијата', slugHelp: 'Се предлага според името на компанијата: Farma A.Ş. → farma.envantertr.com. Адресата може да се измени.', firstName: 'Име на контакт-лицето', lastName: 'Презиме на контакт-лицето', email: 'Е-пошта за сметката',
      phone: 'Телефон · со код на државата', identity: 'Идентификациски број', zip: 'Поштенски број', address: 'Адреса за наплата', city: 'Град', country: 'Држава',
      noticeTitle: 'Употреба на податоците за плаќање и отворање сметка',
      noticeBody: 'Контакт и платежните податоци од формуларот се испраќаат до iyzico за да се создаде платежна сесија. Апликацијата не ги прима податоците за платежната картичка. По потврдата се создаваат компанијата, сопственикот и претплатата; статусот на нарачката и референцата за плаќање се чуваат.',
      consent: 'Го прочитав известувањето за употреба на податоци за плаќање и отворање сметка.', newAttempt: 'Започнете нов обид за плаќање',
      demo: 'Прво разгледајте го демото', pay: 'Продолжи кон безбедно плаќање', submitting: 'Се подготвува плаќањето…',
      footer: 'Безбедна врска · Серверот го проверува плаќањето кај давателот · Компанијата не се отвора пред потврдено плаќање',
      unavailable: 'Онлајн плаќањето моментално не е достапно. Конфигурацијата треба да се заврши пред да испратите податоци.',
      invalidPlan: 'Овој Cloud пакет не е објавен за купување или нема цена за избраниот период.',
      loadError: 'Податоците за пакетот не можеа да се вчитаат. Обидете се повторно подоцна.', missingPlan: 'Изберете валиден Cloud пакет од врската за купување.',
      period: 'Период на наплата', monthlyPeriod: 'Месечна претплата', annualPeriod: 'Годишна претплата',
      sameAttempt: 'Платежното барање сè уште се подготвува. Почекајте неколку секунди и обидете се повторно.',
      freshAttemptReady: 'Претходниот обид заврши. Можете да започнете ново безбедно плаќање со истите податоци.',
      unsafeUrl: 'Не е добиена безбедна врска за плаќање. Податоците за картичката не се испратени.',
      fillFields: 'Проверете ги задолжителните полиња и потврдата за известувањето.',
      demoLink: 'Разгледајте ја пример-компанијата', priceUnavailable: 'Цената за овој период не е објавена.'
    }
  };

  const language = localStorage.getItem('envantertr-home-language') || 'tr';
  const copy = translations[language] || translations.tr;
  const $ = selector => document.querySelector(selector);
  const form = $('#checkoutForm');
  const fields = $('#checkoutFields');
  const notice = $('#checkoutNotice');
  const submit = $('#checkoutSubmit');
  const feedback = $('#checkoutFeedback');
  const newAttempt = $('#newAttempt');
  const params = new URLSearchParams(location.search);
  const planId = params.get('planId');
  let plan = null;
  let term = 'monthly';
  let paymentAvailable = false;
  let slugWasEdited = false;

  document.documentElement.lang = translations[language] ? language : 'tr';
  document.querySelectorAll('[data-i18n]').forEach(element => {
    const key = element.dataset.i18n;
    if (copy[key]) element.textContent = copy[key];
  });
  document.querySelector('[role="group"]').setAttribute('aria-label', copy.period);
  $('#demoLink').textContent = copy.demoLink;

  const setNotice = (message, kind = '') => {
    notice.className = `notice${kind ? ` ${kind}` : ''}`;
    notice.replaceChildren(document.createTextNode(message));
  };
  const money = value => {
    try { return new Intl.NumberFormat(language === 'tr' ? 'tr-TR' : language === 'mk' ? 'mk-MK' : `${language}-${language.toUpperCase()}`, { style: 'currency', currency: plan.currency || 'TRY' }).format(value); }
    catch { return `${Number(value).toLocaleString()} ${plan.currency || 'TRY'}`; }
  };
  const slugify = value => {
    const words = value.trim().toLocaleLowerCase('tr-TR')
      .replaceAll('ı', 'i').replaceAll('ğ', 'g').replaceAll('ü', 'u').replaceAll('ş', 's').replaceAll('ö', 'o').replaceAll('ç', 'c')
      .normalize('NFD').replace(/[\u0300-\u036f]/g, '').match(/[a-z0-9]+/g) || [];
    const legalSuffixes = [
      ['anonim', 'sirketi'], ['anonim', 'sirket'], ['limited', 'sirketi'], ['limited', 'sirket'],
      ['ltd', 'sti'], ['ltd', 'st'], ['a', 's'], ['as'], ['ltd'], ['sti'], ['limited'], ['anonim'], ['sirketi'], ['sirket'], ['firmasi'], ['firma']
    ];
    let removedSuffix;
    do {
      removedSuffix = false;
      for (const suffix of legalSuffixes) {
        if (suffix.every((word, index) => words[words.length - suffix.length + index] === word)) {
          words.splice(-suffix.length);
          removedSuffix = true;
          break;
        }
      }
    } while (removedSuffix);
    if (words[0] === 'firma' || words[0] === 'sirket') words.shift();
    return words.join('-').slice(0, 63).replace(/-+$/g, '');
  };
  const attemptKey = `envantertr-checkout-attempt:${planId}`;
  const currentAttempt = () => {
    try {
      let id = sessionStorage.getItem(attemptKey);
      if (!id) { id = crypto.randomUUID(); sessionStorage.setItem(attemptKey, id); }
      return id;
    } catch { return crypto.randomUUID(); }
  };
  const clearAttempt = () => { try { sessionStorage.removeItem(attemptKey); } catch { /* Session storage may be unavailable. */ } };
  const selectedPrice = () => Number(term === 'annual' ? plan.annualPrice : plan.monthlyPrice);

  function refreshPrice() {
    const hasMonthly = Number(plan.monthlyPrice) > 0;
    const hasAnnual = Number(plan.annualPrice) > 0;
    $('#monthlyTerm').hidden = !hasMonthly;
    $('#annualTerm').hidden = !hasAnnual;
    $('#monthlyTerm').setAttribute('aria-pressed', String(term === 'monthly'));
    $('#annualTerm').setAttribute('aria-pressed', String(term === 'annual'));
    const amount = selectedPrice();
    $('#price').textContent = amount > 0 ? money(amount) : '—';
    $('#termCaption').textContent = term === 'annual' ? copy.annualPeriod : copy.monthlyPeriod;
  }

  async function initialize() {
    if (!planId) { setNotice(copy.missingPlan, 'error'); return; }
    try {
      const [plansResponse, statusResponse] = await Promise.all([
        fetch('/api/plans', { headers: { Accept: 'application/json' }, cache: 'no-store' }),
        fetch('/api/checkout/status', { headers: { Accept: 'application/json' }, cache: 'no-store' })
      ]);
      if (!plansResponse.ok || !statusResponse.ok) throw new Error(copy.loadError);
      const [plans, status] = await Promise.all([plansResponse.json(), statusResponse.json()]);
      plan = Array.isArray(plans) ? plans.find(item => item.id === planId && item.deploymentKind === 'cloud') : null;
      paymentAvailable = status.available === true;
      if (!plan || !(Number(plan.monthlyPrice) > 0 || Number(plan.annualPrice) > 0)) {
        setNotice(copy.invalidPlan, 'error'); return;
      }
      term = Number(plan.annualPrice) > 0 ? 'annual' : 'monthly';
      $('#planName').textContent = plan.name;
      $('#planDescription').textContent = plan.description || copy.unlimited;
      refreshPrice();
      $('#checkoutLayout').classList.remove('hidden');
      if (!paymentAvailable) {
        setNotice(status.message || copy.unavailable, 'error');
        $('#checkoutFields').disabled = true;
        $('#checkoutSubmit').disabled = true;
        const link = document.createElement('a');
        link.href = '/#demo';
        link.textContent = copy.demoLink;
        link.style.cssText = 'display:inline-block;margin-left:8px;font-weight:700';
        notice.append(' ', link);
        return;
      }
      fields.disabled = false;
      submit.disabled = false;
      setNotice(language === 'tr' ? 'Plan fiyatı sunucudan yüklendi. Form gönderilmeden ödeme yapılandırması ve paket geçerliliği kontrol edildi.' : 'The server loaded the plan price and verified checkout availability before collecting or sending your details.', 'success');
    } catch (error) {
      setNotice(error.message || copy.loadError, 'error');
    }
  }

  $('#monthlyTerm').addEventListener('click', () => { if (Number(plan?.monthlyPrice) > 0) { term = 'monthly'; refreshPrice(); } });
  $('#annualTerm').addEventListener('click', () => { if (Number(plan?.annualPrice) > 0) { term = 'annual'; refreshPrice(); } });
  form.elements.companyName.addEventListener('input', event => {
    if (!slugWasEdited) form.elements.slug.value = slugify(event.currentTarget.value);
  });
  form.elements.slug.addEventListener('input', () => { slugWasEdited = true; });
  $('#newAttempt').addEventListener('click', () => {
    clearAttempt();
    newAttempt.classList.remove('show');
    feedback.textContent = copy.freshAttemptReady;
    form.elements.companyName.focus();
  });

  form.addEventListener('submit', async event => {
    event.preventDefault();
    feedback.textContent = '';
    newAttempt.classList.remove('show');
    if (!paymentAvailable || !plan || selectedPrice() <= 0) return;
    if (!form.reportValidity()) { feedback.textContent = copy.fillFields; return; }

    submit.disabled = true;
    submit.textContent = copy.submitting;
    const data = new FormData(form);
    const payload = {
      checkoutId: currentAttempt(),
      companyName: String(data.get('companyName')).trim(),
      slug: String(data.get('slug')).trim().toLowerCase(),
      contactEmail: String(data.get('ownerEmail')).trim(),
      ownerFirstName: String(data.get('ownerFirstName')).trim(),
      ownerLastName: String(data.get('ownerLastName')).trim(),
      ownerEmail: String(data.get('ownerEmail')).trim(),
      phone: String(data.get('phone')).replace(/[\s()-]/g, ''),
      identityNumber: String(data.get('identityNumber')).replace(/\s/g, ''),
      billingAddress: String(data.get('billingAddress')).trim(),
      city: String(data.get('city')).trim(),
      country: String(data.get('country')).trim(),
      zipCode: String(data.get('zipCode')).trim() || null,
      planId: plan.id,
      term,
      dataProcessingAcknowledged: data.get('dataProcessingAcknowledged') === 'on',
      dataProcessingNoticeVersion: noticeVersion
    };

    try {
      const csrfResponse = await fetch('/api/session/csrf', { credentials: 'same-origin', cache: 'no-store' });
      if (!csrfResponse.ok) throw new Error(copy.loadError);
      const csrf = await csrfResponse.json();
      const response = await fetch('/api/checkout/initialize', {
        method: 'POST', credentials: 'same-origin', cache: 'no-store',
        headers: { Accept: 'application/json', 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf.requestToken },
        body: JSON.stringify(payload)
      });
      let result = {};
      try { result = await response.json(); } catch { /* Keep safe generic message. */ }
      if (!response.ok) {
        feedback.textContent = result.message || result.detail || (result.errors ? Object.values(result.errors).flat().join(' ') : copy.loadError);
        if (response.status === 409) {
          const initializing = (result.message || '').includes('hazırlanıyor');
          feedback.textContent = initializing ? copy.sameAttempt : feedback.textContent;
          newAttempt.classList.toggle('show', !initializing);
        } else if (response.status >= 500) {
          newAttempt.classList.add('show');
        }
        return;
      }
      const checkoutUrl = new URL(result.checkoutUrl || '', location.origin);
      if (checkoutUrl.protocol !== 'https:' || !['api.iyzipay.com', 'sandbox-api.iyzipay.com'].includes(checkoutUrl.hostname)) {
        feedback.textContent = copy.unsafeUrl;
        newAttempt.classList.add('show');
        return;
      }
      location.assign(checkoutUrl.href);
    } catch (error) {
      feedback.textContent = error.message || copy.loadError;
      newAttempt.classList.add('show');
    } finally {
      submit.disabled = !paymentAvailable;
      submit.textContent = copy.pay;
    }
  });

  initialize();
})();
