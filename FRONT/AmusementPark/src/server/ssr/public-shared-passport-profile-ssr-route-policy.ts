export function isPublicSharedPassportProfileSsrRoute(path: string): boolean {
  return /^\/[a-z]{2}\/passport\/shared\/profiles\/[^/]+\/?$/i.test(path);
}
