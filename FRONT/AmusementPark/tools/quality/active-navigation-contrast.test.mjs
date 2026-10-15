import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

const styles = resolve(import.meta.dirname, '../../src/styles');
const navigation = readFileSync(resolve(styles, '_navigation.scss'), 'utf8');
const tokens = readFileSync(resolve(styles, '_design-tokens.scss'), 'utf8');

function luminance(channels) {
  const linear = channels.map(channel => channel / 255)
    .map(value => value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4);
  return linear[0] * 0.2126 + linear[1] * 0.7152 + linear[2] * 0.0722;
}

function blend(foreground, background, percentage) {
  const alpha = percentage / 100;
  return foreground.map((channel, index) =>
    channel * alpha + background[index] * (1 - alpha));
}

for (const theme of ['dark', 'light']) {
  test(`active desktop, mobile and contextual navigation meets WCAG AA contrast in ${theme} mode`, () => {
    const source = tokens + readFileSync(resolve(styles, `_theme-${theme}.scss`), 'utf8');
    const variables = new Map([...source.matchAll(/(--[\w-]+):\s*([^;]+);/g)].map(([, key, value]) => [key, value]));
    function color(value) {
      if (value.startsWith('var(')) {
        return color(variables.get(value.slice(4, -1)));
      }
      assert.match(value, /^#[\da-f]{6}$/i);
      return value.slice(1).match(/\w\w/g).map(channel => parseInt(channel, 16));
    }
    const surface = color(variables.get('--app-surface-2'));
    const accent = color(variables.get('--c-orange'));
    for (const selector of ['app-public-nav__links a.active', 'app-mobile-bottom-nav__item.active', 'app-public-park-trail__item--current']) {
      const rule = navigation.match(new RegExp(`\\.${selector.replace('.', '\\.')}\\s*\\{([^}]+)\\}`))[1];
      const foreground = color(rule.match(/color:\s*([^;]+)/)[1]);
      const background = selector.includes('trail')
        ? blend(accent, surface, 10)
        : surface;
      const values = [luminance(foreground), luminance(background)].sort((a,b) => b-a);
      const ratio = (values[0] + 0.05) / (values[1] + 0.05);
      assert.ok(ratio >= 4.5, `${selector}: ${ratio.toFixed(2)}:1 must reach 4.5:1`);
    }
  });
}
