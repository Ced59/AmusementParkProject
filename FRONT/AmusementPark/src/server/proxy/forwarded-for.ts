export type ForwardedForHeaderValue = string | string[] | undefined;

export function appendForwardedFor(
  existingHeader: ForwardedForHeaderValue,
  remoteAddress: string | undefined,
): string {
  const existingChain = normalizeHeaderValue(existingHeader);
  const normalizedRemoteAddress = remoteAddress?.trim() ?? '';

  if (!existingChain) {
    return normalizedRemoteAddress;
  }

  if (!normalizedRemoteAddress) {
    return existingChain;
  }

  return `${existingChain}, ${normalizedRemoteAddress}`;
}

function normalizeHeaderValue(value: ForwardedForHeaderValue): string {
  if (Array.isArray(value)) {
    return value
      .map((candidate: string) => candidate.trim())
      .filter((candidate: string) => candidate.length > 0)
      .join(', ');
  }

  return value?.trim() ?? '';
}
