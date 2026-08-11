const hiddenApiHeaderNames: ReadonlySet<string> = new Set<string>([
  'content-security-policy',
  'content-security-policy-report-only',
  'strict-transport-security',
  'x-accel-buffering',
  'x-powered-by'
]);

const hopByHopHeaderNames: ReadonlySet<string> = new Set<string>([
  'connection',
  'keep-alive',
  'proxy-authenticate',
  'proxy-authorization',
  'te',
  'trailer',
  'transfer-encoding',
  'upgrade',
]);

export function isApiHeaderHiddenFromPublicProxy(name: string): boolean {
  return hiddenApiHeaderNames.has(name.toLowerCase());
}

export function isHopByHopHttpHeader(name: string): boolean {
  return hopByHopHeaderNames.has(name.toLowerCase());
}
