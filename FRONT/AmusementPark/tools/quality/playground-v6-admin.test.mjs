import assert from 'node:assert/strict';
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import test from 'node:test';

const projectRoot = resolve(import.meta.dirname, '../..');
const repositoryRoot = resolve(projectRoot, '../..');
const read = (relativePath) => readFileSync(resolve(projectRoot, relativePath), 'utf8');

const collect = (directory, extension) => readdirSync(directory, { withFileTypes: true })
  .flatMap((entry) => {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) {
      return collect(path, extension);
    }
    return entry.name.endsWith(extension) ? [readFileSync(path, 'utf8')] : [];
  });

const parseInventory = () => {
  const csv = readFileSync(resolve(repositoryRoot, 'docs/design-v6/generated/V6-ROUTE-INVENTORY.csv'), 'utf8');
  const lines = csv.trim().split(/\r?\n/);
  const headers = lines[0].slice(1, -1).split('";"');
  return lines.slice(1).map((line) => {
    const values = line.slice(1, -1).split('";"');
    return Object.fromEntries(headers.map((header, index) => [header, values[index] ?? '']));
  });
};

const inventory = parseInventory();
const adminRoutes = inventory.filter((entry) => entry.family === 'admin');
const adminDirectory = resolve(projectRoot, 'src/app/features/admin');
const adminHtml = collect(adminDirectory, '.html').join('\n');
const adminStyles = [read('src/styles/_admin-skin.scss'), ...collect(adminDirectory, '.scss')].join('\n');
const graphUpsert = read('src/app/features/admin/park-graph-upserts/pages/admin-park-graph-upserts/admin-park-graph-upserts.component.html');
const bulkGraphUpsert = read('src/app/features/admin/park-graph-upserts/pages/admin-bulk-park-graph-upserts/admin-bulk-park-graph-upserts.component.html');
const contextualDrawer = read('src/app/features/admin/contextual-editing/ui/admin-contextual-block-drawer/admin-contextual-block-drawer.component.html');
const historyWorkbench = read('src/app/features/admin/history/pages/admin-history-workbench/admin-history-workbench.component.html');
const liveOperations = read('src/app/features/admin/live-data/pages/admin-live-operations/admin-live-operations.component.html');
const liveMappings = read('src/app/features/admin/live-data/pages/admin-live-target-mappings/admin-live-target-mappings.component.html');
const photoBatch = read('src/app/features/admin/site/pages/admin-site/admin-photo-batch/admin-photo-batch.component.html');
const parkEditor = read('src/app/features/admin/parks/pages/admin-parks/admin-park-edit/admin-park-edit.component.html');

test('historical admin controls use the existing themed cyan instead of the old fixed accent', () => {
  const styles = read('src/app/features/admin/history/pages/admin-history/admin-history.component.scss');
  assert.match(styles, /accent-color:\s*var\(--c-sky\)/);
  assert.doesNotMatch(styles, /accent-color:\s*#38c5f5/);
});

test('the exhaustive inventory keeps all 58 admin terminals under every inherited guard', () => {
  assert.equal(adminRoutes.length, 58);
  for (const entry of adminRoutes) {
    for (const guard of ['languageGuard', 'authGuard', 'adminGuard', 'completeTranslationsGuard']) {
      assert.ok(entry.guards.includes(guard), `${entry.route} must inherit ${guard}`);
    }
    if (entry.kind !== 'redirect') {
      assert.ok(entry.component, `${entry.route} must keep a component`);
      assert.ok(entry.template, `${entry.route} must keep a template`);
      assert.ok(existsSync(resolve(repositoryRoot, entry.componentFile)), `${entry.componentFile} must exist`);
      assert.ok(existsSync(resolve(repositoryRoot, entry.template)), `${entry.template} must exist`);
    }
  }
});

test('all entity catalogues, editors and operational consoles remain routed', () => {
  const routes = new Set(adminRoutes.map((entry) => entry.route));
  for (const route of [
    '/:lang/admin/users/:id',
    '/:lang/admin/parks/edit/:idPark',
    '/:lang/admin/parks/edit/:idPark/zones/:idZone',
    '/:lang/admin/parks/edit/:idPark/items/:idItem',
    '/:lang/admin/standalone-attractions',
    '/:lang/admin/operators/edit/:id',
    '/:lang/admin/founders/edit/:id',
    '/:lang/admin/manufacturers/edit/:id',
    '/:lang/admin/technical-pages/edit/:id',
    '/:lang/admin/images/batch',
    '/:lang/admin/videos',
  ]) {
    assert.ok(routes.has(route), `${route} must remain routed`);
  }
});

test('dangerous graph and contextual mutations preserve preview-before-apply contracts', () => {
  for (const template of [graphUpsert, bulkGraphUpsert]) {
    assert.ok(template.includes('(click)="preview()"'));
    assert.ok(template.includes('[disabled]="!canPreview"'));
    assert.ok(template.includes('(click)="apply()"'));
    assert.ok(template.includes('[disabled]="!canApply"'));
    assert.ok(template.includes('accept=".json,application/json"'));
  }
  for (const contract of [
    '(click)="previewJson(block)"',
    '(click)="applyJson(block)"',
    '[disabled]="isPreviewing() || isApplying() || !canApplyCurrentPreview(block)"',
    '(click)="uploadPhoto(block)"',
    '(change)="importParkGraphUpsertFile(block, $event)"',
  ]) {
    assert.ok(contextualDrawer.includes(contract), `${contract} must remain in contextual editing`);
  }
});

test('history, live operations, mappings and imports keep confirmation and review actions', () => {
  for (const contract of [
    '(click)="openFactEditor()"',
    '(click)="openRelationEditor()"',
    '(click)="openSourceEditor()"',
    "preview('Fact'",
    "advance('Fact'",
    "retract('Fact'",
  ]) {
    assert.ok(historyWorkbench.includes(contract), `${contract} must remain in history workbench`);
  }
  assert.ok(liveOperations.includes('(click)="facade.replayQuarantine()"'));
  assert.ok(liveOperations.includes('(ngSubmit)="confirm()"'));
  for (const decision of ['Correct', 'Suspend', 'Reject', 'Supersede']) {
    assert.ok(liveMappings.includes(`review(mapping, '${decision}')`));
  }
  assert.ok(adminHtml.includes('(click)="captainCoasterPipelineFacade.startImportAsync()"'));
});

test('long editors, uploads and batch actions preserve their dense workflows', () => {
  assert.ok(parkEditor.includes('[formGroup]="form"'));
  assert.ok(parkEditor.includes('(ngSubmit)="onSubmit()"'));
  assert.equal(parkEditor.match(/<app-ui-tab-panel\s/g)?.length, 8);
  assert.ok(parkEditor.includes('(click)="saveOpeningHoursJson()"'));
  for (const contract of [
    'multiple',
    '(click)="uploadSelectedFiles()"',
    '(click)="applyBulkCategorization()"',
    '(click)="loadMoreParkPhotos()"',
    '(click)="loadMoreParkItemPhotos()"',
  ]) {
    assert.ok(photoBatch.includes(contract), `${contract} must remain in batch media`);
  }
  for (const contract of ['missingPhotos', 'missingGeneralLocation', 'missingPreciseLocation', '(keydown.enter)="selectRow(row)"']) {
    assert.ok(adminHtml.includes(contract), `${contract} must remain in field mode`);
  }
});

test('standalone row selection has a named native keyboard alternative while retaining table and checkbox semantics', () => {
  const html = read('src/app/features/admin/standalone-attractions/pages/admin-standalone-attractions/admin-standalone-attractions.component.html');
  assert.match(html, /<tr[^>]+\(click\)="select\(row\)"/);
  assert.match(html, /<button type="button" class="admin-standalone-row__select"[^>]+\[attr.aria-pressed\]="selected\(\)\?\.id === row.id"[^>]+\(click\)="select\(row\); \$event.stopPropagation\(\)"/);
  assert.match(html, /type="checkbox" \[attr.aria-label\]="row.name"/);
  assert.doesNotMatch(html, /<tr[^>]+role="button"/);
});

test('every specialist administration domain remains represented', () => {
  const routes = adminRoutes.map((entry) => entry.route).join('\n');
  for (const segment of [
    'audit-logs',
    'seo-sitemaps',
    'park-weather',
    'contact-grievances',
    'social-share',
    'product-quality',
    'passport-beta',
    'park-fit-data-quality',
    'park-fit-pilot',
    'social-publications',
    'rating-rankings',
    'share-moderation',
    'factual-events',
    'watch-pilot',
    'trip-pilot',
    'technical-stats',
  ]) {
    assert.ok(routes.includes(segment), `${segment} must remain in the admin route tree`);
  }
});

test('admin surfaces use the dense flat Playground system at desktop and mobile widths', () => {
  assert.match(adminStyles, /--admin-panel-bg:\s*var\(--app-surface\)/);
  assert.match(adminStyles, /\.admin-data-table[\s\S]*?data-label/);
  assert.match(adminStyles, /@media \(max-width:\s*760px\)/);
  assert.match(adminStyles, /grid-template-columns:\s*minmax\(7rem, 36%\) minmax\(0, 1fr\)/);
  assert.doesNotMatch(adminStyles, /(?:linear|radial|conic|repeating-linear)-gradient\(/);
  assert.doesNotMatch(adminStyles, /backdrop-filter\s*:/);
});

test('admin image, location, zone and access-order icon buttons retain translated accessible names', () => {
  const base = 'src/app/features/admin/parks/pages/admin-parks/';
  for (const path of ['admin-park-edit/tabs/admin-park-logos-tab/admin-park-logos-tab.component.html',
    'admin-park-edit/tabs/admin-park-photos-tab/admin-park-photos-tab.component.html',
    'admin-park-item-edit/tabs/admin-park-item-photos-tab/admin-park-item-photos-tab.component.html']) {
    const source = read(base + path);
    assert.ok(source.includes('[attr.aria-label]="\'admin.images.batch.setAsCurrent\' | translate"'));
    assert.ok(source.includes('[attr.aria-label]="\'actions.delete\' | translate"'));
    assert.ok(source.includes('setCurrent'));
    assert.ok(source.includes('deletePhoto') || source.includes('deleteLogo'));
  }
  const access = read(base + 'admin-park-item-edit/tabs/admin-park-item-access-conditions-tab/admin-park-item-access-conditions-tab.component.html');
  for (const key of ['moveUp', 'moveDown', 'delete']) assert.ok(access.includes(`[attr.aria-label]="'actions.${key}' | translate"`));
  const locations = read(base + 'admin-park-item-edit/tabs/admin-park-item-locations-tab/admin-park-item-locations-tab.component.html');
  assert.ok(locations.includes("('actions.edit' | translate) + ' — ' + (locationOption.labelKey | translate)"));
  assert.ok(locations.includes("('actions.clear' | translate) + ' — ' + (locationOption.labelKey | translate)"));
  const zones = read(base + 'admin-park-zones/admin-park-zones.component.html');
  for (const key of ['edit', 'delete']) assert.ok(zones.includes(`[attr.aria-label]="'actions.${key}' | translate"`));
  for (const language of ['fr', 'en', 'de', 'nl', 'it', 'es', 'pt', 'pl']) {
    const translations = JSON.parse(read(`src/assets/i18n/source/${language}/common/actions.json`));
    assert.ok(translations.actions.moveUp.trim() && translations.actions.moveDown.trim());
  }
});
