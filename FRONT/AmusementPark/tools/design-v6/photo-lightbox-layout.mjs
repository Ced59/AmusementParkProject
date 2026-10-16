export function photoLightboxLayoutIssues(layout) {
  const issues = [];
  const { width, height, dialog, close, image, footer, documentWidth } = layout;
  if (!dialog || !close || !image || !footer) return ['missing visible lightbox content'];
  if (documentWidth > width + 1) issues.push('document horizontal overflow');
  if (dialog.x < -1 || dialog.right > width + 1 || dialog.y < -1 || dialog.bottom > height + 1) issues.push('dialog outside viewport');
  if (close.y < -1 || close.bottom > height + 1 || close.x < -1 || close.right > width + 1) issues.push('close control outside viewport');
  if (image.bottom > footer.y + 1) issues.push('image overlaps caption or thumbnails');
  if (image.width < 80 || image.height < 80) issues.push('image compressed into an unusable region');
  return issues;
}
