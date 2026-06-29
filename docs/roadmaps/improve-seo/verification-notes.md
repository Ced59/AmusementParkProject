# SEO verification notes

Date: 2026-06-29

## Verified conclusions

- Duplicate public metadata was real. The code confirmed generic SEO fallback on park operator, founder and manufacturer detail routes, English-only zone metadata, and self-canonical URLs when an entity was reached through an old slug.
- Public critical routes are configured for SSR in `app.routes.server.ts`. Admin, auth, account and private areas remain client-rendered or noindex through route/default SEO handling. Public technical pages are editorial content and remain indexable.
- Static, media and zone sitemap providers already skip empty language sections or content-free image/video/zone pages. The current sitemap generation no longer exposes empty section XML files.
- Park item, zone, media and reference sitemap URLs are generated from current entity names and route slugs. The frontend now uses the same loaded entity data for canonical URLs.
- IndexNow already had a selective path through `PublicSeoUpdateNotifier`; the risky path was full sitemap generation submitting every generated public URL.

## Implemented decisions

- Added entity-driven canonical paths for park, weather, map, image, video, zone, item and reference pages.
- Added dedicated localized SEO metadata for public reference pages and park zone pages.
- Enriched attraction fallback descriptions with existing displayed specs only.
- Kept noindex behavior for filtered URLs, empty galleries, empty video lists, empty weather pages, maps, private/admin/account/auth/technical error routes.
- Added a guard so full sitemap generation does not trigger massive IndexNow submissions. Public content updates remain the selective IndexNow path.

## Deliberate non-changes

- Weather sitemap URLs are still generated for public parks. The frontend noindexes a weather page when the forecast is empty, but the sitemap layer does not currently have a bulk, reliable weather-availability signal. Excluding those URLs safely would require a dedicated weather repository query instead of per-park probing.
- No hreflang alternates were removed. The inspected public entity routes are served in the supported languages; video visibility is already language-filtered in sitemap and update resolvers.
- Googlebot 404/5xx URL-level fixes were not implemented because the available GSC exports contain aggregate issue tables, not URL samples. The raw exports in Downloads also did not expose exact failing URLs.
