import { createHash, randomUUID } from 'node:crypto';
import { mkdir, readFile, readdir, rename, rm, stat, writeFile } from 'node:fs/promises';
import { join } from 'node:path';

export interface SeoStaticDocumentResponse {
  readonly statusCode: number;
  readonly body: Buffer;
}

export type FetchSeoStaticDocument = (path: string) => Promise<SeoStaticDocumentResponse>;

export interface SeoStaticSnapshotPublisherOptions {
  readonly directory: string;
  readonly publicOrigin: string;
  readonly fetchDocument: FetchSeoStaticDocument;
  readonly fetchConcurrency?: number;
  readonly maxDocuments?: number;
  readonly maxTotalBytes?: number;
  readonly staleLockMilliseconds?: number;
}

export type SeoStaticSnapshotPublishStatus = 'published' | 'unchanged' | 'busy';

export interface SeoStaticSnapshotPublishResult {
  readonly status: SeoStaticSnapshotPublishStatus;
  readonly documentCount: number;
  readonly totalBytes: number;
  readonly digest?: string;
}

interface SeoStaticSnapshotDocument {
  readonly fileName: string;
  readonly body: Buffer;
}

interface SeoStaticSnapshotManifest {
  readonly digest: string;
  readonly documentCount: number;
  readonly totalBytes: number;
  readonly publishedAtUtc: string;
}

const sitemapFileNamePattern = /^[A-Za-z0-9_-]+\.xml$/;
const defaultFetchConcurrency = 2;
const defaultMaxDocuments = 500;
const defaultMaxTotalBytes = 128 * 1024 * 1024;
const defaultStaleLockMilliseconds = 15 * 60 * 1000;
const manifestFileName = '.manifest.json';
const publicationLockDirectoryName = '.publish-lock';

export class SeoStaticSnapshotPublisher {
  private activeRefresh: Promise<SeoStaticSnapshotPublishResult> | null = null;

  public constructor(private readonly options: SeoStaticSnapshotPublisherOptions) {
  }

  public refresh(): Promise<SeoStaticSnapshotPublishResult> {
    if (this.activeRefresh !== null) {
      return this.activeRefresh;
    }

    this.activeRefresh = this.publish()
      .finally((): void => {
        this.activeRefresh = null;
      });

    return this.activeRefresh;
  }

  private async publish(): Promise<SeoStaticSnapshotPublishResult> {
    const baseDirectory: string = this.options.directory;
    await mkdir(baseDirectory, { recursive: true });

    const lockDirectory: string = join(baseDirectory, publicationLockDirectoryName);
    if (!await acquirePublicationLock(
      lockDirectory,
      this.options.staleLockMilliseconds ?? defaultStaleLockMilliseconds,
    )) {
      return {
        status: 'busy',
        documentCount: 0,
        totalBytes: 0,
      };
    }

    const stagingDirectory: string = join(baseDirectory, `.staging-${process.pid}-${randomUUID()}`);

    try {
      await cleanupStaleStagingDirectories(baseDirectory);
      const documents: SeoStaticSnapshotDocument[] = await this.fetchSnapshotDocuments();
      const totalBytes: number = documents.reduce(
        (sum: number, document: SeoStaticSnapshotDocument): number => sum + document.body.length,
        0,
      );
      const digest: string = calculateSnapshotDigest(documents);
      const currentDirectory: string = join(baseDirectory, 'current');
      const currentManifest: SeoStaticSnapshotManifest | null = await readManifest(currentDirectory);

      if (currentManifest?.digest === digest
        && await snapshotContainsAllDocuments(currentDirectory, documents)) {
        return {
          status: 'unchanged',
          documentCount: documents.length,
          totalBytes,
          digest,
        };
      }

      await mkdir(stagingDirectory, { recursive: false });
      await Promise.all(documents.map(async (document: SeoStaticSnapshotDocument): Promise<void> => {
        await writeFile(join(stagingDirectory, document.fileName), document.body, { mode: 0o644 });
      }));

      const manifest: SeoStaticSnapshotManifest = {
        digest,
        documentCount: documents.length,
        totalBytes,
        publishedAtUtc: new Date().toISOString(),
      };
      await writeFile(
        join(stagingDirectory, manifestFileName),
        `${JSON.stringify(manifest, null, 2)}\n`,
        { encoding: 'utf8', mode: 0o644 },
      );

      await activateSnapshot(baseDirectory, stagingDirectory);

      return {
        status: 'published',
        documentCount: documents.length,
        totalBytes,
        digest,
      };
    } finally {
      await rm(stagingDirectory, { recursive: true, force: true }).catch((): void => undefined);
      await rm(lockDirectory, { recursive: true, force: true }).catch((): void => undefined);
    }
  }

  private async fetchSnapshotDocuments(): Promise<SeoStaticSnapshotDocument[]> {
    const [robotsResponse, indexResponse]: SeoStaticDocumentResponse[] = await Promise.all([
      this.options.fetchDocument('/robots.txt'),
      this.options.fetchDocument('/sitemap.xml'),
    ]);

    validateDocumentResponse('/robots.txt', robotsResponse, 'robots');
    validateDocumentResponse('/sitemap.xml', indexResponse, 'index');

    const publicOrigin: string = normalizePublicOrigin(this.options.publicOrigin);
    const childFileNames: string[] = extractSitemapFileNames(indexResponse.body, publicOrigin);
    const maxDocuments: number = normalizePositiveInteger(
      this.options.maxDocuments,
      defaultMaxDocuments,
    );

    if (childFileNames.length + 2 > maxDocuments) {
      throw new Error(`SEO static snapshot contains ${childFileNames.length + 2} documents; maximum is ${maxDocuments}.`);
    }

    const childDocuments: SeoStaticSnapshotDocument[] = new Array(childFileNames.length);
    const fetchConcurrency: number = Math.min(
      childFileNames.length,
      normalizePositiveInteger(this.options.fetchConcurrency, defaultFetchConcurrency),
    );
    let nextIndex: number = 0;

    const workers: Promise<void>[] = Array.from(
      { length: fetchConcurrency },
      async (): Promise<void> => {
        while (nextIndex < childFileNames.length) {
          const childIndex: number = nextIndex;
          nextIndex += 1;
          const fileName: string = childFileNames[childIndex];
          const response: SeoStaticDocumentResponse = await this.options.fetchDocument(`/sitemaps/${fileName}`);
          validateDocumentResponse(fileName, response, 'section');
          childDocuments[childIndex] = { fileName, body: response.body };
        }
      },
    );

    await Promise.all(workers);

    const documents: SeoStaticSnapshotDocument[] = [
      { fileName: 'robots.txt', body: robotsResponse.body },
      { fileName: 'sitemap.xml', body: indexResponse.body },
      ...childDocuments,
    ];
    const totalBytes: number = documents.reduce(
      (sum: number, document: SeoStaticSnapshotDocument): number => sum + document.body.length,
      0,
    );
    const maxTotalBytes: number = normalizePositiveInteger(
      this.options.maxTotalBytes,
      defaultMaxTotalBytes,
    );

    if (totalBytes > maxTotalBytes) {
      throw new Error(`SEO static snapshot contains ${totalBytes} bytes; maximum is ${maxTotalBytes}.`);
    }

    return documents;
  }
}

function validateDocumentResponse(
  documentName: string,
  response: SeoStaticDocumentResponse,
  documentKind: 'robots' | 'index' | 'section',
): void {
  if (response.statusCode < 200 || response.statusCode >= 300) {
    throw new Error(`SEO document ${documentName} returned HTTP ${response.statusCode}.`);
  }

  if (response.body.length === 0) {
    throw new Error(`SEO document ${documentName} is empty.`);
  }

  const body: string = response.body.toString('utf8');
  if (documentKind === 'robots') {
    if (!/^user-agent\s*:/im.test(body) || !/^sitemap\s*:/im.test(body)) {
      throw new Error('robots.txt does not contain the required User-agent and Sitemap directives.');
    }
    return;
  }

  const expectedRootElement: string = documentKind === 'index' ? 'sitemapindex' : 'urlset';
  if (!new RegExp(`<${expectedRootElement}(?:\\s|>)`, 'i').test(body)
    || !new RegExp(`</${expectedRootElement}>`, 'i').test(body)) {
    throw new Error(`SEO document ${documentName} is not a complete ${expectedRootElement} XML document.`);
  }
}

function extractSitemapFileNames(indexBody: Buffer, publicOrigin: string): string[] {
  const indexXml: string = indexBody.toString('utf8');
  const fileNames: string[] = [];
  const seenFileNames = new Set<string>();
  const locationPattern: RegExp = /<loc>\s*([^<]+?)\s*<\/loc>/gi;
  let match: RegExpExecArray | null = locationPattern.exec(indexXml);

  while (match !== null) {
    const rawLocation: string = decodeXmlText(match[1]);
    const locationUrl: URL = new URL(rawLocation);

    if (locationUrl.origin !== publicOrigin
      || locationUrl.search.length > 0
      || locationUrl.hash.length > 0) {
      throw new Error(`Sitemap index contains an unsupported location: ${rawLocation}`);
    }

    const pathSegments: string[] = locationUrl.pathname.split('/').filter((segment: string): boolean => segment.length > 0);
    if (pathSegments.length !== 1) {
      throw new Error(`Sitemap index location must target a root document: ${rawLocation}`);
    }

    const fileName: string = decodeURIComponent(pathSegments[0]);
    if (!sitemapFileNamePattern.test(fileName)
      || fileName.toLowerCase() === 'sitemap.xml'
      || fileName.toLowerCase() === 'sitemaps.xml') {
      throw new Error(`Sitemap index contains an invalid child file name: ${fileName}`);
    }

    const normalizedFileName: string = fileName.toLowerCase();
    if (seenFileNames.has(normalizedFileName)) {
      throw new Error(`Sitemap index contains a duplicate child file name: ${fileName}`);
    }

    seenFileNames.add(normalizedFileName);
    fileNames.push(fileName);
    match = locationPattern.exec(indexXml);
  }

  if (fileNames.length === 0) {
    throw new Error('Sitemap index does not contain any child sitemap location.');
  }

  return fileNames;
}

function decodeXmlText(value: string): string {
  return value
    .replace(/&amp;/gi, '&')
    .replace(/&quot;/gi, '"')
    .replace(/&apos;/gi, "'")
    .replace(/&lt;/gi, '<')
    .replace(/&gt;/gi, '>')
    .trim();
}

function normalizePublicOrigin(value: string): string {
  const url: URL = new URL(value);
  if (url.pathname !== '/' || url.search.length > 0 || url.hash.length > 0) {
    throw new Error('SEO static snapshot public origin must not contain a path, query string, or fragment.');
  }

  return url.origin;
}

function normalizePositiveInteger(value: number | undefined, defaultValue: number): number {
  if (value === undefined || !Number.isSafeInteger(value) || value <= 0) {
    return defaultValue;
  }

  return value;
}

function calculateSnapshotDigest(documents: SeoStaticSnapshotDocument[]): string {
  const hash = createHash('sha256');
  const orderedDocuments: SeoStaticSnapshotDocument[] = [...documents]
    .sort((left: SeoStaticSnapshotDocument, right: SeoStaticSnapshotDocument): number => left.fileName.localeCompare(right.fileName));

  for (const document of orderedDocuments) {
    hash.update(document.fileName, 'utf8');
    hash.update('\0', 'utf8');
    hash.update(document.body);
    hash.update('\0', 'utf8');
  }

  return hash.digest('hex');
}

async function readManifest(directory: string): Promise<SeoStaticSnapshotManifest | null> {
  try {
    const serializedManifest: string = await readFile(join(directory, manifestFileName), 'utf8');
    const manifest: unknown = JSON.parse(serializedManifest);
    if (!isObject(manifest)
      || typeof manifest['digest'] !== 'string'
      || typeof manifest['documentCount'] !== 'number'
      || typeof manifest['totalBytes'] !== 'number'
      || typeof manifest['publishedAtUtc'] !== 'string') {
      return null;
    }

    return {
      digest: manifest['digest'],
      documentCount: manifest['documentCount'],
      totalBytes: manifest['totalBytes'],
      publishedAtUtc: manifest['publishedAtUtc'],
    };
  } catch {
    return null;
  }
}

async function snapshotContainsAllDocuments(
  directory: string,
  documents: SeoStaticSnapshotDocument[],
): Promise<boolean> {
  try {
    const matches: boolean[] = await Promise.all(documents.map(
      async (document: SeoStaticSnapshotDocument): Promise<boolean> => {
        const documentStats = await stat(join(directory, document.fileName));
        return documentStats.isFile() && documentStats.size === document.body.length;
      },
    ));
    return matches.every((matchesDocument: boolean): boolean => matchesDocument);
  } catch {
    return false;
  }
}

async function cleanupStaleStagingDirectories(baseDirectory: string): Promise<void> {
  const entries = await readdir(baseDirectory, { withFileTypes: true });
  await Promise.all(entries
    .filter((entry): boolean => entry.isDirectory() && entry.name.startsWith('.staging-'))
    .map(async (entry): Promise<void> => {
      await rm(join(baseDirectory, entry.name), { recursive: true, force: true });
    }));
}

async function acquirePublicationLock(lockDirectory: string, staleAfterMilliseconds: number): Promise<boolean> {
  try {
    await mkdir(lockDirectory);
    return true;
  } catch (error: unknown) {
    if (!isNodeErrorWithCode(error, 'EEXIST')) {
      throw error;
    }
  }

  try {
    const lockStats = await stat(lockDirectory);
    if (Date.now() - lockStats.mtimeMs <= staleAfterMilliseconds) {
      return false;
    }

    await rm(lockDirectory, { recursive: true, force: true });
    await mkdir(lockDirectory);
    return true;
  } catch (error: unknown) {
    if (isNodeErrorWithCode(error, 'EEXIST') || isNodeErrorWithCode(error, 'ENOENT')) {
      return false;
    }

    throw error;
  }
}

async function activateSnapshot(baseDirectory: string, stagingDirectory: string): Promise<void> {
  const currentDirectory: string = join(baseDirectory, 'current');
  const previousDirectory: string = join(baseDirectory, 'previous');
  let movedCurrentToPrevious: boolean = false;

  await rm(previousDirectory, { recursive: true, force: true });

  try {
    await rename(currentDirectory, previousDirectory);
    movedCurrentToPrevious = true;
  } catch (error: unknown) {
    if (!isNodeErrorWithCode(error, 'ENOENT')) {
      throw error;
    }
  }

  try {
    await rename(stagingDirectory, currentDirectory);
  } catch (error: unknown) {
    if (movedCurrentToPrevious) {
      await rename(previousDirectory, currentDirectory).catch((): void => undefined);
    }
    throw error;
  }
}

function isObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isNodeErrorWithCode(error: unknown, code: string): boolean {
  return error instanceof Error && 'code' in error && error.code === code;
}
