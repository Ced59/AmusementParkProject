export function isPublicSharedUserRankingSsrRoute(path: string): boolean {
  return /^\/[a-z]{2}\/rankings\/shared\/[^/]+\/?$/i.test(path);
}
