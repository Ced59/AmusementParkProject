import { HtmlResponseWriter, writeSsrHtmlResponse } from './ssr-html-response-writer';

describe('SSR HTML response writer', () => {
  it('writes GET HTML responses with an accurate UTF-8 content length', () => {
    const response = new TestHtmlResponseWriter();
    const html: string = '<!doctype html><html><body>Prêt</body></html>';

    writeSsrHtmlResponse('GET', response, html);

    expect(response.contentType).toBe('html');
    expect(response.headers.get('Content-Length')).toBe('46');
    expect(response.endedChunk).toBe(html);
    expect(response.endedEncoding).toBe('utf8');
    expect(response.closed).toBeTrue();
  });

  it('keeps the GET content length but does not write a body for HEAD HTML responses', () => {
    const response = new TestHtmlResponseWriter();

    writeSsrHtmlResponse('HEAD', response, '<!doctype html><html><body>Ready</body></html>');

    expect(response.contentType).toBe('html');
    expect(response.headers.get('Content-Length')).toBe('46');
    expect(response.endedChunk).toBeUndefined();
    expect(response.endedEncoding).toBeUndefined();
    expect(response.closed).toBeTrue();
  });
});

class TestHtmlResponseWriter implements HtmlResponseWriter {
  contentType: string | null = null;
  readonly headers = new Map<string, string>();
  endedChunk: string | undefined;
  endedEncoding: 'utf8' | undefined;
  closed = false;

  type(contentType: string): HtmlResponseWriter {
    this.contentType = contentType;
    return this;
  }

  setHeader(name: string, value: string): void {
    this.headers.set(name, value);
  }

  end(chunk?: string, encoding?: 'utf8'): void {
    this.endedChunk = chunk;
    this.endedEncoding = encoding;
    this.closed = true;
  }
}
