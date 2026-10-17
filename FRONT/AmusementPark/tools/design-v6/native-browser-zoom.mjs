// Chromium's own profile zoom, not pinch zoom, CSS zoom or a font override.
// References and limits are recorded in V6-VISUAL-REGRESSION.md.
export function nativeBrowserZoomPlan(environment, platform, preflight) {
  if (environment.V6_NATIVE_ZOOM === undefined) return null;
  if (environment.V6_NATIVE_ZOOM !== '200' || environment.CI !== 'true' || platform !== 'linux'
    || environment.V6_ISOLATED_PREFLIGHT !== 'true' || preflight?.phase !== 'admin') {
    throw new Error('Native 200% zoom is restricted to the explicit Linux CI admin-shell diagnostic.');
  }
  return {
    factor: 2,
    cssWidths: [320, 683, 960],
    preferences: { partition: { default_zoom_level: { x: Math.log(2) / Math.log(1.2) } } },
    browserArguments: ['--force-device-scale-factor=1'],
  };
}

export function nativeZoomIssues(metrics) {
  const issues = [];
  if (!Number.isFinite(metrics?.devicePixelRatio) || Math.abs(metrics.devicePixelRatio - 2) > 0.001) issues.push('native page zoom is not 200% at device scale 1');
  if (!Number.isFinite(metrics?.visualScale) || Math.abs(metrics.visualScale - 1) > 0.001) issues.push('pinch zoom is active');
  for (const name of ['rootZoom', 'bodyZoom']) {
    if (!['1', 'normal'].includes(metrics?.[name])) issues.push(`${name} is not unscaled`);
  }
  return issues;
}

export async function resizeNativeZoomViewport({ cdp, evaluate, width, height }) {
  if (![320, 683, 960].includes(width) || ![568, 600, 1200].includes(height)) throw new Error('Unbounded native zoom viewport.');
  // Resize the real contents in DIP, not outer bounds with an assumed browser
  // chrome height. No device-metric/page-scale emulation substitutes for zoom.
  await cdp.send('Emulation.clearDeviceMetricsOverride');
  const { windowId } = await cdp.send('Browser.getWindowForTarget');
  const measurements = [];
  let previous;
  const failure = message => Object.assign(new Error(message), { nativeZoomEvidence: { width, height, windowId, measurements } });
  for (let attempt = 0; attempt < 5; attempt++) {
    try {
      await cdp.send('Browser.setContentsSize', { windowId, width: width * 2, height: height * 2 });
    } catch (error) {
      throw failure(`Native content resize unavailable: ${error.message}`);
    }
    const metrics = await evaluate(`new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve({
      width: innerWidth, height: innerHeight, outerWidth, outerHeight, devicePixelRatio,
      visualScale: visualViewport?.scale ?? null,
      rootZoom: getComputedStyle(document.documentElement).zoom,
      bodyZoom: getComputedStyle(document.body).zoom
    }))))`);
    measurements.push(metrics);
    const issues = nativeZoomIssues(metrics);
    if (issues.length) throw failure(`Native zoom refused: ${issues.join('; ')}.`);
    if (metrics.width === width && metrics.height === height && previous?.width === width && previous?.height === height) {
      return { ...metrics, factor: 2, physicalDeviceScale: 1, resizeAttempts: attempt + 1, sizingMethod: 'Browser.setContentsSize' };
    }
    previous = metrics;
  }
  throw failure('The native 200% viewport did not settle at its exact CSS dimensions.');
}
