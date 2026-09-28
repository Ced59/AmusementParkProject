import { normalizeSeoDescription, normalizeSeoText, stripHtml, truncateSeoText } from './seo-text.utils';

describe('seo text utils', () => {
  describe('stripHtml', () => {
    it('removes tags, scripts and styles while decoding common entities', () => {
      const value: string = '<style>.a{}</style><script>alert(1)</script><p>Tom &amp; Jerry&nbsp;&quot;x&quot;&#39;y&#39;&lt;z&gt;</p>';

      expect(stripHtml(value).replace(/\s+/g, ' ').trim()).toBe('Tom & Jerry "x"\'y\'<z>');
    });

    it('returns an empty string for nullish values', () => {
      expect(stripHtml(null)).toBe('');
      expect(stripHtml(undefined)).toBe('');
    });
  });

  describe('normalizeSeoText', () => {
    it('strips html and collapses whitespace', () => {
      expect(normalizeSeoText('<p>  Hello<br> world </p>', 'fallback')).toBe('Hello world');
    });

    it('returns the fallback when content is empty after normalization', () => {
      expect(normalizeSeoText('<p>&nbsp;</p>', 'fallback')).toBe('fallback');
    });
  });

  describe('normalizeSeoDescription', () => {
    it('uses prose without repeating the rich text heading or inserting section labels', () => {
      const value: string = '<h2><strong>Starfish</strong></h2><p>Starfish était un manège circulaire.</p><H3 class="section">Son histoire</H3><p>Il a fermé.</p>';

      expect(normalizeSeoDescription(value, 'fallback')).toBe('Starfish était un manège circulaire. Il a fermé.');
    });

    it('preserves plain text and inline formatting content', () => {
      expect(normalizeSeoDescription('Starfish était un manège circulaire.', 'fallback')).toBe('Starfish était un manège circulaire.');
      expect(normalizeSeoDescription('<p>Tom &amp; Jerry jouent <strong>au parc</strong></p>', 'fallback')).toBe('Tom & Jerry jouent au parc');
    });

    it('keeps heading text when no prose is available', () => {
      expect(normalizeSeoDescription('<h2>Un lieu historique</h2><p>&nbsp;</p>', 'fallback')).toBe('Un lieu historique');
    });

    it('uses the supplied fallback when the entire description is empty', () => {
      expect(normalizeSeoDescription(null, 'fallback')).toBe('fallback');
      expect(normalizeSeoDescription('<h2>&nbsp;</h2><script>ignored()</script>', 'fallback')).toBe('fallback');
    });
  });

  describe('truncateSeoText', () => {
    it('keeps text shorter than the limit unchanged', () => {
      expect(truncateSeoText('Short text', 20)).toBe('Short text');
    });

    it('truncates at a natural word boundary when one is available after 80 characters', () => {
      const value: string = `${'a'.repeat(85)} boundary after limit should be removed`;

      expect(truncateSeoText(value, 100)).toBe(`${'a'.repeat(85)} boundary…`);
    });

    it('falls back to hard truncation when no useful boundary exists', () => {
      const value: string = 'abcdefghijklmnopqrstuvwxyz';

      expect(truncateSeoText(value, 10)).toBe('abcdefghi…');
    });

    it('handles zero and negative limits without throwing', () => {
      expect(truncateSeoText('abcdef', 0)).toBe('…');
      expect(truncateSeoText('abcdef', -5)).toBe('…');
    });
  });
});
