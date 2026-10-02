import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import test from 'node:test';
import { compileString } from 'sass';

for (const file of ['_buttons.scss', '_navigation.scss']) {
  test(`${file}: public controls animate only compositor-compatible properties`, () => {
    const source = readFileSync(resolve(import.meta.dirname, '../../src/styles', file), 'utf8');
    const { css } = compileString(source, { silenceDeprecations: ['color-functions', 'global-builtin'] });
    const transitions = file === '_buttons.scss'
      ? [...css.matchAll(/\.app-public-(?:layout \.app-button|nav \.btn-nav)\s*\{\s*transition:\s*([^;]+);/g)]
      : [...css.matchAll(/transition:\s*([^;]+);/g)];
    assert.ok(transitions.length > 0);
    for (const [, value] of transitions) {
      for (const part of value.split(',')) {
        assert.match(part.trim(), /^(transform|opacity)\s+/, `Non-composited transition: ${value}`);
      }
    }
    // Keyboard focus and hover feedback must still be present when paint changes are immediate.
    assert.match(css, /:focus-visible/);
    assert.match(css, /:hover/);
    if (file === '_buttons.scss') {
      assert.equal(transitions.length, 2);
      // Shared legacy controls retain their existing transitions in administration.
      for (const selector of ['\\.btn,\\s*\\.app-button', '\\.btn-nav']) {
        const sharedRule = css.match(new RegExp(`${selector}\\s*\\{([^}]+)\\}`))[1];
        const transition = sharedRule.match(/transition:\s*([^;]+);/)[1];
        for (const property of ['border-color', 'background-color', 'color', 'box-shadow']) {
          assert.ok(transition.includes(`${property} var(--transition)`));
        }
      }
    }
  });
}
