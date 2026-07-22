import {
  detectRobotFamilyFromUserAgent,
  shouldAllowRobotCacheMissSsrRender
} from './robot-ssr-policy';
import type { RobotFamily } from './robot-ssr-policy';

describe('robot SSR policy', () => {
  it('allows cold SSR for primary search robots, social preview robots and non-robot requests', () => {
    const allowedFamilies: Array<RobotFamily | null> = [
      null,
      'Googlebot',
      'Bingbot',
      'YandexBot',
      'DuckDuckBot',
      'Applebot',
      'AhrefsBot',
      'AhrefsSiteAudit',
      'Facebook external hit',
      'WhatsApp',
      'TelegramBot',
      'LinkedInBot',
      'PinterestBot',
      'DiscordBot',
      'TwitterBot'
    ];

    allowedFamilies.forEach((family: RobotFamily | null) => {
      expect(shouldAllowRobotCacheMissSsrRender(family)).withContext(`${family ?? 'human'} should render`).toBeTrue();
    });
  });

  it('keeps secondary crawler robots cache-only on cache misses', () => {
    const cacheOnlyFamilies: RobotFamily[] = [
      'BaiduSpider',
      'PetalBot',
      'SemrushBot',
      'MJ12bot',
      'DotBot',
      'ByteSpider',
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
    expect(detectRobotFamilyFromUserAgent('Mozilla/5.0 (compatible; AhrefsSiteAudit/6.1; +http://ahrefs.com/robot/site-audit)')).toBe('AhrefsSiteAudit');
    expect(detectRobotFamilyFromUserAgent('facebookexternalhit/1.1')).toBe('Facebook external hit');
    expect(detectRobotFamilyFromUserAgent('WhatsApp/2.24')).toBe('WhatsApp');
    expect(detectRobotFamilyFromUserAgent('Mozilla/5.0 SomeCrawler')).toBe('Other bot');
  });

  it('does not treat internal targeted refreshes as robot traffic', () => {
    const family: RobotFamily | null = detectRobotFamilyFromUserAgent('AmusementPark-SSR-TargetedRefresh/1.0');

    expect(family).toBeNull();
    expect(shouldAllowRobotCacheMissSsrRender(family)).toBeTrue();
  });
});
