import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import test from 'node:test';

const smokeUrl = new URL('./seo-ssr-smoke.mjs', import.meta.url).href;
const fullHtml = [
  '<html><head><title>Discover the park</title>',
  '<meta name="description" content="Discover the park, its history and its attractions.">',
  '<link rel="canonical" href="https://amusement-parks.fun/fr/parks">',
  '<style>.park{display:grid}</style>',
  '<script type="application/ld+json">{"@context":"https://schema.org"}</script>',
  '</head><body><app-root><main class="park">',
  'Discover the park and its attractions. '.repeat(20),
  '</main></app-root>',
  '<script id="ng-state" type="application/json">{}</script>',
  '<script src="main.js" type="module"></script>',
  '</body></html>',
].join('');
const strippedHtml = fullHtml
  .replace('<script id="ng-state" type="application/json">{}</script>', '')
  .replace('<script src="main.js" type="module"></script>', '')
  .replace('<style>.park{display:grid}</style>', '')
  .replace(' class="park"', '');

function runSmoke(html, userAgent) {
  // Mock fetch in a child process so the CLI is tested without starting a server.
  const code = `globalThis.fetch = async () => new Response(${JSON.stringify(html)}, { status: 200 }); await import(${JSON.stringify(smokeUrl)});`;
  return spawnSync(process.execPath, ['--input-type=module', '-e', code], {
    encoding: 'utf8',
    env: {
      ...process.env,
      PUBLIC_BASE_URL: 'https://amusement-parks.fun',
      SEO_SMOKE_PATHS: '/fr/parks',
      SEO_SMOKE_USER_AGENT: userAgent,
      SEO_SMOKE_MIN_BODY_TEXT_LENGTH: '500',
    },
  });
}

for (const userAgent of ['Googlebot/2.1', 'Google-InspectionTool/1.0']) {
  test(`${userAgent} accepts complete SSR and rejects stripped HTML`, () => {
    const complete = runSmoke(fullHtml, userAgent);
    const stripped = runSmoke(strippedHtml, userAgent);

    assert.equal(complete.status, 0, complete.stderr);
    assert.equal(stripped.status, 1);
    assert.match(stripped.stderr, /Google SSR executable scripts/);
    assert.match(stripped.stderr, /Google SSR Angular transfer state/);
    assert.match(stripped.stderr, /Google SSR styles/);
    assert.match(stripped.stderr, /Google SSR presentation classes/);
  });
}

test('Google cannot pass with JSON-LD and transfer state as its only scripts', () => {
  const result = runSmoke(fullHtml.replace('src="main.js"', ''), 'Googlebot/2.1');

  assert.equal(result.status, 1);
  assert.match(result.stderr, /Google SSR executable scripts/);
});

test('Bing keeps the existing no-JS contract', () => {
  const stripped = runSmoke(strippedHtml, 'bingbot/2.0');
  const complete = runSmoke(fullHtml, 'bingbot/2.0');

  assert.equal(stripped.status, 0, stripped.stderr);
  assert.equal(complete.status, 1);
  assert.match(complete.stderr, /executable script/);
});
