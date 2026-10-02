import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';
import { compileString } from 'sass';

test('decorative noise rasterization stays bounded independently of the viewport', () => {
  const source = readFileSync(resolve(import.meta.dirname, '../../src/styles/_base.scss'), 'utf8');
  const { css } = compileString(source);
  const rule = css.match(/body::after\s*\{([^}]+)\}/)[1];
  const svg = decodeURIComponent(rule.match(/url\("data:image\/svg\+xml,([^"]+)"\)/)[1]);
  const width = Number(svg.match(/<svg\s[^>]*width='(\d+)'/)[1]);
  const height = Number(svg.match(/<svg\s[^>]*height='(\d+)'/)[1]);
  assert.ok(width > 0 && height > 0 && width * height <= 16384);
  assert.match(rule, new RegExp(`background-size: ${width}px ${height}px;`));
  assert.match(rule, /background-repeat: repeat;/);
  assert.match(svg, /stitchTiles='stitch'/);
  assert.match(svg, /type='fractalNoise'/);
  assert.match(rule, /pointer-events: none;/);
  assert.match(rule, /opacity: 0\.018;/);
  assert.match(css, /body\.light-mode::after\s*\{\s*opacity: 0\.025;/);
});
