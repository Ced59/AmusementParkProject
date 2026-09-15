import assert from 'node:assert/strict';
import { test } from 'node:test';
import { installGracefulShutdown } from '../server-graceful-shutdown.ts';

test('waits for the last response and flushes once, including repeated signals', () => {
  const listeners = new Map();
  const exits = [];
  let close;
  let closeCount = 0;
  let flushCount = 0;
  installGracefulShutdown({ close(callback) { close = callback; closeCount++; } },
    () => flushCount++, { on: (name, handler) => listeners.set(name, handler), exit: (code) => exits.push(code) });
  listeners.get('SIGTERM')();
  listeners.get('SIGINT')();
  assert.equal(closeCount, 1);
  assert.deepEqual(exits, []);
  close();
  assert.equal(flushCount, 1);
  assert.deepEqual(exits, [0]);
});

test('reports an expired shutdown as failure instead of successful drainage', (context) => {
  context.mock.timers.enable({ apis: ['setTimeout'] });
  const listeners = new Map();
  const exits = [];
  installGracefulShutdown({ close() {} }, () => assert.fail('not drained'),
    { on: (name, handler) => listeners.set(name, handler), exit: (code) => exits.push(code) }, 100);
  listeners.get('SIGTERM')();
  context.mock.timers.tick(100);
  assert.deepEqual(exits, [1]);
});

test('reports a close error rather than successful shutdown', () => {
  const listeners = new Map();
  const exits = [];
  installGracefulShutdown({ close(callback) { callback(new Error('close failed')); } }, () => {},
    { on: (name, handler) => listeners.set(name, handler), exit: (code) => exits.push(code) });
  listeners.get('SIGINT')();
  assert.deepEqual(exits, [1]);
});
