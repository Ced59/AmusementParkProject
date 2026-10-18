import assert from 'node:assert/strict';
import test from 'node:test';
import { Script } from 'node:vm';
import { exerciseMapGestures, mapGesturePlan } from '../design-v6/browser-map-gestures.mjs';

const target = { x: 150, y: 200, viewportWidth: 390, visibleBottom: 550, unobscured: true };
test('map gesture input is bounded to a measured unobscured map interior', () => {
  assert.deepEqual(mapGesturePlan(target), { start: { x: 150, y: 200 }, end: { x: 210, y: 224 } });
  for (const value of [null, {}, { ...target, x: 10 }, { ...target, y: 10 }, { ...target, x: 330 },
    { ...target, y: 520 }, { ...target, unobscured: false }, { ...target, viewportWidth: NaN }, { ...target, visibleBottom: undefined }]) {
    assert.throws(() => mapGesturePlan(value), /safe visible map target/);
  }
});

async function runGestures({ failInput, scroll = false, staticPane = false } = {}) {
  const calls = [], expressions = [], result = { mapScenarios: { pointerDragNotExercised: true } };
  let transform = 0;
  const cdp = { send: async (method, params) => {
    calls.push({ method, params });
    if (failInput?.(method, params)) throw new Error('native input rejected');
    if (params.type === 'mouseReleased' || params.type === 'touchEnd') transform++;
  } };
  const evaluate = async expression => {
    new Script(expression); expressions.push(expression);
    return expression.startsWith('(() =>') ? target : {
      transform: `translate3d(${staticPane ? 0 : transform * 60}px, 0, 0)`, scrollY: scroll && transform ? 20 : 0, animating: false,
    };
  };
  const operation = () => exerciseMapGestures({ cdp, evaluate, result, waitUntil: async (predicate) => {
    for (let attempt = 0; attempt < 5; attempt++) if (await predicate()) return;
    throw new Error('map pane did not move and settle');
  } });
  return { calls, expressions, result, operation };
}

test('map gestures use trusted mouse/touch input and measure DOM pan and retained page scroll, not Leaflet internals', async () => {
  const run = await runGestures();
  await run.operation();
  assert.equal(run.result.mapScenarios.pointerDragNotExercised, false);
  assert.deepEqual(run.result.mapScenarios.gestures.map(item => item.kind), ['mouse-drag', 'touch-drag']);
  assert.equal(run.calls.filter(item => item.params.type === 'touchMove').length, 6);
  assert.deepEqual(run.calls.at(-1), { method: 'Emulation.setTouchEmulationEnabled', params: { enabled: false } });
  assert.ok(run.expressions.every(expression => !/fetch\(|\.fire\(|setView|setZoom|_leaflet|geolocation/.test(expression)));
});

test('gesture acceptance refuses a static pane or document scrolling instead of marking the map as covered', async () => {
  for (const options of [{ staticPane: true }, { scroll: true }]) {
    const run = await runGestures(options);
    await assert.rejects(run.operation, /did not move|moved the document/);
    assert.equal(run.result.mapScenarios.pointerDragNotExercised, true);
  }
});

test('a refused touch move releases the contact and restores touch emulation even when cancellation fails', async () => {
  for (const failCancel of [false, true]) {
    const run = await runGestures({ failInput: (_, params) => params.type === 'touchMove' || (failCancel && params.type === 'touchCancel') });
    await assert.rejects(run.operation, /native input rejected/);
    assert.ok(run.calls.some(item => item.params.type === 'touchCancel'));
    assert.deepEqual(run.calls.at(-1), { method: 'Emulation.setTouchEmulationEnabled', params: { enabled: false } });
    assert.equal(run.result.mapScenarios.pointerDragNotExercised, true);
  }
});
