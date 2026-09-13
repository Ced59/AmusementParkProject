import {
  HtmlResponseWriter,
  writeSsrHtmlResponse,
} from './ssr-html-response-writer';
import { TestHtmlResponseWriter } from './test-helpers/ssr-html-response-writer/test-html-response-writer';

describe('SSR HTML response writer', () => {
  it('writes GET HTML responses with an accurate UTF-8 content length', () => {
    const response = new TestHtmlResponseWriter();
    const html: string = '<!doctype html><html><body>Prêt</body></html>';

    writeSsrHtmlResponse('GET', response, html);

    expect(response.contentType).toBe('html');
    expect(response.headers.get('X-Accel-Buffering')).toBe('no');
    expect(response.headers.get('Content-Length')).toBe('46');
    expect(response.endedChunk).toBe(html);
    expect(response.endedEncoding).toBe('utf8');
    expect(response.closed).toBe(true);
  });

  it('keeps the GET content length but does not write a body for HEAD HTML responses', () => {
    const response = new TestHtmlResponseWriter();

    writeSsrHtmlResponse(
      'HEAD',
      response,
      '<!doctype html><html><body>Ready</body></html>',
    );

    expect(response.contentType).toBe('html');
    expect(response.headers.get('X-Accel-Buffering')).toBe('no');
    expect(response.headers.get('Content-Length')).toBe('46');
    expect(response.endedChunk).toBeUndefined();
    expect(response.endedEncoding).toBeUndefined();
    expect(response.closed).toBe(true);
  });
});
