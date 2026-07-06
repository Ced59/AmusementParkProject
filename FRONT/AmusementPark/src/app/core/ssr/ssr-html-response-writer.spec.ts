import { HtmlResponseWriter, writeSsrHtmlResponse } from './ssr-html-response-writer';

describe('SSR HTML response writer', () => {
  it('writes GET HTML responses without a fixed content length', () => {
    const response = new TestHtmlResponseWriter();

    writeSsrHtmlResponse('GET', response, '<!doctype html><html><body>Ready</body></html>');

    expect(response.contentType).toBe('html');
    expect(response.removedHeaders).toContain('Content-Length');
    expect(response.writtenChunks).toEqual(['<!doctype html><html><body>Ready</body></html>']);
    expect(response.closed).toBeTrue();
  });

  it('does not write a body for HEAD HTML responses', () => {
    const response = new TestHtmlResponseWriter();

    writeSsrHtmlResponse('HEAD', response, '<!doctype html><html><body>Ready</body></html>');

    expect(response.contentType).toBe('html');
    expect(response.removedHeaders).toContain('Content-Length');
    expect(response.writtenChunks).toEqual([]);
    expect(response.closed).toBeTrue();
  });
});

class TestHtmlResponseWriter implements HtmlResponseWriter {
  contentType: string | null = null;
  readonly removedHeaders: string[] = [];
  readonly writtenChunks: string[] = [];
  closed = false;

  type(contentType: string): HtmlResponseWriter {
    this.contentType = contentType;
    return this;
  }

  removeHeader(name: string): void {
    this.removedHeaders.push(name);
  }

  write(chunk: string, encoding: 'utf8'): boolean {
    expect(encoding).toBe('utf8');
    this.writtenChunks.push(chunk);
    return true;
  }

  end(): void {
    this.closed = true;
  }
}
