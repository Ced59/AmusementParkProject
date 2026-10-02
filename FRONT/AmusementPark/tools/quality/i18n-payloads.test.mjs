import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { gzipSync } from 'node:zlib';
import test from 'node:test';
import { execFileSync } from 'node:child_process';
import { splitTranslationPayloads } from '../i18n-payloads.mjs';

const root = resolve(import.meta.dirname, '../..');
execFileSync(process.execPath, ['tools/i18n-build.mjs'], { cwd: root });
const languages = ['en', 'fr', 'es', 'de', 'it', 'nl', 'pl', 'pt'];
function walk(directory) {
  return readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const file = resolve(directory, entry.name);
    return entry.isDirectory() ? walk(file) : [file];
  });
}
const sharedAdminPaths = new Set(walk(resolve(root, 'src/app'))
  .filter(file => /\.(html|ts)$/.test(file)
    && !/[\\/]admin[\\/]|[\\/]admin-app-layout[\\/]|[\\/]testing[\\/]|[\\/]test-helpers[\\/]|\.spec\.ts$|admin-navigation\.models\.ts$/.test(file))
  .flatMap(file => [...readFileSync(file, 'utf8').matchAll(/['"`](admin\.[\w.]+)(?=['"`$])/g)]
    .map(match => match[1].replace(/\.$/, ''))));

for (const language of languages) {
  test(`${language}: public and admin payloads preserve translations and shared public labels`, () => {
    const translations = JSON.parse(readFileSync(resolve(root, `src/assets/i18n/${language}.json`), 'utf8'));
    const payloads = splitTranslationPayloads(translations);
    assert.deepEqual({ ...payloads.public, ...payloads.admin }, translations);
    for (const key of sharedAdminPaths) {
      const segments = key.split('.');
      const value = segments.reduce((node, segment) => node?.[segment], payloads.public);
      assert.notEqual(value, undefined, `${key} must remain available on public pages`);
      assert.deepEqual(value, segments.reduce((node, segment) => node?.[segment], translations));
    }
    assert.ok(gzipSync(JSON.stringify(payloads.public)).length < gzipSync(JSON.stringify(translations)).length * 0.8);
    for (const scope of ['public', 'admin']) {
      assert.deepEqual(JSON.parse(readFileSync(resolve(root, `src/assets/i18n/${scope}/${language}.json`), 'utf8')), payloads[scope]);
    }
  });
}
