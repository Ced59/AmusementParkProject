import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import { initialThemeObserver, validateInitialTheme } from '../design-v6/initial-theme-evidence.mjs';

test('theme evidence seeds only the existing preference before real document scripts run', () => {
  const index = readFileSync(new URL('../../src/index.html', import.meta.url), 'utf8');
  assert.ok(index.includes("localStorage.getItem('amusement-park-theme')"));
  assert.ok(index.includes('data-app-theme-init'));
  assert.ok(initialThemeObserver(null).includes("localStorage.removeItem('amusement-park-theme')"));
  assert.ok(initialThemeObserver('dark').includes('localStorage.setItem'));
  assert.throws(() => initialThemeObserver('invalid'), /Invalid saved theme/);
  assert.doesNotMatch(initialThemeObserver('light'), /classList\.(?:add|remove|toggle)|fetch\(/);
});

test('correct post-paint frames pass without treating a pre-paint parser state as a visible flash', () => {
  const evidence = { firstContentfulPaintMs: 100, storageError: false, finalTheme: 'light', finalBackground: 'rgb(247, 246, 240)', frames: [
    { timeMs: 50, theme: 'dark', background: 'rgb(21, 26, 23)', color: 'rgb(245, 245, 236)' },
    { timeMs: 110, theme: 'light', background: 'rgb(247, 246, 240)', color: 'rgb(25, 32, 29)' },
  ] };
  assert.deepEqual(validateInitialTheme(evidence, 'light'), { expectedTheme: 'light', firstContentfulPaintMs: 100, observedPostPaintFrames: 1, wrongFrames: 0 });
  const flash = structuredClone(evidence);
  flash.frames[0].timeMs = 101;
  assert.throws(() => validateInitialTheme(flash, 'light'), /flashed or diverged/);
  const wrongColor = structuredClone(evidence);
  wrongColor.frames[1].background = 'rgb(200, 200, 200)';
  assert.throws(() => validateInitialTheme(wrongColor, 'light'), /flashed or diverged/);
});

test('missing paint, missing visible frames, storage failure and wrong final preference never pass', () => {
  const evidence = { firstContentfulPaintMs: 100, storageError: false, finalTheme: 'dark', finalBackground: 'rgb(21, 26, 23)', frames: [
    { timeMs: 110, theme: 'dark', background: 'rgb(21, 26, 23)', color: 'rgb(245, 245, 236)' },
  ] };
  assert.equal(validateInitialTheme(evidence, 'dark').wrongFrames, 0);
  assert.throws(() => validateInitialTheme({ ...evidence, firstContentfulPaintMs: undefined }, 'dark'), /incomplete/);
  assert.throws(() => validateInitialTheme({ ...evidence, frames: [] }, 'dark'), /No post-paint/);
  assert.throws(() => validateInitialTheme({ ...evidence, storageError: true }, 'dark'), /incomplete/);
  assert.throws(() => validateInitialTheme({ ...evidence, finalTheme: 'light' }, 'dark'), /flashed or diverged/);
});
