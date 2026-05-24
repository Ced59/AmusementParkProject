export function normalizeSeoText(value: string | null | undefined, fallback: string): string {
  const normalizedValue: string = stripHtml(value).replace(/\s+/g, ' ').trim();

  if (!normalizedValue) {
    return fallback;
  }

  return normalizedValue;
}

export function truncateSeoText(value: string, maxLength: number): string {
  if (value.length <= maxLength) {
    return value;
  }

  const trimmedValue: string = value.slice(0, Math.max(0, maxLength - 1)).trimEnd();
  const lastSpaceIndex: number = trimmedValue.lastIndexOf(' ');

  if (lastSpaceIndex >= 80) {
    return `${trimmedValue.slice(0, lastSpaceIndex).trimEnd()}…`;
  }

  return `${trimmedValue}…`;
}

export function stripHtml(value: string | null | undefined): string {
  if (!value) {
    return '';
  }

  return value
    .replace(/<script[\s\S]*?<\/script>/gi, ' ')
    .replace(/<style[\s\S]*?<\/style>/gi, ' ')
    .replace(/<[^>]+>/g, ' ')
    .replace(/&nbsp;/gi, ' ')
    .replace(/&amp;/gi, '&')
    .replace(/&quot;/gi, '"')
    .replace(/&#39;/gi, "'")
    .replace(/&lt;/gi, '<')
    .replace(/&gt;/gi, '>');
}
