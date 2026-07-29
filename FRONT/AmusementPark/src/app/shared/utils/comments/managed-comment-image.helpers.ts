export const ManagedCommentImageAltMaxLength: number = 240;
export const ManagedCommentImageIdPattern: RegExp = /^[a-f0-9]{32}$/;

export function normalizeManagedCommentImageId(value: string | null | undefined): string | null {
  const normalizedValue: string = value?.trim() ?? '';
  return ManagedCommentImageIdPattern.test(normalizedValue) ? normalizedValue : null;
}

export function extractManagedCommentImageId(value: string | null | undefined): string | null {
  const match: RegExpMatchArray | null = (value?.trim() ?? '').match(/^\/images\/([a-f0-9]{32})$/);
  return normalizeManagedCommentImageId(match?.[1]);
}
