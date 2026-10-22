export function isPublicStandaloneAttractionArticleRoute(path: string): boolean {
  return /^\/[a-z]{2}\/attraction\/[^/]+\/[^/]+\/history\/(?!page\/)[^/]+\/[^/]+\/?$/i.test(path);
}
