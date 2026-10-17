import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

const styles = resolve(import.meta.dirname, '../../src/styles');
const allowedTransitionProperties = new Set([
  'background-color',
  'border-color',
  'box-shadow',
  'color',
  'opacity',
  'transform',
]);

for (const file of ['_buttons.scss', '_navigation.scss']) {
  test(`${file}: public controls avoid layout-changing transitions`, () => {
    const source = readFileSync(resolve(styles, file), 'utf8');
    const transitions = [...source.matchAll(/transition:\s*([^;]+);/g)];

    assert.ok(transitions.length > 0);
    for (const [, value] of transitions) {
      for (const part of value.split(',')) {
        const property = part.trim().split(/\s+/)[0];
        assert.ok(
          allowedTransitionProperties.has(property),
          `Layout or filter transition is forbidden: ${value}`,
        );
      }
    }

    assert.doesNotMatch(source, /transition:\s*all\b/);
    assert.match(source, /:focus-visible/);
    assert.match(source, /:hover/);
  });
}

test('the Playground home hero is stable and respects direct feedback states', () => {
  const source = readFileSync(
    resolve(
      import.meta.dirname,
      '../../src/app/features/public/home/pages/home.component.scss',
    ),
    'utf8',
  );

  assert.doesNotMatch(source, /animation\s*:/);
  assert.doesNotMatch(source, /(?:linear|radial|conic)-gradient\(/);
  assert.doesNotMatch(source, /translate(?:X|Y|3d)?\(/);
  assert.match(source, /\.home-hero-card:hover\s*\{/);
  assert.match(source, /\.home-hero-card:focus-visible\s*\{/);
  assert.match(source, /@media \(max-width:\s*680px\)/);
});
