export const accountContrastIssue = 'visible flat account text below its AA contrast threshold';

// Preflight may gather contrast findings, but geometry and missing coverage stop immediately.
export function accountLayoutMustStop(label, issues, deferContrastFailure = false) {
  return label === 'v6' && issues.some(issue => !deferContrastFailure || issue !== accountContrastIssue);
}

// Deferred findings still make the entire fixture fail. Master is an unchanged comparison.
export function accountLayoutsHaveFailure(label, layouts) {
  return label === 'v6' && layouts.some(layout => layout.issues.length > 0);
}
