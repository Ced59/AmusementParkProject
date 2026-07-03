import {
  detectRobotFamilyFromUserAgent,
  shouldAllowRobotCacheMissSsrRender
} from './robot-ssr-policy';
import type { RobotFamily } from './robot-ssr-policy';

describe('robot SSR policy', () => {
  it('allows cold SSR only for primary search robots and non-robot requests', () => {
    const allowedFamilies: Array<RobotFamily | null> = [
      null,
      'Googlebot',
      'Bingbot',
      'YandexBot',
      'DuckDuckBot'
    ];

    allowedFamilies.forEach((family: RobotFamily | null) => {
      expect(shouldAllowRobotCacheMissSsrRender(family)).withContext(`${family ?? 'human'} should render`).toBeTrue();
    });
  });

  it('keeps secondary robots cache-only on cache misses', () => {
    const cacheOnlyFamilies: RobotFamily[] = [
      'Applebot',
      'BaiduSpider',
      'PetalBot',
      'AhrefsBot',
      'SemrushBot',
      'MJ12bot',
      'DotBot',
      'Other bot'
    ];

    cacheOnlyFamilies.forEach((family: RobotFamily) => {
      expect(shouldAllowRobotCacheMissSsrRender(family)).withContext(`${family} should stay cache-only`).toBeFalse();
    });
  });

  it('detects named robot families before falling back to Other bot', () => {
    expect(detectRobotFamilyFromUserAgent('Mozilla/5.0 Applebot/0.1')).toBe('Applebot');
    expect(detectRobotFamilyFromUserAgent('Mozilla/5.0 (compatible; Baiduspider/2.0)')).toBe('BaiduSpider');
    expect(detectRobotFamilyFromUserAgent('Mozilla/5.0 PetalBot')).toBe('PetalBot');
    expect(detectRobotFamilyFromUserAgent('Mozilla/5.0 AhrefsBot')).toBe('AhrefsBot');
    expect(detectRobotFamilyFromUserAgent('Mozilla/5.0 SomeCrawler')).toBe('Other bot');
  });

  it('does not treat internal targeted refreshes as robot traffic', () => {
    const family: RobotFamily | null = detectRobotFamilyFromUserAgent('AmusementPark-SSR-TargetedRefresh/1.0');

    expect(family).toBeNull();
    expect(shouldAllowRobotCacheMissSsrRender(family)).toBeTrue();
  });
});
