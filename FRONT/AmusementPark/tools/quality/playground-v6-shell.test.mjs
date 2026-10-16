import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

const projectRoot = resolve(import.meta.dirname, '../..');
const read = (relativePath) =>
  readFileSync(resolve(projectRoot, relativePath), 'utf8');

const header = read('src/app/ui/layouts/public-header/public-header.component.html');
const footer = read('src/app/ui/layouts/public-footer/public-footer.component.html');
const mobileNav = read(
  'src/app/ui/layouts/public-mobile-bottom-nav/public-mobile-bottom-nav.component.html',
);
const navigationStyles = read('src/styles/_navigation.scss');
const browserReview = read('tools/design-v6/capture-public-ssr.mjs');
const footerStyles = read('src/styles/_footer.scss');
const layoutStyles = read('src/styles/_layout.scss');
const wordmarkStyles = read('src/styles/_brand-wordmark.scss');
const accessibilityStyles = read('src/styles/_accessibility-polish.scss');
const publicLayoutStyles = read(
  'src/app/ui/layouts/public-app-layout/public-app-layout.component.scss',
);

const protectedWordmarkParts = [
  'app-brand-wordmark__base',
  'app-brand-wordmark__dot',
  'app-brand-wordmark__fun-text',
  'AMUSEMENT-PARKS',
  '>.<',
  '>fun<',
];

test('mobile navigation gives long localized labels the full equal-width column without shrinking text', () => {
  assert.match(navigationStyles, /\.app-mobile-bottom-nav\s*\{[^}]*gap:\s*0;/);
  assert.match(navigationStyles, /\.app-mobile-bottom-nav__item\s*\{[^}]*min-width:\s*0;[^}]*min-height:\s*3\.9rem;[^}]*flex:\s*1 1 0;[^}]*padding:\s*0\.5rem 0\.125rem;/);
  assert.match(navigationStyles, /\.app-mobile-bottom-nav__label\s*\{[^}]*font-size:\s*0\.68rem;[^}]*overflow-wrap:\s*anywhere;[^}]*white-space:\s*normal;/);
  assert.doesNotMatch(navigationStyles, /\.app-mobile-bottom-nav__label\s*\{[^}]*(?:text-overflow:\s*ellipsis|overflow:\s*hidden|line-clamp)/);
});

test('the mobile park trail keeps complete localized labels and wraps its 44px links instead of clipping them', () => {
  assert.match(navigationStyles, /\.app-public-park-trail\s*\{[^}]*flex-wrap:\s*wrap/);
  assert.match(navigationStyles, /\.app-public-park-trail__item\s*\{[^}]*min-width:\s*0;[^}]*max-width:\s*100%;[^}]*min-height:\s*44px/);
  const labelRule = navigationStyles.match(/\.app-public-park-trail__label\s*\{[^}]*\}/)[0];
  assert.match(labelRule, /white-space:\s*normal/);
  assert.match(labelRule, /overflow-wrap:\s*anywhere/);
  assert.doesNotMatch(labelRule, /ellipsis|overflow:\s*hidden/);
});

test('the protected wordmark keeps its original ink independently from the V6 heading palette', () => {
  assert.match(wordmarkStyles, /\.app-brand-wordmark\s*\{[^}]*--brand-wordmark-base-ink:\s*#ffffff/);
  assert.match(wordmarkStyles, /body\.light-mode \.app-brand-wordmark\s*\{[^}]*--brand-wordmark-base-ink:\s*#160a00/);
  assert.match(wordmarkStyles, /color:\s*var\(--brand-wordmark-base-ink\)/);
  assert.match(wordmarkStyles, /font-family:\s*var\(--font-display\)/);
  assert.match(wordmarkStyles, /\.app-brand-wordmark\s*\{[^}]*--font-display:\s*"Bebas Neue", "Syne", "Plus Jakarta Sans", sans-serif/);
  assert.match(wordmarkStyles, /font-family:\s*"Satisfy Brand Fun"/);
});

test('keyboard focus uses an opaque semantic outline in addition to the soft halo', () => {
  assert.match(accessibilityStyles, /:focus-visible\s*\{[^}]*outline:\s*3px solid var\(--app-accent-cyan\)/);
});

test('the public header and footer keep the protected wordmark without the optional pictogram', () => {
  for (const template of [header, footer]) {
    for (const part of protectedWordmarkParts) {
      assert.ok(template.includes(part), `${part} must remain in both brand locations`);
    }

    assert.doesNotMatch(template, /logo-amusementpark-ui/i);
    assert.doesNotMatch(template, /__logo-mark/);
  }

  assert.match(footer, /footer\.brand\.sloganData/);
  assert.match(footer, /footer\.brand\.sloganFun/);
});

test('the complete public navigation contract remains represented', () => {
  for (const key of [
    'sidebar.home',
    'sidebar.parks',
    'sidebar.technical',
    'sidebar.rankings',
    'sidebar.manufacturers',
    'sidebar.about',
    'sidebar.admin',
  ]) {
    assert.ok(header.includes(key), `${key} must remain in the desktop header`);
  }

  for (const interaction of [
    'toggleMeasurementSystem()',
    "openModal('loginModal')",
    "openModal('languageModal')",
    '<app-theme-switcher>',
    'navigateToCurrentUserProfile()',
    'isLoggedIn() && isAdmin()',
  ]) {
    assert.ok(header.includes(interaction), `${interaction} must remain available`);
  }
});

test('the isolated hydrated review exercises units, all language choices and the actual park tree without production writes', () => {
  for (const proof of [
    'anonymous unit preference restored',
    'language dialog exposes all eight existing choices',
    'real language choice preserves the home route in German',
    'real language choice restores the French home route',
    'real park overview tree exposes catalogue and current park',
    'real inventory tree exposes its current list',
    'real park navigation tree closes without changing the route',
  ]) assert.ok(browserReview.includes(proof), `${proof} must remain in the browser recipe`);
  assert.ok(browserReview.includes("if (!['GET', 'HEAD'].includes(request.method)"));
  assert.ok(browserReview.includes('const origin = options.networkProbe ?'));
  assert.ok(browserReview.includes(": 'https://apf-review.invalid';"));
  const treeFacade = read('src/app/features/public/navigation/state/public-park-navigation-tree.facade.ts');
  assert.match(treeFacade, /context\.pageKind === 'park-items' && !zoneId/);
  assert.ok(browserReview.includes('failed-hydrated-check.webp'));
});

test('the mobile navigation keeps every public entry and its conditional admin access', () => {
  for (const key of [
    'sidebar.home',
    'sidebar.parks',
    'sidebar.rankings',
    'sidebar.technical',
    'sidebar.manufacturers',
    'sidebar.profile',
    'sidebar.admin',
  ]) {
    assert.ok(mobileNav.includes(key), `${key} must remain in the mobile navigation`);
  }

  assert.match(mobileNav, /isLoggedIn\(\) && isAdmin\(\)/);
  assert.match(mobileNav, /navigateToCurrentUserProfile\(\)/);
});

test('the Playground shell uses editorial width, flat navigation and safe mobile insets', () => {
  assert.match(publicLayoutStyles, /--content-max-width:\s*1160px/);
  assert.match(publicLayoutStyles, /--content-wide-max-width:\s*1480px/);
  assert.match(navigationStyles, /grid-template-areas:\s*"brand actions"\s*"links links"/);
  assert.match(navigationStyles, /grid-template-rows:\s*minmax\(85px, auto\) auto/);
  assert.match(navigationStyles, /border-block:\s*2px solid var\(--app-heading\)/);
  assert.match(navigationStyles, /grid-template-areas:\s*"brand" "actions"/);
  assert.match(navigationStyles, /html:has\(\.app-public-nav\)\s*\{[^}]*--topbar-height:\s*134px/);
  assert.match(navigationStyles, /\.app-public-nav__links > li > a/);
  assert.match(header, /app-public-nav__language-code[^>]*>[\s\S]*?selectedLanguage\(\)\.toUpperCase\(\)/);
  assert.match(navigationStyles, /env\(safe-area-inset-bottom\)/);
  assert.match(navigationStyles, /\.app-mobile-bottom-nav[\s\S]*?border-radius:\s*0/);
  assert.match(navigationStyles, /\.app-language-dialog \.language-option--active\s*\{/);
  assert.match(navigationStyles, /@media \(max-width:\s*680px\)[\s\S]*?--topbar-height:\s*104px/);
  assert.match(wordmarkStyles, /@media \(max-width:\s*430px\)[\s\S]*?max-width:\s*100%/);
  assert.doesNotMatch(navigationStyles, /(?:linear|radial|conic)-gradient\(/);
  assert.doesNotMatch(navigationStyles, /backdrop-filter\s*:/);
});

test('the footer and account/admin shells share the flat Playground composition', () => {
  assert.match(
    footerStyles,
    /grid-template-columns:\s*minmax\(15rem, 1\.35fr\) repeat\(3, minmax\(8rem, 0\.75fr\)\)/,
  );
  assert.match(footerStyles, /border-top:\s*4px solid var\(--c-orange\)/);
  assert.doesNotMatch(footerStyles, /(?:linear|radial|conic)-gradient\(/);
  assert.match(layoutStyles, /\.app-account-layout__aside[\s\S]*?border-top:\s*4px solid var\(--c-orange\)/);
  assert.match(layoutStyles, /\.app-admin-layout__body[\s\S]*?grid-template-columns:\s*minmax\(15rem, 17rem\) minmax\(0, 1fr\)/);
});
