import assert from 'node:assert/strict';
import { promises as fs, unlinkSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { test } from 'node:test';
import { readCacheKeyFromHeader, readDiskPageCacheJson, readMatchingDiskPageCacheEntry } from '../src/server/ssr/disk-page-cache-invalidation-reader.ts';

function entry(cacheKey, html = 'large HTML') {
  return JSON.stringify({ buildVersion: 'test-build', cacheKey, statusCode: 200, html, seoReady: true, expiresAt: 42 });
}

test('preserves JSON escaping and Unicode in the leading cache key', () => {
  const key = 'https://example.test/fr/parc/été?title="test"&path=\\';
  assert.equal(readCacheKeyFromHeader(entry(key)), key);
});

test('never confuses HTML, nested keys, legacy fields or a truncated key with the header', () => {
  for (const header of [
    JSON.stringify({ html: entry('wrong'), cacheKey: 'right' }),
    JSON.stringify({ nested: { cacheKey: 'wrong' }, cacheKey: 'right' }),
    JSON.stringify({ cacheKey: 'legacy', html: 'body' }),
    entry('x'.repeat(9000)).slice(0, 8192),
    entry(''),
    '{"buildVersion":"v","cacheKey":"bad\\q","statusCode":200,"html":"',
  ]) {
    assert.equal(readCacheKeyFromHeader(header), null);
  }
});

test('reads at most an 8 KiB header for an unrelated 2 MiB HTML entry and closes the file', async (context) => {
  let bytesRequested = 0;
  let closed = false;
  context.mock.method(fs, 'open', async () => ({
    async read(buffer, offset, length, position) {
      assert.equal(position, 0);
      bytesRequested += length;
      const content = Buffer.from(entry('unrelated', 'x'.repeat(2 * 1024 * 1024)));
      const bytesRead = content.copy(buffer, offset, 0, length);
      return { bytesRead };
    },
    async readFile() { assert.fail('must not read unrelated HTML'); },
    async close() { closed = true; },
  }));
  assert.equal(await readMatchingDiskPageCacheEntry('cache.json', (key) => key === 'target'), null);
  assert.equal(bytesRequested, 8192);
  assert.equal(closed, true);
});

test('reads a complete matching entry from byte zero and preserves the unrelated file', async () => {
  const directory = await fs.mkdtemp(join(tmpdir(), 'ap-cache-read-'));
  const path = join(directory, 'entry.json');
  const content = entry('target', 'é'.repeat(20000));
  try {
    await fs.writeFile(path, content);
    assert.equal(await readMatchingDiskPageCacheEntry(path, (key) => key === 'target'), content);
    assert.equal(await readMatchingDiskPageCacheEntry(path, () => false), null);
    assert.equal(await fs.readFile(path, 'utf8'), content);
  } finally {
    await fs.rm(directory, { recursive: true });
  }
});

test('keeps the legacy full-read path for reordered, corrupt and oversized headers', async () => {
  const directory = await fs.mkdtemp(join(tmpdir(), 'ap-cache-legacy-'));
  const path = join(directory, 'entry.json');
  try {
    for (const content of [JSON.stringify({ html: 'body', cacheKey: 'target' }), '{invalid', entry('x'.repeat(9000))]) {
      await fs.writeFile(path, content);
      assert.equal(await readMatchingDiskPageCacheEntry(path, () => false), content);
    }
  } finally {
    await fs.rm(directory, { recursive: true });
  }
});

test('closes the descriptor even if matching or full reading fails', async (context) => {
  let closes = 0;
  context.mock.method(fs, 'open', async () => ({
    async read(buffer) {
      const bytesRead = Buffer.from(entry('target')).copy(buffer);
      return { bytesRead };
    },
    async readFile() { throw new Error('read failed'); },
    async close() { closes++; },
  }));
  await assert.rejects(readMatchingDiskPageCacheEntry('entry', () => { throw new Error('matcher failed'); }), /matcher failed/);
  await assert.rejects(readMatchingDiskPageCacheEntry('entry', () => true), /read failed/);
  assert.equal(closes, 2);
});

test('evicts a prefix-valid truncated nonmatching entry on its first lookup', async () => {
  const directory = await fs.mkdtemp(join(tmpdir(), 'ap-cache-corrupt-'));
  const path = join(directory, 'entry.json');
  let removals = 0;
  const remove = () => { unlinkSync(path); removals++; };
  try {
    const truncated = entry('target', 'x'.repeat(20000)).slice(0, -30);
    await fs.writeFile(path, truncated);
    assert.equal(await readMatchingDiskPageCacheEntry(path, () => false), null);
    assert.throws(() => readDiskPageCacheJson(path, remove), SyntaxError);
    assert.equal(removals, 1);
    await assert.rejects(fs.stat(path), { code: 'ENOENT' });
    assert.throws(() => readDiskPageCacheJson(path, remove), { code: 'ENOENT' });
    assert.equal(removals, 1);
  } finally {
    await fs.rm(directory, { recursive: true });
  }
});

test('retains valid JSON and does not treat a file-access failure as corruption', async () => {
  const directory = await fs.mkdtemp(join(tmpdir(), 'ap-cache-valid-'));
  const path = join(directory, 'entry.json');
  const remove = () => assert.fail('valid or inaccessible entries must not be removed');
  try {
    await fs.writeFile(path, entry('target'));
    assert.equal(readDiskPageCacheJson(path, remove).cacheKey, 'target');
    assert.throws(() => readDiskPageCacheJson(join(directory, 'missing.json'), remove), { code: 'ENOENT' });
    assert.equal(await fs.readFile(path, 'utf8'), entry('target'));
  } finally {
    await fs.rm(directory, { recursive: true });
  }
});
