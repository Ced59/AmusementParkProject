// Trusted Chromium input against the rendered map only. No Leaflet internals,
// invented coordinates, external tiles, directions or physical-device claim.
export function mapGesturePlan(target) {
  if (!target || !['x', 'y', 'viewportWidth', 'visibleBottom'].every(key => Number.isFinite(target[key])) || target.unobscured !== true
      || target.x < 30 || target.y < 30 || target.x + 90 > target.viewportWidth
      || target.y + 50 > target.visibleBottom) throw new Error('No safe visible map target for pointer acceptance.');
  return { start: { x: target.x, y: target.y }, end: { x: target.x + 60, y: target.y + 24 } };
}

export async function clickVisibleMapMarker({ cdp, evaluate, result }) {
  if (!cdp || typeof cdp.send !== 'function') throw new Error('Map marker acceptance requires native browser input.');
  const target = await evaluate(`(() => {
    const map = document.querySelector('.park-items-map .leaflet-container');
    const bounds = map.getBoundingClientRect();
    for (const marker of map.querySelectorAll('.leaflet-marker-icon:not(.app-leaflet-marker-cluster)')) {
      const rect = (marker.querySelector('.app-leaflet-marker__head') ?? marker).getBoundingClientRect();
      const x = rect.left + rect.width / 2, y = rect.top + rect.height / 2;
      const hit = document.elementFromPoint(x, y);
      if (x > Math.max(0, bounds.left) && x < Math.min(innerWidth, bounds.right)
        && y > Math.max(0, bounds.top) && y < Math.min(innerHeight, bounds.bottom) && hit && marker.contains(hit)) {
        return { x, y, viewportWidth: innerWidth, viewportHeight: innerHeight, unobscured: true };
      }
    }
    return null;
  })()`);
  if (!target || !['x', 'y', 'viewportWidth', 'viewportHeight'].every(key => Number.isFinite(target[key]))
    || target.unobscured !== true || target.x <= 0 || target.x >= target.viewportWidth
    || target.y <= 0 || target.y >= target.viewportHeight) throw new Error('No visible unobscured map marker for native pointer acceptance.');
  const point = { x: target.x, y: target.y };
  await cdp.send('Input.dispatchMouseEvent', { type: 'mouseMoved', ...point, button: 'none' });
  try {
    await cdp.send('Input.dispatchMouseEvent', { type: 'mousePressed', ...point, button: 'left', buttons: 1, clickCount: 1 });
  } finally {
    await cdp.send('Input.dispatchMouseEvent', { type: 'mouseReleased', ...point, button: 'left', buttons: 0, clickCount: 1 });
  }
  result.mapScenarios.markerClick = { ...target, source: 'trusted CDP pointer input on an actual rendered marker' };
}

export async function exerciseMapGestures({ cdp, evaluate, waitUntil, result }) {
  if (!cdp || typeof cdp.send !== 'function') throw new Error('Map gesture acceptance requires native browser input.');
  const getPlan = async () => mapGesturePlan(await evaluate(`(() => {
    const map = document.querySelector('.park-items-map .leaflet-container');
    const rect = map.getBoundingClientRect();
    const nav = document.querySelector('.app-mobile-bottom-nav');
    const navRect = nav?.getBoundingClientRect();
    const visibleBottom = Math.min(innerHeight, navRect?.height ? navRect.top : innerHeight, rect.bottom);
    for (const [fx, fy] of [[0.5, 0.5], [0.35, 0.6], [0.6, 0.4]]) {
      const x = rect.left + rect.width * fx, y = Math.max(0, rect.top) + (visibleBottom - Math.max(0, rect.top)) * fy;
      const hit = document.elementFromPoint(x, y);
      if (x + 90 < Math.min(rect.right, innerWidth) && y + 50 < visibleBottom
        && hit && map.contains(hit) && !hit.closest('.leaflet-control, .leaflet-marker-icon, .leaflet-popup')) {
        return { x, y, viewportWidth: innerWidth, visibleBottom, unobscured: true };
      }
    }
    return null;
  })()`));
  const plan = await getPlan();
  const sample = () => evaluate(`({ transform: document.querySelector('.park-items-map .leaflet-map-pane').style.transform,
    scrollY, animating: !!document.querySelector('.park-items-map .leaflet-zoom-anim, .park-items-map .leaflet-pan-anim') })`);
  const evidence = [];
  const movedAndSettled = async (before, kind) => {
    let last, stable = 0;
    await waitUntil(async () => {
      const after = await sample();
      if (Math.abs(after.scrollY - before.scrollY) > 1) throw new Error(`${kind} moved the document instead of retaining map interaction.`);
      stable = !after.animating && after.transform !== before.transform && after.transform === last ? stable + 1 : 0;
      last = after.transform;
      if (stable >= 2) { evidence.push({ kind, before, after, source: 'trusted CDP input, not a physical device' }); return true; }
      return false;
    }, `${kind} changes the real map pane, settles and does not scroll the page`);
  };
  const beforeMouse = await sample();
  await cdp.send('Input.dispatchMouseEvent', { type: 'mouseMoved', ...plan.start, button: 'none' });
  await cdp.send('Input.dispatchMouseEvent', { type: 'mousePressed', ...plan.start, button: 'left', buttons: 1, clickCount: 1 });
  try {
    for (let step = 1; step <= 6; step++) {
      await cdp.send('Input.dispatchMouseEvent', { type: 'mouseMoved', x: plan.start.x + 10 * step,
        y: plan.start.y + 4 * step, button: 'left', buttons: 1 });
      await new Promise(resolve => setTimeout(resolve, 25));
    }
  } finally {
    await cdp.send('Input.dispatchMouseEvent', { type: 'mouseReleased', ...plan.end, button: 'left', buttons: 0, clickCount: 1 });
  }
  await movedAndSettled(beforeMouse, 'mouse-drag');
  const touchPlan = await getPlan();
  const beforeTouch = await sample();
  await cdp.send('Emulation.setTouchEmulationEnabled', { enabled: true, maxTouchPoints: 1 });
  let touchActive = false;
  try {
    await cdp.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [{ ...touchPlan.start, id: 1 }] });
    touchActive = true;
    for (let step = 1; step <= 6; step++) {
      await cdp.send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [{ x: touchPlan.start.x + 10 * step,
        y: touchPlan.start.y + 4 * step, id: 1 }] });
      await new Promise(resolve => setTimeout(resolve, 25));
    }
    await cdp.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
    touchActive = false;
    await movedAndSettled(beforeTouch, 'touch-drag');
  } finally {
    try {
      if (touchActive) await cdp.send('Input.dispatchTouchEvent', { type: 'touchCancel', touchPoints: [] });
    } finally {
      await cdp.send('Emulation.setTouchEmulationEnabled', { enabled: false });
    }
  }
  result.mapScenarios.gestures = evidence;
  result.mapScenarios.pointerDragNotExercised = false;
}
