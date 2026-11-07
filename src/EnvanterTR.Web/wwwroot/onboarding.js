(() => {
  'use strict';

  const panel = document.getElementById('companyOnboarding');
  if (!panel) return;

  const copy = {
    tr: {
      heading: 'Şirket alanınızı hazırlayın',
      intro: 'Beş kısa adımda firma profilini, markanızı, başlangıç verinizi ve ekibinizi düzenleyin.',
      progress: (done, total) => `${done}/${total} tamamlandı`, open: 'Bölümü aç', done: 'Tamamlandı',
      titles: { company: 'Firma bilgileri', branding: 'Logo ve renk', import: 'İlk toplu veri aktarımı', members: 'Ekip üyelerini davet edin', firstAsset: 'İlk varlığı kaydedin' },
      descriptions: {
        company: 'Firma adı, e-posta ve şirket adresini gözden geçirin.',
        branding: 'Logo ekleyin veya markanızın ana rengini seçin.',
        import: 'Varlıklar bölümünden CSV ya da Excel başlangıç listenizi içe aktarın.',
        members: 'Ekibinizi uygun erişim rolüyle davet edin.',
        firstAsset: 'İlk cihazınızı veya şirket varlığınızı kaydedin.'
      }
    },
    en: {
      heading: 'Set up your company space',
      intro: 'Use these five steps to prepare your company profile, brand, starting data, and team.',
      progress: (done, total) => `${done}/${total} complete`, open: 'Open section', done: 'Complete',
      titles: { company: 'Company profile', branding: 'Logo and color', import: 'First bulk import', members: 'Invite teammates', firstAsset: 'Record your first asset' },
      descriptions: {
        company: 'Review the company name, email, and workspace address.',
        branding: 'Add a logo or choose your primary brand color.',
        import: 'Import your starting asset list from CSV or Excel in Assets.',
        members: 'Invite teammates with the appropriate access role.',
        firstAsset: 'Record your first device or company asset.'
      }
    },
    ru: {
      heading: 'Настройте пространство компании', intro: 'Подготовьте профиль, бренд, начальные данные и команду за пять шагов.',
      progress: (done, total) => `Готово: ${done}/${total}`, open: 'Открыть раздел', done: 'Готово',
      titles: { company: 'Профиль компании', branding: 'Логотип и цвет', import: 'Первый массовый импорт', members: 'Пригласите коллег', firstAsset: 'Добавьте первый актив' },
      descriptions: { company: 'Проверьте название, адрес электронной почты и адрес пространства.', branding: 'Добавьте логотип или выберите основной цвет бренда.', import: 'Импортируйте начальный список активов из CSV или Excel.', members: 'Пригласите коллег с подходящей ролью.', firstAsset: 'Добавьте первое устройство или актив компании.' }
    },
    uk: {
      heading: 'Налаштуйте простір компанії', intro: 'Підготуйте профіль, бренд, початкові дані та команду за п’ять кроків.',
      progress: (done, total) => `Готово: ${done}/${total}`, open: 'Відкрити розділ', done: 'Готово',
      titles: { company: 'Профіль компанії', branding: 'Логотип і колір', import: 'Перший масовий імпорт', members: 'Запросіть колег', firstAsset: 'Додайте перший актив' },
      descriptions: { company: 'Перевірте назву, електронну адресу та адресу простору.', branding: 'Додайте логотип або виберіть основний колір бренду.', import: 'Імпортуйте початковий список активів із CSV або Excel.', members: 'Запросіть колег із відповідною роллю.', firstAsset: 'Додайте перший пристрій або актив компанії.' }
    },
    mk: {
      heading: 'Поставете го просторот на компанијата', intro: 'Подгответе профил, бренд, почетни податоци и тим во пет чекори.',
      progress: (done, total) => `Завршено ${done}/${total}`, open: 'Отвори дел', done: 'Завршено',
      titles: { company: 'Профил на компанијата', branding: 'Лого и боја', import: 'Прв масовен увоз', members: 'Поканете колеги', firstAsset: 'Запишете го првото средство' },
      descriptions: { company: 'Проверете име, е-пошта и адреса на просторот.', branding: 'Додајте лого или изберете главна боја на брендот.', import: 'Увезете почетна листа на средства преку CSV или Excel.', members: 'Поканете колеги со соодветна улога.', firstAsset: 'Запишете го првиот уред или средство на компанијата.' }
    }
  };
  const text = copy[(document.documentElement.lang || 'tr').slice(0, 2)] || copy.tr;
  let loading = false;

  async function refresh() {
    if (loading || document.body.classList.contains('auth-required')) return;
    loading = true;
    try {
      const response = await fetch('/api/onboarding/status', { headers: { Accept: 'application/json' }, cache: 'no-store' });
      if (!response.ok) { panel.hidden = true; return; }
      const status = await response.json();
      panel.replaceChildren();
      if (status.visible !== true) { panel.hidden = true; return; }

      const header = document.createElement('div');
      header.className = 'onboarding-head';
      const headingGroup = document.createElement('div');
      const heading = document.createElement('h2');
      heading.id = 'onboardingTitle'; heading.textContent = text.heading;
      const intro = document.createElement('p'); intro.textContent = text.intro;
      headingGroup.append(heading, intro);
      const progress = document.createElement('span');
      progress.className = 'onboarding-progress'; progress.setAttribute('role', 'status');
      progress.textContent = text.progress(status.completedSteps, status.totalSteps);
      header.append(headingGroup, progress);

      const list = document.createElement('ol'); list.className = 'onboarding-list';
      for (const step of status.steps || []) {
        const item = document.createElement('li');
        item.className = `onboarding-step${step.complete ? ' is-complete' : ''}`;
        const stepTitle = text.titles[step.key] || step.title;
        const title = document.createElement('h3'); title.textContent = stepTitle;
        const description = document.createElement('p'); description.textContent = text.descriptions[step.key] || '';
        const button = document.createElement('button'); button.type = 'button';
        button.textContent = step.complete ? text.done : text.open;
        button.disabled = step.complete;
        button.setAttribute('aria-label', `${stepTitle}: ${button.textContent}`);
        button.addEventListener('click', () => {
          const navigation = [...document.querySelectorAll('#sidebar [data-page]')]
            .find(candidate => candidate.dataset.page === step.page && !candidate.hidden);
          if (navigation) navigation.click();
        });
        item.append(title, description, button);
        list.append(item);
      }
      panel.append(header, list);
      panel.hidden = false;
    } catch {
      panel.hidden = true;
    } finally {
      loading = false;
    }
  }

  const observer = new MutationObserver(() => {
    if (!document.body.classList.contains('auth-required')) void refresh();
  });
  observer.observe(document.body, { attributes: true, attributeFilter: ['class'] });
  if (!document.body.classList.contains('auth-required')) void refresh();
  document.addEventListener('visibilitychange', () => {
    if (!document.hidden && !document.body.classList.contains('auth-required')) void refresh();
  });
})();
