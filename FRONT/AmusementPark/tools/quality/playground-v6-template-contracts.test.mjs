import assert from 'node:assert/strict';
import test from 'node:test';
import { compareTemplateContracts, templateContracts } from '../design-v6/template-contract-evidence.mjs';

test('moving controls and photographs does not remove their source contracts', () => {
  const before = `<section><button *ngIf="canEdit()" [disabled]="busy()" (click)="save()" aria-label="Save">{{ label() }}</button><app-image-display [imageId]="image.id" [sizes]="oldSizes" loading="eager"/></section>`;
  const after = `<main><app-image-display [imageId]="image.id" sizes="729px" loading="eager"/><aside><button *ngIf="canEdit()" class="new" [disabled]="busy()" (click)="save()" aria-label="Save">{{ label() }}</button></aside></main>`;
  assert.deepEqual(compareTemplateContracts(templateContracts(before), templateContracts(after)).missing, []);
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
