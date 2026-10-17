import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import { runInNewContext } from 'node:vm';
import { collectContrastCandidates } from '../design-v6/browser-text-contrast.mjs';
import { collectLayoutOverflow } from '../design-v6/browser-layout-overflow.mjs';
import { isCanceledInterceptionError, isRetiredDocumentInterceptionError, advanceMainDocument } from '../design-v6/browser-interception-errors.mjs';

const responsiveScript = await readFile(new URL('../responsive/trip-pages-browser-check.mjs', import.meta.url), 'utf8');
const designTokens = await readFile(new URL('../../../../docs/design-v6/V6-DESIGN-TOKENS.md', import.meta.url), 'utf8');
const httpScript = await readFile(new URL('../design-v6/verify-public-http.mjs', import.meta.url), 'utf8');
const baseStyles = await readFile(new URL('../../src/styles/_base.scss', import.meta.url), 'utf8');

test('enlarged text reflows shell tracks and preserves measured fixed-control clearance', async () => {
  const read = path => readFile(new URL(`../../src/${path}`, import.meta.url), 'utf8');
  const footer = await read('styles/_footer.scss');
  const nav = await read('styles/_navigation.scss');
  const profile = await read('app/features/profile/pages/profile-page/profile-page.component.scss');
  const jump = await read('app/shared/components/page-jump-button/page-jump-button.component.ts');
  assert.match(footer, /container-type: inline-size/);
  assert.match(footer, /font-size: min\(28\.8px, 10\.8cqi\)/);
  assert.doesNotMatch(footer, /minmax\((?:15|8)rem/);
  assert.match(nav, /padding-bottom: max\(5\.8rem, calc\(var\(--bottom-nav-height\) \+ 1rem\)\)/);
  assert.match(nav, /font-size: clamp\(20px, 7vw, 24\.8px\)/);
  assert.match(profile, /font-size: min\(clamp\(2\.4rem, 5vw, 4\.35rem\), 14vw\)/);
  assert.match(profile, /width: 120px;[\s\S]*?max-width: 100%/);
  assert.match(profile, /repeat\(auto-fit, minmax\(min\(100%, max\(8rem,/);
  assert.match(jump, /\['\.app-mobile-bottom-nav', '--bottom-nav-height'\]/);
  assert.match(jump, /\['app-admin-public-view-toolbar', '--app-admin-toolbar-height'\]/);
  assert.match(jump, /rootStyle\.getPropertyValue\(token\) !== value/);
  assert.match(jump, /style\.removeProperty\(token\)/);
  assert.match(jump, /resizeObserver\?\.unobserve\(previous\)/);
  assert.match(jump, /measuredControls\.clear\(\)/);
  for (const source of [footer, nav, profile]) assert.doesNotMatch(source, /overflow-x:\s*(?:hidden|clip)/);
});

test('fixed toolbar and jump control reserve their real stack and short screens keep locally scrollable controls', async () => {
  const read = path => readFile(new URL(`../../src/${path}`, import.meta.url), 'utf8');
  const toolbar = await read('app/features/admin/contextual-editing/ui/admin-public-view-toolbar/admin-public-view-toolbar.component.scss');
  const jump = await read('app/shared/components/page-jump-button/page-jump-button.component.scss');
  const tokens = await read('styles/_design-tokens.scss');
  const privacy = await read('app/features/public/legal/pages/privacy-policy-page.component.scss');
  const navigation = await read('styles/_navigation.scss');
  assert.match(tokens, /--app-admin-toolbar-height: 0px/);
  assert.match(tokens, /--app-floating-control-gap: clamp\(8px, calc\(32px - 1rem\), 16px\)/);
  assert.match(jump, /width: 44px;\s+height: 44px/);
  assert.match(jump, /bottom: var\(--app-page-jump-bottom\)/);
  assert.doesNotMatch(jump, /:host-context/);
  assert.match(navigation, /--app-page-jump-bottom: calc\(var\(--bottom-nav-height\) \+ var\(--app-admin-toolbar-height\) \+ 2 \* var\(--app-floating-control-gap\)\)/);
  assert.match(navigation, /--app-page-jump-bottom: calc\(var\(--app-admin-toolbar-height\) \+ 1rem \+ var\(--app-floating-control-gap\)\)/);
  assert.match(tokens, /--app-page-jump-bottom: clamp\(1rem, 2vw, 1\.5rem\)/);
  assert.doesNotMatch(jump, /rgba\(56, 197, 245/);
  assert.match(toolbar, /@media \(max-width: 680px\) and \(max-height: 680px\)/);
  assert.match(toolbar, /flex-direction: row;[\s\S]*?overflow-x: scroll;[\s\S]*?scrollbar-color:/);
  assert.doesNotMatch(toolbar, /overflow-x:\s*(?:hidden|clip)/);
  assert.match(privacy, /max-width: 24ch/);
  assert.match(privacy, /font-size: clamp\(1\.75rem, 4\.5vw, 3\.6rem\)/);
  assert.match(privacy, /text-wrap: balance/);
});

test('enlarged legal copy and header identity reflow instead of being clipped into narrow fixed rows', async () => {
  const privacy = await readFile(new URL('../../src/app/features/public/legal/pages/privacy-policy-page.component.scss', import.meta.url), 'utf8');
  const nav = await readFile(new URL('../../src/styles/_navigation.scss', import.meta.url), 'utf8');
  assert.match(privacy, /\.privacy-consent\s*\{\s*display: flex;\s*flex-wrap: wrap;/);
  assert.match(privacy, /\.privacy-consent__copy\s*\{[^}]*flex: 1 1 26rem;[^}]*min-width: 0;/);
  assert.match(privacy, /\.privacy-consent__actions button\s*\{[^}]*max-width: 100%;[^}]*white-space: normal;[^}]*overflow-wrap: anywhere;/);
  assert.match(nav, /@media \(min-width: 681px\)\s*\{\s*\.app-public-nav__profile\s*\{\s*height: auto;/);
  assert.match(nav, /min-height: min\(3\.9rem, 72px\)/);
  assert.match(nav, /font-size: min\(1rem, 24px\)/);
  assert.match(nav, /padding-bottom: max\(min\(0\.4rem, 6\.4px\), env\(safe-area-inset-bottom\)\)/);
});

test('overflow diagnostics retain bounded geometry only without concealing offending content', () => {
  const element = (right, display = 'block') => ({ tagName: 'DIV', id: '', classList: ['probe'], scrollWidth: right, clientWidth: right,
    getBoundingClientRect: () => ({ x: 0, right, width: right, height: 20 }), style: { display, visibility: 'visible', fontSize: '32px', minWidth: 'auto', gridTemplateColumns: 'none' } });
  const elements = [element(320), element(1000, 'none'), ...Array.from({ length: 20 }, (_, index) => element(400 + index))];
  const result = JSON.parse(JSON.stringify(runInNewContext(`(${collectLayoutOverflow.toString()})()`, {
    document: { body: { querySelectorAll: () => elements } }, innerWidth: 320, getComputedStyle: element => element.style,
  })));
  assert.equal(result.length, 12);
  assert.equal(result[0].right, 419);
  assert.equal(result[0].selector, 'div.probe');
  assert.ok(result.every(entry => entry.right > 321 && entry.right < 1000 && !('text' in entry)));
});

test('the shared browser contrast probe measures composited text, distinguishes large text and reports unmeasured media honestly', () => {
  const probe = (foreground, { size = '16px', weight = '400', backgroundImage = 'none', opacity = '1' } = {}, includeCoverage = true) => {
    const style = { color: foreground, backgroundColor: 'rgb(255, 255, 255)', display: 'block', visibility: 'visible', backgroundImage, opacity, fontSize: size, fontWeight: weight };
    const element = { tagName: 'SPAN', classList: ['probe'], closest: () => null, parentElement: null,
      getBoundingClientRect: () => ({ width: 100, height: 30, top: 0, bottom: 30 }) };
    let returned = false;
    return JSON.parse(JSON.stringify(runInNewContext(`(${collectContrastCandidates.toString()})({includeCoverage: ${includeCoverage}})`, {
      document: { body: {}, createTreeWalker: () => ({ nextNode: () => { if (returned) return null; returned = true; return { textContent: 'Visible text', parentElement: element }; } }) },
      NodeFilter: { SHOW_TEXT: 4 }, innerHeight: 100, getComputedStyle: () => style,
    })));
  };
  assert.equal(probe('rgb(118, 118, 118)').warnings.length, 0);
  assert.equal(probe('rgb(119, 119, 119)').warnings[0].required, 4.5);
  assert.equal(probe('rgb(119, 119, 119)', { size: '24px' }).warnings.length, 0);
  assert.equal(probe('rgba(0, 0, 0, 0.5)').warnings.length, 1);
  const media = probe('rgb(119, 119, 119)', { backgroundImage: 'url(photo.webp)' });
  assert.equal(media.checked, 0);
  assert.equal(media.skipped, 1);
  assert.match(media.scope, /not full AA acceptance/);
  assert.ok(Array.isArray(probe('rgb(0, 0, 0)', {}, false)));
});

test('the page body cannot conceal horizontal layout regressions', () => {
  assert.doesNotMatch(baseStyles, /overflow-x:\s*(?:hidden|clip)/);
});

test('the real HTTP acceptance tool cannot start a server on the Windows workstation', () => {
  assert.match(httpScript, /process\.env\.CI !== 'true' \|\| process\.platform !== 'linux'/);
  assert.ok(httpScript.indexOf("if (process.env.CI") < httpScript.indexOf('portProbe.listen('));
  assert.match(httpScript, /method: 'GET', redirect: 'manual'/);
  assert.match(httpScript, /SSR_RENDER_MAX_CONCURRENCY: '1'/);
  assert.match(httpScript, /'x-forwarded-host': 'amusement-parks\.fun'/);
  assert.match(httpScript, /SSR_API_INTERNAL_URL: publicOrigin/);
  assert.match(httpScript, /path: '\/fr\/not-found', status: 404, lang: 'en', csr: true/);
  assert.match(httpScript, /path: '\/fr\/apf-v6-nonexistent-route', status: 404, lang: 'en', csr: true/);
  assert.match(httpScript, /error shell has no language alternates/);
  assert.match(httpScript, /server\.kill\('SIGTERM'\)/);
  assert.match(httpScript, /http-ssr-report\.json/);
  assert.match(httpScript, /route\.home \? jsonLd\.length === 0 : jsonLd\.length > 0/);
  assert.match(httpScript, /for \(const \[, json\] of jsonLd\) JSON\.parse\(json\)/);
});

test('hydrated review accepts only protocol errors with a proven matching router cancellation', () => {
  const canceled = new Set(['canceled-request']);
  const invalidId = new Error('Invalid InterceptionId.');
  assert.equal(isCanceledInterceptionError(invalidId, 'canceled-request', canceled), true);
  assert.equal(isCanceledInterceptionError(invalidId, 'active-request', canceled), false);
  assert.equal(isCanceledInterceptionError(invalidId, undefined, canceled), false);
  assert.equal(isCanceledInterceptionError(new Error('CDP timeout: Fetch.fulfillRequest'), 'canceled-request', canceled), false);
  assert.equal(isCanceledInterceptionError(new Error('Rejected asset traversal.'), 'canceled-request', canceled), false);
});

test('native document retirement requires the exact previous main frame/loader and never an active or restored document', () => {
  const retired = new Set();
  const first = { id: 'main', loaderId: 'first' };
  const second = { id: 'main', loaderId: 'second' };
  const requests = new Map([['old', { frameId: 'main', loaderId: 'first' }], ['active', { frameId: 'main', loaderId: 'second' }], ['other-frame', { frameId: 'child', loaderId: 'first' }]]);
  let current = advanceMainDocument(undefined, first, requests, retired);
  current = advanceMainDocument(current, first, requests, retired);
  assert.equal(retired.size, 0);
  assert.equal(advanceMainDocument(current, { id: 'child', loaderId: 'child-load', parentId: 'main' }, requests, retired), current);
  current = advanceMainDocument(current, second, requests, retired);
  const invalid = new Error('Invalid InterceptionId.');
  assert.equal(isRetiredDocumentInterceptionError(invalid, 'old', retired), true);
  for (const id of ['active', 'other-frame', 'missing', undefined]) assert.equal(isRetiredDocumentInterceptionError(invalid, id, retired), false);
  assert.equal(isRetiredDocumentInterceptionError(new Error('CDP timeout: Fetch.fulfillRequest'), 'old', retired), false);
  current = advanceMainDocument(current, first, requests, retired);
  requests.set('restored-active', { frameId: 'main', loaderId: 'first' });
  assert.equal(isRetiredDocumentInterceptionError(invalid, 'restored-active', retired), false);
  assert.equal(isRetiredDocumentInterceptionError(invalid, 'old', retired), true);
  assert.equal(isRetiredDocumentInterceptionError(invalid, 'active', retired), true);
  assert.equal(current, JSON.stringify(['main', 'first']));
});

test('the V6 Chromium fixture covers every contractual responsive width', () => {
  const contractualWidths = [320, 360, 390, 414, 480, 680, 768, 960, 1024, 1180, 1366, 1440, 1920];
  for (const width of contractualWidths) {
    assert.match(responsiveScript, new RegExp(`\\b${width}\\b`));
    assert.match(designTokens, new RegExp(`\\b${width}\\b`));
  }
});

test('the fixture exercises both themes and constrained heights', () => {
  assert.match(responsiveScript, /const themes = \['light', 'dark'\]/);
  assert.match(responsiveScript, /height: 568, label: 'mobile-low-height'/);
  assert.match(responsiveScript, /height: 600, label: 'tablet-landscape-low-height'/);
});

test('the fixture rejects horizontal overflow and navigation overlap', () => {
  assert.match(responsiveScript, /reason: 'horizontal-overflow'/);
  assert.match(responsiveScript, /reason: 'out-of-bounds'/);
  assert.match(responsiveScript, /reason: 'fixed-navigation-overlap'/);
});

test('the fixture protects touch targets and layout reservations', () => {
  assert.match(responsiveScript, /reason: 'undersized-rating-target'/);
  assert.match(responsiveScript, /reason: 'unreserved-logo-space'/);
  assert.match(responsiveScript, /reason: 'unreserved-map-space'/);
  assert.match(responsiveScript, /reason: 'unstable-park-photo-height'/);
  assert.match(responsiveScript, /reason: 'inaccurate-park-image-size'/);
});

test('the fixture can persist visual evidence without an application server', () => {
  assert.match(responsiveScript, /V6_RESPONSIVE_EVIDENCE_DIR/);
  assert.match(responsiveScript, /Page\.captureScreenshot/);
  assert.match(responsiveScript, /responsive-report\.json/);
  assert.doesNotMatch(responsiveScript, /ng serve|serve:ssr|createServer\([^)]*http/i);
});
