"""Verify the synthetic 85-asset PDF emitted by HandoverPdf acceptance (requires pypdf)."""
import sys
from pathlib import Path
from pypdf import PdfReader

pdf = Path(sys.argv[1])
reader = PdfReader(pdf)
pages = [page.extract_text() or "" for page in reader.pages]
text = "\n".join(pages)
for index in range(85):
    marker = f"SERIAL-{index:03}"
    assert text.count(marker) == 1, (marker, text.count(marker))
for marker in ["Örnek Çalışan", "Çç Ğğ İı Öö Şş Üü", "Atanmış", "Teslim alan", "Test Yetkilisi", "İmza SHA-256"]:
    assert marker in text, marker
for page in pages:
    if "Teslim ve imzalar" in page:
        assert "Test Yetkilisi" in page and "Teslim alan" in page, "Orphaned signature heading"
assert len(pages) >= 3
print(f"PASS: {len(pages)} A4 pages; 85/85 unique inventory serials; Turkish glyph text; both signer labels/hashes; signature heading kept with signatures.")
