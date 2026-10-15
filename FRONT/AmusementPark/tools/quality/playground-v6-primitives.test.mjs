import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

const styles = resolve(import.meta.dirname, '../../src/styles');
const read = (name) => readFileSync(resolve(styles, name), 'utf8');
const buttons = read('_buttons.scss');
const cards = read('_cards.scss');
const forms = read('_forms.scss');
const maps = read('_maps.scss');
const surfaces = read('_surfaces.scss');
const primitives = read('_ui-primitives.scss');
const chips = read('_chips.scss');

test('Playground shared surfaces and cards are flat editorial panels', () => {
  assert.doesNotMatch(`${surfaces}\n${cards}\n${forms}`, /(?:linear|radial|conic)-gradient\(/);
  assert.doesNotMatch(`${surfaces}\n${primitives}`, /backdrop-filter\s*:/);
  assert.match(surfaces, /border-radius:\s*var\(--radius-md\)/);
  assert.match(cards, /\.ui-park-card[\s\S]*?border-radius:\s*var\(--radius-md\)/);
});

test('Playground buttons and fields keep square control geometry', () => {
  assert.match(buttons, /\.btn,[\s\S]*?border-radius:\s*var\(--radius-sm\)/);
  assert.match(forms, /\.public-input,[\s\S]*?border-radius:\s*var\(--radius-sm\)/);
  assert.match(primitives, /\.p-button\s*\{[\s\S]*?border-radius:\s*var\(--app-radius-sm\)/);
  assert.match(primitives, /\.p-inputtext,[\s\S]*?background:\s*var\(--app-field\)/);
  assert.doesNotMatch(buttons, /translateY\(/);
});

test('Playground primitives cover dense data, overlays and feedback states', () => {
  for (const selector of ['.p-datatable', '.p-paginator', '.p-tabs', '.p-dialog', '.p-toast', '.p-editor-container']) {
    assert.ok(primitives.includes(selector), `${selector} must remain styled`);
  }
  assert.match(maps, /background:\s*var\(--app-map\)/);
  assert.doesNotMatch(primitives, /backdrop-filter\s*:/);
});

test('small primary kickers and chips use semantic foregrounds rather than decorative accents', () => {
  assert.match(chips, /\.app-kicker\s*\{[^}]*color:\s*var\(--app-primary-strong\)/);
  assert.match(chips, /\.app-chip--primary,[\s\S]*?\.public-pill\s*\{[^}]*color:\s*var\(--app-primary-strong\)/);
});

