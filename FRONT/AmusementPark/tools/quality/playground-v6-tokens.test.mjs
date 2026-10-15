import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

const styles = resolve(import.meta.dirname, '../../src/styles');
const designTokens = readFileSync(resolve(styles, '_design-tokens.scss'), 'utf8');
const base = readFileSync(resolve(styles, '_base.scss'), 'utf8');
const light = readFileSync(resolve(styles, '_theme-light.scss'), 'utf8');
const dark = readFileSync(resolve(styles, '_theme-dark.scss'), 'utf8');
const adapters = readFileSync(resolve(styles, '_tokens.scss'), 'utf8');

function variables(source) {
  return new Map([...source.matchAll(/(--[\w-]+):\s*([^;]+);/g)].map(([, name, value]) => [name, value.trim()]));
}

function rgb(hex) {
  return hex.match(/[\da-f]{2}/gi).map((channel) => Number.parseInt(channel, 16) / 255);
}

function luminance(color) {
  const channels = (Array.isArray(color) ? color : rgb(color))
    .map((value) => value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4);
  return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722;
}

function mix(foreground, background, foregroundWeight) {
  const foregroundChannels = Array.isArray(foreground) ? foreground : rgb(foreground);
  const backgroundChannels = Array.isArray(background) ? background : rgb(background);
  return foregroundChannels.map(
    (channel, index) => channel * foregroundWeight + backgroundChannels[index] * (1 - foregroundWeight),
  );
}

function contrast(foreground, background) {
  const values = [luminance(foreground), luminance(background)].sort((a, b) => b - a);
  return (values[0] + 0.05) / (values[1] + 0.05);
}

function resolveThemeColor(theme, name) {
  const value = theme.get(name);
  const alias = /^var\((--[\w-]+)\)$/.exec(value);
  return alias ? resolveThemeColor(theme, alias[1]) : value;
}

test('editorial fonts match the V6 reference without changing the protected wordmark', () => {
  const tokens = variables(designTokens);
  assert.equal(tokens.get('--font-heading'), 'Arial, Helvetica, sans-serif');
  assert.equal(tokens.get('--font-display'), 'Arial, Helvetica, sans-serif');
  assert.equal(tokens.get('--font-body'), 'system-ui, -apple-system, "Segoe UI", Arial, sans-serif');
});

test('Playground V6 keeps the canonical palette in both existing theme selectors', () => {
  const lightVariables = variables(light);
  const darkVariables = variables(dark);

  assert.match(light, /body:not\(\.dark-mode\),\s*body\.light-mode/);
  assert.match(dark, /body\.dark-mode,\s*body:not\(\.light-mode\)/);
  assert.equal(lightVariables.get('--bg'), '#f7f6f0');
  assert.equal(lightVariables.get('--text'), '#19201d');
  assert.equal(lightVariables.get('--bg-card'), '#eeefe8');
  assert.equal(lightVariables.get('--field'), '#ffffff');
  assert.equal(lightVariables.get('--c-orange'), '#e54b28');
  assert.equal(lightVariables.get('--c-lime'), '#d5ed60');
  assert.equal(lightVariables.get('--c-sky'), '#0c8ca3');
  assert.equal(darkVariables.get('--bg'), '#151a17');
  assert.equal(darkVariables.get('--text'), '#f5f5ec');
  assert.equal(darkVariables.get('--bg-card'), '#252c26');
  assert.equal(darkVariables.get('--field'), '#1c241e');
  assert.equal(darkVariables.get('--c-orange'), '#ff7551');
  assert.equal(darkVariables.get('--c-lime'), '#d1ec70');
  assert.equal(darkVariables.get('--c-sky'), '#49c9df');
  assert.equal(darkVariables.get('--c-rose'), '#ff8fae');
  assert.equal(darkVariables.get('--c-gold'), '#f5c451');
  assert.equal(darkVariables.get('--c-purple'), '#c4a7ff');
});

test('Playground V6 text colors meet WCAG AA on the page background', () => {
  for (const source of [light, dark]) {
    const theme = variables(source);
    assert.ok(contrast(theme.get('--text'), theme.get('--bg')) >= 4.5);
    assert.ok(contrast(theme.get('--text-muted'), theme.get('--bg')) >= 4.5);
  }
});

test('Playground V6 semantic accents meet AA for small search labels on every supporting surface', () => {
  for (const source of [light, dark]) {
    const theme = new Map([...variables(designTokens), ...variables(source)]);
    const resolveColor = (name) => resolveThemeColor(theme, name);
    for (const foreground of ['--app-primary-strong', '--app-accent-cyan', '--app-accent-lime']) {
      for (const background of ['--field', '--bg', '--bg-card']) {
        assert.ok(contrast(resolveColor(foreground), resolveColor(background)) >= 4.5,
          `${foreground} must meet AA on ${background}`);
      }
    }
  }
});

test('Playground small primary chips and faint text meet AA on their actual surfaces', () => {
  for (const source of [light, dark]) {
    const theme = new Map([...variables(designTokens), ...variables(source)]);
    const primary = resolveThemeColor(theme, '--app-primary-strong');
    const chipSurface = mix(theme.get('--c-orange'), theme.get('--bg-card'), 0.14);
    assert.ok(contrast(primary, chipSurface) >= 4.5, 'primary chips must meet AA on the tinted surface');
    for (const background of ['--field', '--bg', '--bg-card', '--bg-3']) {
      assert.ok(contrast(theme.get('--text-faint'), theme.get(background)) >= 4.5,
        `faint text must meet AA on ${background}`);
    }
  }
});

test('Playground light semantic chip tones meet AA on the composited soft backgrounds', () => {
  const theme = variables(light);
  for (const tone of ['lime', 'sky', 'gold', 'rose', 'purple']) {
    const foreground = resolveThemeColor(theme, `--app-light-accent-${tone}`);
    const soft = theme.get(`--app-light-accent-${tone}-soft`);
    const channels = soft.match(/[\d.]+/g).map(Number);
    const surface = rgb(theme.get('--bg-card'));
    const background = channels.slice(0, 3).map((channel, index) =>
      channel / 255 * channels[3] + surface[index] * (1 - channels[3]));
    assert.ok(contrast(foreground, background) >= 4.5, `${tone} chips must meet AA in light mode`);
  }
});

test('Playground category aliases resolve where the theme values live, not on root', () => {
  assert.doesNotMatch(designTokens, /--type-(?:coaster|water|family|show|food|thrill):/);
  const body = adapters.match(/body\s*\{([^}]+)\}/)?.[1];
  assert.ok(body);
  for (const [category, accent] of [['coaster', 'orange'], ['water', 'sky'], ['family', 'lime'], ['show', 'purple'], ['food', 'gold'], ['thrill', 'rose']]) {
    assert.ok(body.includes(`--type-${category}: var(--c-${accent});`));
  }
});

test('calendar today labels and active gallery counts meet AA in both themes', () => {
  for (const source of [light, dark]) {
    const theme = new Map([...variables(designTokens), ...variables(source)]);
    assert.ok(contrast(resolveThemeColor(theme, '--app-on-gold'), resolveThemeColor(theme, '--app-accent-gold')) >= 4.5);
    for (const accent of ['orange', 'sky', 'lime', 'gold', 'rose', 'purple']) {
      const axisSurface = mix(theme.get(`--c-${accent}`), theme.get('--bg-card'), 0.1);
      const countSurface = mix(theme.get(`--c-${accent}`), axisSurface, 0.22);
      assert.ok(contrast(theme.get('--heading'), countSurface) >= 4.5, `${accent} gallery counts must meet AA`);
    }
  }
});

test('Playground V6 secondary dark accents remain readable on their soft surfaces', () => {
  const theme = variables(dark);

  for (const accent of ['--c-rose', '--c-gold', '--c-purple']) {
    const foreground = theme.get(accent);
    const softSurface = mix(foreground, theme.get('--bg-card'), 0.14);
    assert.ok(contrast(foreground, softSurface) >= 4.5, `${accent} must meet AA on its soft surface`);
  }

  assert.ok(contrast(theme.get('--app-on-rose'), theme.get('--c-rose')) >= 4.5);
  assert.ok(contrast(theme.get('--app-on-purple'), theme.get('--c-purple')) >= 4.5);
});

test('Playground V6 accent button labels meet AA at rest and on hover in both themes', () => {
  for (const source of [light, dark]) {
    const theme = new Map([...variables(designTokens), ...variables(source)]);
    const pairs = [
      ['--on-accent', '--c-orange'],
      ['--app-on-accent-hover', '--c-orange-2'],
      ['--on-accent', '--c-sky'],
      ['--app-on-accent-hover', '--c-sky-2'],
      ['--on-accent', '--c-lime'],
      ['--on-accent', '--c-lime-2'],
      ['--app-on-gold', '--c-gold'],
      ['--app-on-rose', '--c-rose'],
      ['--app-on-purple', '--c-purple'],
    ];

    for (const [foreground, background] of pairs) {
      assert.ok(contrast(theme.get(foreground), theme.get(background)) >= 4.5,
        `${foreground} must meet AA on ${background}`);
    }

    for (const tone of ['gold', 'rose', 'purple']) {
      const background = mix(theme.get(`--c-${tone}`), theme.get('--app-accent-hover-mix'), 0.82);
      assert.ok(contrast(theme.get(`--app-on-${tone}`), background) >= 4.5,
        `${tone} button must meet AA on hover`);
    }
  }
});

test('Playground V6 uses the editorial width, restrained geometry and flat page surfaces', () => {
  assert.match(designTokens, /--content-max-width:\s*1160px/);
  assert.match(designTokens, /--radius-md:\s*0\.375rem/);
  assert.match(designTokens, /--radius-2xl:\s*1rem/);
  assert.doesNotMatch(`${light}\n${dark}\n${base}`, /(?:linear|radial|conic)-gradient\(/);
  assert.doesNotMatch(base, /fractalNoise|data:image\/svg\+xml/);
});

