import { isPublicCommentSsrRoute } from './public-comment-ssr-route-policy';

describe('public comment SSR route policy', () => {
  it('classifies park and park item comment pages for cache and critical rendering policies', () => {
    expect(
      isPublicCommentSsrRoute('/fr/park/park-1/parc-test/comments'),
    ).toBe(true);
    expect(
      isPublicCommentSsrRoute(
        '/fr/park/park-1/parc-test/item/item-1/attraction-test/comments',
      ),
    ).toBe(true);
  });

  it('does not classify malformed or unrelated routes as comment pages', () => {
    expect(
      isPublicCommentSsrRoute('/fr/park/park-1/parc-test/comments/extra'),
    ).toBe(false);
    expect(
      isPublicCommentSsrRoute('/fr/park/park-1/parc-test/item/item-1/comments'),
    ).toBe(false);
    expect(isPublicCommentSsrRoute('/fr/comments')).toBe(false);
  });
});
