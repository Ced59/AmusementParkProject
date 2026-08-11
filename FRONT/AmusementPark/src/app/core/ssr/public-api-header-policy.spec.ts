import {
  isApiHeaderHiddenFromPublicProxy,
  isHopByHopHttpHeader,
} from './public-api-header-policy';

describe('public API header policy', () => {
  it('hides upstream headers owned by the public edge or SSR layer', () => {
    expect(isApiHeaderHiddenFromPublicProxy('Content-Security-Policy')).toBe(
      true,
    );
    expect(
      isApiHeaderHiddenFromPublicProxy('Content-Security-Policy-Report-Only'),
    ).toBe(true);
    expect(isApiHeaderHiddenFromPublicProxy('Strict-Transport-Security')).toBe(
      true,
    );
    expect(isApiHeaderHiddenFromPublicProxy('X-Powered-By')).toBe(true);
    expect(isApiHeaderHiddenFromPublicProxy('X-Accel-Buffering')).toBe(true);
  });

  it('identifies hop-by-hop headers that must not cross the Node API proxy', () => {
    expect(isHopByHopHttpHeader('Connection')).toBe(true);
    expect(isHopByHopHttpHeader('Keep-Alive')).toBe(true);
    expect(isHopByHopHttpHeader('Transfer-Encoding')).toBe(true);
    expect(isHopByHopHttpHeader('Upgrade')).toBe(true);
    expect(isHopByHopHttpHeader('Content-Length')).toBe(false);
    expect(isHopByHopHttpHeader('Content-Type')).toBe(false);
  });

  it('keeps regular API response headers visible', () => {
    expect(isApiHeaderHiddenFromPublicProxy('Cache-Control')).toBe(false);
    expect(isApiHeaderHiddenFromPublicProxy('Content-Type')).toBe(false);
    expect(isApiHeaderHiddenFromPublicProxy('ETag')).toBe(false);
  });

  it('matches header names case-insensitively', () => {
    expect(isApiHeaderHiddenFromPublicProxy('strict-transport-security')).toBe(
      true,
    );
    expect(isApiHeaderHiddenFromPublicProxy('STRICT-TRANSPORT-SECURITY')).toBe(
      true,
    );
  });
});
