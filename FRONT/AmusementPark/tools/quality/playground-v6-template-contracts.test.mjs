import assert from 'node:assert/strict';
import test from 'node:test';
import { compareTemplateContracts, inlineComponentTemplates, templateContracts } from '../design-v6/template-contract-evidence.mjs';

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
