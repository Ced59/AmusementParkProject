import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';
test('Playground page background stays flat and texture-free', () => {
  const source = readFileSync(resolve(import.meta.dirname, '../../src/styles/_base.scss'), 'utf8');
  assert.doesNotMatch(source, /data:image\/svg\+xml/);
  assert.doesNotMatch(source, /fractalNoise/);
  assert.doesNotMatch(source, /(?:linear|radial|conic)-gradient\(/);
  assert.match(source, /body::before\s*\{\s*content:\s*none;/);
  assert.match(source, /body::after\s*\{\s*content:\s*none;/);
});
