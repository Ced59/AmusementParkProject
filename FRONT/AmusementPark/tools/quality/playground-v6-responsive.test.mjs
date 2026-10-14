import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

const responsiveScript = await readFile(new URL('../responsive/trip-pages-browser-check.mjs', import.meta.url), 'utf8');
const designTokens = await readFile(new URL('../../../../docs/design-v6/V6-DESIGN-TOKENS.md', import.meta.url), 'utf8');

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
