import { buildShareSocialImageUrl } from './share-social-image-url';

describe('buildShareSocialImageUrl', () => {
  it('includes publication and template versions in the immutable image URL', () => {
    const result: string = buildShareSocialImageUrl(
      'passport',
      'opaque/token',
      7,
      'fr'
    );

    expect(result).toContain(
      'sharing/social-images/passport/opaque%2Ftoken/v7/t1/fr.png'
    );
  });
});
