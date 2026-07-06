export interface HtmlResponseWriter {
  type(contentType: string): HtmlResponseWriter;
  removeHeader(name: string): void;
  write(chunk: string, encoding: 'utf8'): boolean;
  end(): void;
}

export function writeSsrHtmlResponse(method: string, response: HtmlResponseWriter, html: string): void {
  response.type('html');
  response.removeHeader('Content-Length');

  if (method.toUpperCase() === 'HEAD') {
    response.end();
    return;
  }

  response.write(html, 'utf8');
  response.end();
}
