export interface HtmlResponseWriter {
  type(contentType: string): HtmlResponseWriter;
  removeHeader(name: string): void;
  write(chunk: string, encoding: 'utf8'): boolean;
  once(eventName: 'drain', listener: () => void): HtmlResponseWriter;
  end(): void;
}

const htmlResponseChunkSize = 16 * 1024;

export function writeSsrHtmlResponse(method: string, response: HtmlResponseWriter, html: string): void {
  response.type('html');
  response.removeHeader('Content-Length');

  if (method.toUpperCase() === 'HEAD') {
    response.end();
    return;
  }

  writeHtmlChunk(response, html, 0);
}

function writeHtmlChunk(response: HtmlResponseWriter, html: string, startIndex: number): void {
  let currentIndex: number = startIndex;

  while (currentIndex < html.length) {
    const nextIndex: number = Math.min(currentIndex + htmlResponseChunkSize, html.length);
    const canContinue: boolean = response.write(html.slice(currentIndex, nextIndex), 'utf8');
    currentIndex = nextIndex;

    if (!canContinue) {
      response.once('drain', (): void => writeHtmlChunk(response, html, currentIndex));
      return;
    }
  }

  response.end();
}
