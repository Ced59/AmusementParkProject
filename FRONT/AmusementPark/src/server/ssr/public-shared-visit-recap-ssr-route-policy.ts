export function isPublicSharedVisitRecapSsrRoute(path: string): boolean {
  return /^\/[a-z]{2}\/passport\/shared\/visits\/[^/]+\/?$/i.test(path);
}
