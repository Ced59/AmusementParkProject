export interface PublicResponseHeaderWriter {
  setHeader(name: string, value: string): void;
}

export const PUBLIC_PROXY_BUFFERING_HEADER: string = 'X-Accel-Buffering';

export function disablePublicProxyResponseBuffering(
  response: PublicResponseHeaderWriter,
): void {
  response.setHeader(PUBLIC_PROXY_BUFFERING_HEADER, 'no');
}
