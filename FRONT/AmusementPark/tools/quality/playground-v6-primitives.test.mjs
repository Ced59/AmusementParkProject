import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';
import { componentPresentations, isProductionPresentationFile } from '../design-v6/presentation-inventory-evidence.mjs';

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

function fixedLegacyCorners(source) {
  return [...source.matchAll(/border-(?:(?:top|bottom)-(?:left|right)-)?radius:\s*([^;]+);/g)]
    .filter(match => {
      let value = match[1];
      // Token fallbacks are not rendered corners when the declared token exists.
      while (/var\([^()]*\)/.test(value)) value = value.replace(/var\([^()]*\)/g, '');
      return /[\d.]rem\b/.test(value) || [...value.matchAll(/(\d*\.?\d+)px\b/g)]
        .some(([, number]) => Number(number) > 7 && ![999, 9999].includes(Number(number)));
    }).map(match => match[0]);
}

test('the corner audit catches small rem and multi-value legacy corners without rejecting circles or tokens', () => {
  assert.deepEqual(fixedLegacyCorners('.a{border-radius:.3rem;} .b{border-radius:.35rem .35rem .1rem .1rem;}'),
    ['border-radius:.3rem;', 'border-radius:.35rem .35rem .1rem .1rem;']);
  assert.deepEqual(fixedLegacyCorners('.a{border-top-left-radius:12px;}'), ['border-top-left-radius:12px;']);
  assert.deepEqual(fixedLegacyCorners('.a{border-radius:50%;} .b{border-radius:var(--radius-sm,.5rem);} .c{border-radius:999px;}'), []);
});

test('all local and global surfaces use existing V6 radius tokens instead of fixed legacy corners', () => {
  const appRoot = resolve(styles, '../app');
  const violations = [];
  for (const path of readdirSync(appRoot, { recursive: true }).filter(path => path.endsWith('.scss') || isProductionPresentationFile(path))) {
    const source = readFileSync(resolve(appRoot, path), 'utf8');
    const styles = path.endsWith('.scss') ? source : /@Component\s*\(/.test(source)
      ? componentPresentations(source, path).flatMap(component => component.inlineStyles).join('\n') : '';
    for (const radius of fixedLegacyCorners(styles)) violations.push({ path, radius });
  }
  for (const path of readdirSync(styles).filter(path => path.endsWith('.scss'))) {
    for (const radius of fixedLegacyCorners(read(path))) violations.push({ path, radius });
  }
  assert.deepEqual(violations, []);
  for (const name of ['comparison-tab', 'admin-provider']) {
    const source = readFileSync(resolve(appRoot, `features/admin/data/ui/captain-coaster-${name}.component.scss`), 'utf8');
    assert.ok(source.includes('border-radius: var(--app-radius-pill)'));
  }
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

test('segmented tabs use flat editorial separators, visible focus and reduced motion', () => {
  const tabs = readFileSync(resolve(styles, '../app/ui/primitives/segmented-tabs/ui-segmented-tabs.component.scss'), 'utf8');
  assert.doesNotMatch(tabs, /box-shadow|border-radius:\s*[1-9]/);
  assert.match(tabs, /border-bottom-color:\s*var\(--app-primary-strong\)/);
  assert.match(tabs, /outline:\s*3px solid var\(--app-primary-strong\)/);
  assert.match(tabs, /overflow-wrap:\s*anywhere/);
  assert.match(tabs, /prefers-reduced-motion:\s*reduce/);
});

test('shared panel and dialog retain native controls and translated names without container click targets', () => {
  const panel = readFileSync(resolve(styles, '../app/shared/ui/primitives/panel.ts'), 'utf8');
  const dialog = readFileSync(resolve(styles, '../app/shared/ui/primitives/dialog.ts'), 'utf8');
  assert.doesNotMatch(panel, /<div[^>]*\(click\)/);
  assert.match(panel, /<button[\s\S]*?\[attr\.aria-expanded\]="!collapsed"[\s\S]*?\(click\)="toggle\(\)"/);
  assert.ok(panel.includes("'actions.expand' : 'actions.collapse'"));
  assert.doesNotMatch(dialog, /<div[^>]*\(click\)/);
  assert.match(dialog, /<button\s+\*ngIf="dismissableMask"[^>]*class="p-dialog-mask p-dialog-mask__backdrop"[\s\S]*?\(click\)="onMaskClick\(\$event\)"/);
  assert.ok(dialog.includes('[cdkTrapFocus]="modal" [cdkTrapFocusAutoCapture]="modal"'));
  assert.ok(dialog.includes("[attr.aria-label]=\"'actions.close' | translate\""));
  assert.match(primitives, /\.p-panel-toggle\s*\{[^}]*width:\s*100%;[^}]*min-height:\s*2\.75rem/);
});

test('sortable headers keep native table semantics and route keyboard activation through the same click', () => {
  const sortable = readFileSync(resolve(styles, '../app/shared/ui/primitives/sortable-column.ts'), 'utf8');
  assert.ok(sortable.includes("@HostBinding('attr.tabindex')"));
  assert.ok(sortable.includes("@HostListener('keydown', ['$event'])"));
  assert.ok(sortable.includes('event.target !== this.elementRef.nativeElement'));
  assert.ok(sortable.includes("['Enter', ' '].includes(event.key)"));
  assert.ok(sortable.includes('this.elementRef.nativeElement.click()'));
  assert.doesNotMatch(sortable, /attr\.role|aria-sort|onSort\.emit|onLazyLoad\.emit/);
  assert.match(primitives, /\.p-sortable-column:focus-visible\s*\{[^}]*outline:\s*3px solid/);
});
