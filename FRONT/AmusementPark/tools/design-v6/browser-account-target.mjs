// Reposition after viewport/theme reflow, then measure the actual captured target.
export function positionAccountCaptureTarget(selector) {
  const target = document.querySelector(selector);
  if (!target) throw new Error('The requested account capture target is missing.');
  target.scrollIntoView({ block: 'start', behavior: 'instant' });
  const header = document.querySelector('.app-public-nav')?.getBoundingClientRect();
  const topInset = Math.max(0, Math.min(innerHeight, header?.bottom ?? 0)) + 12;
  const first = target.getBoundingClientRect();
  if (first.top < topInset) window.scrollBy({ top: first.top - topInset, behavior: 'instant' });
  const box = target.getBoundingClientRect();
  const bottomNav = document.querySelector('.app-mobile-bottom-nav')?.getBoundingClientRect();
  const bottomInset = bottomNav?.height > 0 ? bottomNav.top : innerHeight;
  const visibleHeight = Math.max(0, Math.min(box.bottom, bottomInset) - Math.max(box.top, topInset));
  return { x: box.x, y: box.y, right: box.right, bottom: box.bottom, width: box.width, height: box.height,
    visible: box.width > 0 && box.height > 0 && visibleHeight >= Math.min(44, box.height) };
}
