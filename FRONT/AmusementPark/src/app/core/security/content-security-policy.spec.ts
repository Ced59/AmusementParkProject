import { buildContentSecurityPolicy } from './content-security-policy';
import { SUPPORTED_VIDEO_EMBED_ORIGINS } from './video-embed-policy';

describe('Content security policy', () => {
  it('allows every supported public video embed origin', () => {
    const policy: string = buildContentSecurityPolicy({
      allowLocalSources: false,
      reportUri: '/api/security/csp-report'
    });
    const frameDirective: string | undefined = policy
      .split('; ')
      .find((directive: string): boolean => directive.startsWith('frame-src '));

    expect(frameDirective).toBeDefined();

    for (const origin of SUPPORTED_VIDEO_EMBED_ORIGINS) {
      expect(frameDirective).withContext(origin).toContain(origin);
    }
  });

  it('keeps local development sources out of the production policy', () => {
    const policy: string = buildContentSecurityPolicy({
      allowLocalSources: false,
      reportUri: '/api/security/csp-report'
    });

    expect(policy).not.toContain('localhost:*');
    expect(policy).not.toContain('amusement.localhost:*');
  });
});
