import { SafeExternalUrlPipe } from './safe-external-url.pipe';
import { UrlSecurityService } from '@shared/utils/security/url-security.service';

describe('SafeExternalUrlPipe', () => {
  let pipe: SafeExternalUrlPipe;

  beforeEach(() => {
    pipe = new SafeExternalUrlPipe(new UrlSecurityService());
  });

  it('returns sanitized external urls', () => {
    expect(pipe.transform('example.com')).toBe('https://example.com');
  });

  it('returns null for unsafe urls', () => {
    expect(pipe.transform('javascript:alert(1)')).toBeNull();
  });
});
