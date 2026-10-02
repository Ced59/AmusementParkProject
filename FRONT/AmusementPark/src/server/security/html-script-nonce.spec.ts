import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { CSP_NONCE_PLACEHOLDER, prepareHtmlScriptNonceResponse } from './html-script-nonce';

describe('HTML script nonce responses', () => {
  const template: string = `<head><script nonce="${CSP_NONCE_PLACEHOLDER}" data-app-theme-init>theme()</script><link rel="modulepreload" href="chunk-ABC123.js"><style nonce="${CSP_NONCE_PLACEHOLDER}">body{color:red}</style></head><body><app-root ngcspnonce="${CSP_NONCE_PLACEHOLDER}">Contenu français</app-root><script nonce="${CSP_NONCE_PLACEHOLDER}" src="main-ABC123.js" type="module"></script><script nonce="${CSP_NONCE_PLACEHOLDER}">window.__jsaction_bootstrap()</script></body>`;

  it('replaces all Angular and build nonce attributes with one unpredictable response nonce', () => {
    const result = prepareHtmlScriptNonceResponse(template);
    expect(result).not.toBeNull();
    expect(result?.nonce).toMatch(/^[A-Za-z0-9+/]{32}$/);
    expect(result?.html).not.toContain(CSP_NONCE_PLACEHOLDER);
    expect(result?.html.match(new RegExp(`nonce="${result?.nonce.replace(/[+\/]/g, '\\$&')}"`, 'g'))).toHaveLength(6);
    expect(result?.html).toContain('Contenu français');
  });

  it('issues different nonces for repeated requests from the same immutable cached HTML', () => {
    const first = prepareHtmlScriptNonceResponse(template);
    const second = prepareHtmlScriptNonceResponse(template);
    expect(first?.nonce).not.toBe(second?.nonce);
    expect(template).toContain(CSP_NONCE_PLACEHOLDER);
  });

  it('does not authorize injected scripts, event attributes or external module preloads', () => {
    const unsafe: string = '<script>untrusted()</script><img onerror="untrusted()"><link rel="modulepreload" href="https://untrusted.example/chunk-ABC123.js"><link rel="modulepreload" href="other.js">';
    const result = prepareHtmlScriptNonceResponse(template + unsafe);
    expect(result?.html).toContain(unsafe);
  });

  it('handles single quotes and case-insensitive Angular attributes', () => {
    const result = prepareHtmlScriptNonceResponse(`<app-root ngCspNonce='${CSP_NONCE_PLACEHOLDER}'></app-root><script NONCE='${CSP_NONCE_PLACEHOLDER}'>theme()</script>`);
    expect(result?.html).toContain(`ngCspNonce='${result?.nonce}'`);
    expect(result?.html).toContain(`NONCE='${result?.nonce}'`);
  });

  it('preserves documents without the build placeholder', () => {
    expect(prepareHtmlScriptNonceResponse('<html><body>Legacy cache entry</body></html>')).toBeNull();
    expect(prepareHtmlScriptNonceResponse(`<html><body>${CSP_NONCE_PLACEHOLDER}</body></html>`)).toBeNull();
    expect(prepareHtmlScriptNonceResponse(`<div data-nonce="${CSP_NONCE_PLACEHOLDER}"></div>`)).toBeNull();
  });

  it('does not mistake data attributes or duplicate destinations for local build preloads', () => {
    const untrusted: string = '<link rel="modulepreload" href="https://untrusted.example/main-ABC123.js" data-href="main-ABC123.js"><link rel="modulepreload" href="https://untrusted.example/main-ABC123.js" href="main-ABC123.js"><link data-rel="modulepreload" href="main-ABC123.js">';
    expect(prepareHtmlScriptNonceResponse(template + untrusted)?.html).toContain(untrusted);
  });

  it('keeps the source index compatible with Angular CLI nonce propagation', () => {
    const index: string = readFileSync(resolve(process.cwd(), 'src/index.html'), 'utf8');
    expect(index).toContain(`<app-root ngCspNonce="${CSP_NONCE_PLACEHOLDER}">`);
  });
});
