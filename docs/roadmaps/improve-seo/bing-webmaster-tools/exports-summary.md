# Bing Webmaster Tools / IndexNow exports — 2026-06-29

This file consolidates the Bing Webmaster Tools and IndexNow export data provided by the user on 2026-06-29.

## Original files and pasted data

| Original export | Main content |
|---|---|
| `amusement-parks.fun_AIPerformanceOverviewStats_6_29_2026.csv` | AI/search performance overview export. |
| `amusement-parks.fun_IndexNowIndexCoverageDatestats_6_29_2026.csv` | IndexNow coverage date stats. |
| `amusement-parks.fun_IndexNowIndexCoverageIssues_6_29_2026.csv` | IndexNow coverage issues, mostly empty. |
| `amusement-parks.fun_IndexNowSubmittedUrls_6_29_2026.csv` | Submitted IndexNow URLs. |
| `amusement-parks.fun_SearchPerformanceOverview_All_6_29_2026.csv` | Bing search performance overview. |
| `amusement-parks.fun_SiteExplorerUrls_6_29_2026.csv` | Site Explorer URL export. |
| `amusement-parks.fun_SubmittedUrls_6_29_2026.csv` | Submitted URLs export. |
| `amusement-parks.fun_sitemaps_6_29_2026.csv` | Bing sitemap export. |
| pasted sitemap table | Manually added sitemaps and sitemaps discovered in robots.txt. |

## Global conclusion

Bing receives the sitemap structure and IndexNow submissions, but visibility is still very weak. The SEO strategy should not be “submit more”; it should be “submit cleaner, more stable, canonical, high-value URLs”.

## Sitemap signals from the pasted Bing table

The user pasted a Bing table showing many sitemap files in status `ok`.

Examples:

| Sitemap | Status | Last loaded | URLs |
|---|---|---:|---:|
| `https://amusement-parks.fun/sitemap.xml` | ok | 2026-06-29 00:40 | 80 |
| `park-images-de.xml` | ok | 2026-06-28 10:02 | 92 |
| `park-images-en.xml` | ok | 2026-06-28 13:45 | 92 |
| `park-item-images-fr.xml` | ok | 2026-06-29 00:25 | 1,065 |
| `park-items-en.xml` | ok | 2026-06-29 05:17 | 4,758 |
| `parks-en.xml` | ok | 2026-06-22 13:15 | 3,512 |
| `references-fr.xml` | ok | 2026-06-26 18:26 | 430 |
| `static-fr.xml` | ok | 2026-06-28 08:12 | 7 |
| `technical-pages-fr.xml` | ok | 2026-06-27 07:29 | 2 |

## Important sitemap quality issues to keep in mind

Some sitemaps were listed with 0 links in the pasted/exported data at some points:

| Sitemap | URLs in file | Risk |
|---|---:|---|
| `park-items-it.xml` | 0 | Empty sitemap; should not be exposed unless deliberately temporary. |
| `park-items-pl.xml` | 0 | Empty sitemap; same risk. |
| `parks-nl.xml` | 0 | Empty sitemap; same risk. |

## IndexNow signal

The IndexNow export indicates a very high submitted URL volume, around 183k submitted URLs in the consolidated analysis.

### Interpretation

- IndexNow is working as a submission channel.
- Submission volume alone does not create indexing.
- Over-submission can make the signal noisy.
- Bing still needs canonical, stable, high-value pages.

## Recommended Bing-specific roadmap

| Priority | Action | Reason |
|---|---|---|
| P0 | Remove noindex, redirected and non-canonical URLs from sitemap. | Bing should not be asked to crawl contradictory URLs. |
| P0 | Stop exposing empty sitemaps or mark them as intentionally empty only if unavoidable. | Empty sitemap files are low-value crawl signals. |
| P1 | Make IndexNow selective. | Submit only create/update/delete events for meaningful public URLs. |
| P1 | Prioritize pages already proven in GSC: Bellewaerde, weather, photos, F.L.Y. specs. | Bing should receive the strongest pages first. |
| P2 | Add sitemap health admin panel. | Codex/developer should see URL counts, last generation, errors and regenerated state. |
| P2 | Keep sitemap index stable. | Bing is less forgiving when submitted sitemap structure changes too often. |

## Codex implementation notes

1. Create a single source of truth for public indexability.
2. Reuse it for sitemap generation, robots/noindex decisions and IndexNow submissions.
3. Add logs for IndexNow submissions: URL, reason, entity type, timestamp, response.
4. Add a guard against bulk re-submitting unchanged URLs.
5. Add tests that generated sitemap URLs are canonical and return 200.
