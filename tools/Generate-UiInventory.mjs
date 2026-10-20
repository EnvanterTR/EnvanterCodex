import { readFile, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const sources = [
  ['Şirket uygulaması', 'src/EnvanterTR.Web/wwwroot/index.html'],
  ['Platform yönetimi', 'src/EnvanterTR.Web/wwwroot/platform-admin.html'],
  ['Platform kurulumu', 'src/EnvanterTR.Web/wwwroot/platform-setup.html'],
  ['Satış sitesi', 'src/EnvanterTR.PublicSite/index.html'],
  ['Ödeme sayfası', 'src/EnvanterTR.PublicSite/checkout.html']
];

const decode = value => value.replace(/&nbsp;/g, ' ').replace(/&amp;/g, '&').replace(/&lt;/g, '<')
  .replace(/&gt;/g, '>').replace(/&quot;/g, '"').replace(/&#39;|&apos;/g, "'").replace(/&#(\d+);/g, (_, n) => String.fromCodePoint(Number(n)));
const cleanText = value => decode(value.replace(/<script\b[^>]*>[\s\S]*?<\/script>/gi, ' ')
  .replace(/<style\b[^>]*>[\s\S]*?<\/style>/gi, ' ').replace(/<[^>]+>/g, ' '))
  .replace(/\s+/g, ' ').trim();
const attr = (markup, name) => markup.match(new RegExp(`\\b${name}\\s*=\\s*(?:"([^"]*)"|'([^']*)'|([^\\s>]+))`, 'i'))?.slice(1).find(Boolean) || '';
const esc = value => String(value ?? '').replaceAll('|', '\\|').replaceAll('\n', ' ');
const records = [];
const counts = new Map();

for (const [surface, relativePath] of sources) {
  const path = resolve(root, relativePath);
  let html;
  try { html = await readFile(path, 'utf8'); }
  catch { continue; }
  let ordinal = 0;
  const add = (kind, markup, inner = '') => {
    ordinal++;
    const id = attr(markup, 'id');
    const label = attr(markup, 'aria-label') || attr(markup, 'title') || attr(markup, 'data-page') ||
      attr(markup, 'data-group') || attr(markup, 'name') || cleanText(inner) || id || '(etiketsiz)';
    const target = attr(markup, 'href') || attr(markup, 'formaction') || attr(markup, 'data-page') || attr(markup, 'data-group') || '';
    const detail = [
      id && `id=${id}`, attr(markup, 'type') && `type=${attr(markup, 'type')}`,
      attr(markup, 'name') && `name=${attr(markup, 'name')}`, target && `hedef=${target}`,
      attr(markup, 'data-action') && `action=${attr(markup, 'data-action')}`,
      /\bhidden\b/.test(markup) && 'başlangıçta gizli'
    ].filter(Boolean).join('; ');
    const optionDetail = kind === 'Seçim alanı' && inner ? `seçenekler=${cleanText(inner)}` : '';
    records.push({ surface, path: relativePath, order: ordinal, kind, label: cleanText(label), detail: [detail, optionDetail].filter(Boolean).join('; ') });
    counts.set(kind, (counts.get(kind) || 0) + 1);
  };

  for (const match of html.matchAll(/<a\b([^>]*)>([\s\S]*?)<\/a\s*>/gi)) add('Bağlantı', match[0], match[2]);
  for (const match of html.matchAll(/<button\b([^>]*)>([\s\S]*?)<\/button\s*>/gi)) add('Düğme', match[0], match[2]);
  for (const match of html.matchAll(/<(input|select|textarea)\b[^>]*>/gi)) {
    const markup = match[0], tag = match[1].toLowerCase();
    const kind = tag === 'select' ? 'Seçim alanı' : tag === 'textarea' ? 'Metin alanı' : 'Girdi alanı';
    let inner = '';
    if (tag === 'select') {
      const close = html.indexOf('</select', match.index);
      const end = close < 0 ? match.index + markup.length : close;
      inner = [...html.slice(match.index, end).matchAll(/<option\b([^>]*)>([\s\S]*?)<\/option\s*>/gi)]
        .map(option => `${attr(option[0], 'value') || '(değer yok)'}: ${cleanText(option[2])}`).join(', ');
    }
    add(kind, markup, inner);
  }
  for (const match of html.matchAll(/<form\b[^>]*>/gi)) add('Form', match[0]);
  for (const match of html.matchAll(/<dialog\b[^>]*>([\s\S]*?)<\/dialog\s*>/gi)) add('Modal', match[0], match[1].match(/<h[1-6]\b[^>]*>([\s\S]*?)<\/h[1-6]>/i)?.[1] || '');
  for (const match of html.matchAll(/<table\b[^>]*>([\s\S]*?)<\/table\s*>/gi)) {
    const caption = match[1].match(/<caption\b[^>]*>([\s\S]*?)<\/caption>/i)?.[1] || '';
    const headers = [...match[1].matchAll(/<th\b[^>]*>([\s\S]*?)<\/th>/gi)].map(header => cleanText(header[1])).join(' / ');
    add('Tablo', match[0], caption || headers);
  }
  for (const match of html.matchAll(/<[^>]+\brole\s*=\s*["']tab["'][^>]*>/gi)) add('Sekme', match[0]);
  for (const match of html.matchAll(/<[^>]+\bdata-page\s*=\s*["'][^"']+["'][^>]*>/gi)) add('Sayfa rotası', match[0]);
  for (const match of html.matchAll(/<[^>]+\bdata-group\s*=\s*["'][^"']+["'][^>]*>/gi)) add('Menü grubu', match[0]);
}

const kindOrder = ['Sayfa rotası', 'Menü grubu', 'Bağlantı', 'Düğme', 'Form', 'Girdi alanı', 'Seçim alanı', 'Metin alanı', 'Modal', 'Sekme', 'Tablo'];
const lines = [
  '# EnvanterTR statik arayüz envanteri', '',
  `Oluşturulma: ${new Date().toISOString()}`, '',
  '> Bu dosya mevcut HTML kaynaklarında bulunan statik arayüz öğelerinin dökümüdür. Bir öğenin listelenmesi onun çalıştığını kanıtlamaz. JavaScript ile sonradan oluşturulan kontroller, API/DB davranışı, rol varyasyonları ve erişilebilirlik ayrıca uçtan uca kabul gerektirir. Kaynak değiştikçe `node tools/Generate-UiInventory.mjs` ile yeniden üretin; `node tools/Generate-UiInventory.mjs --check` güncel olup olmadığını doğrular.', '',
  '## Sayım', '', '| Tür | Statik öğe |', '|---|---:|',
  ...kindOrder.map(kind => `| ${kind} | ${counts.get(kind) || 0} |`),
  `| **Toplam kayıt** | **${records.length}** |`, '',
  '## Tam döküm', '',
  ...sources.filter(([, path]) => records.some(record => record.path === path)).flatMap(([surface, path]) => {
    const entries = records.filter(record => record.path === path);
    return [`### ${surface} — \`${path}\``, '', '| # | Tür | Etiket / görünen metin | Kaynak ipucu |', '|---:|---|---|---|',
      ...entries.map(record => `| ${record.order} | ${record.kind} | ${esc(record.label) || '(etiketsiz)'} | ${esc(record.detail)} |`), ''];
  }),
  '## Kapsam notları', '',
  '- `data-page` ve `data-group` kayıtları yönlendirme hedefi/menü grubu envanteridir; aynı HTML elemanı bağlantı veya düğme olarak ayrıca listelenebilir.',
  '- Form alanları için `id`, `name`, tür ve varsa select seçenekleri kaynak ipucu olarak kaydedilir. Etiket ilişkisi tek başına bu statik dökümle doğrulanmış sayılmaz.',
  '- Açılır menüler, koşullu alanlar ve JavaScript tarafından üretilen tablo satırı/eylemleri kaynakta statik değillerse bu sayımda yer almayabilir.',
  '- Fonksiyon kabul sonuçları `docs/UX-MENU-ACTION-AUDIT.md` ve `docs/USER-REQUEST-CHECKLIST-2026-10-11.md` içinde kanıt ve açık kalan kapsamla izlenir.', ''
];

const output = resolve(root, 'docs/ui-inventory.md');
const generated = lines.join('\n');
if (process.argv.includes('--check')) {
  let existing;
  try { existing = await readFile(output, 'utf8'); }
  catch { throw new Error(`Inventory is missing: ${output}. Run node tools/Generate-UiInventory.mjs first.`); }
  const normalizeTimestamp = value => value.replace(/^Oluşturulma: .*$/m, 'Oluşturulma: [zaman damgası]');
  if (normalizeTimestamp(existing) !== normalizeTimestamp(generated))
    throw new Error(`Inventory is stale: ${output}. Run node tools/Generate-UiInventory.mjs and commit the refreshed artifact.`);
  console.log(`Inventory matches current source: ${output}`);
} else {
  await writeFile(output, generated, 'utf8');
  console.log(`Wrote ${output}`);
}
console.log(`Static controls inventoried: ${records.length}`);
for (const kind of kindOrder) console.log(`${kind}: ${counts.get(kind) || 0}`);
