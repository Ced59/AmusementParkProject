import {
  disablePublicProxyResponseBuffering,
  PublicResponseHeaderWriter,
  PUBLIC_PROXY_BUFFERING_HEADER,
} from './public-response-transport-policy';

describe('public response transport policy', () => {
  it('asks the public reverse proxy to stream dynamic responses without buffering them', () => {
    const response: TestPublicResponseHeaderWriter = new TestPublicResponseHeaderWriter();

    disablePublicProxyResponseBuffering(response);

    expect(response.headers.get(PUBLIC_PROXY_BUFFERING_HEADER)).toBe('no');
  });
});

class TestPublicResponseHeaderWriter implements PublicResponseHeaderWriter {
  readonly headers: Map<string, string> = new Map<string, string>();

  setHeader(name: string, value: string): void {
    this.headers.set(name, value);
  }
}
