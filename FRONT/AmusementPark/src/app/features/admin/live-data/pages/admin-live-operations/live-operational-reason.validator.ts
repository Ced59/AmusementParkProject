const minimumReasonLength = 3;
const maximumReasonLength = 500;
const controlCharacterPattern = /\p{Cc}/u;

export function isValidLiveOperationalReason(value: string): boolean {
  const normalized: string = value.trim();
  return normalized.length >= minimumReasonLength
    && normalized.length <= maximumReasonLength
    && !controlCharacterPattern.test(normalized);
}
