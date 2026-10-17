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
  return { adminPath, width: innerWidth, height: innerHeight, publicLayout: !!document.querySelector('.app-public-layout'), entries,
    bottomItemCount: document.querySelectorAll('.app-mobile-bottom-nav__item').length,
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
  if (!visible.length || visible.some(entry => !entry.unobscured || entry.width < 24 || entry.height < 40
    || entry.x < -1 || entry.y < -1 || entry.right > layout.width + 1 || entry.bottom > layout.height + 1)) {
    issues.push('conditional admin navigation hidden, clipped or obscured');
  }
  const toolbar = layout.toolbar;
  if (typeof layout.publicLayout !== 'boolean') issues.push('admin shell layout scope not measured');
  if (layout.publicLayout && (!toolbar || toolbar.width <= 0 || toolbar.height <= 0 || toolbar.x < -1 || toolbar.y < -1
    || toolbar.right > layout.width + 1 || toolbar.bottom > layout.height + 1)) {
    issues.push('public admin toolbar missing or outside the viewport');
  }
  if (layout.publicLayout === false && toolbar) issues.push('unexpected public admin toolbar in the account layout');
  const nav = layout.bottomNavigation;
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
