import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';

const styles = resolve(import.meta.dirname, '../../src/styles');
const footer = readFileSync(resolve(styles, '_footer.scss'), 'utf8');
function luminance(hex) {
  const channels = hex.match(/\w\w/g).map(channel => parseInt(channel, 16) / 255)
    .map(value => value <= 0.04045 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4);
  return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722;
}
for (const theme of ['dark', 'light']) {
  test(`small footer text meets WCAG AA contrast in ${theme} mode`, () => {
    const source = readFileSync(resolve(styles, `_theme-${theme}.scss`), 'utf8');
    const variables = new Map([...source.matchAll(/(--[\w-]+):\s*([^;]+);/g)].map(([, key, value]) => [key, value]));
    function color(value) {
      return value.startsWith('var(') ? color(variables.get(value.slice(4, -1))) : value.slice(1);
    }
    const background = color(footer.match(/\.app-public-footer\s*\{[^}]*background:\s*([^;]+)/)[1]);
    for (const selector of ['col-title', 'copy']) {
      const foreground = color(footer.match(new RegExp(`\\.app-public-footer__${selector}\\s*\\{[^}]*color:\\s*([^;]+)`))[1]);
      const values = [luminance(background), luminance(foreground)].sort((a, b) => b - a);
      assert.ok((values[0] + 0.05) / (values[1] + 0.05) >= 4.5, `${selector} contrast must be at least 4.5:1`);
    }
  });
}
