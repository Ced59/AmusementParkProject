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

  it('splits large HTML responses and waits for drain before closing', () => {
    const response = new TestHtmlResponseWriter([false, true]);
    const html: string = 'A'.repeat(20_000);

    writeSsrHtmlResponse('GET', response, html);

    expect(response.closed).toBeFalse();
    expect(response.writtenChunks.length).toBe(1);
    expect(response.writtenChunks[0].length).toBe(16 * 1024);

    response.flushDrain();

    expect(response.writtenChunks.join('')).toBe(html);
    expect(response.closed).toBeTrue();
  });
});

class TestHtmlResponseWriter implements HtmlResponseWriter {
  contentType: string | null = null;
  readonly removedHeaders: string[] = [];
  readonly writtenChunks: string[] = [];
  private readonly writeResults: boolean[];
  private drainListener: (() => void) | null = null;
  closed = false;

  constructor(writeResults: boolean[] = []) {
    this.writeResults = writeResults;
  }

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
    return this.writeResults.shift() ?? true;
  }

  once(eventName: 'drain', listener: () => void): HtmlResponseWriter {
    expect(eventName).toBe('drain');
    this.drainListener = listener;
    return this;
  }

  end(): void {
    this.closed = true;
  }

  flushDrain(): void {
    const listener: (() => void) | null = this.drainListener;
    this.drainListener = null;
    listener?.();
  }
}
