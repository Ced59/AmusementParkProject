import { Buffer } from 'node:buffer';
import { promises as fs, readFileSync } from 'node:fs';

const headerBytes = 8192;
const jsonString = '"(?:[^"\\\\\\r\\n]|\\\\.)*"';
// Only the current writer's leading fields qualify for the fast path. Legacy,
// reordered or incomplete headers retain the full-read invalidation behavior.
const cacheHeader = new RegExp(
  '^\\{"buildVersion":' + jsonString + ',"cacheKey":(' + jsonString + '),"statusCode":\\d+,"html":"'
);

/** Reject corrupt JSON on its first lookup, independently of invalidation scans. */
export function readDiskPageCacheJson(filePath: string, removeCorruptEntry: () => void): unknown {
  // File access failures must not delete an otherwise valid cache entry.
  const serialized: string = readFileSync(filePath, 'utf8');
  try {
    return JSON.parse(serialized) as unknown;
  } catch (error: unknown) {
    if (error instanceof SyntaxError) {
      removeCorruptEntry();
    }
    throw error;
  }
}

export function readCacheKeyFromHeader(header: string): string | null {
  const match: RegExpExecArray | null = cacheHeader.exec(header);
  if (match === null) {
    return null;
  }

  try {
    const key: unknown = JSON.parse(match[1]);
    return typeof key === 'string' && key.length > 0 ? key : null;
  } catch {
    return null;
  }
}

/** Read the HTML only when its key may match the targeted invalidation. */
export async function readMatchingDiskPageCacheEntry(
  filePath: string,
  matches: (cacheKey: string) => boolean
): Promise<string | null> {
  const file = await fs.open(filePath, 'r');
  try {
    const buffer = Buffer.alloc(headerBytes);
    const result = await file.read(buffer, 0, buffer.length, 0);
    const key: string | null = readCacheKeyFromHeader(buffer.toString('utf8', 0, result.bytesRead));
    if (key !== null && !matches(key)) {
      return null;
    }

    // The positional header read leaves the file position at zero.
    return await file.readFile({ encoding: 'utf8' });
  } finally {
    await file.close();
  }
}
