import { mkdtemp, readFile, rm, unlink } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import {
  SeoStaticDocumentResponse,
  SeoStaticSnapshotPublisher,
  SeoStaticSnapshotPublishResult,
} from './seo-static-snapshot-publisher';

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
    expect(firstResult.documentCount).toBe(6);
    expect(firstResult.fetchControlStatus).toBe('included');
    expect(await readFile(join(directory, 'current', 'robots.txt'), 'utf8')).toContain('Sitemap:');
    expect(await readFile(join(directory, 'current', 'parks-en.xml'), 'utf8')).toContain('<urlset');
    const control: Buffer = await readFile(join(directory, 'current', 'sitemap-static-fr.xml'));
    expect(control.equals(responses['/sitemaps/static-fr.xml'].body)).toBe(true);
    expect(control.toString('utf8').match(/<url>/g)).toHaveLength(9);
    expect(firstResult.totalBytes).toBe(Object.values(responses).reduce((sum, item) => sum + item.body.length, 0) + control.length);
    expect(await readFile(join(directory, 'current', 'sitemap.xml'), 'utf8')).toBe(responses['/sitemap.xml'].body.toString('utf8'));
    expect(await readFile(join(directory, 'current', 'robots.txt'), 'utf8')).toBe(responses['/robots.txt'].body.toString('utf8'));
    expect(secondResult.status).toBe('unchanged');
    expect(secondResult.fetchControlStatus).toBe('included');
    expect(secondManifest).toBe(firstManifest);
    expect(fetchCount).toBe(10);
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

  it.each(['parks-en.xml', 'sitemap-static-fr.xml'])('republishes an otherwise identical snapshot when %s is missing', async (fileName: string) => {
    const directory: string = await createTemporaryDirectory();
    const responses: Record<string, SeoStaticDocumentResponse> = { ...buildValidResponses() };
    const publisher = new SeoStaticSnapshotPublisher({
      directory,
      publicOrigin: 'https://amusement-parks.fun',
      fetchDocument: async (path: string): Promise<SeoStaticDocumentResponse> => responses[path],
    });

    await publisher.refresh();
    await unlink(join(directory, 'current', fileName));
    const result: SeoStaticSnapshotPublishResult = await publisher.refresh();

    expect(result.status).toBe('published');
    expect(await readFile(join(directory, 'current', fileName), 'utf8')).toContain('<urlset');
    expect((await readFile(join(directory, 'current', 'sitemap-static-fr.xml'))).equals(responses['/sitemaps/static-fr.xml'].body)).toBe(true);
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

  it('updates the control from the validated source on later generations without an extra fetch', async () => {
    const directory: string = await createTemporaryDirectory();
    const responses = buildValidResponses();
    const fetchDocument = vi.fn(async (path: string): Promise<SeoStaticDocumentResponse> => responses[path]);
    const publisher = new SeoStaticSnapshotPublisher({ directory, publicOrigin: 'https://amusement-parks.fun', fetchDocument });
    const first = await publisher.refresh();
    responses['/sitemaps/static-fr.xml'] = response(`${responses['/sitemaps/static-fr.xml'].body.toString('utf8')}\n`);

    const changed = await publisher.refresh();
    const unchanged = await publisher.refresh();

    expect(changed.status).toBe('published');
    expect(changed.digest).not.toBe(first.digest);
    expect(unchanged.status).toBe('unchanged');
    expect(unchanged.digest).toBe(changed.digest);
    expect((await readFile(join(directory, 'current', 'sitemap-static-fr.xml'))).equals(responses['/sitemaps/static-fr.xml'].body)).toBe(true);
    expect(fetchDocument).toHaveBeenCalledTimes(15);
    expect(fetchDocument).not.toHaveBeenCalledWith('/sitemaps/sitemap-static-fr.xml');
  });

  it.each<[string, SeoStaticDocumentResponse]>([
    ['incomplete', response('<urlset>')],
    ['unavailable', response('Unavailable', 503)],
    ['empty', response('')]
  ])('keeps the last valid snapshot when the French source is %s', async (_case: string, invalidSource: SeoStaticDocumentResponse) => {
    const directory: string = await createTemporaryDirectory();
    const responses = buildValidResponses();
    const publisher = new SeoStaticSnapshotPublisher({
      directory, publicOrigin: 'https://amusement-parks.fun',
      fetchDocument: async (path: string): Promise<SeoStaticDocumentResponse> => responses[path]
    });
    await publisher.refresh();
    const original = await readFile(join(directory, 'current', 'sitemap-static-fr.xml'));
    const originalManifest = await readFile(join(directory, 'current', '.manifest.json'), 'utf8');
    responses['/sitemaps/static-fr.xml'] = invalidSource;

    await expect(publisher.refresh()).rejects.toThrow();
    expect((await readFile(join(directory, 'current', 'sitemap-static-fr.xml'))).equals(original)).toBe(true);
    expect(await readFile(join(directory, 'current', '.manifest.json'), 'utf8')).toBe(originalManifest);
  });

  it('publishes the main snapshot without a control when the French source is no longer announced', async () => {
    const directory: string = await createTemporaryDirectory();
    const responses = buildValidResponses();
    const fetchDocument = vi.fn(async (path: string): Promise<SeoStaticDocumentResponse> => responses[path]);
    const publisher = new SeoStaticSnapshotPublisher({ directory, publicOrigin: 'https://amusement-parks.fun', fetchDocument });
    await publisher.refresh();
    responses['/sitemap.xml'] = response(buildIndexXml(['https://amusement-parks.fun/parks-en.xml']));
    fetchDocument.mockClear();

    const result = await publisher.refresh();
    expect(result.status).toBe('published');
    expect(result.fetchControlStatus).toBe('source-missing');
    expect(result.documentCount).toBe(3);
    expect(fetchDocument).toHaveBeenCalledTimes(3);
    expect(await readFile(join(directory, 'current', 'sitemap.xml'), 'utf8')).toBe(responses['/sitemap.xml'].body.toString('utf8'));
    expect(await readFile(join(directory, 'current', 'parks-en.xml'), 'utf8')).toContain('<urlset');
    await expect(readFile(join(directory, 'current', 'sitemap-static-fr.xml'))).rejects.toMatchObject({ code: 'ENOENT' });
    expect((await publisher.refresh()).fetchControlStatus).toBe('source-missing');
  });

  it.each(['sitemap-static-fr.xml', 'SITEMAP-STATIC-FR.xml'])('preserves the advertised child %s and reports a collision', async (controlFileName: string) => {
    const directory: string = await createTemporaryDirectory();
    const responses = buildValidResponses();
    responses['/sitemap.xml'] = response(buildIndexXml([
      'https://amusement-parks.fun/static-fr.xml',
      `https://amusement-parks.fun/${controlFileName}`
    ]));
    responses[`/sitemaps/${controlFileName}`] = response(buildUrlSetXml('https://amusement-parks.fun/fr/parks'));
    const fetchDocument = vi.fn(async (path: string): Promise<SeoStaticDocumentResponse> => responses[path]);
    const publisher = new SeoStaticSnapshotPublisher({ directory, publicOrigin: 'https://amusement-parks.fun', fetchDocument });

    const result = await publisher.refresh();
    expect(result.status).toBe('published');
    expect(result.fetchControlStatus).toBe('name-conflict');
    expect(result.documentCount).toBe(4);
    expect(fetchDocument).toHaveBeenCalledTimes(4);
    expect((await readFile(join(directory, 'current', controlFileName))).equals(responses[`/sitemaps/${controlFileName}`].body)).toBe(true);
    expect(await readFile(join(directory, 'current', 'sitemap.xml'), 'utf8')).toBe(responses['/sitemap.xml'].body.toString('utf8'));
  });

  it.each([5, 6])('preserves the main snapshot within a limit of %i documents', async (maxDocuments: number) => {
    const directory: string = await createTemporaryDirectory();
    const responses = buildValidResponses();
    const fetchDocument = vi.fn(async (path: string): Promise<SeoStaticDocumentResponse> => responses[path]);
    const publisher = new SeoStaticSnapshotPublisher({ directory, publicOrigin: 'https://amusement-parks.fun', fetchDocument, maxDocuments });

    const result = await publisher.refresh();
    expect(result.status).toBe('published');
    expect(result.documentCount).toBe(maxDocuments);
    expect(result.fetchControlStatus).toBe(maxDocuments === 5 ? 'document-limit' : 'included');
    expect(fetchDocument).toHaveBeenCalledTimes(5);
    expect(await readFile(join(directory, 'current', 'sitemap.xml'), 'utf8')).toBe(responses['/sitemap.xml'].body.toString('utf8'));
    if (maxDocuments === 5) {
      await expect(readFile(join(directory, 'current', 'sitemap-static-fr.xml'))).rejects.toMatchObject({ code: 'ENOENT' });
    }
    const unchanged = await publisher.refresh();
    expect(unchanged.status).toBe('unchanged');
    expect(unchanged.fetchControlStatus).toBe(result.fetchControlStatus);
  });

  it('still rejects a main snapshot exceeding the document limit before fetching children', async () => {
    const directory: string = await createTemporaryDirectory();
    const responses = buildValidResponses();
    const fetchDocument = vi.fn(async (path: string): Promise<SeoStaticDocumentResponse> => responses[path]);
    const publisher = new SeoStaticSnapshotPublisher({ directory, publicOrigin: 'https://amusement-parks.fun', fetchDocument, maxDocuments: 4 });

    await expect(publisher.refresh()).rejects.toThrow('contains 5 documents; maximum is 4');
    expect(fetchDocument).toHaveBeenCalledTimes(2);
  });

  it.each(['main-only', 'one-byte-short', 'with-control'])('preserves the main snapshot at the %s byte boundary', async (boundary: string) => {
    const directory: string = await createTemporaryDirectory();
    const responses = buildValidResponses();
    const sourceBytes: number = Object.values(responses).reduce((sum, item) => sum + item.body.length, 0);
    const totalBytes: number = sourceBytes + responses['/sitemaps/static-fr.xml'].body.length;
    const maxTotalBytes: number = boundary === 'main-only' ? sourceBytes : totalBytes - (boundary === 'one-byte-short' ? 1 : 0);
    const includesControl: boolean = boundary === 'with-control';
    const publisher = new SeoStaticSnapshotPublisher({
      directory, publicOrigin: 'https://amusement-parks.fun', maxTotalBytes,
      fetchDocument: async (path: string): Promise<SeoStaticDocumentResponse> => responses[path]
    });

    const result = await publisher.refresh();
    expect(result.status).toBe('published');
    expect(result.fetchControlStatus).toBe(includesControl ? 'included' : 'byte-limit');
    expect(result.documentCount).toBe(includesControl ? 6 : 5);
    expect(result.totalBytes).toBe(includesControl ? totalBytes : sourceBytes);
    expect(result.totalBytes).toBeLessThanOrEqual(maxTotalBytes);
    expect(await readFile(join(directory, 'current', 'sitemap.xml'), 'utf8')).toBe(responses['/sitemap.xml'].body.toString('utf8'));
    if (!includesControl) {
      await expect(readFile(join(directory, 'current', 'sitemap-static-fr.xml'))).rejects.toMatchObject({ code: 'ENOENT' });
    }
    const unchanged = await publisher.refresh();
    expect(unchanged.status).toBe('unchanged');
    expect(unchanged.fetchControlStatus).toBe(result.fetchControlStatus);
  });

  it('still rejects a main snapshot exceeding the byte limit', async () => {
    const directory: string = await createTemporaryDirectory();
    const responses = buildValidResponses();
    const sourceBytes: number = Object.values(responses).reduce((sum, item) => sum + item.body.length, 0);
    const publisher = new SeoStaticSnapshotPublisher({
      directory, publicOrigin: 'https://amusement-parks.fun', maxTotalBytes: sourceBytes - 1,
      fetchDocument: async (path: string): Promise<SeoStaticDocumentResponse> => responses[path]
    });

    await expect(publisher.refresh()).rejects.toThrow(`contains ${sourceBytes} bytes; maximum is ${sourceBytes - 1}`);
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
      'https://amusement-parks.fun/static-fr.xml',
    ])),
    '/sitemaps/parks-en.xml': response(buildUrlSetXml('https://amusement-parks.fun/en/parks')),
    '/sitemaps/parks-fr.xml': response(buildUrlSetXml('https://amusement-parks.fun/fr/parcs')),
    '/sitemaps/static-fr.xml': response(buildUrlSetXml([
      'home', 'parks', 'rankings', 'rankings/methodology', 'manufacturers',
      'about', 'contact', 'versions', 'privacy'
    ].map(segment => `https://amusement-parks.fun/fr/${segment}`))),
  };
}

function buildIndexXml(locations: string[]): string {
  const entries: string = locations
    .map((location: string): string => `<sitemap><loc>${location}</loc></sitemap>`)
    .join('');
  return `<?xml version="1.0" encoding="utf-8"?><sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">${entries}</sitemapindex>`;
}

function buildUrlSetXml(location: string | readonly string[]): string {
  const locations: readonly string[] = typeof location === 'string' ? [location] : location;
  const entries: string = locations.map(value => `<url><loc>${value}</loc></url>`).join('');
  return `<?xml version="1.0" encoding="utf-8"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">${entries}</urlset>`;
}

function response(body: string, statusCode: number = 200): SeoStaticDocumentResponse {
  return { statusCode, body: Buffer.from(body, 'utf8') };
}
