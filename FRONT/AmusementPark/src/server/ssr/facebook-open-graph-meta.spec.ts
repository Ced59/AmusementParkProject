import {
  injectFacebookAppIdMeta,
  normalizeFacebookAppId,
} from './facebook-open-graph-meta';

describe('Facebook Open Graph metadata', () => {
  it('normalizes a numeric Facebook application identifier', () => {
    expect(normalizeFacebookAppId(' 123456789012345 ')).toBe('123456789012345');
  });

  it('rejects missing and non-numeric identifiers', () => {
    expect(normalizeFacebookAppId(undefined)).toBeNull();
    expect(normalizeFacebookAppId('')).toBeNull();
    expect(normalizeFacebookAppId('123<script>')).toBeNull();
  });

  it('inserts the application identifier before the closing head tag', () => {
    const html: string = '<html><head><title>Park</title></head><body></body></html>';

    const result: string = injectFacebookAppIdMeta(html, '123456789012345');

    expect(result).toContain('<meta property="fb:app_id" content="123456789012345">');
    expect(result.indexOf('property="fb:app_id"')).toBeLessThan(result.indexOf('</head>'));
  });

  it('replaces an existing identifier without duplicating the tag', () => {
    const html: string = '<html><head><meta content="old" property="fb:app_id"></head></html>';

    const result: string = injectFacebookAppIdMeta(html, '123456789012345');

    expect(result).toContain('<meta property="fb:app_id" content="123456789012345">');
    expect(result.match(/property="fb:app_id"/g)).toHaveLength(1);
  });

  it('leaves HTML unchanged when configuration or head markup is unavailable', () => {
    const html: string = '<html><head></head><body></body></html>';

    expect(injectFacebookAppIdMeta(html, 'invalid')).toBe(html);
    expect(injectFacebookAppIdMeta('<div>fragment</div>', '123456789012345'))
      .toBe('<div>fragment</div>');
  });
});
