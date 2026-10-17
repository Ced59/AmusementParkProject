import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import { createLeafletMapDisposer } from '../../src/app/shared/utils/maps/leaflet-map-disposer.ts';

function fakeMap(cssZoom = false) {
  const handlers = new Map();
  let removed = 0;
  const map = { on: (event, listener) => { handlers.set(event, listener); },
    getPane: () => ({ classList: { contains: () => cssZoom } }), remove: () => { removed++; } };
  return { map, fire: event => handlers.get(event)?.(), removed: () => removed };
}

test('idle Leaflet disposal is immediate and idempotent', () => {
  const fixture = fakeMap();
  const dispose = createLeafletMapDisposer(fixture.map);
  dispose(); dispose();
  assert.equal(fixture.removed(), 1);
});

test('a zoom started before its CSS class or already active at binding keeps the pane until dispatch ends', async () => {
  for (const cssZoom of [false, true]) {
    const fixture = fakeMap(cssZoom);
    const dispose = createLeafletMapDisposer(fixture.map);
    if (!cssZoom) fixture.fire('zoomstart');
    dispose();
    assert.equal(fixture.removed(), 0);
    fixture.fire('zoomend');
    assert.equal(fixture.removed(), 0);
    await Promise.resolve();
    assert.equal(fixture.removed(), 1);
  }
});

test('destruction inside a zoomend dispatch is not missed by a late listener', async () => {
  const fixture = fakeMap();
  const dispose = createLeafletMapDisposer(fixture.map);
  fixture.fire('zoomstart'); fixture.fire('zoomend'); dispose();
  assert.equal(fixture.removed(), 0);
  await Promise.resolve();
  assert.equal(fixture.removed(), 1);
});

test('a new zoom started during the previous end dispatch cannot be removed by the old microtask', async () => {
  const fixture = fakeMap();
  const dispose = createLeafletMapDisposer(fixture.map);
  fixture.fire('zoomstart'); fixture.fire('zoomend'); fixture.fire('zoomstart'); dispose();
  await Promise.resolve();
  assert.equal(fixture.removed(), 0);
  fixture.fire('zoomend');
  await Promise.resolve();
  dispose();
  assert.equal(fixture.removed(), 1);
});

test('both map adapters use the same public-API cleanup without changing zoom interactions', () => {
  for (const component of ['leaflet-map', 'leaflet-static-map']) {
    const source = readFileSync(new URL(`../../src/app/shared/components/${component}/${component}.component.ts`, import.meta.url), 'utf8');
    assert.ok(source.includes('this.disposeMap = createLeafletMapDisposer(this.map)'));
    assert.ok(source.includes('this.disposeMap();'));
    assert.ok(source.includes('if (this.destroyed)'));
  }
  const helper = readFileSync(new URL('../../src/app/shared/utils/maps/leaflet-map-disposer.ts', import.meta.url), 'utf8');
  assert.doesNotMatch(helper, /_animatingZoom|_mapPane|\.setView\(|\.setZoom\(|setTimeout\(|\.stop\(/);
});
