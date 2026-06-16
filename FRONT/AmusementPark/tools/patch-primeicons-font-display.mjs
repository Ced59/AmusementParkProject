import { readdir, readFile, writeFile } from 'node:fs/promises';
import { join, resolve } from 'node:path';

const browserOutputDirectory = resolve('dist', 'amusement-park', 'browser');
const primeIconsFontDisplayPattern = /(@font-face\{font-family:primeicons;)font-display:block;/g;
const primeIconsFontDisplayReplacement = '$1font-display:swap;';

let patchedFiles = 0;

try {
  const entries = await readdir(browserOutputDirectory, { withFileTypes: true });

  for (const entry of entries) {
    if (!entry.isFile() || !entry.name.startsWith('styles-') || !entry.name.endsWith('.css')) {
      continue;
    }

    const filePath = join(browserOutputDirectory, entry.name);
    const content = await readFile(filePath, 'utf8');
    const patchedContent = content.replace(primeIconsFontDisplayPattern, primeIconsFontDisplayReplacement);

    if (patchedContent !== content) {
      await writeFile(filePath, patchedContent, 'utf8');
      patchedFiles += 1;
    }
  }
} catch (error) {
  throw new Error(`Unable to patch PrimeIcons font-display in ${browserOutputDirectory}: ${error}`);
}

if (patchedFiles === 0) {
  throw new Error('PrimeIcons font-display block rule was not found in the production CSS output.');
}

console.log(`Patched PrimeIcons font-display in ${patchedFiles} CSS file(s).`);
