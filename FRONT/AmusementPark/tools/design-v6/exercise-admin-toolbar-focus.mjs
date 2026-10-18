// Presentation only: focus and local scrolling, never a view-mode/edit action.
export async function exerciseAdminToolbarFocus({ evaluate, waitUntil, result, label = 'v6' }) {
  if (!['master', 'v6'].includes(label)) throw new Error('Unknown toolbar comparison label.');
  const initial = await evaluate(`(() => { const buttons = [...document.querySelectorAll('.admin-public-view-toolbar button')];
    return { width: innerWidth, height: innerHeight, rootFontSizePx: parseFloat(getComputedStyle(document.documentElement).fontSize),
      modes: document.querySelectorAll('.admin-public-view-toolbar__mode').length,
      indexes: buttons.flatMap((button, index) => button.disabled ? [] : [index]) }; })()`);
  if (initial.modes !== 4 || initial.indexes.length < 5) throw new Error('The existing admin toolbar controls are incomplete.');
  const measurements = [];
  for (const index of initial.indexes) {
    let state;
    const probe = async () => {
      state = await evaluate(`(() => { const button = document.querySelectorAll('.admin-public-view-toolbar button')[${index}];
        button.scrollIntoView({ block: 'nearest', inline: 'center', behavior: 'instant' }); button.focus({ preventScroll: true });
        const box = button.getBoundingClientRect(); const toolbar = button.closest('.admin-public-view-toolbar').getBoundingClientRect();
        const header = document.querySelector('.app-public-nav')?.getBoundingClientRect(); const nav = document.querySelector('.app-mobile-bottom-nav')?.getBoundingClientRect();
        return { index: ${index}, x: box.x, y: box.y, right: box.right, bottom: box.bottom, width: box.width, height: box.height,
          focused: document.activeElement === button,
          visible: box.x >= toolbar.x - 1 && box.right <= toolbar.right + 1 && box.y >= Math.max(toolbar.y, header?.bottom ?? 0) - 1
            && box.bottom <= Math.min(toolbar.bottom, nav?.height > 0 ? nav.top : innerHeight) + 1,
          unobscured: button.contains(document.elementFromPoint(box.x + box.width / 2, box.y + box.height / 2)) }; })()`);
      result.lastToolbarFocus = state;
      return state.focused && state.visible && state.unobscured && state.width >= 24 && state.height >= 40;
    };
    if (label === 'master') {
      // Preserve old geometry findings honestly; never relax the V6 gate.
      await probe();
    } else {
      await waitUntil(probe, `admin toolbar control ${index} remains focusable and uncovered in the short viewport`);
    }
    measurements.push(state);
  }
  (result.toolbarFocusResults ??= []).push({ ...initial, label, measurements,
    layoutPass: measurements.every(state => state.focused && state.visible && state.unobscured && state.width >= 24 && state.height >= 40), actionsInvoked: false });
}
