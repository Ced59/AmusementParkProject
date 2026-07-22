export type RobotFamily =
  | 'Googlebot'
  | 'GoogleOther'
  | 'Google-InspectionTool'
  | 'Google-Agent'
  | 'GoogleAgent-Mariner'
  | 'Google-GeminiNotebook'
  | 'Bingbot'
  | 'DuckDuckBot'
  | 'DuckAssistBot'
  | 'YandexBot'
  | 'OAI-SearchBot'
  | 'ChatGPT-User'
  | 'OAI-AdsBot'
  | 'GPTBot'
  | 'Claude-SearchBot'
  | 'Claude-User'
  | 'ClaudeBot'
  | 'PerplexityBot'
  | 'Perplexity-User'
  | 'AhrefsBot'
  | 'AhrefsSiteAudit'
  | 'SemrushBot'
  | 'BaiduSpider'
  | 'Yahoo Slurp'
  | 'Applebot'
  | 'PetalBot'
  | 'Bravebot'
  | 'Amazonbot'
  | 'YouBot'
  | 'KagiBot'
  | 'PhindBot'
  | 'ExaBot'
  | 'Meta-ExternalFetcher'
  | 'Meta-ExternalAgent'
  | 'CCBot'
  | 'cohere-ai'
  | 'AI2Bot'
  | 'Diffbot'
  | 'ImagesiftBot'
  | 'MJ12bot'
  | 'DotBot'
  | 'ByteSpider'
  | 'Facebook external hit'
  | 'WhatsApp'
  | 'TelegramBot'
  | 'LinkedInBot'
  | 'PinterestBot'
  | 'DiscordBot'
  | 'TwitterBot'
  | 'Other bot';

const coldRenderRobotFamilies: ReadonlySet<RobotFamily> = new Set<RobotFamily>([
  'Googlebot',
  'Google-InspectionTool',
  'Google-Agent',
  'GoogleAgent-Mariner',
  'Google-GeminiNotebook',
  'Bingbot',
  'YandexBot',
  'DuckDuckBot',
  'DuckAssistBot',
  'Applebot',
  'OAI-SearchBot',
  'ChatGPT-User',
  'OAI-AdsBot',
  'Claude-SearchBot',
  'Claude-User',
  'PerplexityBot',
  'Perplexity-User',
  'Bravebot',
  'Amazonbot',
  'YouBot',
  'KagiBot',
  'PhindBot',
  'ExaBot',
  'Meta-ExternalFetcher',
  'AhrefsBot',
  'AhrefsSiteAudit'
]);

const socialPreviewRobotFamilies: ReadonlySet<RobotFamily> = new Set<RobotFamily>([
  'Facebook external hit',
  'WhatsApp',
  'TelegramBot',
  'LinkedInBot',
  'PinterestBot',
  'DiscordBot',
  'TwitterBot'
]);

const robotFamilyMatchers: ReadonlyArray<readonly [RobotFamily, ReadonlyArray<string>]> = [
  ['OAI-SearchBot', ['oai-searchbot']],
  ['ChatGPT-User', ['chatgpt-user']],
  ['OAI-AdsBot', ['oai-adsbot']],
  ['GPTBot', ['gptbot']],
  ['Claude-SearchBot', ['claude-searchbot']],
  ['Claude-User', ['claude-user']],
  ['ClaudeBot', ['claudebot']],
  ['Perplexity-User', ['perplexity-user']],
  ['PerplexityBot', ['perplexitybot']],
  ['Google-GeminiNotebook', ['google-gemininotebook', 'google-notebooklm']],
  ['Google-InspectionTool', ['google-inspectiontool']],
  ['GoogleAgent-Mariner', ['googleagent-mariner']],
  ['Google-Agent', ['google-agent']],
  ['GoogleOther', ['googleother']],
  ['Googlebot', ['googlebot', 'adsbot-google', 'mediapartners-google']],
  ['Bingbot', ['bingbot', 'msnbot']],
  ['DuckAssistBot', ['duckassistbot']],
  ['DuckDuckBot', ['duckduckbot']],
  ['YandexBot', ['yandexbot']],
  ['AhrefsSiteAudit', ['ahrefssiteaudit']],
  ['AhrefsBot', ['ahrefsbot']],
  ['SemrushBot', ['semrushbot']],
  ['BaiduSpider', ['baiduspider']],
  ['Yahoo Slurp', ['slurp']],
  ['Applebot', ['applebot']],
  ['PetalBot', ['petalbot']],
  ['Bravebot', ['bravebot']],
  ['Amazonbot', ['amazonbot']],
  ['YouBot', ['youbot']],
  ['KagiBot', ['kagibot']],
  ['PhindBot', ['phindbot']],
  ['ExaBot', ['exabot']],
  ['Meta-ExternalFetcher', ['meta-externalfetcher']],
  ['Meta-ExternalAgent', ['meta-externalagent']],
  ['CCBot', ['ccbot']],
  ['cohere-ai', ['cohere-ai']],
  ['AI2Bot', ['ai2bot']],
  ['Diffbot', ['diffbot']],
  ['ImagesiftBot', ['imagesiftbot']],
  ['MJ12bot', ['mj12bot']],
  ['DotBot', ['dotbot']],
  ['ByteSpider', ['bytespider']],
  ['Facebook external hit', ['facebookexternalhit']],
  ['WhatsApp', ['whatsapp']],
  ['TelegramBot', ['telegrambot']],
  ['LinkedInBot', ['linkedinbot']],
  ['PinterestBot', ['pinterestbot', 'pinterest']],
  ['DiscordBot', ['discordbot']],
  ['TwitterBot', ['twitterbot']]
];

export function detectRobotFamilyFromUserAgent(userAgentHeader: string): RobotFamily | null {
  const userAgent: string = userAgentHeader.toLowerCase();
  if (userAgent.length === 0 || userAgent.includes('amusementpark-ssr-targetedrefresh')) {
    return null;
  }

  for (const [family, markers] of robotFamilyMatchers) {
    if (markers.some((marker: string): boolean => userAgent.includes(marker))) {
      return family;
    }
  }

  if (/(?:bot|crawler|spider|slurp|facebookexternalhit|whatsapp|pinterest)/i.test(userAgent)) {
    return 'Other bot';
  }

  return null;
}

export function shouldAllowRobotCacheMissSsrRender(robotFamily: RobotFamily | null): boolean {
  return robotFamily === null
    || coldRenderRobotFamilies.has(robotFamily)
    || socialPreviewRobotFamilies.has(robotFamily);
}

export function shouldServeRobotOptimizedNoJsHtml(robotFamily: RobotFamily | null): boolean {
  return robotFamily !== null && robotFamily !== 'GoogleAgent-Mariner';
}

export function getRobotFamilyCategory(robotFamily: string): string {
  switch (robotFamily) {
    case 'Googlebot':
    case 'GoogleOther':
    case 'Google-InspectionTool':
    case 'Google-Agent':
    case 'GoogleAgent-Mariner':
    case 'Google-GeminiNotebook':
      return 'google';
    case 'Bingbot':
      return 'bing';
    case 'YandexBot':
      return 'yandex';
    default:
      return 'other';
  }
}
