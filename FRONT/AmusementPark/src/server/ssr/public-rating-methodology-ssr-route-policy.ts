export function isPublicRatingMethodologySsrRoute(path: string): boolean {
  return /^\/[a-z]{2}\/rankings\/methodology(?:\/[^/]+)?\/?$/i.test(path);
}
