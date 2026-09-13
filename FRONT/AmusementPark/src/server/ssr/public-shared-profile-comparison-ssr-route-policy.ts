export function isPublicSharedProfileComparisonSsrRoute(path: string): boolean {
  return /^\/[a-z]{2}\/passport\/shared\/comparisons\/[^/]+\/?$/i.test(path);
}
