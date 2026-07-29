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

export function extractManagedCommentImageIdsFromHtml(
  value: string | null | undefined
): ReadonlySet<string> {
  const imageIds: Set<string> = new Set<string>();
  if (typeof document === 'undefined') {
    return imageIds;
  }

  const template: HTMLTemplateElement = document.createElement('template');
  template.innerHTML = value ?? '';
  for (const image of Array.from(template.content.querySelectorAll('img'))) {
    const imageId: string | null = extractManagedCommentImageId(image.getAttribute('src'));
    if (imageId !== null) {
      imageIds.add(imageId);
    }
  }

  return imageIds;
}
