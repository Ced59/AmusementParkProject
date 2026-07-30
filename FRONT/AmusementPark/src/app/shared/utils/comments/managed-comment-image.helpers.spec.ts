import {
  extractManagedCommentImageId,
  extractManagedCommentImageIdsFromHtml,
  normalizeManagedCommentImageId
} from './managed-comment-image.helpers';

describe('managed comment image helpers', () => {
  it('accepts only lowercase 32-character hexadecimal ids', () => {
    const validId: string = '0123456789abcdef0123456789abcdef';

    expect(normalizeManagedCommentImageId(validId)).toBe(validId);
    expect(extractManagedCommentImageId(`/images/${validId}`)).toBe(validId);
    expect(normalizeManagedCommentImageId(validId.toUpperCase())).toBeNull();
    expect(normalizeManagedCommentImageId('image-42')).toBeNull();
    expect(normalizeManagedCommentImageId(`${validId}00`)).toBeNull();
    expect(extractManagedCommentImageId(`https://cdn.test/images/${validId}`)).toBeNull();
  });

  it('extracts only canonical managed image sources from rich html', () => {
    const firstId: string = '0123456789abcdef0123456789abcdef';
    const secondId: string = 'abcdef0123456789abcdef0123456789';

    expect(Array.from(extractManagedCommentImageIdsFromHtml(`
      <p>/images/11111111111111111111111111111111 is plain text</p>
      <img alt="first" src="/images/${firstId}">
      <img src='/images/${secondId}' class="rich-text__image">
      <img src="/images/${firstId}">
      <img src="/images/${secondId.toUpperCase()}">
      <img src="https://example.test/images/${secondId}">
      <img data-note='src="/images/11111111111111111111111111111111"'>
    `))).toEqual([firstId, secondId]);
  });
});
