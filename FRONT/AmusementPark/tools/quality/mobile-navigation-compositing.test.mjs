import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

test('public navigation uses opaque Playground surfaces without backdrop filtering', () => {
  const source = readFileSync(resolve(import.meta.dirname, '../../src/styles/_navigation.scss'), 'utf8');
  const mobile = source.slice(source.lastIndexOf('@media (max-width: 680px)'));
  assert.match(mobile, /\.app-public-nav,\s*\.app-public-park-trail,\s*\.app-mobile-bottom-nav\s*\{/);
  assert.match(mobile, /background: var\(--app-bg\);/);
  assert.doesNotMatch(mobile, /(?:display|visibility|content-visibility|opacity|position|height|top|bottom):/);

  assert.match(source, /\.app-public-nav\s*\{[\s\S]*?position: sticky;/);
  assert.match(source, /\.app-mobile-bottom-nav\s*\{[\s\S]*?position: fixed;/);
  assert.match(source, /\.app-public-park-trail\s*\{[\s\S]*?position: sticky;/);
  assert.doesNotMatch(source, /backdrop-filter\s*:/);
  assert.doesNotMatch(source, /(?:linear|radial|conic)-gradient\(/);
  assert.doesNotMatch(mobile, /admin|account/);

  for (const theme of ['dark', 'light']) {
    const themeSource = readFileSync(resolve(import.meta.dirname, `../../src/styles/_theme-${theme}.scss`), 'utf8');
    assert.match(themeSource, /--app-bg: var\(--bg\);/);
    assert.match(themeSource, /--bg: #[\da-fA-F]{6};/);
  }
});
