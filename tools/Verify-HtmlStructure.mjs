// index.html ve platform-admin.html işaretlemesinde açılış/kapanış etiketlerinin dengeli olduğunu doğrular.
// Neden: tek bir yanlış "</div>" ana ".app" kapsayıcısını kapatıp sonraki 14 sayfayı düzenin dışına itmiş,
// bu sayfalar boş ekran olarak görünmüştü (2026-11-07). Ayrıca her ".workspace-page" bölümünün <main> içinde kalması zorunludur.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const files = ['src/EnvanterTR.Web/wwwroot/index.html', 'src/EnvanterTR.Web/wwwroot/platform-admin.html'];
const voidTags = new Set(['area', 'base', 'br', 'col', 'embed', 'hr', 'img', 'input', 'link', 'meta', 'source', 'track', 'wbr']);
const failures = [];

for (const relative of files) {
  const full = path.join(root, relative);
  if (!fs.existsSync(full)) continue;
  const source = fs.readFileSync(full, 'utf8').replace(/\r\n/g, '\n');
  // Referans ürün adı yayımlanan arayüzde (yorumlar dahil) geçemez; güvenlik kabul testi de bunu denetler.
  if (new RegExp(['snipe', '-?it'].join(''), 'i').test(source)) failures.push(`${relative}: referans ürün adı geçiyor (yorumlar dahil kaldırın)`);
  const bodyStart = source.indexOf('<body'), bodyEnd = source.lastIndexOf('</body>');
  if (bodyStart < 0 || bodyEnd < 0) { failures.push(`${relative}: <body> bulunamadı`); continue; }
  const blank = match => match.replace(/[^\n]/g, ' ');
  const body = source.slice(bodyStart, bodyEnd)
    .replace(/<script\b[^>]*>[\s\S]*?<\/script>/gi, blank)
    .replace(/<style\b[^>]*>[\s\S]*?<\/style>/gi, blank)
    .replace(/<!--[\s\S]*?-->/g, blank);
  const lineBase = source.slice(0, bodyStart).split('\n').length;
  const lineAt = index => lineBase + body.slice(0, index).split('\n').length - 1;
  const stack = [];
  const tagPattern = /<(\/?)([a-zA-Z][a-zA-Z0-9-]*)\b(?:[^>"']|"[^"]*"|'[^']*')*?(\/?)>/g;
  for (const match of body.matchAll(tagPattern)) {
    const [, closing, rawTag, selfClosing] = match; const tag = rawTag.toLowerCase();
    if (voidTags.has(tag) || selfClosing) continue;
    if (!closing) {
      const id = /\bid="([^"]+)"/.exec(match[0])?.[1] || '';
      const isPage = /class="[^"]*\bworkspace-page\b/.test(match[0]);
      if (isPage && !stack.some(entry => entry.tag === 'main')) failures.push(`${relative}:${lineAt(match.index)} #${id} sayfası <main> dışında`);
      stack.push({ tag, line: lineAt(match.index) });
      continue;
    }
    if (stack.length && stack.at(-1).tag === tag) { stack.pop(); continue; }
    const open = stack.slice(-3).map(entry => `<${entry.tag}>@${entry.line}`).join(' ');
    failures.push(`${relative}:${lineAt(match.index)} beklenmeyen </${tag}> (açık: ${open || 'yok'})`);
    const index = stack.map(entry => entry.tag).lastIndexOf(tag);
    if (index >= 0) stack.splice(index);
  }
  const leftovers = stack.filter(entry => entry.tag !== 'body');
  for (const entry of leftovers.slice(0, 5)) failures.push(`${relative}:${entry.line} kapanmamış <${entry.tag}>`);
}

if (failures.length) {
  console.error(`HTML yapı doğrulaması başarısız (${failures.length}):\n- ${failures.slice(0, 25).join('\n- ')}`);
  process.exit(1);
}
console.log('HTML yapı doğrulaması geçti: etiketler dengeli, tüm çalışma sayfaları <main> içinde.');
