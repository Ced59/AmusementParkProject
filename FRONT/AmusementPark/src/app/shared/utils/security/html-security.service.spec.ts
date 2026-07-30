import { HtmlSecurityService } from './html-security.service';
import { UrlSecurityService } from './url-security.service';

describe('HtmlSecurityService', () => {
  let service: HtmlSecurityService;

  beforeEach(() => {
    service = new HtmlSecurityService(document, new UrlSecurityService());
  });

  it('returns an empty string for nullish or blank html', () => {
    expect(service.sanitizeRichHtml(null)).toBe('');
    expect(service.sanitizeRichHtml('   ')).toBe('');
  });

  it('removes dangerous elements with their content', () => {
    const result: string = service.sanitizeRichHtml('<p>Safe</p><script>alert(1)</script><iframe src="https://evil.test"></iframe>');

    expect(result).toContain('<p>Safe</p>');
    expect(result).not.toContain('script');
    expect(result).not.toContain('iframe');
    expect(result).not.toContain('alert');
  });

  it('unwraps unsupported harmless elements but keeps their sanitized text content', () => {
    const result: string = service.sanitizeRichHtml('<section><p>Hello <custom onclick="x()">world</custom></p></section>');

    expect(result).toContain('<p>Hello world</p>');
    expect(result).not.toContain('section');
    expect(result).not.toContain('custom');
    expect(result).not.toContain('onclick');
  });

  it('removes event handlers and unknown attributes from allowed elements', () => {
    const result: string = service.sanitizeRichHtml('<p onclick="x()" data-test="x" title="ok">Text</p>');

    expect(result).toContain('title="ok"');
    expect(result).not.toContain('onclick');
    expect(result).not.toContain('data-test');
  });

  it('keeps only approved classes', () => {
    const result: string = service.sanitizeRichHtml('<p class="ql-align-center evil rich-text__lead">Text</p>');

    expect(result).toContain('class="ql-align-center rich-text__lead"');
    expect(result).not.toContain('evil');
  });

  it('keeps safe inline style declarations and removes unsafe ones', () => {
    const result: string = service.sanitizeRichHtml('<p style="color:#fff; background-image:url(javascript:alert(1)); text-align:CENTER; position:absolute; font-weight:700">Text</p>');

    expect(result).toContain('color: #fff');
    expect(result).toContain('text-align: center');
    expect(result).toContain('font-weight: 700');
    expect(result).not.toContain('background-image');
    expect(result).not.toContain('position');
  });

  it('sanitizes anchors and adds safe target and rel attributes', () => {
    const result: string = service.sanitizeRichHtml('<a href="javascript:alert(1)">bad</a><a href="https://example.com">good</a>');

    expect(result).not.toContain('javascript:');
    expect(result).toContain('href="https://example.com"');
    expect(result).toContain('target="_blank"');
    expect(result).toContain('rel="noopener noreferrer nofollow"');
  });

  it('sanitizes image sources and forces lazy async loading', () => {
    const result: string = service.sanitizeRichHtml('<img src="data:text/html;base64,AAAA" alt="bad"><img src="/img.png" alt="ok" width="100" height="50">');

    expect(result).not.toContain('data:text/html');
    expect(result).toContain('src="/img.png"');
    expect(result).toContain('loading="lazy"');
    expect(result).toContain('decoding="async"');
  });

  it('keeps only pipeline-managed images in comment rich text', () => {
    const result: string = service.sanitizeManagedImageRichHtml(`
      <p>Avant</p>
      <img src="/images/0123456789abcdef0123456789abcdef" class="rich-text__image rich-text__image--left evil" alt="Train">
      <img src="https://evil.test/hotlink.jpg" class="rich-text__image rich-text__image--right">
      <img src="data:image/png;base64,AAAA" class="rich-text__image">
      <p>Après</p>
    `);

    expect(result).toContain(
      'src="/images/0123456789abcdef0123456789abcdef" class="rich-text__image rich-text__image--left"'
    );
    expect(result).toContain('alt="Train"');
    expect(result).not.toContain('evil.test');
    expect(result).not.toContain('base64');
    expect(result.match(/<img/g)).toHaveLength(1);
  });

  it('preserves a managed image id and layout when content is copied between languages', () => {
    const french: string = service.sanitizeManagedImageRichHtml(
      '<p>Texte français</p><img src="/images/abcdef0123456789abcdef0123456789" class="rich-text__image rich-text__image--right" alt="Vue du parc">'
    );
    const translatedCopy: string = french.replace('Texte français', 'English text');

    expect(service.sanitizeManagedImageRichHtml(translatedCopy)).toContain(
      '<img src="/images/abcdef0123456789abcdef0123456789" class="rich-text__image rich-text__image--right" alt="Vue du parc"'
    );
  });

  it('rejects managed image identifiers that are not exactly 32 hexadecimal characters', () => {
    const result: string = service.sanitizeManagedImageRichHtml(
      '<img src="/images/image-42" class="rich-text__image rich-text__image--full">'
      + '<img src="/images/0123456789abcdef0123456789abcdeg" class="rich-text__image rich-text__image--full">'
    );

    expect(result).not.toContain('<img');
  });

  it('limits managed image alternative text to 240 characters', () => {
    const result: string = service.sanitizeManagedImageRichHtml(
      `<img src="/images/0123456789abcdef0123456789abcdef" class="rich-text__image rich-text__image--full" alt="${'a'.repeat(300)}">`
    );
    const template: HTMLTemplateElement = document.createElement('template');
    template.innerHTML = result;

    expect(template.content.querySelector('img')?.getAttribute('alt')).toHaveLength(240);
  });

  it('decodes encoded rich html before sanitizing it', () => {
    const result: string = service.sanitizeRichHtml('&lt;p onclick=&quot;evil()&quot;&gt;Hello&lt;/p&gt;');

    expect(result).toBe('<p>Hello</p>');
  });
});
