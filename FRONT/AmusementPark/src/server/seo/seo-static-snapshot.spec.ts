import { mkdtemp, readFile, rm, unlink } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import {
  SeoStaticDocumentResponse,
  SeoStaticSnapshotPublisher,
  SeoStaticSnapshotPublishResult,
} from './seo-static-snapshot';

describe('SeoStaticSnapshotPublisher', () => {
  const temporaryDirectories: string[] = [];

  afterEach(async (): Promise<void> => {
    await Promise.all(temporaryDirectories.splice(0).map(
      async (directory: string): Promise<void> => rm(directory, { recursive: true, force: true }),
    ));
  });

  it('publishes a complete snapshot and keeps identical files unchanged', async () => {
    const directory: string = await createTemporaryDirectory();
    const responses: Readonly<Record<string, SeoStaticDocumentResponse>> = buildValidResponses();
    let fetchCount: number = 0;
    const publisher = new SeoStaticSnapshotPublisher({
      directory,
      publicOrigin: 'https://amusement-parks.fun',
      fetchDocument: async (path: string): Promise<SeoStaticDocumentResponse> => {
        fetchCount += 1;
        const response: SeoStaticDocumentResponse | undefined = responses[path];
        if (response === undefined) {
          throw new Error(`Unexpected path: ${path}`);
        }
        return response;
      },
    });

    const firstResult: SeoStaticSnapshotPublishResult = await publisher.refresh();
    const firstManifest: string = await readFile(join(directory, 'current', '.manifest.json'), 'utf8');
    const secondResult: SeoStaticSnapshotPublishResult = await publisher.refresh();
    const secondManifest: string = await readFile(join(directory, 'current', '.manifest.json'), 'utf8');

    expect(firstResult.status).toBe('published');
    expect(firstResult.documentCount).toBe(4);
    expect(await readFile(join(directory, 'current', 'robots.txt'), 'utf8')).toContain('Sitemap:');
    expect(await readFile(join(directory, 'current', 'parks-en.xml'), 'utf8')).toContain('<urlset');
    expect(secondResult.status).toBe('unchanged');
    expect(secondManifest).toBe(firstManifest);
    expect(fetchCount).toBe(8);
  });

  it('retains the current snapshot when a refreshed child sitemap is incomplete', async () => {
    const directory: string = await createTemporaryDirectory();
    const responses: Record<string, SeoStaticDocumentResponse> = { ...buildValidResponses() };
    const publisher = new SeoStaticSnapshotPublisher({
      directory,
      publicOrigin: 'https://amusement-parks.fun',
      fetchDocument: async (path: string): Promise<SeoStaticDocumentResponse> => responses[path],
    });

    await publisher.refresh();
    const originalChild: string = await readFile(join(directory, 'current', 'parks-en.xml'), 'utf8');
    responses['/sitemaps/parks-en.xml'] = response('<urlset>');

    await expect(publisher.refresh()).rejects.toThrow('not a complete urlset XML document');
    expect(await readFile(join(directory, 'current', 'parks-en.xml'), 'utf8')).toBe(originalChild);
  });

  it('republishes an otherwise identical snapshot when an active file is missing', async () => {
    const directory: string = await createTemporaryDirectory();
    const responses: Record<string, SeoStaticDocumentResponse> = { ...buildValidResponses() };
    const publisher = new SeoStaticSnapshotPublisher({
      directory,
      publicOrigin: 'https://amusement-parks.fun',
      fetchDocument: async (path: string): Promise<SeoStaticDocumentResponse> => responses[path],
    });

    await publisher.refresh();
    await unlink(join(directory, 'current', 'parks-en.xml'));
    const result: SeoStaticSnapshotPublishResult = await publisher.refresh();

    expect(result.status).toBe('published');
    expect(await readFile(join(directory, 'current', 'parks-en.xml'), 'utf8')).toContain('<urlset');
  });

  it('rejects child sitemap locations outside the configured public origin', async () => {
    const directory: string = await createTemporaryDirectory();
    const responses: Record<string, SeoStaticDocumentResponse> = {
      ...buildValidResponses(),
      '/sitemap.xml': response(buildIndexXml([
        'https://malicious.example/parks-en.xml',
      ])),
    };
    const publisher = new SeoStaticSnapshotPublisher({
      directory,
      publicOrigin: 'https://amusement-parks.fun',
      fetchDocument: async (path: string): Promise<SeoStaticDocumentResponse> => responses[path],
    });

    await expect(publisher.refresh()).rejects.toThrow('unsupported location');
  });

  function createTemporaryDirectory(): Promise<string> {
    return mkdtemp(join(tmpdir(), 'amusementpark-seo-snapshot-'))
      .then((directory: string): string => {
        temporaryDirectories.push(directory);
        return directory;
      });
  }
});

function buildValidResponses(): Record<string, SeoStaticDocumentResponse> {
  return {
    '/robots.txt': response([
      'User-agent: *',
      'Allow: /',
      'Sitemap: https://amusement-parks.fun/sitemap.xml',
      '',
    ].join('\n')),
    '/sitemap.xml': response(buildIndexXml([
      'https://amusement-parks.fun/parks-en.xml',
      'https://amusement-parks.fun/parks-fr.xml',
    ])),
    '/sitemaps/parks-en.xml': response(buildUrlSetXml('https://amusement-parks.fun/en/parks')),
    '/sitemaps/parks-fr.xml': response(buildUrlSetXml('https://amusement-parks.fun/fr/parcs')),
  };
}

function buildIndexXml(locations: string[]): string {
  const entries: string = locations
    .map((location: string): string => `<sitemap><loc>${location}</loc></sitemap>`)
    .join('');
  return `<?xml version="1.0" encoding="utf-8"?><sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">${entries}</sitemapindex>`;
}

function buildUrlSetXml(location: string): string {
  return `<?xml version="1.0" encoding="utf-8"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"><url><loc>${location}</loc></url></urlset>`;
}

function response(body: string, statusCode: number = 200): SeoStaticDocumentResponse {
  return { statusCode, body: Buffer.from(body, 'utf8') };
}
