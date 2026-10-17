import assert from 'node:assert/strict';
import test from 'node:test';
import { readFileSync } from 'node:fs';
import { nativeBrowserZoomPlan, nativeZoomIssues, resizeNativeZoomViewport } from '../design-v6/native-browser-zoom.mjs';

const environment = { CI: 'true', V6_ISOLATED_PREFLIGHT: 'true', V6_NATIVE_ZOOM: '200' };
const validMetrics = { width: 683, height: 600, outerWidth: 1366, outerHeight: 1300, devicePixelRatio: 2, visualScale: 1, rootZoom: '1', bodyZoom: '1' };

test('native zoom requires explicit Linux CI admin diagnostics and uses the real Chromium preference', () => {
  assert.equal(nativeBrowserZoomPlan({}, 'win32', null), null);
  for (const [env, platform, phase] of [[environment, 'win32', 'admin'], [{ ...environment, CI: 'false' }, 'linux', 'admin'],
    [{ ...environment, V6_NATIVE_ZOOM: '2' }, 'linux', 'admin'], [{ ...environment, V6_ISOLATED_PREFLIGHT: undefined }, 'linux', 'admin'],
    [environment, 'linux', 'core'], [environment, 'linux', undefined]]) {
    assert.throws(() => nativeBrowserZoomPlan(env, platform, { phase }), /restricted/);
  }
  const plan = nativeBrowserZoomPlan(environment, 'linux', { phase: 'admin' });
  assert.equal(1.2 ** plan.preferences.partition.default_zoom_level.x, 2);
  assert.deepEqual(plan.browserArguments, ['--force-device-scale-factor=1']);
  assert.deepEqual(plan.cssWidths, [320, 683, 960]);
});

test('native zoom rejects missing zoom, pinch scaling, CSS zoom and nonfinite measurements', () => {
  assert.deepEqual(nativeZoomIssues(validMetrics), []);
  for (const override of [{ devicePixelRatio: 1 }, { devicePixelRatio: NaN }, { visualScale: 2 }, { visualScale: null },
    { rootZoom: '2' }, { bodyZoom: '200%' }]) assert.ok(nativeZoomIssues({ ...validMetrics, ...override }).length);
  assert.ok(nativeZoomIssues(null).length);
});

test('native zoom resizes the real window and accounts for measured browser chrome without metrics emulation', async () => {
  const calls = [];
  const cdp = { send: async (method, parameters) => { calls.push({ method, parameters }); return { windowId: 17 }; } };
  let count = 0;
  const result = await resizeNativeZoomViewport({ cdp, evaluate: async expression => {
    assert.doesNotThrow(() => new Function(`return (${expression});`));
    return { ...validMetrics, height: count++ ? 600 : 606 };
  }, width: 683, height: 600 });
  assert.equal(result.resizeAttempts, 2);
  assert.equal(result.factor, 2);
  assert.deepEqual(calls.map(call => call.method), ['Emulation.clearDeviceMetricsOverride', 'Browser.getWindowForTarget', 'Browser.setWindowBounds', 'Browser.setWindowBounds']);
  assert.deepEqual(calls.at(-1).parameters, { windowId: 17, bounds: { width: 1366, height: 1288 } });
});

test('native zoom refuses a wrong factor immediately and an unstable viewport after five attempts', async () => {
  const cdp = { send: async () => ({ windowId: 1 }) };
  await assert.rejects(resizeNativeZoomViewport({ cdp, evaluate: async () => ({ ...validMetrics, devicePixelRatio: 1 }), width: 683, height: 600 }), /not 200%/);
  let evaluations = 0;
  await assert.rejects(resizeNativeZoomViewport({ cdp, evaluate: async () => { evaluations++; return { ...validMetrics, width: 684 }; }, width: 683, height: 600 }), /did not settle/);
  assert.equal(evaluations, 5);
  await assert.rejects(resizeNativeZoomViewport({ cdp, evaluate: async () => validMetrics, width: 390, height: 600 }), /Unbounded/);
});

test('native zoom evidence is attached to each layout and never substitutes for the complete campaign or inset test', () => {
  const read = path => readFileSync(new URL(path, import.meta.url), 'utf8');
  const capture = read('../design-v6/capture-account-layout.mjs');
  const main = read('../design-v6/capture-public-ssr.mjs');
  const workflow = read('../../../../.github/workflows/production.yml');
  assert.ok(capture.includes('if (zoomEvidence) layout.nativeZoom = zoomEvidence'));
  assert.ok(capture.includes('nativeViewportRepairs < 2'));
  assert.ok(capture.includes('layout.width === width && layout.height === height'));
  assert.ok(capture.includes('if (!nativeZoom) await withBottomSafeArea'));
  assert.ok(main.includes("join(tempRoot, 'Default/Preferences')"));
  assert.ok(main.includes('report.fullCampaignRequired = preflightPlan.fullCampaignRequired'));
  assert.ok(workflow.includes("V6_NATIVE_ZOOM: '200'"));
  assert.ok(workflow.indexOf('id: v6-native-zoom') < workflow.indexOf('id: v6-review'));
  assert.doesNotMatch(read('../design-v6/native-browser-zoom.mjs'), /setPageScaleFactor|setDeviceMetricsOverride|style\.zoom/);
});
