import {
  detectRobotFamilyFromUserAgent,
  getRobotFamilyCategory,
  shouldAllowRobotCacheMissSsrRender,
  shouldServeRobotOptimizedNoJsHtml
} from './robot-ssr-policy';
import type { RobotFamily } from './robot-ssr-policy';

describe('robot SSR policy', () => {
  it('allows cold SSR for search, user-triggered, audit and social preview robots', () => {
    const allowedFamilies: Array<RobotFamily | null> = [
      null,
      'Googlebot', 'Google-InspectionTool', 'Google-Agent', 'GoogleAgent-Mariner', 'Google-GeminiNotebook',
      'Bingbot', 'YandexBot', 'DuckDuckBot', 'DuckAssistBot', 'Applebot',
      'OAI-SearchBot', 'ChatGPT-User', 'OAI-AdsBot',
      'Claude-SearchBot', 'Claude-User', 'PerplexityBot', 'Perplexity-User',
      'Bravebot', 'Amazonbot', 'YouBot', 'KagiBot', 'PhindBot', 'ExaBot',
      'Meta-ExternalFetcher', 'AhrefsBot', 'AhrefsSiteAudit',
      'Facebook external hit', 'WhatsApp', 'TelegramBot', 'LinkedInBot',
      'PinterestBot', 'DiscordBot', 'TwitterBot'
    ];

    allowedFamilies.forEach((family: RobotFamily | null) => {
      expect(shouldAllowRobotCacheMissSsrRender(family)).withContext(`${family ?? 'human'} should render`).toBeTrue();
    });
  });

  it('keeps training, ambiguous and secondary crawler robots cache-only on cache misses', () => {
    const cacheOnlyFamilies: RobotFamily[] = [
      'GPTBot', 'ClaudeBot', 'Meta-ExternalAgent', 'GoogleOther', 'CCBot',
      'cohere-ai', 'AI2Bot', 'Diffbot', 'ImagesiftBot', 'ByteSpider',
      'BaiduSpider', 'PetalBot', 'SemrushBot', 'MJ12bot', 'DotBot', 'Other bot'
    ];

    cacheOnlyFamilies.forEach((family: RobotFamily) => {
      expect(shouldAllowRobotCacheMissSsrRender(family)).withContext(`${family} should stay cache-only`).toBeFalse();
    });
  });

  it('distinguishes official AI search, user-triggered and training agents', () => {
    const cases: ReadonlyArray<readonly [string, RobotFamily]> = [
      ['compatible; OAI-SearchBot/1.4; +https://openai.com/searchbot', 'OAI-SearchBot'],
      ['compatible; ChatGPT-User/1.0; +https://openai.com/bot', 'ChatGPT-User'],
      ['compatible; OAI-AdsBot/1.0; +https://openai.com/adsbot', 'OAI-AdsBot'],
      ['compatible; GPTBot/1.4; +https://openai.com/gptbot', 'GPTBot'],
      ['Claude-SearchBot/1.0', 'Claude-SearchBot'],
      ['Claude-User/1.0', 'Claude-User'],
      ['ClaudeBot/1.0', 'ClaudeBot'],
      ['compatible; PerplexityBot/1.0; +https://perplexity.ai/perplexitybot', 'PerplexityBot'],
      ['compatible; Perplexity-User/1.0; +https://perplexity.ai/perplexity-user', 'Perplexity-User'],
      ['Meta-ExternalFetcher/1.1', 'Meta-ExternalFetcher'],
      ['Meta-ExternalAgent/1.1', 'Meta-ExternalAgent'],
      ['Google-GeminiNotebook', 'Google-GeminiNotebook'],
      ['Google-NotebookLM', 'Google-GeminiNotebook'],
      ['Google-Agent', 'Google-Agent'],
      ['GoogleAgent-Mariner/1.0', 'GoogleAgent-Mariner'],
      ['DuckAssistBot/1.2', 'DuckAssistBot'],
      ['Bravebot/1.0', 'Bravebot'],
      ['Amazonbot/0.1', 'Amazonbot'],
      ['YouBot/1.0', 'YouBot'],
      ['KagiBot/1.0', 'KagiBot'],
      ['PhindBot/1.0', 'PhindBot'],
      ['ExaBot/1.0', 'ExaBot'],
      ['CCBot/2.0', 'CCBot'],
      ['cohere-ai/1.0', 'cohere-ai'],
      ['AI2Bot/1.0', 'AI2Bot'],
      ['Diffbot/1.0', 'Diffbot'],
      ['ImagesiftBot/1.0', 'ImagesiftBot']
    ];

    cases.forEach(([userAgent, expectedFamily]: readonly [string, RobotFamily]) => {
      expect(detectRobotFamilyFromUserAgent(userAgent)).withContext(userAgent).toBe(expectedFamily);
    });
  });

  it('detects established crawler and preview families before falling back to Other bot', () => {
    expect(detectRobotFamilyFromUserAgent('Mozilla/5.0 Applebot/0.1')).toBe('Applebot');
    expect(detectRobotFamilyFromUserAgent('Mozilla/5.0 (compatible; Baiduspider/2.0)')).toBe('BaiduSpider');
    expect(detectRobotFamilyFromUserAgent('Mozilla/5.0 PetalBot')).toBe('PetalBot');
    expect(detectRobotFamilyFromUserAgent('Mozilla/5.0 AhrefsBot')).toBe('AhrefsBot');
    expect(detectRobotFamilyFromUserAgent('Mozilla/5.0 (compatible; AhrefsSiteAudit/6.1)')).toBe('AhrefsSiteAudit');
    expect(detectRobotFamilyFromUserAgent('facebookexternalhit/1.1')).toBe('Facebook external hit');
    expect(detectRobotFamilyFromUserAgent('WhatsApp/2.24')).toBe('WhatsApp');
    expect(detectRobotFamilyFromUserAgent('Mozilla/5.0 SomeCrawler')).toBe('Other bot');
  });

  it('groups Google-specific agents in Google technical statistics', () => {
    expect(getRobotFamilyCategory('Google-Agent')).toBe('google');
    expect(getRobotFamilyCategory('GoogleAgent-Mariner')).toBe('google');
    expect(getRobotFamilyCategory('Google-GeminiNotebook')).toBe('google');
    expect(getRobotFamilyCategory('OAI-SearchBot')).toBe('other');
  });

  it('tracks Mariner as a robot without stripping scripts from its interactive browser session', () => {
    expect(detectRobotFamilyFromUserAgent('GoogleAgent-Mariner/1.0')).toBe('GoogleAgent-Mariner');
    expect(shouldAllowRobotCacheMissSsrRender('GoogleAgent-Mariner')).toBeTrue();
    expect(shouldServeRobotOptimizedNoJsHtml('GoogleAgent-Mariner')).toBeFalse();
    expect(shouldServeRobotOptimizedNoJsHtml('Googlebot')).toBeTrue();
  });

  it('does not treat internal targeted refreshes as robot traffic', () => {
    const family: RobotFamily | null = detectRobotFamilyFromUserAgent('AmusementPark-SSR-TargetedRefresh/1.0');

    expect(family).toBeNull();
    expect(shouldAllowRobotCacheMissSsrRender(family)).toBeTrue();
  });
});
