import {
  hasFacebookImageOverrideQuery,
  hasOnlyFacebookImageOverrideQuery,
  injectFacebookAppIdMeta,
  injectFacebookImageOverrideMeta,
  normalizeFacebookImageOverrideCacheUrl,
  normalizeFacebookAppId,
  resolveFacebookOpenGraphAppId,
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
    const html: string = '<html><head>'
      + '<meta content="old" property="fb:app_id">'
      + '<meta property="fb:app_id" content="duplicate">'
      + '</head></html>';

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

  it('overrides social image metadata for a validated Facebook image query', () => {
    const html: string = '<html><head>'
      + '<meta property="og:image" content="https://example.test/default.png">'
      + '<meta property="og:image:secure_url" content="https://example.test/default.png">'
      + '<meta property="og:image:type" content="image/png">'
      + '<meta property="og:image:width" content="1200">'
      + '<meta property="og:image:height" content="630">'
      + '<meta name="twitter:image" content="https://example.test/default.png">'
      + '</head><body></body></html>';

    const result: string = injectFacebookImageOverrideMeta(
      html,
      '/fr/park/park-1/test?facebook-image=image_1',
      'https://amusement-parks.fun/fr/park/park-1/test?facebook-image=image_1',
    );

    expect(result).toContain(
      'property="og:image" content="https://amusement-parks.fun/api/images/binary/image_1/social-preview-v2'
      + '?expectedOwnerType=PARK&amp;expectedOwnerId=park-1&amp;expectedCategory=PARK"',
    );
    expect(result).toContain(
      'property="og:image:secure_url" content="https://amusement-parks.fun/api/images/binary/image_1/social-preview-v2'
      + '?expectedOwnerType=PARK&amp;expectedOwnerId=park-1&amp;expectedCategory=PARK"',
    );
    expect(result).toContain('property="og:image:type" content="image/jpeg"');
    expect(result).toContain(
      'name="twitter:image" content="https://amusement-parks.fun/api/images/binary/image_1/social-preview-v2'
      + '?expectedOwnerType=PARK&amp;expectedOwnerId=park-1&amp;expectedCategory=PARK"',
    );
    expect(result).not.toContain('property="og:image:width"');
    expect(result).not.toContain('property="og:image:height"');
  });

  it('removes every competing single-valued social image tag', () => {
    const html: string = '<html><head>'
      + '<title>Park title</title>'
      + '<link rel="canonical" href="https://amusement-parks.fun/fr/park/park-1/test">'
      + '<link rel="alternate" hreflang="fr" href="https://amusement-parks.fun/fr/park/park-1/test">'
      + '<meta name="robots" content="index,follow">'
      + '<meta property="og:title" content="Park title">'
      + '<meta property="og:description" content="Park description">'
      + '<meta property="og:image" content="https://example.test/old-1.jpg">'
      + '<meta content="https://example.test/old-2.jpg" property="og:image">'
      + '<meta property="og:image:secure_url" content="https://example.test/old-1.jpg">'
      + '<meta property="og:image:secure_url" content="https://example.test/old-2.jpg">'
      + '<meta property="og:image:type" content="image/png">'
      + '<meta property="og:image:type" content="image/webp">'
      + '<meta property="og:image:width" content="1200">'
      + '<meta property="og:image:width" content="800">'
      + '<meta property="og:image:height" content="630">'
      + '<meta property="og:image:height" content="600">'
      + '<meta name="twitter:image" content="https://example.test/old-1.jpg">'
      + '<meta content="https://example.test/old-2.jpg" name="twitter:image">'
      + '</head><body>Park</body></html>';

    const result: string = injectFacebookImageOverrideMeta(
      html,
      '/fr/park/park-1/test?facebook-image=image-1',
      'https://amusement-parks.fun/fr/park/park-1/test?facebook-image=image-1',
    );

    expect(result.match(/property="og:image"/g)).toHaveLength(1);
    expect(result.match(/property="og:image:secure_url"/g)).toHaveLength(1);
    expect(result.match(/property="og:image:type"/g)).toHaveLength(1);
    expect(result.match(/name="twitter:image"/g)).toHaveLength(1);
    expect(result).not.toContain('property="og:image:width"');
    expect(result).not.toContain('property="og:image:height"');
    expect(result).not.toContain('old-1.jpg');
    expect(result).not.toContain('old-2.jpg');
    expect(result).toContain('<title>Park title</title>');
    expect(result).toContain(
      'rel="canonical" href="https://amusement-parks.fun/fr/park/park-1/test"',
    );
    expect(result).not.toContain(
      'rel="canonical" href="https://amusement-parks.fun/fr/park/park-1/test?',
    );
    expect(result).toContain('hreflang="fr"');
    expect(result).toContain('name="robots" content="index,follow"');
    expect(result).toContain('property="og:title" content="Park title"');
    expect(result).toContain('property="og:description" content="Park description"');
  });

  it('accepts the image override alongside existing query parameters', () => {
    const html: string = '<html><head><meta property="og:image" content="default"></head></html>';

    expect(hasFacebookImageOverrideQuery(
      '/fr/park/park-1/test?utm_source=facebook&facebook-image=image-1',
    )).toBe(true);
    expect(hasOnlyFacebookImageOverrideQuery(
      '/fr/park/park-1/test?utm_source=facebook&facebook-image=image-1',
    )).toBe(false);
    expect(hasOnlyFacebookImageOverrideQuery(
      '/fr/park/park-1/test?facebook-image=image-1',
    )).toBe(true);
    expect(injectFacebookImageOverrideMeta(
      html,
      '/fr/park/park-1/test?utm_source=facebook&facebook-image=image-1',
      'https://amusement-parks.fun/fr/park/park-1/test',
    )).toContain('expectedOwnerId=park-1');
  });

  it('requires the application identifier only when Facebook publishing is enabled', () => {
    expect(resolveFacebookOpenGraphAppId(undefined, 'false')).toBeNull();
    expect(resolveFacebookOpenGraphAppId(undefined, undefined)).toBeNull();
    expect(resolveFacebookOpenGraphAppId('123456789012345', 'true'))
      .toBe('123456789012345');
    expect(() => resolveFacebookOpenGraphAppId(undefined, 'true')).toThrowError(
      'FACEBOOK_APP_ID must be configured when SOCIAL_PUBLISHING_FACEBOOK_ENABLED is true.',
    );
    expect(() => resolveFacebookOpenGraphAppId('not-numeric', ' TRUE ')).toThrowError();
  });

  it('uses the base page cache URL for a sole image override', () => {
    expect(normalizeFacebookImageOverrideCacheUrl(
      '/fr/park/park-1/test?facebook-image=image-1',
    )).toBe('/fr/park/park-1/test');
    expect(normalizeFacebookImageOverrideCacheUrl(
      '/fr/park/park-1/test?facebook-image=another-image',
    )).toBe('/fr/park/park-1/test');
    expect(normalizeFacebookImageOverrideCacheUrl(
      'https://amusement-parks.fun/fr/park/park-1/test?facebook-image=image-1',
    )).toBe('https://amusement-parks.fun/fr/park/park-1/test');
    expect(normalizeFacebookImageOverrideCacheUrl(
      '/fr/park/park-1/test?utm_source=facebook&facebook-image=image-1',
    )).toBe('/fr/park/park-1/test?utm_source=facebook&facebook-image=image-1');
  });

  it('binds a park item override to that item ownership', () => {
    const html: string = '<html><head><meta property="og:image" content="default"></head></html>';

    const result: string = injectFacebookImageOverrideMeta(
      html,
      '/fr/park/park-1/test/item/item-1/roller?facebook-image=image-1',
      'https://amusement-parks.fun/fr/park/park-1/test/item/item-1/roller',
    );

    expect(result).toContain('expectedOwnerType=PARK_ITEM');
    expect(result).toContain('expectedOwnerId=item-1');
    expect(result).toContain('expectedCategory=PARK_ITEM');
  });

  it('ignores invalid, duplicated, or ownerless Facebook image overrides', () => {
    const html: string = '<html><head><meta property="og:image" content="default"></head></html>';

    expect(hasFacebookImageOverrideQuery('/fr/home?facebook-image=image-1')).toBe(false);
    expect(hasFacebookImageOverrideQuery(
      '/fr/park/park-1/test?facebook-image=image-1&facebook-image=image-2',
    )).toBe(false);
    expect(injectFacebookImageOverrideMeta(
      html,
      '/fr/home?facebook-image=%3Cscript%3E',
      'https://amusement-parks.fun/fr/home',
    )).toBe(html);
  });
});
