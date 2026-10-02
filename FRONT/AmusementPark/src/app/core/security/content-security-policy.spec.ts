import { buildContentSecurityPolicy, CROSS_ORIGIN_OPENER_POLICY } from './content-security-policy';
import { SUPPORTED_VIDEO_EMBED_ORIGINS } from './video-embed-policy';

describe('Content security policy', () => {
  it('uses the response nonce and strict dynamic instead of unrestricted inline scripts', () => {
    const policy: string = buildContentSecurityPolicy({ allowLocalSources: false, reportUri: '/api/security/csp-report', scriptNonce: 'a'.repeat(32) });
    const scriptDirective: string | undefined = policy.split('; ').find((directive: string) => directive.startsWith('script-src '));
    expect(scriptDirective).toContain(`'nonce-${'a'.repeat(32)}'`);
    expect(scriptDirective).toContain("'strict-dynamic'");
    expect(scriptDirective).not.toContain("'unsafe-inline'");
    expect(policy).toContain("frame-ancestors 'none'");
    expect(policy).toContain("style-src 'self' 'unsafe-inline'");
    expect(policy).toContain('https://accounts.google.com');
  });

  it('rejects a nonce that could inject an extra CSP directive', () => {
    expect(() => buildContentSecurityPolicy({ allowLocalSources: false, reportUri: '/api/security/csp-report', scriptNonce: "bad'; script-src *" })).toThrow();
  });

  it('keeps opener isolation compatible with authentication popups', () => {
    expect(CROSS_ORIGIN_OPENER_POLICY).toBe('same-origin-allow-popups');
  });

  it('allows every supported public video embed origin', () => {
    const policy: string = buildContentSecurityPolicy({
      allowLocalSources: false,
      reportUri: '/api/security/csp-report',
    });
    const frameDirective: string | undefined = policy
      .split('; ')
      .find((directive: string): boolean => directive.startsWith('frame-src '));

    expect(frameDirective).toBeDefined();

    for (const origin of SUPPORTED_VIDEO_EMBED_ORIGINS) {
      expect(frameDirective, origin).toContain(origin);
    }
  });

  it('keeps local development sources out of the production policy', () => {
    const policy: string = buildContentSecurityPolicy({
      allowLocalSources: false,
      reportUri: '/api/security/csp-report',
    });

    expect(policy).not.toContain('localhost:*');
    expect(policy).not.toContain('amusement.localhost:*');
  });
});
