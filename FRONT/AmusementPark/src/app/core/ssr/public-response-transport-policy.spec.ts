import {
  disablePublicProxyResponseBuffering,
  PublicResponseHeaderWriter,
  PUBLIC_PROXY_BUFFERING_HEADER,
} from './public-response-transport-policy';
import { TestPublicResponseHeaderWriter } from './test-helpers/public-response-transport-policy/test-public-response-header-writer';

describe('public response transport policy', () => {
  it('asks the public reverse proxy to stream dynamic responses without buffering them', () => {
    const response: TestPublicResponseHeaderWriter = new TestPublicResponseHeaderWriter();

    disablePublicProxyResponseBuffering(response);

    expect(response.headers.get(PUBLIC_PROXY_BUFFERING_HEADER)).toBe('no');
  });
});
