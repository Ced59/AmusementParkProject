export interface HtmlResponseWriter {
  type(contentType: string): HtmlResponseWriter;
  setHeader(name: string, value: string): void;
  end(): void;
  end(chunk: string, encoding: 'utf8'): void;
}

const utf8Encoder = new TextEncoder();

export function writeSsrHtmlResponse(method: string, response: HtmlResponseWriter, html: string): void {
  response.type('html');
  response.setHeader('Content-Length', utf8Encoder.encode(html).length.toString());

  if (method.toUpperCase() === 'HEAD') {
    response.end();
    return;
  }

  response.end(html, 'utf8');
}
