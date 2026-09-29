import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';

const APP_ROOT = join(process.cwd(), 'src', 'app');
const ALLOWED_ADAPTER = 'core/analytics/matomo-product-analytics.service.ts';
const BANNED_MARKERS = [
  'PASSPORT_PRODUCT_ANALYTICS_PORT',
  'SHARE_PRODUCT_ANALYTICS_PORT',
  'PassportProductAnalyticsPort',
  'ShareProductAnalyticsPort',
  'passport-product-analytics.port',
  'share-product-analytics.port',
];

function walk(directory) {
  return readdirSync(directory)
    .flatMap((entry) => {
      const path = join(directory, entry);
      return statSync(path).isDirectory() ? walk(path) : [path];
    });
}

const sourceFiles = walk(APP_ROOT).filter((path) => path.endsWith('.ts'));
const violations = [];

for (const path of sourceFiles) {
  const source = readFileSync(path, 'utf8');
  const normalizedPath = relative(APP_ROOT, path).replaceAll('\\', '/');

  for (const marker of BANNED_MARKERS) {
    if (source.includes(marker)) {
      violations.push(`${normalizedPath}: legacy analytics marker ${marker}`);
    }
  }

  if (
    normalizedPath.startsWith('features/')
    && /matomo\.php|searchParams\.set\(['"]e_[acn]['"]/.test(source)
  ) {
    violations.push(`${normalizedPath}: constructs a Matomo product request outside the adapter`);
  }

  if (
    normalizedPath.startsWith('core/analytics/matomo-')
    && normalizedPath.endsWith('-product-analytics.service.ts')
    && normalizedPath !== ALLOWED_ADAPTER
  ) {
    violations.push(`${normalizedPath}: duplicates the common product analytics adapter`);
  }
}

if (violations.length > 0) {
  console.error('Product analytics architecture check failed:');
  for (const violation of violations) {
    console.error(`- ${violation}`);
  }

  process.exit(1);
}

console.log(`Product analytics architecture check passed for ${sourceFiles.length} TypeScript files.`);
