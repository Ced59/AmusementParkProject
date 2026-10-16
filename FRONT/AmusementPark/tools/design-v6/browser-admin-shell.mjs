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
  return { adminPath, width: innerWidth, height: innerHeight, entries,
    bottomItemCount: document.querySelectorAll('.app-mobile-bottom-nav__item').length,
    toolbar: rect('.admin-public-view-toolbar'), bottomNavigation: rect('.app-mobile-bottom-nav') };
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
  if (!toolbar || toolbar.width <= 0 || toolbar.height <= 0 || toolbar.x < -1 || toolbar.y < -1
    || toolbar.right > layout.width + 1 || toolbar.bottom > layout.height + 1) {
    issues.push('public admin toolbar missing or outside the viewport');
  }
  const nav = layout.bottomNavigation;
  if (toolbar && nav?.height > 0 && toolbar.x < nav.right && toolbar.right > nav.x
    && toolbar.y < nav.bottom && toolbar.bottom > nav.y) issues.push('admin toolbar overlaps the mobile navigation');
  return issues;
}
