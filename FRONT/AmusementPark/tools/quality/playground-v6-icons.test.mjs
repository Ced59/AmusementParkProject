import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { createHash } from 'node:crypto';
import { completedIconAliases, completedIconStyles, missingIconAliases, encodeIconSvg } from '../design-v6/lucide-compat-completion.mjs';

const root = resolve(import.meta.dirname, '../..');
const read = path => readFileSync(resolve(root, path), 'utf8').replaceAll('\r\n', '\n');
const styles = read('src/styles/_lucide-icon-compat.scss');

test('all production PI icon classes have a real existing-system mask, never a solid square fallback', () => {
  const documents = ['src/app', 'src/styles'].flatMap(directory => readdirSync(resolve(root, directory), { recursive: true })
    .filter(path => /\.(?:scss|html|ts)$/.test(path) && !path.endsWith('.spec.ts') && !path.endsWith('_lucide-icon-compat.scss'))
    .map(path => ({ path: `${directory}/${path}`, source: read(`${directory}/${path}`) })));
  assert.deepEqual(missingIconAliases(documents, styles), []);
  assert.deepEqual(missingIconAliases([{ path: 'bad.html', source: '<i class="pi pi-spin pi-made-up"></i>' }], styles), [{ name: 'pi-made-up', paths: ['bad.html'] }]);
});

test('completed masks reproduce literal SVG data from the locked existing Lucide package', () => {
  const locked = JSON.parse(read('package-lock.json')).packages['node_modules/@lucide/angular'];
  const installed = JSON.parse(read('node_modules/@lucide/angular/package.json'));
  assert.equal(installed.version, locked.version);
  const generated = completedIconStyles(read('node_modules/@lucide/angular/fesm2022/lucide-angular.mjs'));
  assert.equal(Object.keys(completedIconAliases).length, 43);
  for (const block of generated.trim().split('\n\n')) assert.ok(styles.includes(block), 'checked-in mask differs from its installed source');
  assert.doesNotMatch(generated, /url\("https?:\/\/|<script|foreignObject|onload/);
  assert.equal(read('src/assets/licenses/lucide.txt'), read('node_modules/@lucide/angular/LICENSE'));
});

test('filled watch and collection states remain visually distinct without changing their conditions', () => {
  for (const name of ['heart', 'bookmark', 'bell']) {
    const filled = styles.match(new RegExp(`\\.pi\\.pi-${name}-fill \\{([^}]+)\\}`))?.[1];
    const outline = styles.match(new RegExp(`\\.pi\\.pi-${name} \\{([^}]+)\\}`))?.[1];
    assert.ok(filled && outline && filled !== outline);
    assert.ok(decodeURIComponent(filled).includes("fill='black'"));
    assert.ok(decodeURIComponent(outline).includes("fill='none'"));
  }
});

test('compact data URLs preserve the geometry of all 144 original masks and escape CSS-sensitive bytes', () => {
  const original = [...styles.matchAll(/\.pi\.(pi-[a-z0-9-]+)\s*\{\s*--app-icon-mask: url\("data:image\/svg\+xml,([^"\r\n]+)"\);\s*\}/g)]
    .filter(match => !Object.hasOwn(completedIconAliases, match[1]))
    .map(match => [match[1], decodeURIComponent(match[2]).replaceAll('"', "'")])
    .sort(([left], [right]) => left.localeCompare(right));
  assert.equal(original.length, 144);
  // Captured before lossless re-encoding; drawings and names must not drift.
  assert.equal(createHash('sha256').update(JSON.stringify(original)).digest('hex'),
    '3df4e51789726c0966c425b340676e3d540cb76a7f1299513827d1f71cfe12ba');
  const mask = selector => styles.match(new RegExp(`${selector}\\s*\\{\\s*--app-icon-mask: url\\("data:image/svg\\+xml,([^"\\r\\n]+)"\\);`))?.[1];
  assert.equal(decodeURIComponent(mask('\\.pi-chip-remove-icon')), decodeURIComponent(mask('\\.pi\\.pi-times')),
    'the extra chip-close mask must retain its original X geometry too');
  const source = '<svg xmlns="http://www.w3.org/2000/svg"><path d="M1 1h2" fill="#fff" /></svg>';
  const compact = encodeIconSvg(source);
  assert.equal(decodeURIComponent(compact), source.replaceAll('"', "'"));
  assert.doesNotMatch(compact, /["<>#\r\n]/);
  assert.ok(compact.length < encodeURIComponent(source).length);
});

test('icon conversion rejects executable data instead of evaluating a package module', () => {
  const source = 'class LucideTextAlignCenter extends LucideIconBase {\n    static icon = {\n        size: run(), node: []\n    };';
  assert.throws(() => completedIconStyles(source), /Only literal/);
});
