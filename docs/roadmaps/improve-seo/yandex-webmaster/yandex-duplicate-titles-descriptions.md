# Yandex Webmaster — duplicate titles and descriptions

Source: pasted manually by the user on 2026-06-29.

## Main findings

| Yandex signal | Volume | Share |
|---|---:|---:|
| Pages with the same title | 364 | 22% |
| Pages with the same description | 247 | 15% |

Yandex recommends using informative and appealing descriptions that reflect page content. For automatically generated titles, it recommends diversifying content with page number, product characteristics or other page-specific information. If GET parameters generate duplicates, use `Clean-param`. If different URL cases generate duplicates, add `rel=canonical` or 301 redirects. After corrections, wait for Yandex to recrawl or send the most important pages for reindexing.

## Examples from the Yandex report

### `Amusement Parks` — 32 such titles

- `/es/park-manufacturer/1b825443-d1d0-428a-8640-521cbf11e62a/ride-technic` — crawl date 06/20/2026
- `/pt/park-manufacturer/1b825443-d1d0-428a-8640-521cbf11e62a/ride-technic` — crawl date 06/20/2026
- `/pt/park-operator/29a241e0-8f95-498c-8c5a-9fef90bbeff7/circus-circus-las-vegas` — crawl date 06/20/2026

### `Al-Qidah Park — Amusement Parks` — 5 such titles

- `/es/park/3212fabf-2a98-4af0-9111-917461fccbb5/al-qidah-park` — crawl date 06/17/2026
- `/pt/park/3212fabf-2a98-4af0-9111-917461fccbb5/al-qidah-park` — crawl date 06/17/2026
- `/de/park/3212fabf-2a98-4af0-9111-917461fccbb5/al-qidah-park` — crawl date 06/16/2026

### `Universal Studios Beijing — China — Amusement Parks` — 4 such titles

- `/pt/park/f9b6635e-8b5a-4810-826a-97c873e9b19c/universal-studios-beijing` — crawl date 06/16/2026
- `/de/park/f9b6635e-8b5a-4810-826a-97c873e9b19c/universal-studios-beijing` — crawl date 06/16/2026
- `/es/park/f9b6635e-8b5a-4810-826a-97c873e9b19c/universal-studios-beijing` — crawl date 06/16/2026

### `Phantasialand zones — Amusement Parks` — 4 such titles

- `/es/park/50555bb2-abd4-4c3a-b0d2-b2fa5f547c6e/phantasialand/zones` — crawl date 06/21/2026
- `/de/park/50555bb2-abd4-4c3a-b0d2-b2fa5f547c6e/phantasialand/zones` — crawl date 06/21/2026
- `/en/park/50555bb2-abd4-4c3a-b0d2-b2fa5f547c6e/phantasialand/zones` — crawl date 06/21/2026

### Other examples mentioned

- `Beogradski Luna Park — Amusement Parks` — 4 such titles.
- `Babylon Park Tel Aviv — Amusement Parks` — 4 such titles.
- `TFG Amusement Park — Amusement Parks` — 3 such titles.
- `China Hiin City — China — Amusement Parks` — 3 such titles.
- `Rankings — Amusement Parks` — 3 such titles.
- `Monaco themed area at Europa-Park — Amusement Parks` — 3 such titles, including both `/monaco` and `/monaco-themed-area` examples.

## Development consequence

This export justifies a P0 task: implement unique SEO title and meta description generation per route type, language and entity type.

## Required SEO template changes

| Route type | Required differentiation |
|---|---|
| Park detail | Park name, city, country, language, key page intent. |
| Park weather | Park name, weather intent, visit preparation wording. |
| Park images | Park name, images/photos intent, gallery wording. |
| Park zones | Park name, zones/themed areas wording, count if available. |
| Park item detail | Item name, parent park, category, specs if available. |
| Manufacturer | Manufacturer name, type, known activity, page intent. |
| Operator | Operator name, role, associated parks if available. |
| Rankings | Ranking type, language, page scope. |

## Canonical and redirect implications

The `Monaco` examples indicate a possible duplicate slug/canonical problem. Codex should verify whether both URLs are intentional:

- `/zone/.../monaco`
- `/zone/.../monaco-themed-area`

Expected behavior:

1. Choose one canonical URL.
2. Redirect the other with 301, or at least canonicalize it.
3. Ensure only the canonical URL is present in sitemaps.
