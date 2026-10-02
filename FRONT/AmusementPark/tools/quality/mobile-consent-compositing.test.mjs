import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';
import { compileString } from 'sass';

test('mobile consent keeps a readable opaque panel without backdrop filtering', () => {
  const source = readFileSync(resolve(import.meta.dirname, '../../src/app/ui/layouts/cookie-consent-banner/cookie-consent-banner.component.scss'), 'utf8');
  const { css } = compileString(source);
  const breakpoint = css.indexOf('@media (max-width: 780px)');
  assert.ok(breakpoint > 0);
  const desktopPanel = css.slice(0, breakpoint).match(/\.app-cookie-consent__inner\s*\{([^}]+)\}/)[1];
  const mobile = css.slice(breakpoint);
  const mobilePanel = mobile.match(/\.app-cookie-consent__inner\s*\{([^}]+)\}/)[1];
  assert.match(desktopPanel, /backdrop-filter: blur\(18px\);/);
  assert.match(desktopPanel, /pointer-events: auto;/);
  assert.match(mobilePanel, /backdrop-filter: none;/);
  assert.match(mobilePanel, /background: linear-gradient\(180deg, var\(--app-surface\), var\(--app-surface-2\)\);/);
  assert.match(mobilePanel, /background-color: var\(--app-bg\);/);
  assert.match(mobilePanel, /grid-template-columns: 1fr;/);
  assert.doesNotMatch(mobile, /(?:display|visibility|content-visibility|opacity):\s*(?:none|hidden|0)\s*;/);
  assert.match(mobile, /bottom: calc\(var\(--bottom-nav-height\) \+ 0\.9rem\);/);
  assert.match(mobile, /\.app-cookie-consent__button\s*\{\s*flex: 1 1 13rem;/);
});
