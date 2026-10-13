(() => {
  'use strict';

  const dialog = document.querySelector('#demoRequestDialog');
  const form = document.querySelector('#demoRequestForm');
  if (!dialog || !form) return;

  const copy = {
    tr: { eyebrow: 'ENVANTERTR · TANITIM', title: 'Size özel ürün tanıtımı', intro: 'İhtiyacınızı yazın; ekibimiz sizinle iletişime geçsin.', company: 'Şirket adı', contact: 'Yetkili adı soyadı', job: 'Görevi', phone: 'Telefon', email: 'İş e-postası', team: 'Ekip büyüklüğü', assets: 'Yaklaşık envanter sayısı', message: 'İhtiyacınız', placeholder: 'Takip etmek istediğiniz varlıkları veya ekip akışını kısaca anlatın.', select: 'Seçin', omit: 'Belirtmek istemiyorum', consent: 'Bu demo talebi için paylaştığım bilgilerle benimle iletişime geçilmesini kabul ediyorum.', cancel: 'Vazgeç', submit: 'Tanıtım isteği gönder', close: 'Kapat', sending: 'Talebiniz gönderiliyor…', success: 'Talebiniz alındı. Ekibimiz sizinle iletişime geçecek.', error: 'Talep gönderilemedi. Biraz sonra yeniden deneyin.', teams: ['1-10', '11-50', '51-200', '201-1000', '1000+'], assetsOptions: ['1-50', '51-250', '251-1000', '1001-5000', '5000+'] },
    en: { eyebrow: 'ENVANTERTR · PRODUCT TOUR', title: 'A product tour for your team', intro: 'Tell us what you need and our team will get in touch.', company: 'Company name', contact: 'Full name', job: 'Job title', phone: 'Phone', email: 'Work email', team: 'Team size', assets: 'Approximate inventory size', message: 'What would you like to manage?', placeholder: 'Briefly describe the assets or workflows you want to track.', select: 'Select', omit: 'Prefer not to say', consent: 'I agree to be contacted about this product tour using the information I provide.', cancel: 'Cancel', submit: 'Request a product tour', close: 'Close', sending: 'Sending your request…', success: 'Your request was received. Our team will contact you.', error: 'We could not send your request. Please try again shortly.', teams: ['1–10', '11–50', '51–200', '201–1,000', '1,000+'], assetsOptions: ['1–50', '51–250', '251–1,000', '1,001–5,000', '5,000+'] },
    ru: { eyebrow: 'ENVANTERTR · ПРЕЗЕНТАЦИЯ', title: 'Презентация для вашей команды', intro: 'Опишите задачу, и наша команда свяжется с вами.', company: 'Название компании', contact: 'Имя и фамилия', job: 'Должность', phone: 'Телефон', email: 'Рабочая почта', team: 'Размер команды', assets: 'Примерный размер инвентаря', message: 'Что вы хотите учитывать?', placeholder: 'Кратко опишите активы или процессы, которые хотите отслеживать.', select: 'Выберите', omit: 'Не хочу указывать', consent: 'Согласен(на), чтобы со мной связались по поводу презентации, используя предоставленные данные.', cancel: 'Отмена', submit: 'Запросить презентацию', close: 'Закрыть', sending: 'Отправляем запрос…', success: 'Запрос получен. Наша команда свяжется с вами.', error: 'Не удалось отправить запрос. Попробуйте позже.', teams: ['1–10', '11–50', '51–200', '201–1 000', '1 000+'], assetsOptions: ['1–50', '51–250', '251–1 000', '1 001–5 000', '5 000+'] },
    uk: { eyebrow: 'ENVANTERTR · ПРЕЗЕНТАЦІЯ', title: 'Презентація для вашої команди', intro: 'Опишіть потребу, і наша команда зв’яжеться з вами.', company: 'Назва компанії', contact: 'Ім’я та прізвище', job: 'Посада', phone: 'Телефон', email: 'Робоча пошта', team: 'Розмір команди', assets: 'Приблизний розмір інвентарю', message: 'Що потрібно обліковувати?', placeholder: 'Коротко опишіть активи або процеси, які хочете відстежувати.', select: 'Оберіть', omit: 'Не бажаю вказувати', consent: 'Погоджуюся, щоб зі мною зв’язалися щодо презентації за наданими мною даними.', cancel: 'Скасувати', submit: 'Замовити презентацію', close: 'Закрити', sending: 'Надсилаємо запит…', success: 'Запит отримано. Наша команда зв’яжеться з вами.', error: 'Не вдалося надіслати запит. Спробуйте пізніше.', teams: ['1–10', '11–50', '51–200', '201–1 000', '1 000+'], assetsOptions: ['1–50', '51–250', '251–1 000', '1 001–5 000', '5 000+'] },
    mk: { eyebrow: 'ENVANTERTR · ПРЕЗЕНТАЦИЈА', title: 'Презентација за вашиот тим', intro: 'Опишете што ви треба и нашиот тим ќе ве контактира.', company: 'Име на компанијата', contact: 'Име и презиме', job: 'Работно место', phone: 'Телефон', email: 'Работна е-пошта', team: 'Големина на тимот', assets: 'Приближна големина на инвентарот', message: 'Што сакате да евидентирате?', placeholder: 'Накратко опишете ги средствата или процесите што сакате да ги следите.', select: 'Изберете', omit: 'Не сакам да наведам', consent: 'Се согласувам да ме контактираат за оваа презентација преку податоците што ги доставив.', cancel: 'Откажи', submit: 'Побарај презентација', close: 'Затвори', sending: 'Барањето се испраќа…', success: 'Барањето е примено. Нашиот тим ќе ве контактира.', error: 'Барањето не може да се испрати. Обидете се повторно подоцна.', teams: ['1–10', '11–50', '51–200', '201–1.000', '1.000+'], assetsOptions: ['1–50', '51–250', '251–1.000', '1.001–5.000', '5.000+'] }
  };

  const status = document.querySelector('#demoRequestFeedback');
  const submit = document.querySelector('#demoRequestSubmit');
  const setLabel = (id, text) => {
    const label = document.getElementById(id);
    if (label?.firstChild) label.firstChild.nodeValue = `${text}\n`;
  };

  function updateLanguage(language) {
    const t = copy[language] || copy.tr;
    document.querySelector('#demoRequestEyebrow').textContent = t.eyebrow;
    document.querySelector('#demoRequestTitle').textContent = t.title;
    document.querySelector('#demoRequestIntro').textContent = t.intro;
    setLabel('demoCompanyLabel', t.company); setLabel('demoContactLabel', t.contact);
    setLabel('demoJobLabel', t.job); setLabel('demoPhoneLabel', t.phone);
    setLabel('demoEmailLabel', t.email); setLabel('demoTeamLabel', t.team);
    setLabel('demoAssetsLabel', t.assets); setLabel('demoMessageLabel', t.message);
    form.elements.message.placeholder = t.placeholder;
    form.elements.teamSize.options[0].textContent = t.select;
    [...form.elements.teamSize.options].slice(1).forEach((option, index) => { option.textContent = t.teams[index]; });
    form.elements.assetCount.options[0].textContent = t.omit;
    [...form.elements.assetCount.options].slice(1).forEach((option, index) => { option.textContent = t.assetsOptions[index]; });
    document.querySelector('#demoConsentText').textContent = t.consent;
    document.querySelector('#demoRequestCancel').textContent = t.cancel;
    submit.textContent = t.submit;
    document.querySelector('#demoRequestClose').setAttribute('aria-label', t.close);
  }

  document.querySelectorAll('[data-open-demo-request]').forEach((trigger) => trigger.addEventListener('click', (event) => {
    event.preventDefault();
    updateLanguage(document.querySelector('#homeLanguage')?.value || localStorage.getItem('envantertr-home-language') || 'tr');
    form.reset(); status.textContent = ''; status.classList.remove('success'); submit.disabled = false;
    dialog.showModal(); form.elements.companyName.focus();
  }));
  document.querySelector('#demoRequestClose').addEventListener('click', () => dialog.close());
  document.querySelector('#demoRequestCancel').addEventListener('click', () => dialog.close());
  dialog.addEventListener('click', (event) => { if (event.target === dialog) dialog.close(); });
  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    const t = copy[document.querySelector('#homeLanguage')?.value] || copy.tr;
    submit.disabled = true; status.textContent = t.sending; status.classList.remove('success');
    try {
      const response = await fetch('/api/demo-requests', {
        method: 'POST', credentials: 'same-origin',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        body: JSON.stringify({
          companyName: form.elements.companyName.value,
          contactName: form.elements.contactName.value,
          email: form.elements.email.value,
          phone: form.elements.phone.value || null,
          jobTitle: form.elements.jobTitle.value || null,
          teamSize: form.elements.teamSize.value,
          assetCount: form.elements.assetCount.value || null,
          message: form.elements.message.value || null,
          consent: form.elements.consent.checked,
          website: form.elements.website.value || null
        })
      });
      if (!response.ok) {
        let message = t.error;
        try { const body = await response.json(); message = body.message || body.title || message; } catch { /* Keep a localized message. */ }
        throw new Error(message);
      }
      status.textContent = t.success; status.classList.add('success'); form.reset();
      window.setTimeout(() => { if (dialog.open) dialog.close(); }, 1800);
    } catch (error) { status.textContent = error.message || t.error; }
    finally { submit.disabled = false; }
  });
  updateLanguage(document.querySelector('#homeLanguage')?.value || 'tr');
})();
