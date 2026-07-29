import {
  extractManagedCommentImageId,
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
});
