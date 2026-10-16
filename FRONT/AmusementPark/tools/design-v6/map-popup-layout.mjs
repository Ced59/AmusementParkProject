// Centre the actual map in the usable viewport, after responsive page reflow.
// This scrolls the document only; it never pans Leaflet or reopens a popup.
export function mapReviewScrollDelta({ mapTop, mapHeight, viewportHeight, bottomNavigationHeight }) {
  const targetTop = Math.max(0, (viewportHeight - bottomNavigationHeight - mapHeight) / 2);
  return mapTop - targetTop;
}

export function mapPopupLayoutIssues(layout) {
  if (!layout.map || !layout.popup || !layout.heading || !layout.close || !layout.actions?.length) return ['missing actual map popup content'];
  const inside = (child, parent) => child.x >= parent.x - 1 && child.right <= parent.right + 1
    && child.y >= parent.y - 1 && child.bottom <= parent.bottom + 1;
  const issues = [];
  if (!inside(layout.popup, layout.map)) issues.push('popup clipped by map bounds');
  if (layout.heading && !layout.heading.unobscured) issues.push('popup heading covered by map controls');
  for (const control of [layout.close, ...layout.actions]) {
    if (!inside(control, layout.popup)) issues.push('popup control outside its surface');
    if (control.right - control.x < 24 || control.bottom - control.y < 24) issues.push('compressed popup target');
    if (!control.unobscured) issues.push('popup control covered or outside the visible viewport');
  }
  return issues;
}
