export function isPublicCommentSsrRoute(path: string): boolean {
  return /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/comments\/?$/i.test(path)
    || /^\/[a-z]{2}\/park\/[^/]+\/[^/]+\/item\/[^/]+\/[^/]+\/comments\/?$/i.test(path);
}
