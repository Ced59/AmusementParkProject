# Google Search Console exports — 2026-06-29

This file consolidates the Google Search Console data exported by the user on 2026-06-29.

## Original files

| Original export | Main content |
|---|---|
| `amusement-parks.fun-Performance-on-Search-2026-06-29.xlsx` | Search performance: dates, queries, pages, countries, devices, filters. |
| `amusement-parks.fun-Coverage-2026-06-29.xlsx` | Indexing coverage graph and issue tables. |
| `amusement-parks.fun-Https-2026-06-29.xlsx` | HTTPS report. |
| `amusement-parks.fun-Breadcrumbs-2026-06-29.xlsx` | Breadcrumb enhancement report. |
| `amusement-parks.fun-More sample links-2026-06-29.xlsx` | External link sample export, empty except headers. |
| `amusement-parks.fun-Latest links-2026-06-29.xlsx` | Latest external link export, empty except headers. |
| `amusement-parks.fun-Crawl-stats-2026-06-29.xlsx` | Crawl stats: requests, bytes, response time, hosts, responses, file types, purposes, Googlebot types. |

## Performance overview

| Metric | Value |
|---|---:|
| Period with useful data | 2026-06-07 to 2026-06-27 |
| Clicks | 1 |
| Impressions | 356 |
| CTR | 0.28% |
| Weighted average position | 39.56 |
| Visible query rows | 105 |
| Visible query impressions | 288 |
| Hidden/anonymized impressions | 68 |

## First Google click

| Dimension | Value |
|---|---|
| Date | 2026-06-27 |
| Country | Belgium |
| Device | Tablet |
| Page | `https://amusement-parks.fun/en/park/c31436aa-3df2-42c1-b983-01d820ba1fa1/bellewaerde` |
| Query | Hidden/anonymized by Google Search Console |

## Weekly trend

| Week | Clicks | Impressions | Average position |
|---|---:|---:|---:|
| 2026-06-07 to 2026-06-13 | 0 | 18 | 27.82 |
| 2026-06-14 to 2026-06-20 | 0 | 110 | 47.24 |
| 2026-06-21 to 2026-06-27 | 1 | 228 | 36.78 |

## Main query opportunities

| Query | Impressions | Average position | Interpretation |
|---|---:|---:|---|
| `météo bellewaerde` | 11 | 9.82 | Very strong long-tail opportunity. |
| `bellewaerde weather` | 7 | 6.71 | Very strong English weather opportunity. |
| `photos de bellewaerde` | 6 | 10.33 | Photos pages are already promising. |
| `aatapi wonderland weather` | 4 | 7.00 | Weather pages can rank for small parks. |
| `astoria village` | 4 | 8.25 | Small/local park opportunity. |
| `mirapolis adresse` | 3 | 10.67 | Historical/address pages can rank. |
| `fly mindestgröße` | 2 | 7.50 | Attraction specs work in German. |
| `phantasialand fly geschwindigkeit` | 2 | 10.00 | F.L.Y. specs should be enriched. |

## Main page clusters

| Cluster | Impressions | Average position | Priority |
|---|---:|---:|---|
| Bellewaerde | 84 | 24.26 | P1: first real SEO winner. |
| Phantasialand | 136 | 56.01 | P1/P2: use attraction long-tail, not broad brand query first. |
| Aatapi Wonderland | 6 | 7.67 | P2: weather/parc detail opportunity. |
| Astoria Village | 12 | 9.42 | P2: local query opportunity. |
| Mirapolis | 11 | 11.64 | P2: historical/address opportunity. |

## Index coverage

| Last exported coverage point | Non-indexed | Indexed | Impressions |
|---|---:|---:|---:|
| 2026-06-12 | 29 | 42 | 8 |

Critical exported issues:

| Issue | Source | Validation | Pages | Priority |
|---|---|---|---:|---|
| Crawled, currently not indexed | Google systems | Started | 20 | P0/P1: classify exact URLs. |
| Page with redirect | Website | Not started | 7 | P0: remove from sitemap or fix canonical. |
| Excluded by `noindex` tag | Website | Not started | 1 | P0: verify if intended. |
| Duplicate, Google chose different canonical | Google systems | Not started | 1 | P0: inspect canonical/hreflang. |

## HTTPS

| Last exported point | Non-HTTPS URLs | HTTPS URLs |
|---|---:|---:|
| 2026-06-29 | 0 | 32 |

Conclusion: HTTPS is clean in the export.

## Breadcrumbs

| Last exported point | Invalid | Valid |
|---|---:|---:|
| 2026-06-28 | 0 | 9 |

Conclusion: breadcrumbs are valid in the export.

## Links

Both exported link workbooks only contain headers. GSC does not expose significant external backlinks yet.

## Crawl stats

| Metric | Value |
|---|---:|
| Period | 2026-06-08 to 2026-06-27 |
| Total crawl requests | 3,946 |
| Total downloaded | 143.1 MB |
| Average requests/day | 197 |
| Weighted average response time | 1,278 ms |
| Biggest crawl day | 2026-06-16, 601 requests |
| Worst response time day | 2026-06-14, 6,010 ms |

### Crawl responses

| Response | Share | Approx. requests | Priority |
|---|---:|---:|---|
| 200 OK | 96.63% | ~3,813 | Good. |
| 404 Not found | 2.74% | ~108 | P0: export examples and fix. |
| 301 Permanent redirect | 0.38% | ~15 | Verify sitemap does not include redirected URLs. |
| robots.txt unavailable | 0.18% | ~7 | P0: must be stable. |
| 5xx server error | 0.08% | ~3 | P0: identify in logs. |

### Crawled file types

| File type | Share | Approx. requests | Interpretation |
|---|---:|---:|---|
| JavaScript | 71.95% | ~2,839 | Too much crawl budget spent on JS. |
| HTML | 17.44% | ~688 | Public HTML must be useful and indexable. |
| JSON | 4.82% | ~190 | API/JSON is visible in crawl activity. |
| CSS | 1.50% | ~59 | Normal. |
| XML | 0.53% | ~21 | Sitemap crawl visible. |
| Image | 0.41% | ~16 | Image crawl still low. |

## Development consequences

1. Export exact GSC sample URLs for 404, 5xx, robots unavailable and crawled-not-indexed.
2. Remove redirected/noindex/non-canonical URLs from sitemaps.
3. Optimize Bellewaerde first: park detail, weather, photos, internal links.
4. Enrich attraction specs pages such as F.L.Y. with minimum height, speed, manufacturer, year, type and photos.
5. Reduce JavaScript-heavy crawl through SSR, useful initial HTML, cache headers and smaller public bundles.
