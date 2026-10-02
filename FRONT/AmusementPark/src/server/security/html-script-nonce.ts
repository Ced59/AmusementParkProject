import { randomBytes } from 'node:crypto';

export const CSP_NONCE_PLACEHOLDER: string = '__AMUSEMENTPARK_CSP_NONCE__';

export interface HtmlScriptNonceResponse {
  readonly html: string;
  readonly nonce: string;
}

const templateAttribute = /(^|\s)(nonce|ngcspnonce)=(['"])__AMUSEMENTPARK_CSP_NONCE__\3/gi;

export function prepareHtmlScriptNonceResponse(html: string): HtmlScriptNonceResponse | null {
  // Older cache entries and non-Angular documents keep their existing policy.
  if (!html.includes(CSP_NONCE_PLACEHOLDER)
    || !/<app-root\b[^>]*\sngcspnonce=(['"])__AMUSEMENTPARK_CSP_NONCE__\1/i.test(html)) {
    return null;
  }
  const nonce: string = randomBytes(24).toString('base64');
  const preparedHtml: string = html.replace(templateAttribute,
    (_match: string, prefix: string, attribute: string, quote: string): string => `${prefix}${attribute}=${quote}${nonce}${quote}`);
  if (preparedHtml === html) {
    return null;
  }

  // Only the build's local JavaScript chunks may receive a preload authorization.
  const withPreloads: string = preparedHtml.replace(/<link\b([^>]*\srel=(['"])modulepreload\2[^>]*)>/gi,
    (tag: string, attributes: string): string => {
      if (/(?:^|\s)nonce\s*=/i.test(attributes)
        || (attributes.match(/(?:^|\s)href\s*=/gi)?.length ?? 0) !== 1
        || !/(?:^|\s)href=(['"])(?:\.?\/)?(?:chunk|main|polyfills)-[A-Za-z0-9_-]+\.js\1/i.test(attributes)) {
        return tag;
      }
      return `<link${attributes} nonce="${nonce}">`;
    });
  return { html: withPreloads, nonce };
}
