import { execFileSync } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { compareTemplateContracts, inlineComponentTemplates, templateContracts } from './template-contract-evidence.mjs';

const base = process.env.V6_BASELINE_SHA;
if (!/^[a-f0-9]{40}$/.test(base ?? '')) throw new Error('An exact V6 baseline SHA is required.');
const repositoryRoot = resolve(import.meta.dirname, '../../../..');
const gitOptions = ['-c', 'gc.auto=0', '-c', 'pack.threads=1', '-c', 'pack.windowMemory=32m', '-c', 'core.deltaBaseCacheLimit=16m', '--no-pager'];
const git = args => execFileSync('git', [...gitOptions, ...args], { cwd: repositoryRoot, encoding: 'utf8', maxBuffer: 1024 * 1024 });
const deleted = git(['diff', '--name-only', '--diff-filter=D', base, '--', 'FRONT/AmusementPark/src/**/*.html', 'FRONT/AmusementPark/src/**/*.ts']).trim().split('\n').filter(Boolean);
if (deleted.length > 64) throw new Error('Deleted-template comparison exceeds its explicit bound.');
for (const file of deleted) {
  if (file.endsWith('.html') || inlineComponentTemplates(git(['show', `${base}:${file}`]), file).length) {
    throw new Error(`A baseline template was deleted; source-contract coverage cannot silently omit it: ${file}`);
  }
}
const files = git(['diff', '--name-only', '--diff-filter=M', base, '--', 'FRONT/AmusementPark/src/**/*.html', 'FRONT/AmusementPark/src/**/*.ts']).trim().split('\n').filter(file => file && !file.endsWith('.spec.ts'));
if (files.length > 64) throw new Error('Template contract comparison exceeds its explicit bound.');
const pairs = [];
for (const file of files) {
  const before = git(['show', `${base}:${file}`]);
  const after = readFileSync(resolve(repositoryRoot, file), 'utf8');
  if (file.endsWith('.html')) pairs.push({ file, before, after });
  else {
    const original = inlineComponentTemplates(before, file);
    const current = inlineComponentTemplates(after, file);
    if (original.length !== current.length || original.some((template, index) => template.name !== current[index].name)) throw new Error(`An inline component template disappeared or changed identity: ${file}`);
    original.forEach((template, index) => pairs.push({ file: `${file}#${template.name}`, before: template.source, after: current[index].source }));
  }
}
const results = [];
for (const { file, before, after } of pairs) {
  const comparison = compareTemplateContracts(templateContracts(before, file), templateContracts(after, file));
  // Exact, reviewed presentation-only exception: the old dialog container no
  // longer needs to stop bubbling into a clickable parent. Its backdrop is now
  // a named native button, while both real close handlers remain protected.
  if (file === 'FRONT/AmusementPark/src/app/ui/media/photo-carousel/ui-photo-carousel.component.html'
    && /<button\s+class="photo-lightbox__backdrop"\s+type="button"\s+tabindex="-1"\s+\[attr\.aria-label\]="closeFullscreenLabelKey \| translate"\s+\(click\)="closeLightbox\(\)"/.test(after)
    && after.includes('cdkTrapFocus [cdkTrapFocusAutoCapture]="true"')
    && comparison.missing.length === 1
    && JSON.stringify(comparison.missing[0]) === JSON.stringify(['event', 'click', 0, '$event.stopPropagation()'])) {
    comparison.presentationChanges = [{ contract: comparison.missing[0], reason: 'Bubbling guard obsolete with the named native backdrop button; close handlers preserved.' }];
    comparison.missing = [];
  }
  if (file === 'FRONT/AmusementPark/src/app/shared/ui/primitives/dialog.ts#Dialog'
    && after.includes("[attr.aria-modal]=\"modal ? 'true' : null\"")
    && after.includes('[cdkTrapFocus]="modal" [cdkTrapFocusAutoCapture]="modal"')
    && comparison.missing.length === 1
    && JSON.stringify(comparison.missing[0]) === JSON.stringify(['attribute', 'section', 'aria-modal', 'true'])) {
    comparison.presentationChanges = [{ contract: comparison.missing[0], reason: 'Modal semantics and focus trap follow the existing modal input; non-modal dialogs must not claim modality.' }];
    comparison.missing = [];
  }
  results.push({ file, ...comparison });
}
const report = { scope: 'existing template source contracts; not complete browser/role acceptance', base, results };
if (process.env.V6_SSR_EVIDENCE_DIR) {
  const directory = resolve(process.env.V6_SSR_EVIDENCE_DIR);
  mkdirSync(directory, { recursive: true });
  writeFileSync(resolve(directory, 'template-contract-comparison.json'), JSON.stringify(report, null, 2));
}
console.log(JSON.stringify({ ...report, results: results.map(result => ({ ...result,
  missingCount: result.missing.length, addedCount: result.added.length,
  missing: result.missing.slice(0, 10), added: result.added.slice(0, 10) })) }, null, 2));
if (results.some(result => result.missing.length)) throw new Error('An existing template contract disappeared; correct or document its exact presentation-only exception.');
