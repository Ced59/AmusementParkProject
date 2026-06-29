# Improve SEO — roadmap and search-engine exports

This folder groups the SEO roadmap and all search-engine export data used to build it.

## Structure

- `roadmap.html` — responsive HTML roadmap and conclusions.
- `google-search-console/` — normalized text exports from Google Search Console XLSX files.
- `bing-webmaster-tools/` — Bing Webmaster Tools / IndexNow CSV exports and pasted sitemap export.
- `yandex-webmaster/` — Yandex Webmaster duplicate titles/descriptions report and related exported tables.

## Notes for Codex

The original Google Search Console XLSX files were converted to Markdown files with CSV code blocks, one section per worksheet. This makes the data easier to diff, search and parse.

The Bing CSV files are preserved as CSV text when possible. Empty gzip CSV exports are documented in `bing-webmaster-tools/empty-gzip-csv-exports.md`.

The Yandex duplicate title/description report was pasted manually by the user and normalized into `yandex-webmaster/yandex-duplicate-titles-descriptions.md`.

## Main development priorities from the exports

1. Generate unique localized SEO titles and descriptions per route type.
2. Clean sitemap/canonical/noindex consistency.
3. Fix Googlebot 404/5xx/robots availability issues.
4. Reduce JavaScript-heavy crawl through better SSR/HTML/caching.
5. Use IndexNow selectively instead of bulk-style resubmission.
6. Optimize long-tail clusters already visible in GSC: Bellewaerde weather/photos, F.L.Y. specs, Mirapolis address, Aatapi weather.
