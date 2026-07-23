export const SUPPORTED_VIDEO_EMBED_ORIGINS: readonly string[] = [
  'https://youtube.com',
  'https://www.youtube.com',
  'https://www.youtube-nocookie.com',
  'https://www.dailymotion.com',
  'https://player.vimeo.com'
];

export function isAllowedVideoEmbedUrl(url: URL): boolean {
  const hostname: string = url.hostname.toLowerCase();
  const pathname: string = url.pathname.toLowerCase();

  if ((hostname === 'www.youtube.com' || hostname === 'youtube.com' || hostname === 'www.youtube-nocookie.com')
    && pathname.startsWith('/embed/')) {
    return true;
  }

  if (hostname === 'www.dailymotion.com' && pathname.startsWith('/embed/video/')) {
    return true;
  }

  return hostname === 'player.vimeo.com' && pathname.startsWith('/video/');
}
