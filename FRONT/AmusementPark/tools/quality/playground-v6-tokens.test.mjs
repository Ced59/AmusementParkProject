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

test('account notification, passport and trip labels keep AA ink on CI-observed surfaces', () => {
  const cases = [
    [light, '--app-accent-cyan', [0.8092548986666667, 0.8829020156862745, 0.8719216125490197]],
    [light, '--app-text-muted', '#eeefe8'],
    [light, '--app-accent-gold', '#eeefe8'],
    [light, '--app-accent-lime', '#f7f6f0'],
    [light, '--app-primary-strong', [0.928392, 0.847216, 0.804392]],
    [dark, '--app-primary-strong', [0.264784, 0.212627, 0.172627]],
    [light, '--app-primary-strong', [0.950784, 0.920784, 0.907255]],
    [light, '--app-primary-strong', [0.931569, 0.905098, 0.872157]],
  ];
  for (const [source, token, background] of cases) {
    assert.ok(contrast(resolveThemeColor(variables(source), token), background) >= 4.5, `${token}: measured account surface`);
  }
  for (const source of [light, dark]) {
    const theme = new Map([...variables(designTokens), ...variables(adapters), ...variables(source)]);
    for (const fill of ['--app-primary', '--c-sky']) {
      assert.ok(contrast(resolveThemeColor(theme, '--app-on-accent'), resolveThemeColor(theme, fill)) >= 4.5);
    }
    assert.ok(contrast(resolveThemeColor(theme, '--app-accent-lime'), mix(resolveThemeColor(theme, '--c-lime'), resolveThemeColor(theme, '--app-surface-2'), 0.09)) >= 4.5);
  }
  for (const [file, selector, token] of [
    ['watchlists/pages/user-notifications-page.component.scss', 'notifications-page__counter strong', '--app-accent-cyan'],
    ['watchlists/pages/user-notifications-page.component.scss', 'notifications-filters__pill--active', '--app-accent-cyan'],
    ['watchlists/ui/live-alert-inbox.component.scss', 'live-inbox__header p', '--app-accent-cyan'],
    ['profile/trips/components/trip-participant-panel/trip-participant-panel.component.scss', 'participant-card__identity small', '--app-text-muted'],
    ['profile/trips/components/trip-invitation-panel/trip-invitation-panel.component.scss', 'invitation-panel__step', '--app-on-accent'],
    ['profile/passport/components/year-recap-share-control/year-recap-share-control.component.scss', 'year-recap-card > header span', '--app-accent-gold'],
    ['profile/passport/components/visit-recap-share-control/visit-recap-share-control.component.scss', 'visit-share__kicker', '--app-accent-gold'],
    ['profile/passport/components/visit-recap-share-control/visit-recap-share-control.component.scss', 'visit-share-editor__privacy', '--app-accent-lime'],
    ['profile/trips/components/trip-invitation-panel/trip-invitation-panel.component.scss', 'invitation-preview > p', '--app-accent-lime'],
    ['profile/passport/components/passport-historical-existence-report/passport-historical-existence-report.component.scss', 'history-report__kicker', '--app-accent-gold'],
    ['profile/passport/pages/passport-visits-overview-page/passport-visits-overview-page.component.scss', 'passport-overview__privacy', '--app-accent-gold'],
    ['profile/passport/pages/passport-visit-editor-page/passport-visit-editor-page.component.scss', 'passport-visit__eyebrow', '--app-primary-strong'],
    ['profile/passport/pages/passport-visit-editor-page/passport-visit-editor-page.component.scss', 'passport-occurrence__index', '--app-primary-strong'],
    ['profile/passport/pages/passport-visit-editor-page/passport-visit-editor-page.component.scss', 'passport-ride-assessment > summary strong', '--app-primary-strong'],
  ]) {
    const source = readFileSync(resolve(styles, `../app/features/${file}`), 'utf8');
    const escaped = selector.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    assert.match(source, new RegExp(`\\.${escaped}\\s*\\{[^}]*color:\\s*var\\(${token}\\)`));
  }
  const transition = readFileSync(resolve(styles, '../app/features/profile/trips/pages/trip-passport-transition-page/trip-passport-transition-page.component.scss'), 'utf8');
  assert.match(transition, /\.trip-passport-day__confirm > span\s*\{/);
  assert.doesNotMatch(transition, /\.trip-passport-day__confirm span\s*\{/);
  const share = readFileSync(resolve(styles, '../app/features/profile/passport/pages/passport-profile-share-page/passport-profile-share-page.component.scss'), 'utf8');
  assert.match(share, /\.passport-share__section-heading > span\s*\{[^}]*background:\s*var\(--app-primary\);\s*color:\s*var\(--app-on-accent\)/);
});

test('active collection filter text meets AA on the exact CI-observed rose surface', () => {
  const source = readFileSync(resolve(styles, '../app/features/collections/pages/user-collections-page.component.scss'), 'utf8');
  assert.match(source, /\.collections-page__filter--active\s*\{[^}]*color:\s*var\(--app-accent-pink\)/);
  const measuredBackground = [0.90451, 0.82549, 0.828039];
  const theme = variables(light);
  assert.ok(contrast(resolveThemeColor(theme, '--app-accent-pink'), measuredBackground) >= 4.5);
  assert.ok(contrast('#bd315d', measuredBackground) < 4.5, 'the observed failing decorative rose remains a regression case');
});

test('small trip priority and date text uses AA semantic ink, not decorative orange', () => {
  for (const [page, selector] of [
    ['trip-preferences-page', 'preference-card__current--mustdo'],
    ['trip-export-page', 'trip-export-day__heading time'],
    ['trip-passport-transition-page', 'trip-passport-day__header time'],
  ]) {
    const source = readFileSync(resolve(styles, `../app/features/profile/trips/pages/${page}/${page}.component.scss`), 'utf8');
    const escaped = selector.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    assert.match(source, new RegExp(`\\.${escaped}\\s*\\{[^}]*color:\\s*var\\(--app-primary-strong\\)`));
  }
  for (const source of [light, dark]) {
    const theme = variables(source);
    const text = resolveThemeColor(theme, '--app-primary-strong');
    for (const surface of ['--app-bg', '--app-surface', '--app-surface-2']) {
      assert.ok(contrast(text, resolveThemeColor(theme, surface)) >= 4.5, `${surface}: trip text must meet 4.5:1`);
    }
  }
});

test('editorial fonts match the V6 reference without changing the protected wordmark', () => {
  const tokens = variables(designTokens);
  assert.equal(tokens.get('--font-heading'), 'Arial, Helvetica, sans-serif');
  assert.equal(tokens.get('--font-display'), 'Arial, Helvetica, sans-serif');
  assert.equal(tokens.get('--font-body'), 'system-ui, -apple-system, "Segoe UI", Arial, sans-serif');
});

test('transparent entity logos retain a light neutral matte in both themes without image inversion', () => {
  const matte = variables(designTokens).get('--app-logo-surface');
  assert.equal(matte, '#f7f6f0');
  assert.ok(contrast('#19201d', matte) >= 4.5);
  for (const source of [light, dark]) assert.doesNotMatch(source, /--app-logo-surface:/);
  const cards = readFileSync(resolve(styles, '_cards.scss'), 'utf8');
  const home = readFileSync(resolve(styles, '../app/features/public/home/pages/home.component.scss'), 'utf8');
  const park = readFileSync(resolve(styles, '../app/features/public/parks/ui/park-detail-view.component.scss'), 'utf8');
  assert.match(cards, /\.ui-park-card__visual\s*\{[^}]*background:\s*var\(--app-logo-surface\)/);
  assert.match(cards, /\.ui-search-result-card__logo\s*\{[^}]*background:\s*var\(--app-logo-surface\)/);
  assert.match(cards, /--featured-park-logo-bg:\s*var\(--app-logo-surface\)/);
  assert.match(home, /\.home-hero-card--1 \.home-hero-card__media\s*\{[^}]*background:\s*var\(--app-logo-surface\)/);
  assert.match(park, /\.park-logo-frame\s*\{[^}]*background:\s*var\(--app-logo-surface\)/);
  for (const source of [cards, home, park]) assert.doesNotMatch(source, /filter:\s*invert\(/);
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

test('practical offer links and weather legend text meet AA on card and secondary surfaces', () => {
  const body = adapters.match(/body\s*\{([^}]+)\}/)?.[1];
  assert.match(body, /--app-surface-muted:\s*var\(--app-surface-2\)/);
  for (const source of [light, dark]) {
    const theme = new Map([...variables(designTokens), ...variables(source)]);
    for (const background of ['--bg-card', '--bg-card-2']) {
      assert.ok(contrast(resolveThemeColor(theme, '--app-primary-strong'), theme.get(background)) >= 4.5);
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

test('small sky kickers remain AA on the actual nested profile surface found by Chromium', () => {
  const theme = variables(light);
  const foreground = resolveThemeColor(theme, '--app-light-accent-sky');
  // Run 38900388260: two 11.52px profile kickers measured 4.25:1.
  // Preserve the measured composite as a regression case, not a browser PASS.
  const observedBackground = [0.8037894588235295, 0.8525680329411766, 0.8132834823529411];
  assert.ok(contrast('#086b7c', observedBackground) < 4.5);
  assert.ok(contrast(foreground, observedBackground) >= 4.5);
  assert.equal(resolveThemeColor(theme, '--app-accent-cyan'), foreground);
});

test('small gold kickers remain AA on the desktop nested profile surface found by Chromium', () => {
  const theme = variables(light);
  const foreground = resolveThemeColor(theme, '--app-light-accent-gold');
  const observedBackground = [0.855767505882353, 0.8121790090196078, 0.7198372];
  assert.ok(contrast('#8a4f00', observedBackground) < 4.5);
  assert.ok(contrast(foreground, observedBackground) >= 4.5);
  assert.equal(resolveThemeColor(theme, '--app-accent-gold'), foreground);
});

test('light semantic kickers retain AA when nested on existing tinted profile cards', () => {
  const theme = new Map([...variables(designTokens), ...variables(light)]);
  const failures = [];
  for (const tone of ['lime', 'sky', 'gold', 'rose', 'purple']) {
    const foreground = resolveThemeColor(theme, `--app-light-accent-${tone}`);
    const channels = theme.get(`--app-light-accent-${tone}-soft`).match(/[\d.]+/g).map(Number);
    for (const parentTone of ['orange', 'sky', 'gold', 'rose', 'purple']) {
      const parent = mix(resolveThemeColor(theme, `--c-${parentTone}`), theme.get('--bg-card'), 0.08);
      const composite = mix(channels.slice(0, 3).map(channel => channel / 255), parent, channels[3]);
      const ratio = contrast(foreground, composite);
      if (ratio < 4.5) failures.push({ tone, parentTone, ratio });
    }
  }
  assert.deepEqual(failures, []);
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

test('the active inventory map filter uses readable ink on its actual primary-tinted card', () => {
  for (const source of [light, dark]) {
    const theme = new Map([...variables(designTokens), ...variables(source)]);
    const surface = mix(theme.get('--c-orange'), theme.get('--bg-card'), 0.16);
    assert.ok(contrast(theme.get('--heading'), surface) >= 4.5);
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
  for (const radius of ['xs', 'sm', 'md', 'lg', 'xl', '2xl']) {
    assert.match(designTokens, new RegExp(`--radius-${radius}:\\s*0\\.125rem`));
  }
  assert.doesNotMatch(`${light}\n${dark}\n${base}`, /(?:linear|radial|conic)-gradient\(/);
  assert.doesNotMatch(base, /fractalNoise|data:image\/svg\+xml/);
});

