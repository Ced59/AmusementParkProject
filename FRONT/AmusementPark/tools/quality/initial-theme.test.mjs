import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import { resolve } from 'node:path';
import { runInNewContext } from 'node:vm';
import test from 'node:test';
import Beasties from 'beasties';
import { compileString } from 'sass';

const indexPath = resolve(import.meta.dirname, '../../src/index.html');
const html = readFileSync(indexPath, 'utf8');
const script = html.match(/<script data-app-theme-init>([\s\S]*?)<\/script>/)?.[1];

function createElement() {
  const classes = new Set(['dark-mode']);
  return {
    classes,
    classList: {
      toggle(name, enabled) {
        if (enabled) { classes.add(name); } else { classes.delete(name); }
      }
    }
  };
}

for (const savedTheme of ['light', 'dark', null, 'invalid']) {
  for (const prefersDark of [true, false]) {
    test(`first paint respects saved=${savedTheme}, system dark=${prefersDark}`, () => {
      const documentElement = createElement();
      const body = createElement();
      runInNewContext(script, {
        document: { documentElement, body },
        localStorage: { getItem: () => savedTheme },
        window: { matchMedia: () => ({ matches: prefersDark }) }
      });
      const expected = savedTheme === 'light' || savedTheme === 'dark'
        ? savedTheme : prefersDark ? 'dark' : 'light';
      assert.deepEqual([...body.classes], [`${expected}-mode`]);
      assert.deepEqual([...documentElement.classes], [`${expected}-mode`]);
    });
  }
}

test('blocked storage still applies the system preference before Angular', () => {
  const body = createElement();
  runInNewContext(script, {
    document: { documentElement: createElement(), body },
    localStorage: { getItem: () => { throw new Error('Blocked'); } },
    window: { matchMedia: () => ({ matches: false }) }
  });
  assert.deepEqual([...body.classes], ['light-mode']);
  assert.ok(html.indexOf('data-app-theme-init') < html.indexOf('<app-root '));
});

test('critical font preloads resolve to bundled fonts without duplicate credentials', () => {
  const fontPreloads = [...html.matchAll(/<link rel="preload" href="([^"]+)" as="font" type="font\/woff2" crossorigin>/g)];
  assert.equal(fontPreloads.length, 3);
  for (const [, href] of fontPreloads) {
    assert.ok(existsSync(resolve(import.meta.dirname, '../../src' + href)));
  }
});

test('SSR critical CSS retains both palettes before the browser selects its theme', async () => {
  const themeDirectory = resolve(import.meta.dirname, '../../src/styles');
  const css = compileString(
    readFileSync(resolve(themeDirectory, '_theme-dark.scss'), 'utf8') + '\n' +
    readFileSync(resolve(themeDirectory, '_theme-light.scss'), 'utf8'),
    { style: 'compressed' }
  ).css;
  const critical = await new Beasties({ logLevel: 'silent' }).process(
    `<html><head><style>${css}</style></head><body class="dark-mode"><app-root></app-root></body></html>`
  );
  assert.match(critical, /body:not\(\.dark-mode\)/);
  assert.match(critical, /--bg:\s*#fff9f0/);
  assert.match(critical, /body\.dark-mode/);
  assert.match(critical, /--bg:\s*#0f0b06/);
});
