export type RobotFamily =
  | 'Googlebot'
  | 'Bingbot'
  | 'DuckDuckBot'
  | 'YandexBot'
  | 'AhrefsBot'
  | 'AhrefsSiteAudit'
  | 'SemrushBot'
  | 'BaiduSpider'
  | 'Yahoo Slurp'
  | 'Applebot'
  | 'PetalBot'
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
  'Bingbot',
  'YandexBot',
  'DuckDuckBot',
  'Applebot',
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

export function detectRobotFamilyFromUserAgent(userAgentHeader: string): RobotFamily | null {
  const userAgent: string = userAgentHeader.toLowerCase();
  if (userAgent.length === 0 || userAgent.includes('amusementpark-ssr-targetedrefresh')) {
    return null;
  }

  if (userAgent.includes('googlebot') || userAgent.includes('adsbot-google') || userAgent.includes('mediapartners-google')) {
    return 'Googlebot';
  }

  if (userAgent.includes('bingbot') || userAgent.includes('msnbot')) {
    return 'Bingbot';
  }

  if (userAgent.includes('duckduckbot')) {
    return 'DuckDuckBot';
  }

  if (userAgent.includes('yandexbot')) {
    return 'YandexBot';
  }

  if (userAgent.includes('ahrefsbot')) {
    return 'AhrefsBot';
  }

  if (userAgent.includes('ahrefssiteaudit')) {
    return 'AhrefsSiteAudit';
  }

  if (userAgent.includes('semrushbot')) {
    return 'SemrushBot';
  }

  if (userAgent.includes('baiduspider')) {
    return 'BaiduSpider';
  }

  if (userAgent.includes('slurp')) {
    return 'Yahoo Slurp';
  }

  if (userAgent.includes('applebot')) {
    return 'Applebot';
  }

  if (userAgent.includes('petalbot')) {
    return 'PetalBot';
  }

  if (userAgent.includes('mj12bot')) {
    return 'MJ12bot';
  }

  if (userAgent.includes('dotbot')) {
    return 'DotBot';
  }

  if (userAgent.includes('bytespider')) {
    return 'ByteSpider';
  }

  if (userAgent.includes('facebookexternalhit')) {
    return 'Facebook external hit';
  }

  if (userAgent.includes('whatsapp')) {
    return 'WhatsApp';
  }

  if (userAgent.includes('telegrambot')) {
    return 'TelegramBot';
  }

  if (userAgent.includes('linkedinbot')) {
    return 'LinkedInBot';
  }

  if (userAgent.includes('pinterest')) {
    return 'PinterestBot';
  }

  if (userAgent.includes('discordbot')) {
    return 'DiscordBot';
  }

  if (userAgent.includes('twitterbot')) {
    return 'TwitterBot';
  }

  if (/(?:bot|crawler|spider|slurp|facebookexternalhit|whatsapp|telegrambot|linkedinbot|pinterest|discordbot|twitterbot)/i.test(userAgent)) {
    return 'Other bot';
  }

  return null;
}

export function shouldAllowRobotCacheMissSsrRender(robotFamily: RobotFamily | null): boolean {
  return robotFamily === null
    || coldRenderRobotFamilies.has(robotFamily)
    || socialPreviewRobotFamilies.has(robotFamily);
}

export function getRobotFamilyCategory(robotFamily: string): string {
  switch (robotFamily) {
    case 'Googlebot':
      return 'google';
    case 'Bingbot':
      return 'bing';
    case 'YandexBot':
      return 'yandex';
    default:
      return 'other';
  }
}
