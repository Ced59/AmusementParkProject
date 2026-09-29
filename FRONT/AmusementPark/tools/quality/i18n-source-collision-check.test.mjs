import assert from 'node:assert/strict';
import test from 'node:test';

import { collisionFingerprint, findLeafCollisions } from './i18n-source-collision-check.mjs';

test('detects only leaf keys overwritten by multiple source modules', () => {
  const collisions = findLeafCollisions([
    { owner: 'passport/main.json', value: { passport: { title: 'Passport', intro: 'Intro' } } },
    { owner: 'passport/stats.json', value: { passport: { title: 'Stats', charts: 'Charts' } } },
    { owner: 'common/navigation.json', value: { navigation: { home: 'Home' } } }
  ]);

  assert.deepEqual(collisions, [{
    key: 'passport.title',
    owners: ['passport/main.json', 'passport/stats.json']
  }]);
});

test('accepts modules that share a namespace without overriding a leaf', () => {
  const collisions = findLeafCollisions([
    { owner: 'passport/main.json', value: { passport: { title: 'Passport' } } },
    { owner: 'passport/stats.json', value: { passport: { charts: 'Charts' } } }
  ]);

  assert.deepEqual(collisions, []);
});

test('detects a leaf that replaces a translation subtree from another module', () => {
  const collisions = findLeafCollisions([
    { owner: 'common/summary.json', value: { passport: { summary: 'Summary' } } },
    { owner: 'passport/details.json', value: { passport: { summary: { title: 'Details' } } } }
  ]);

  assert.deepEqual(collisions, [{
    key: 'passport.summary',
    owners: ['common/summary.json', 'passport/details.json']
  }]);
});

test('includes every source owner in the collision fingerprint', () => {
  const first = collisionFingerprint('fr', { key: 'passport.title', owners: ['main.json', 'stats.json'] });
  const widened = collisionFingerprint('fr', { key: 'passport.title', owners: ['extra.json', 'main.json', 'stats.json'] });

  assert.notEqual(first, widened);
});
