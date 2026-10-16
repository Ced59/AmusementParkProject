import { readdir, readFile, stat } from 'node:fs/promises';
import { join } from 'node:path';

export function browserModuleEvidence(filename, source) {
  return { filename, bytes: Buffer.byteLength(source),
    staticImports: [...source.matchAll(/(?:from\s*|import\s*)["']\.\/(chunk-[\w-]+\.js)["']/g)].map(match => match[1]),
    behaviors: {
      focusTrap: /cdk-focus-trap-anchor|focusLastTabbableElement|focusInitialElementWhenReady|cdkTrapFocus/.test(source),
      visuallyHiddenStyles: source.includes('cdk-visually-hidden'),
      sharedDialog: /p-dialog-mask|p-dialog-header-close/.test(source),
    } };
}

export async function readBrowserModuleEvidence(root) {
  const files = (await readdir(root)).filter(file => file.endsWith('.js')).sort();
  if (files.length > 1024) throw new Error('Browser module evidence exceeds its file bound.');
  const modules = [];
  let totalBytes = 0;
  for (const file of files) {
    const size = (await stat(join(root, file))).size;
    totalBytes += size;
    if (size > 1024 * 1024 || totalBytes > 12 * 1024 * 1024) throw new Error('Browser module evidence exceeds its byte bound.');
    const source = await readFile(join(root, file), 'utf8');
    const evidence = browserModuleEvidence(file, source);
    modules.push(evidence);
  }
  return modules;
}
