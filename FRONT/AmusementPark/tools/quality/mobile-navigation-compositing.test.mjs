import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';
import { compileString } from 'sass';

test('mobile public navigation has opaque surfaces without backdrop filtering', () => {
  const source = readFileSync(resolve(import.meta.dirname, '../../src/styles/_navigation.scss'), 'utf8');
  const { css } = compileString(source);
  const mobile = css.slice(css.lastIndexOf('@media (max-width: 680px)'));
  assert.match(mobile, /\.app-public-nav,\s*\.app-public-park-trail,\s*\.app-mobile-bottom-nav\s*\{/);
  assert.match(mobile, /background: var\(--app-bg\);/);
  assert.match(mobile, /backdrop-filter: none;/);
  assert.doesNotMatch(mobile, /(?:display|visibility|content-visibility|opacity|position|height|top|bottom):/);

  const desktop = css.slice(0, css.lastIndexOf('@media (max-width: 680px)'));
  assert.match(desktop, /\.app-public-nav\s*\{[^}]*position: sticky;[^}]*backdrop-filter: blur\(18px\) saturate\(150%\);/);
  assert.match(desktop, /\.app-mobile-bottom-nav\s*\{[^}]*position: fixed;[^}]*backdrop-filter: blur\(18px\) saturate\(150%\);/);
  assert.match(desktop, /\.app-public-park-trail\s*\{[^}]*position: sticky;[^}]*backdrop-filter: blur\(18px\) saturate\(150%\);/);
  assert.doesNotMatch(mobile, /admin|account/);

  for (const theme of ['dark', 'light']) {
    const themeSource = readFileSync(resolve(import.meta.dirname, `../../src/styles/_theme-${theme}.scss`), 'utf8');
    assert.match(themeSource, /--app-bg: var\(--bg\);/);
    assert.match(themeSource, /--bg: #[\da-fA-F]{6};/);
  }
});
