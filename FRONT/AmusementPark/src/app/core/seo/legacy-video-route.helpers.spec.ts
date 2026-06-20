import { buildCanonicalVideoRouteRedirectPath } from './legacy-video-route.helpers';

describe('legacy video route helpers', () => {
  it('redirects legacy park video routes to canonical video routes', () => {
    expect(buildCanonicalVideoRouteRedirectPath('/fr/park/park-1/demo-park/video/s/video-1/demo-video'))
      .toBe('/fr/park/park-1/demo-park/videos/video-1/demo-video');
    expect(buildCanonicalVideoRouteRedirectPath('/fr/park/park-1/demo-park/video/video-1/demo-video'))
      .toBe('/fr/park/park-1/demo-park/videos/video-1/demo-video');
  });

  it('redirects legacy item video routes to canonical video routes', () => {
    expect(buildCanonicalVideoRouteRedirectPath('/fr/park/park-1/demo-park/item/item-1/demo-item/video/s/video-1/demo-video'))
      .toBe('/fr/park/park-1/demo-park/item/item-1/demo-item/videos/video-1/demo-video');
    expect(buildCanonicalVideoRouteRedirectPath('/fr/park/park-1/demo-park/item/item-1/demo-item/video/video-1/demo-video'))
      .toBe('/fr/park/park-1/demo-park/item/item-1/demo-item/videos/video-1/demo-video');
  });

  it('keeps query strings on redirects', () => {
    expect(buildCanonicalVideoRouteRedirectPath('/fr/park/park-1/demo-park/video/s/video-1/demo-video?share=x'))
      .toBe('/fr/park/park-1/demo-park/videos/video-1/demo-video?share=x');
  });

  it('ignores canonical and unrelated routes', () => {
    expect(buildCanonicalVideoRouteRedirectPath('/fr/park/park-1/demo-park/videos/video-1/demo-video')).toBeNull();
    expect(buildCanonicalVideoRouteRedirectPath('/fr/park/park-1/demo-park/item/item-1/demo-item')).toBeNull();
  });
});
