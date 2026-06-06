import { UrlSecurityService } from './url-security.service';

describe('UrlSecurityService', () => {
  let service: UrlSecurityService;

  beforeEach(() => {
    service = new UrlSecurityService();
  });

  describe('sanitizeExternalUrl', () => {
    it('accepts http, https, mailto and tel absolute urls', () => {
      expect(service.sanitizeExternalUrl('https://example.com/path')).toBe('https://example.com/path');
      expect(service.sanitizeExternalUrl('http://example.com')).toBe('http://example.com');
      expect(service.sanitizeExternalUrl('mailto:test@example.com')).toBe('mailto:test@example.com');
      expect(service.sanitizeExternalUrl('tel:+33123456789')).toBe('tel:+33123456789');
    });

    it('normalizes naked domains and protocol-relative urls to https', () => {
      expect(service.sanitizeExternalUrl('example.com/path')).toBe('https://example.com/path');
      expect(service.sanitizeExternalUrl('//example.com/path')).toBe('https://example.com/path');
    });

    it('rejects javascript, relative and empty urls', () => {
      expect(service.sanitizeExternalUrl('javascript:alert(1)')).toBeNull();
      expect(service.sanitizeExternalUrl('/local/path')).toBeNull();
      expect(service.sanitizeExternalUrl('')).toBeNull();
      expect(service.sanitizeExternalUrl(null)).toBeNull();
    });
  });

  describe('sanitizeImageUrl', () => {
    it('accepts relative, blob, http and https image urls', () => {
      expect(service.sanitizeImageUrl('/images/test.png')).toBe('/images/test.png');
      expect(service.sanitizeImageUrl('blob:https://example.com/id')).toBe('blob:https://example.com/id');
      expect(service.sanitizeImageUrl('https://example.com/image.webp')).toBe('https://example.com/image.webp');
    });

    it('accepts and compacts safe data image urls', () => {
      expect(service.sanitizeImageUrl('data:image/png;base64, aa bb ==')).toBe('data:image/png;base64,aabb==');
    });

    it('rejects unsafe image url protocols and non image data urls', () => {
      expect(service.sanitizeImageUrl('javascript:alert(1)')).toBeNull();
      expect(service.sanitizeImageUrl('data:text/html;base64,PHNjcmlwdA==')).toBeNull();
      expect(service.sanitizeImageUrl('ftp://example.com/image.png')).toBeNull();
    });
  });

  describe('sanitizeRichHtmlUrl', () => {
    it('allows anchors and relative rich text links', () => {
      expect(service.sanitizeRichHtmlUrl('#section')).toBe('#section');
      expect(service.sanitizeRichHtmlUrl('/park/1')).toBe('/park/1');
      expect(service.sanitizeRichHtmlUrl('./local')).toBe('./local');
      expect(service.sanitizeRichHtmlUrl('../parent')).toBe('../parent');
    });

    it('allows image data urls only when explicitly requested', () => {
      const dataUrl: string = 'data:image/webp;base64,AAAA';

      expect(service.sanitizeRichHtmlUrl(dataUrl)).toBeNull();
      expect(service.sanitizeRichHtmlUrl(dataUrl, true)).toBe(dataUrl);
    });
  });
});
