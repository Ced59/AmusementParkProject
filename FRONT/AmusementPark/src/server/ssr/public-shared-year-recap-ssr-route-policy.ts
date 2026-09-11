export function isPublicSharedYearRecapSsrRoute(path: string): boolean {
  return /^\/[a-z]{2}\/passport\/shared\/years\/[^/]+\/?$/i.test(path);
}
