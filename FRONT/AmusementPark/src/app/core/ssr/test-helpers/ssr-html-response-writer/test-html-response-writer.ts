import { HtmlResponseWriter } from '../../ssr-html-response-writer';

export class TestHtmlResponseWriter implements HtmlResponseWriter {
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
