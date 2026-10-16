import assert from 'node:assert/strict';
import test from 'node:test';
import { compareTemplateContracts, inlineComponentTemplates, templateContracts } from '../design-v6/template-contract-evidence.mjs';
import { resumeStaticCaptures } from '../design-v6/browser-phase-isolation.mjs';
import { componentPresentations, isProductionPresentationFile, presentationInteractions } from '../design-v6/presentation-inventory-evidence.mjs';
import { compareLocalizationContracts } from '../design-v6/localization-contract-evidence.mjs';

test('localization contracts preserve exact accents, slogans, arrays and existing keys while allowing new labels', () => {
  const before = { footer: { brand: { sloganData: 'La rigueur des données', sloganFun: 'pour toujours plus de fun' } }, weekdays: ['Lun', 'Mar'] };
  const after = { ...before, actions: { expand: 'Développer' } };
  assert.deepEqual(compareLocalizationContracts(before, after), { baselineCount: 3, currentCount: 4, missing: [], changed: [], added: ['actions.expand'] });
  const changed = structuredClone(after);
  changed.footer.brand.sloganData = 'La rigueur des donnees';
  changed.weekdays.reverse();
  assert.deepEqual(compareLocalizationContracts(before, changed).changed, ['footer.brand.sloganData', 'weekdays']);
  delete changed.footer.brand.sloganFun;
  assert.deepEqual(compareLocalizationContracts(before, changed).missing, ['footer.brand.sloganFun']);
  assert.deepEqual(compareLocalizationContracts({ optional: null, count: 0, enabled: false }, { optional: '', count: '0', enabled: true }).changed, ['optional', 'count', 'enabled']);
});

test('production inventory includes non-suffixed primitives but excludes test-only component hosts', () => {
  assert.equal(isProductionPresentationFile('src/app/shared/ui/primitives/dialog.ts'), true);
  assert.equal(isProductionPresentationFile('src/app/ui/page.component.ts'), true);
  for (const name of ['src/app/page.spec.ts', 'src/app/test-helpers/host.ts', 'src/app/fixtures/host.component.ts', 'src/testing/host.ts']) {
    assert.equal(isProductionPresentationFile(name), false);
  }
});

test('inventory discovers inline primitives, external views and inline styles from real decorators', () => {
  const source = '// @Component({template: "ignore"})\n@Component({selector:"app-panel", template:`<button>Toggle</button>`, styles:[`:host{display:block}`]}) export class Panel {}';
  assert.deepEqual(componentPresentations(source), [{ name: 'Panel', selector: 'app-panel', template: '<button>Toggle</button>', templateUrl: null,
    styleUrls: [], inlineStyles: [':host{display:block}'] }]);
  const external = componentPresentations('@Component({templateUrl:"page.html", styleUrl:"page.scss"}) export class Page {}')[0];
  assert.equal(external.templateUrl, 'page.html');
  assert.deepEqual(external.styleUrls, ['page.scss']);
  assert.throws(() => componentPresentations('@Component({styles: computedStyles}) export class Dynamic {}'), /Dynamic component presentation/);
});

test('inventory retains complete bindings, generic events and inherited visibility without wrapper duplicates', () => {
  const source = '@if (canEdit()) { <button *ngIf="enabled" [disabled]="rows.length > 1" (click)="save()" (keydown.enter)="save()">{{ "actions.save" | translate }}</button><app-ui-paginator (onPageChange)="page($event)"/><div [routerLink]="target()">Open</div> } @else { <input disabled name="readonly"/> }';
  const rows = presentationInteractions(source);
  assert.equal(rows.length, 4);
  assert.equal(rows[0].disabled, 'rows.length > 1');
  assert.equal(rows[0].click, 'save()');
  assert.ok(rows[0].label.includes('actions.save'));
  assert.ok(rows[0].visibility.includes('canEdit()'));
  assert.ok(rows[0].visibility.includes('ngIf: enabled'));
  assert.ok(rows[0].events.includes('keydown.enter'));
  assert.equal(rows[1].type, 'app-ui-paginator');
  assert.ok(rows[1].events.includes('onPageChange'));
  assert.equal(rows[2].routerLink, 'target()');
  assert.equal(rows[3].disabled, 'true');
  assert.ok(rows[3].visibility.includes('else'));
  assert.throws(() => presentationInteractions('<div>{{ broken( }}</div>'), /Cannot inventory/);
});

test('static captures unblock only after the hydrated application document has been destroyed', async () => {
  const calls = [];
  let evaluations = 0;
  const cdp = { send: async (method, params) => {
    calls.push({ method, params });
    if (method === 'Runtime.evaluate') return { result: { value: ++evaluations === 2 } };
    return {};
  } };
  assert.deepEqual(await resumeStaticCaptures(cdp, async () => {}), { hydratedDocumentDestroyed: true, networkBlockedUntilBlank: true });
  assert.deepEqual(calls.map(call => call.method), ['Page.navigate', 'Runtime.evaluate', 'Runtime.evaluate', 'Network.setBlockedURLs']);
  assert.equal(calls[0].params.url, 'about:blank');
  assert.deepEqual(calls.at(-1).params.urls, []);
  const blockedCalls = [];
  await assert.rejects(resumeStaticCaptures({ send: async method => {
    blockedCalls.push(method);
    return { result: { value: false } };
  } }, async () => {}), /network remains blocked/);
  assert.ok(!blockedCalls.includes('Network.setBlockedURLs'));
});

test('inline contracts come only from actual component decorators, never comments or helper strings', () => {
  const source = '// @Component({ template: "not a component" })\nconst fixture = { template: "ignore" };\n@Component({ selector: "app-test", template: `<button (click)="save()">Save</button>` })\nexport class TestComponent {}';
  const templates = inlineComponentTemplates(source);
  assert.deepEqual(templates, [{ name: 'TestComponent', source: '<button (click)="save()">Save</button>' }]);
  assert.deepEqual(inlineComponentTemplates('@Component({templateUrl: "page.html"}) export class External {}'), []);
  assert.throws(() => inlineComponentTemplates('@Component({template: buildTemplate()}) export class Dynamic {}'), /Dynamic inline/);
  assert.throws(() => inlineComponentTemplates('@Component(metadata) export class Dynamic {}'), /Dynamic component metadata/);
  assert.ok(compareTemplateContracts(templateContracts(templates[0].source), templateContracts('<button>Save</button>')).missing.length);
});

test('moving controls and photographs does not remove their source contracts', () => {
  const before = `<section><button *ngIf="canEdit()" [disabled]="busy()" (click)="save()" aria-label="Save">{{ label() }}</button><app-image-display [imageId]="image.id" [sizes]="oldSizes" loading="eager"/></section>`;
  const after = `<main><app-image-display [imageId]="image.id" sizes="729px" loading="eager"/><aside><button *ngIf="canEdit()" class="new" [disabled]="busy()" (click)="save()" aria-label="Save">{{ label() }}</button></aside></main>`;
  assert.deepEqual(compareTemplateContracts(templateContracts(before), templateContracts(after)).missing, []);
});

test('structural wrappers do not double-count a DOM event and native replacements preserve the action', () => {
  const before = templateContracts('<section *ngIf="open" (click)="close()"></section>');
  const after = templateContracts('<section *ngIf="open"><button (click)="close()"></button></section>');
  assert.equal(before.filter(value => JSON.parse(value)[0] === 'event').length, 1);
  assert.deepEqual(compareTemplateContracts(before, after).missing, []);
  assert.ok(compareTemplateContracts(before, templateContracts('<section *ngIf="open"></section>')).missing.length);
});

test('lost handlers, conditions, labels, image loading and duplicate controls fail comparison', () => {
  for (const [before, after] of [
    ['<button (click)="save()"></button>', '<button></button>'],
    ['<button *ngIf="canEdit()"></button>', '<button></button>'],
    ['<button aria-label="Save"></button>', '<button></button>'],
    ['<img loading="lazy"/>', '<img/>'],
    ['<input required maxlength="20"/>', '<input/>'],
    ['<a [routerLink]="route()">{{ name() }}</a>', '<a [routerLink]="other()">{{ name() }}</a>'],
    ['<button (click)="save()"></button><button (click)="save()"></button>', '<button (click)="save()"></button>'],
  ]) assert.ok(compareTemplateContracts(templateContracts(before), templateContracts(after)).missing.length);
});

test('defer triggers, delays and visible loop expressions remain protected', () => {
  const before = '@defer (on viewport; prefetch on idle) { <p>{{ value() }}</p> } @placeholder (minimum 200ms) { <span></span> } @for (item of items(); track item.id) { <b>{{ item.name }}</b> }';
  const after = '@defer (on immediate) { <p>{{ value() }}</p> } @placeholder { <span></span> } @for (item of otherItems(); track item.id) { <b>{{ item.name }}</b> }';
  const missing = compareTemplateContracts(templateContracts(before), templateContracts(after)).missing;
  assert.ok(missing.some(value => value[0] === 'defer'));
  assert.ok(missing.some(value => value[0] === 'deferTiming'));
  assert.ok(missing.some(value => value[0] === 'expression'));
  assert.throws(() => templateContracts('<div>{{ broken( }}</div>'), /Cannot parse template/);
});
