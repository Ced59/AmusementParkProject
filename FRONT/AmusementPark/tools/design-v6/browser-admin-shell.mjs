// Browser-only presentation evidence. Does not navigate, edit or authorize a server action.
export function collectAdminShellLayout() {
  const adminPath = `/${location.pathname.split('/')[1]}/admin`;
  const entries = [...document.querySelectorAll('.app-public-nav__links a, .app-mobile-bottom-nav a')]
    .filter(link => link.getAttribute('href') === adminPath).map(link => {
      const box = link.getBoundingClientRect();
      return { scope: link.closest('.app-mobile-bottom-nav') ? 'mobile' : 'header',
        href: link.getAttribute('href'), x: box.x, y: box.y, right: box.right, bottom: box.bottom,
        width: box.width, height: box.height,
        visible: box.width > 0 && box.height > 0,
        unobscured: box.width > 0 && box.height > 0
          && link.contains(document.elementFromPoint(box.x + box.width / 2, box.y + box.height / 2)) };
    });
  const rect = selector => {
    const node = document.querySelector(selector);
    if (!node) return null;
    const box = node.getBoundingClientRect();
    return { x: box.x, y: box.y, right: box.right, bottom: box.bottom, width: box.width, height: box.height };
  };
  const measureLabels = selector => [...document.querySelectorAll(selector)].map(label => {
    const box = label.getBoundingClientRect();
    const text = label.firstChild;
    const measured = text?.nodeType === 3;
    const words = measured ? [...text.textContent.matchAll(/\S+/gu)].map(match => {
      const range = document.createRange();
      range.setStart(text, match.index);
      range.setEnd(text, match.index + match[0].length);
      const lines = new Set([...range.getClientRects()].filter(part => part.width > 0).map(part => Math.round(part.top * 2) / 2)).size;
      return { word: match[0], lines };
    }) : [];
    return { text: label.textContent, visible: box.width > 0 && box.height > 0, measured, words };
  });
  const bottomLabels = measureLabels('.app-mobile-bottom-nav__label');
  const toolbarLabels = measureLabels('.admin-public-view-toolbar__label, .admin-public-view-toolbar__mode > span:not(.app-visually-hidden), .admin-public-view-toolbar__edition > span');
  return { adminPath, width: innerWidth, height: innerHeight, scrollY: globalThis.scrollY ?? 0,
    headerNavigation: rect('.app-public-nav'), publicLayout: !!document.querySelector('.app-public-layout'), entries,
    bottomItemCount: document.querySelectorAll('.app-mobile-bottom-nav__item').length, bottomLabels, toolbarLabels,
    toolbar: rect('.admin-public-view-toolbar'), bottomNavigation: rect('.app-mobile-bottom-nav'), jumpAction: rect('.page-jump-button') };
}

export function adminShellLayoutIssues(layout) {
  const issues = [];
  if (layout.entries.length !== 2 || !['header', 'mobile'].every(scope =>
    layout.entries.filter(entry => entry.scope === scope && entry.href === layout.adminPath).length === 1)) {
    issues.push('conditional admin navigation missing or duplicated');
  }
  if (layout.bottomItemCount !== 7) issues.push('admin mobile navigation must preserve all seven entries');
  const visible = layout.entries.filter(entry => entry.visible);
  // A below-the-fold target may naturally scroll the whole existing masthead
  // above the viewport. Never confuse that with a hidden/missing entry at top.
  const scrolledHeader = Number.isFinite(layout.scrollY) && layout.scrollY > 0
    && layout.headerNavigation?.height > 0 && layout.headerNavigation.bottom <= 0;
  if (!visible.length || visible.some(entry => entry.width < 24 || entry.height < 40 || entry.x < -1 || entry.right > layout.width + 1
    || (!(scrolledHeader && entry.scope === 'header' && entry.bottom <= 0)
      && (!entry.unobscured || entry.y < -1 || entry.bottom > layout.height + 1)))) {
    issues.push('conditional admin navigation hidden, clipped or obscured');
  }
  const toolbar = layout.toolbar;
  if (typeof layout.publicLayout !== 'boolean') issues.push('admin shell layout scope not measured');
  if (layout.publicLayout && (!toolbar || toolbar.width <= 0 || toolbar.height <= 0 || toolbar.x < -1 || toolbar.y < -1
    || toolbar.right > layout.width + 1 || toolbar.bottom > layout.height + 1)) {
    issues.push('public admin toolbar missing or outside the viewport');
  }
  if (layout.publicLayout === false && toolbar) issues.push('unexpected public admin toolbar in the account layout');
  if (layout.publicLayout) {
    if (!Array.isArray(layout.toolbarLabels) || layout.toolbarLabels.length !== 6
      || layout.toolbarLabels.some(label => !label.visible || !label.measured || !label.words?.length
        || label.words.some(word => !Number.isInteger(word.lines) || word.lines < 1))) {
      issues.push('admin toolbar label readability not measured');
    } else if (layout.toolbarLabels.some(label => label.words.some(word => word.lines > 1))) {
      issues.push('admin toolbar splits words into narrow columns');
    }
  }
  const nav = layout.bottomNavigation;
  if (nav?.height > 0) {
    if (!Array.isArray(layout.bottomLabels) || layout.bottomLabels.length !== layout.bottomItemCount
      || layout.bottomLabels.some(label => !label.visible || !label.measured || !label.words?.length
        || label.words.some(word => !Number.isInteger(word.lines) || word.lines < 1))) {
      issues.push('mobile navigation label readability not measured');
    } else if (layout.bottomLabels.some(label => label.words.some(word => word.lines > 1))) {
      issues.push('mobile navigation splits words into narrow columns');
    }
  }
  if (toolbar && nav?.height > 0 && toolbar.x < nav.right && toolbar.right > nav.x
    && toolbar.y < nav.bottom && toolbar.bottom > nav.y) issues.push('admin toolbar overlaps the mobile navigation');
  const jump = layout.jumpAction;
  if (jump?.height > 0) {
    if (jump.x < -1 || jump.y < -1 || jump.right > layout.width + 1 || jump.bottom > layout.height + 1) issues.push('page jump action outside the viewport');
    if (toolbar && jump.x < toolbar.right && jump.right > toolbar.x
      && jump.y < toolbar.bottom && jump.bottom > toolbar.y) issues.push('page jump action overlaps the admin toolbar');
    if (nav?.height > 0 && jump.x < nav.right && jump.right > nav.x
      && jump.y < nav.bottom && jump.bottom > nav.y) issues.push('page jump action overlaps the mobile navigation');
  }
  return issues;
}
