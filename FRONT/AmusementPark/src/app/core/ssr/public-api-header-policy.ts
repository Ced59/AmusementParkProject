const hiddenApiHeaderNames: ReadonlySet<string> = new Set<string>([
  'content-security-policy',
  'content-security-policy-report-only',
  'strict-transport-security',
  'x-powered-by'
]);

export function isApiHeaderHiddenFromPublicProxy(name: string): boolean {
  return hiddenApiHeaderNames.has(name.toLowerCase());
}
