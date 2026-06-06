import { NaturalTextTruncatorService } from './natural-text-truncator.service';

describe('NaturalTextTruncatorService', () => {
  let service: NaturalTextTruncatorService;

  beforeEach(() => {
    service = new NaturalTextTruncatorService();
  });

  it('returns null for empty normalized text', () => {
    expect(service.truncate('   \n\t  ', { maxLength: 10 })).toBeNull();
    expect(service.truncate(null, { maxLength: 10 })).toBeNull();
  });

  it('collapses whitespace and keeps short text unchanged', () => {
    expect(service.truncate(' Hello\n world ', { maxLength: 20 })).toBe('Hello world');
  });

  it('returns only the ellipsis when maxLength is zero or negative', () => {
    expect(service.truncate('Hello', { maxLength: 0 })).toBe('…');
    expect(service.truncate('Hello', { maxLength: -10, ellipsis: '...' })).toBe('...');
  });

  it('truncates at a natural breakpoint near the limit', () => {
    expect(service.truncate('This is a sentence, with a natural breakpoint', { maxLength: 28, ellipsis: '...' })).toBe('This is a sentence...');
  });

  it('hard truncates when no natural breakpoint is useful', () => {
    expect(service.truncate('abcdefghijklmnopqrstuvwxyz', { maxLength: 8 })).toBe('abcdefg…');
  });
});
