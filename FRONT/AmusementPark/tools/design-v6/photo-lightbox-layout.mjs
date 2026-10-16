export const photoLightboxLayoutExpression = `(() => {
  const rect = selector => { const node = document.querySelector(selector); if (!node) return null;
    const { x, y, width, height, right, bottom } = node.getBoundingClientRect(); return { x, y, width, height, right, bottom }; };
  return { width: innerWidth, height: innerHeight, documentWidth: document.documentElement.scrollWidth,
    theme: document.body.classList.contains('light-mode') ? 'light' : 'dark',
    dialog: rect('.photo-lightbox__dialog'), close: rect('.photo-lightbox__close'),
    closeUnobscured: (() => { const node = document.querySelector('.photo-lightbox__close'); if (!node) return false;
      const r = node.getBoundingClientRect(); return node.contains(document.elementFromPoint(r.x + r.width / 2, r.y + r.height / 2)); })(),
    image: rect('img.photo-lightbox__image'), footer: rect('.photo-lightbox__footer') };
})()`;

export function photoLightboxLayoutIssues(layout) {
  const issues = [];
  const { width, height, dialog, close, image, footer, documentWidth } = layout;
  if (!dialog || !close || !image || !footer) return ['missing visible lightbox content'];
  if (documentWidth > width + 1) issues.push('document horizontal overflow');
  if (dialog.x < -1 || dialog.right > width + 1 || dialog.y < -1 || dialog.bottom > height + 1) issues.push('dialog outside viewport');
  if (close.y < -1 || close.bottom > height + 1 || close.x < -1 || close.right > width + 1) issues.push('close control outside viewport');
  if (layout.closeUnobscured === false) issues.push('close control covered by another layer');
  if (image.bottom > footer.y + 1) issues.push('image overlaps caption or thumbnails');
  if (image.width < 80 || image.height < 80) issues.push('image compressed into an unusable region');
  return issues;
}
