import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import test from 'node:test';

import { analyseTemplate, compareBaseline, fingerprintFindings, scanApplication } from './accessibility-check.mjs';

test('accepts named native controls and alternative image text', () => {
  const findings = analyseTemplate(`
    <button type="button">Save</button>
    <a routerLink="/parks" aria-label="Parks"><span aria-hidden="true">★</span></a>
    <button type="button" aria-label="{{ 'Close' }}"><app-icon name="close" /></button>
    <button type="button" [attr.aria-label]="ready ? 'Save' : 'Retry'"><app-icon name="save" /></button>
    <button type="button" [attr.aria-label]="name || 'Save'"><app-icon name="save" /></button>
    <button type="button" [attr.aria-label]="true ? 'Save' : ''"><app-icon name="save" /></button>
    <button type="button"><span [attr.aria-hidden]="visible ? 'false' : null">Save</span></button>
    <button type="button"><span [hidden]="visible ? false : 0">Save</span></button>
    <button type="button"><span [attr.hidden]="visible ? null : undefined">Save</span></button>
    <button appUiButton type="button" [label]="translatedLabel"></button>
    <input type="button" value="Save">
    <input type="submit">
    <input type="reset">
    <input type="image" src="save.webp" alt="Save">
    <img src="park.webp" alt="Roller coaster">
    <img src="separator.webp" alt="">
    <button i18n>{count, plural, =0 {None} other {Many}}</button>
  `);

  assert.deepEqual(findings, []);
});

test('recognizes native labels associated with interactive inputs', () => {
  const findings = analyseTemplate(`
    <label for="save">Save</label>
    <input id="save" type="button">
    <label for="bound-save">Save</label>
    <input [id]="'bound-save'" type="button">
    <label>
      Wrapped action
      <input type="button">
    </label>
    @if (ready) {
      <label for="conditional-save">Save</label>
      <input id="conditional-save" type="button">
    } @else {
      <label for="conditional-save"></label>
    }
  `);

  assert.deepEqual(findings, []);
});

test('rejects empty or unreachable native labels', () => {
  const findings = analyseTemplate(`
    <label for="empty-save"></label>
    <input id="empty-save" type="button">
    @if (ready) { <label for="other-branch">Save</label> }
    @else { <input id="other-branch" type="button"> }
    @if (ready) { <label for="partially-named">Save</label> }
    @else { <label for="partially-named"></label> }
    <input id="partially-named" type="button">
  `);

  assert.deepEqual(
    findings.map((finding) => finding.rule),
    ['interactive-name', 'interactive-name', 'interactive-name']
  );
});

test('combines concurrent native labels while validating conditional alternatives', () => {
  const findings = analyseTemplate(`
    <label for="save">Save</label>
    <label for="save"></label>
    <input id="save" type="button">
  `);

  assert.deepEqual(findings, []);
});

test('recognizes names rendered through Angular control-flow branches', () => {
  const findings = analyseTemplate(`
    <button type="button">
      @if (saving()) {
        <span>{{ 'actions.saving' | translate }}</span>
      } @else {
        <span>{{ 'actions.save' | translate }}</span>
      }
    </button>
    <button type="button">
      @switch (state()) {
        @case ('saving') { Saving }
        @default { Save }
      }
    </button>
    <button type="button">
      @for (action of actions(); track action.id) {
        {{ action.name }}
      } @empty {
        No action
      }
    </button>
    <button type="button">
      @defer { Save }
      @placeholder { Preparing }
      @loading { Loading }
      @error { Unavailable }
    </button>
  `);

  assert.deepEqual(findings, []);
});

test('resolves static aria-labelledby references to accessible content', () => {
  const findings = analyseTemplate(`
    <span id="save-label">Save</span>
    <button type="button" aria-labelledby="save-label"><img alt=""></button>
    <span id="hidden-label" hidden>Hidden but referenced label</span>
    <button type="button" aria-labelledby="hidden-label"><img alt=""></button>
    <span id="authored-label" aria-label="Authored label"></span>
    <button type="button" aria-labelledby="authored-label"><img alt=""></button>
    <button type="button"><span role="img" aria-label="Save"></span></button>
    <span id="empty-label"></span>
    <button type="button" aria-labelledby="empty-label"><img alt=""></button>
    <button type="button" aria-labelledby="missing-label"><img alt=""></button>
    @if (ready) { <span id="conditional-label"></span> }
    @else { <span id="conditional-label">Save</span> }
    <button type="button" aria-labelledby="conditional-label"><img alt=""></button>
    <span [id]="'bound-label'">Save</span>
    <button type="button" aria-labelledby="bound-label"><img alt=""></button>
    @if (ready) {
      <span id="correlated-label">Save</span>
      <button type="button" aria-labelledby="correlated-label"><img alt=""></button>
    } @else {
      <span id="correlated-label"></span>
    }
  `);

  assert.deepEqual(
    findings.map((finding) => finding.rule),
    ['interactive-name', 'interactive-name', 'interactive-name']
  );
});

test('requires id references to cover every state where the source is rendered', () => {
  const findings = analyseTemplate(`
    @if (ready) { <span id="partial-label">Save</span> }
    <button type="button" aria-labelledby="partial-label"><img alt=""></button>
    @if (ready) { <span id="complete-label">Save</span> }
    @else { <span id="complete-label">Retry</span> }
    <button type="button" aria-labelledby="complete-label"><img alt=""></button>
  `);

  assert.deepEqual(findings.map((finding) => finding.rule), ['interactive-name']);
});

test('does not resolve id references or labels from dormant template declarations', () => {
  const findings = analyseTemplate(`
    <ng-template><span id="dormant-name">Save</span></ng-template>
    <button type="button" aria-labelledby="dormant-name"><img alt=""></button>
    <ng-template><label for="dormant-input">Save</label></ng-template>
    <input id="dormant-input" type="button">
  `);

  assert.deepEqual(findings.map((finding) => finding.rule), ['interactive-name', 'interactive-name']);
});

test('recognizes names rendered through legacy ngIf branches', () => {
  const findings = analyseTemplate(`
    <button type="button">
      <span *ngIf="ready; else busy">Save</span>
      <ng-template #busy>Busy</ng-template>
    </button>
    <button type="button"><span *ngIf="true">Save</span></button>
    <button type="button"><span *ngIf="ready">Save</span></button>
  `);

  assert.deepEqual(findings.map((finding) => finding.rule), ['interactive-name']);
});

test('recognizes names rendered through static template outlets', () => {
  const findings = analyseTemplate(`
    <button type="button">
      <ng-container *ngTemplateOutlet="label"></ng-container>
    </button>
    <button type="button">
      <ng-container [ngTemplateOutlet]="saving ? savingLabel : saveLabel"></ng-container>
    </button>
    <ng-template #label>Save</ng-template>
    <ng-template #savingLabel>Saving</ng-template>
    <ng-template #saveLabel>Save</ng-template>
  `);

  assert.deepEqual(findings, []);
});

test('scopes duplicate template references to compatible control-flow branches', () => {
  const findings = analyseTemplate(`
    @if (ready) {
      <ng-template #label></ng-template>
      <button type="button">
        <ng-container [ngTemplateOutlet]="label" />
      </button>
    } @else {
      <ng-template #label>Save</ng-template>
    }
  `);

  assert.deepEqual(findings.map((finding) => finding.rule), ['interactive-name']);
});

test('recognizes text rendered through innerText and textContent bindings', () => {
  const findings = analyseTemplate(`
    <button type="button"><span [innerText]="'Save'"></span></button>
    <button type="button"><span [textContent]="label"></span></button>
    <button type="button"><span [textContent]="ready ? 'Save' : ''"></span></button>
  `);

  assert.deepEqual(findings.map((finding) => finding.rule), ['interactive-name']);
});

test('preserves important inline CSS declarations when detecting hidden labels', () => {
  const findings = analyseTemplate(`
    <button type="button">
      <span style="display:none !important; display:block">Hidden label</span>
    </button>
    <button type="button"><span [style.display]="displayMode">Potentially hidden label</span></button>
    <button type="button"><span [style.visibility]="visibilityMode">Potentially hidden label</span></button>
  `);

  assert.deepEqual(
    findings.map((finding) => finding.rule),
    ['interactive-name', 'interactive-name', 'interactive-name']
  );
});

test('evaluates constant unary, comparison and arithmetic expressions', () => {
  const findings = analyseTemplate(`
    <button type="button" [attr.aria-label]="!false ? 'Save' : ''"><img alt=""></button>
    <button type="button">@if (1 === 1) { Save }</button>
    <button type="button">@if (1 + 1 >= 2) { Save }</button>
    <button type="button">@if (1 > 2) { Save }</button>
    <button type="button" [attr.aria-label]="('')"></button>
  `);

  assert.deepEqual(findings.map((finding) => finding.rule), ['interactive-name', 'interactive-name']);
});

test('normalizes bound aria-hidden token values', () => {
  const findings = analyseTemplate(`
    <button type="button"><span [attr.aria-hidden]="' TRUE '">Hidden label</span></button>
  `);

  assert.deepEqual(findings.map((finding) => finding.rule), ['interactive-name']);
});

test('evaluates only reachable constant control-flow branches', () => {
  const findings = analyseTemplate(`
    <button type="button">@if (true) { Save }</button>
    <button type="button">@if (false) { } @else { Save }</button>
    <button type="button">@if (false) { Save }</button>
    <button type="button">@switch ('save') { @case ('save') { Save } }</button>
    <button type="button">@switch ('retry') { @case ('save') { Save } @default { Retry } }</button>
    <button type="button">@switch ('retry') { @case ('save') { Save } }</button>
    @if (false) { <button type="button"><img alt=""></button> }
    @if ((false)) { <img src="parenthesized-unreachable.webp"> }
    @switch ('save') { @case ('retry') { <img src="unreachable.webp"> } }
    @if (false) { <span id="unreachable-label">Save</span> }
    <button type="button" aria-labelledby="unreachable-label"><img alt=""></button>
  `);

  assert.deepEqual(
    findings.map((finding) => finding.rule),
    ['interactive-name', 'interactive-name', 'interactive-name']
  );
});

test('correlates id references across for and deferred rendering states', () => {
  const findings = analyseTemplate(`
    @for (item of items; track item.id) {
      <span id="loop-label">Save</span>
      <button type="button" aria-labelledby="loop-label"><img alt=""></button>
    } @empty {
      <span id="loop-label"></span>
    }
    @defer {
      <span id="defer-label">Save</span>
      <button type="button" aria-labelledby="defer-label"><img alt=""></button>
    } @placeholder {
      <span id="defer-label"></span>
    } @loading {
      <span id="defer-label"></span>
    } @error {
      <span id="defer-label"></span>
    }
  `);

  assert.deepEqual(findings, []);
});

test('traverses switch cases and for-loop empty blocks', () => {
  const findings = analyseTemplate(`
    @switch (kind()) {
      @case ('image') {
        <img src="park.webp">
      }
    }
    @for (item of items(); track item.id) {
      <span>{{ item.name }}</span>
    } @empty {
      <button type="button"><app-icon name="close" /></button>
    }
  `);

  assert.deepEqual(findings.map((finding) => finding.rule), ['image-alt', 'interactive-name']);
});

test('detects unnamed controls and images without alt', () => {
  const findings = analyseTemplate(`
    <button type="button"><app-icon name="save" /></button>
    <button type="button" aria-label=""><app-icon name="close" /></button>
    <button type="button" [attr.aria-label]="''"><app-icon name="close" /></button>
    <button type="button"><img src="decorative.webp" alt=""></button>
    <button type="button"><img src="decorative.webp" [alt]="''"></button>
    <button type="button"><span aria-hidden="true">Hidden label</span></button>
    <button type="button"><span hidden>Hidden label</span></button>
    <button type="button">{{ '' }}</button>
    <button type="button">{{ null }}</button>
    <button type="button" label="Not an accessible name"><img alt=""></button>
    <button type="button"><span [attr.hidden]="false">Hidden label</span></button>
    <button type="button"><span style="display: none">Hidden label</span></button>
    <button type="button"><span style="visibility: hidden">Hidden label</span></button>
    <button type="button"><span style="display: none !important">Hidden label</span></button>
    <button type="button"><span [style]="'visibility: hidden !important'">Hidden label</span></button>
    <button type="button"><span [style.display]="'none !important'">Hidden label</span></button>
    <button type="button"><span [style]="hidden ? 'display:none' : ''">Hidden label</span></button>
    <button type="button"><span [style.display]="hidden ? 'none' : ''">Hidden label</span></button>
    <button type="button" [attr.aria-label]="name && 'Save'"><img alt=""></button>
    <button type="button"><span [hidden]="'false'">Hidden label</span></button>
    <button type="button"><ng-template>Hidden label</ng-template></button>
    <button type="button">@if (saving()) { Saving }</button>
    <button type="button">@switch (state()) { @case ('saving') { Saving } }</button>
    <button type="button">@for (action of actions(); track action.id) { {{ action.name }} }</button>
    <button type="button">@defer { Save } @placeholder { }</button>
    <button type="button" aria-label="{{ '' }}"><img alt=""></button>
    <button type="button" [attr.aria-label]="ready ? 'Save' : null"><img alt=""></button>
    <button type="button" attr.aria-label="{{ ready ? 'Save' : '' }}"><img alt=""></button>
    <button type="button">{{ ready ? 'Save' : '' }}</button>
    <button type="button"><span [attr.aria-hidden]="hidden ? 'true' : null">Hidden label</span></button>
    <button type="button"><span [ariaHidden]="true">Hidden label</span></button>
    <button type="button"><span inert>Hidden label</span></button>
    <button type="button"><span [attr.inert]="false">Hidden label</span></button>
    <input type="button" value="">
    <input type="image" src="save.webp">
    <input [type]="action ? 'button' : 'hidden'">
    <a routerLink="/parks"><app-icon name="map" /></a>
    <img src="park.webp">
    <img src="park.webp" [class.alt]="enabled">
    <img src="park.webp" [attr.alt]="null">
    <img src="park.webp" [attr.alt]="undefined">
    <img src="park.webp" [attr.alt]="decorative ? '' : null">
    <IMG src="park.webp">
    <BUTTON type="button"><img alt=""></BUTTON>
    <button i18n>{count, plural, =0 {} other {Many}}</button>
    <button type="button" [attr.aria-label]="name || ''"><img alt=""></button>
    <img src="park.webp" [attr.alt]="caption ?? null">
  `);

  assert.deepEqual(
    findings.map((finding) => finding.rule),
    [
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'interactive-name',
      'image-alt',
      'image-alt',
      'image-alt',
      'image-alt',
      'image-alt',
      'image-alt',
      'interactive-name',
      'interactive-name',
      'image-alt'
    ]
  );
});

test('validates conservatively inputs with unknown runtime types', () => {
  const findings = analyseTemplate('<input [type]="controlType">');

  assert.deepEqual(findings.map((finding) => finding.rule), ['interactive-name']);
});

test('traverses interactive elements rendered inside ICU cases', () => {
  const findings = analyseTemplate(`
    <div i18n>
      {count, plural,
        =0 {<img src="empty.webp">}
        other {<button type="button"><img alt=""></button>}
      }
    </div>
  `);

  assert.deepEqual(
    findings.map((finding) => finding.rule),
    ['image-alt', 'interactive-name']
  );
});

test('analyses statically composed inline templates and fails closed on dynamic expressions', (context) => {
  const frontendRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'accessibility-inline-'));
  const applicationRoot = path.join(frontendRoot, 'src', 'app');
  fs.mkdirSync(applicationRoot, { recursive: true });
  context.after(() => fs.rmSync(frontendRoot, { recursive: true, force: true }));

  fs.writeFileSync(path.join(applicationRoot, 'static.component.ts'), `
    import { Component as NgComponent } from '@angular/core';
    const CONTROL = '<button type="button">';
    const TEMPLATE = CONTROL + '</button>';
    @NgComponent({ template: TEMPLATE })
    export class StaticComponent {}
  `);
  fs.writeFileSync(path.join(applicationRoot, 'dynamic.component.ts'), `
    import { Component } from '@angular/core';
    @Component({ template: buildTemplate() })
    export class DynamicComponent {}
  `);
  fs.writeFileSync(path.join(applicationRoot, 'namespace.component.ts'), `
    import * as ngCore from '@angular/core';
    @ngCore.Component({ 'template': '<img src="park.webp">' })
    export class NamespaceComponent {}
  `);
  fs.writeFileSync(path.join(applicationRoot, 'computed.component.ts'), `
    import { Component } from '@angular/core';
    const TEMPLATE_KEY = 'template';
    @Component({ [TEMPLATE_KEY]: '<img src="park.webp">' })
    export class ComputedComponent {}
  `);
  fs.writeFileSync(path.join(applicationRoot, 'shorthand.component.ts'), `
    import { Component } from '@angular/core';
    const template = '<img src="park.webp">';
    @Component({ template })
    export class ShorthandComponent {}
  `);
  fs.writeFileSync(path.join(applicationRoot, 'dynamic-metadata.component.ts'), `
    import { Component } from '@angular/core';
    @Component(buildMetadata())
    export class DynamicMetadataComponent {}
  `);
  const testHelperRoot = path.join(applicationRoot, 'test-helpers');
  fs.mkdirSync(testHelperRoot, { recursive: true });
  fs.writeFileSync(path.join(testHelperRoot, 'minimal.component.html'), '<button type="button"></button>');
  fs.writeFileSync(path.join(applicationRoot, 'minimal.component.spec.html'), '<img src="fixture.webp">');

  const findings = scanApplication(applicationRoot, frontendRoot);

  assert.deepEqual(
    findings.map((finding) => finding.rule).sort(),
    ['image-alt', 'image-alt', 'image-alt', 'inline-template-analysis', 'inline-template-analysis', 'interactive-name']
  );
});

test('requires keyboard support and semantics on non-native click targets', () => {
  const invalid = analyseTemplate('<div (click)="open()">Open</div>');
  const invalidRouterLink = analyseTemplate('<div routerLink="/parks">Parks</div>');
  const invalidRouterLinkEventControl = analyseTemplate('<div role="link" tabindex="0" routerLink="/parks" (keydown.enter)="$event.preventDefault()">Parks</div>');
  const invalidRoleOnly = analyseTemplate('<div role="button" (click)="open()" (keydown.enter)="open()">Open</div>');
  const invalidFocusOnly = analyseTemplate('<div tabindex="0" (click)="open()" (keydown.enter)="open()">Open</div>');
  const invalidEscapeOnly = analyseTemplate('<div role="button" tabindex="0" (click)="open()" (keydown.escape)="close()">Open</div>');
  const invalidBareHandler = analyseTemplate('<div role="button" tabindex="0" (click)="open()" (keydown)="close()">Open</div>');
  const invalidSpecificHandler = analyseTemplate('<div role="button" tabindex="0" (click)="open()" (keydown.enter)="close()">Open</div>');
  const invalidSubstringHandler = analyseTemplate('<div role="button" tabindex="0" (click)="save()" (keydown.enter)="dontsave()">Save</div>');
  const invalidQuotedHandler = analyseTemplate(`<div role="button" tabindex="0" (click)="save()" (keydown.enter)="log(';save();')">Save</div>`);
  const invalidDifferentArguments = analyseTemplate('<div role="button" tabindex="0" (click)="save(1)" (keydown.enter)="save(2)">Save</div>');
  const invalidSharedNestedCall = analyseTemplate('<div role="button" tabindex="0" (click)="save(getId())" (keydown.enter)="cancel(getId())">Save</div>');
  const invalidMissingClickAction = analyseTemplate('<div role="button" tabindex="0" (click)="save(); close()" (keydown.enter)="save()">Save</div>');
  const invalidReorderedActions = analyseTemplate('<div role="button" tabindex="0" (click)="save(); close()" (keydown.enter)="close(); save()">Save</div>');
  const invalidQuotedEvent = analyseTemplate(`<div role="button" tabindex="0" (click)="save()" (keydown.enter)="log('$event')">Save</div>`);
  const invalidStringWhitespace = analyseTemplate(`<div role="button" tabindex="0" (click)="save('a b')" (keydown.enter)="save('ab')">Save</div>`);
  const invalidStaticNegativeTabIndex = analyseTemplate('<div role="button" tabindex="-2" (click)="open()" (keydown.enter)="open()">Open</div>');
  const invalidBoundNegativeTabIndex = analyseTemplate('<div role="button" [tabindex]="-1" (click)="open()" (keydown.enter)="open()">Open</div>');
  const invalidClassTabIndex = analyseTemplate('<div role="button" [class.tabindex]="enabled" (click)="open()" (keydown.enter)="open()">Open</div>');
  const invalidUnnamedRole = analyseTemplate('<div role="button" tabindex="0" (click)="save()" (keydown.enter)="save()"><img alt=""></div>');
  const invalidNullHref = analyseTemplate('<a [attr.href]="null" (click)="open()">Open</a>');
  const invalidConditionalHref = analyseTemplate('<a [attr.href]="enabled ? \'/parks\' : null" (click)="open()">Open</a>');
  const invalidConditionalTabIndex = analyseTemplate('<div role="button" [tabindex]="enabled ? 0 : -1" (click)="open()" (keydown.enter)="open()">Open</div>');
  const invalidFallbackHref = analyseTemplate('<a [attr.href]="url ?? null" (click)="open()">Open</a>');
  const invalidFallbackTabIndex = analyseTemplate('<div role="button" [tabindex]="value ?? -1" (click)="open()" (keydown.enter)="open()">Open</div>');
  const invalidBareSameAction = analyseTemplate('<div role="button" tabindex="0" (click)="open()" (keydown)="open()">Open</div>');
  const invalidConflictingActivation = analyseTemplate('<div role="button" tabindex="0" (click)="save()" (keydown.enter)="save()" (keydown.space)="cancel()">Save</div>');
  const invalidMissingButtonSpace = analyseTemplate('<div role="button" tabindex="0" (click)="save()" (keydown.enter)="save()">Save</div>');
  const invalidLinkSpaceOnly = analyseTemplate('<div role="link" tabindex="0" (click)="open()" (keydown.space)="open()">Open</div>');
  const invalidCheckboxEnterOnly = analyseTemplate('<div role="checkbox" tabindex="0" (click)="toggle()" (keydown.enter)="toggle()">Choice</div>');
  const valid = analyseTemplate('<div role="button" tabindex="0" (click)="open()" (keydown.enter)="openFromKeyboard($event)" (keydown.space)="openFromKeyboard($event)">Open</div>');
  const validBoundRole = analyseTemplate('<div [attr.role]="\'button\'" tabindex="0" (click)="open()" (keydown.enter)="open()" (keydown.space)="open()">Open</div>');
  const validConditionalRole = analyseTemplate('<div [attr.role]="isLink ? \'link\' : \'button\'" tabindex="0" (click)="open()" (keydown.enter)="open()" (keydown.space)="open()">Open</div>');
  const validBareDelegated = analyseTemplate('<div role="button" tabindex="0" (click)="open()" (keydown)="onKeydown($event)">Open</div>');
  const validAssignment = analyseTemplate('<div role="button" tabindex="0" (click)="selected = true" (keydown.enter)="selected = true" (keydown.space)="selected = true">Select</div>');
  const validEquivalentQuoteStyle = analyseTemplate(`<div role="button" tabindex="0" (click)="save('x')" (keydown.enter)='save("x")' (keydown.space)="save('x')">Save</div>`);
  const validRedundantParentheses = analyseTemplate(`<div role="button" tabindex="0" (click)="save('x')" (keydown.enter)="(save('x'))" (keydown.space)="save('x')">Save</div>`);
  const validMultipleActivationKeys = analyseTemplate('<div role="button" tabindex="0" (click)="save()" (keydown.enter)="save()" (keydown.space)="save(); $event.preventDefault()">Save</div>');
  const validLinkActivation = analyseTemplate('<div role="link" tabindex="0" (click)="open()" (keydown.enter)="open()">Open</div>');
  const validPropagationOnly = analyseTemplate('<div (click)="$event.stopPropagation()">Overlay content</div>');
  const validClickEventControl = analyseTemplate('<div role="button" tabindex="0" (click)="save(); $event.stopPropagation()" (keydown.enter)="save()" (keydown.space)="save(); $event.preventDefault()">Save</div>');
  const validCheckboxActivation = analyseTemplate('<div role="checkbox" tabindex="0" (click)="toggle()" (keydown.space)="toggle()">Choice</div>');
  const validRouterLinkActivation = analyseTemplate('<div role="link" tabindex="0" routerLink="/parks" (keydown.enter)="open()">Parks</div>');
  const validDisabledRouterLinks = analyseTemplate(`
    <a [routerLink]="null">Unavailable</a>
    <div [routerLink]="undefined">Unavailable</div>
  `);
  const invalidConditionalRouterLink = analyseTemplate('<div [routerLink]="enabled ? \'/parks\' : null">Parks</div>');
  const validConditionalAnchorRouterLink = analyseTemplate('<a [routerLink]="enabled ? \'/parks\' : null">Parks</a>');
  const invalidUnnamedConditionalLinks = analyseTemplate(`
    <a [routerLink]="enabled ? '/parks' : null"><img alt=""></a>
    <a [href]="enabled ? '/parks' : null"><img alt=""></a>
  `);
  const validTabEnterActivation = analyseTemplate('<div role="tab" tabindex="0" (click)="select()" (keydown.enter)="select()">Tab</div>');
  const validTabSpaceActivation = analyseTemplate('<div role="tab" tabindex="0" (click)="select()" (keydown.space)="select()">Tab</div>');

  assert.deepEqual(invalid.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidRouterLink.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidRouterLinkEventControl.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidRoleOnly.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidFocusOnly.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidEscapeOnly.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidBareHandler.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidSpecificHandler.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidSubstringHandler.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidQuotedHandler.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidDifferentArguments.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidSharedNestedCall.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidMissingClickAction.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidReorderedActions.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidQuotedEvent.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidStringWhitespace.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidStaticNegativeTabIndex.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidBoundNegativeTabIndex.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidClassTabIndex.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidUnnamedRole.map((finding) => finding.rule), ['interactive-name', 'click-keyboard']);
  assert.deepEqual(invalidNullHref.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidConditionalHref.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidConditionalTabIndex.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidFallbackHref.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidFallbackTabIndex.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidBareSameAction.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidConflictingActivation.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidMissingButtonSpace.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidLinkSpaceOnly.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(invalidCheckboxEnterOnly.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(valid, []);
  assert.deepEqual(validBoundRole, []);
  assert.deepEqual(validConditionalRole, []);
  assert.deepEqual(validBareDelegated, []);
  assert.deepEqual(validAssignment, []);
  assert.deepEqual(validEquivalentQuoteStyle, []);
  assert.deepEqual(validRedundantParentheses, []);
  assert.deepEqual(validMultipleActivationKeys, []);
  assert.deepEqual(validLinkActivation, []);
  assert.deepEqual(validPropagationOnly, []);
  assert.deepEqual(validClickEventControl, []);
  assert.deepEqual(validCheckboxActivation, []);
  assert.deepEqual(validRouterLinkActivation, []);
  assert.deepEqual(validDisabledRouterLinks, []);
  assert.deepEqual(invalidConditionalRouterLink.map((finding) => finding.rule), ['click-keyboard']);
  assert.deepEqual(validConditionalAnchorRouterLink, []);
  assert.deepEqual(
    invalidUnnamedConditionalLinks.map((finding) => finding.rule),
    ['interactive-name', 'interactive-name']
  );
  assert.deepEqual(validTabEnterActivation, []);
  assert.deepEqual(validTabSpaceActivation, []);
});

test('reports additions and resolved debt against a baseline', () => {
  const current = [{ fingerprint: 'new' }, { fingerprint: 'kept' }];
  const baseline = [{ fingerprint: 'old' }, { fingerprint: 'kept' }];
  const comparison = compareBaseline(current, baseline);

  assert.deepEqual(comparison.added, [{ fingerprint: 'new' }]);
  assert.deepEqual(comparison.resolved, [{ fingerprint: 'old' }]);
});

test('keeps fingerprints stable when an unchanged finding moves structurally', () => {
  const original = fingerprintFindings([{
    rule: 'interactive-name',
    path: 'sample.html',
    line: 2,
    context: '<button type="button">',
    identity: '<button type="button"></button>',
    locator: 'root/Element:0',
    message: 'missing name'
  }]);
  const relocated = fingerprintFindings([{
    rule: 'interactive-name',
    path: 'sample.html',
    line: 8,
    context: '<button type="button">',
    identity: '<button type="button"></button>',
    locator: 'root/Element:1',
    message: 'missing name'
  }]);
  const comparison = compareBaseline(relocated, original);

  assert.equal(comparison.added.length, 0);
  assert.equal(comparison.resolved.length, 0);

  const duplicates = fingerprintFindings([
    { ...original[0], identity: '<button type="button"></button>', locator: 'root/Element:0' },
    { ...original[0], identity: '<button type="button"></button>', locator: 'root/Element:1' }
  ]);
  assert.notEqual(duplicates[0].fingerprint, duplicates[1].fingerprint);
});

test('keeps distinct branch findings visible when identical controls move between branches', () => {
  const baseline = fingerprintFindings([{
    rule: 'interactive-name',
    path: 'sample.html',
    line: 2,
    context: '<button type="button">',
    identity: '<button type="button"></button>',
    scopeIdentity: '@if (ready) {::@if (ready) {',
    locator: 'root/IfBlock:0/children:0/Element:0',
    message: 'missing name'
  }]);
  const current = fingerprintFindings([{
    rule: 'interactive-name',
    path: 'sample.html',
    line: 8,
    context: '<button type="button">',
    identity: '<button type="button"></button>',
    scopeIdentity: '@if (admin) {::@if (admin) {',
    locator: 'root/IfBlock:1/children:0/Element:0',
    message: 'missing name'
  }]);
  const comparison = compareBaseline(current, baseline);

  assert.equal(comparison.added.length, 1);
  assert.equal(comparison.resolved.length, 1);
});

test('distinguishes repeated control-flow headers without depending on unrelated siblings', () => {
  const baseline = fingerprintFindings(analyseTemplate(`
    @if (ready) { <button type="button"></button> }
    <p>Unrelated content</p>
    @if (ready) { <button type="button" aria-label="Save"></button> }
  `, 'sample.html'));
  const current = fingerprintFindings(analyseTemplate(`
    <div>Inserted unrelated sibling</div>
    @if (ready) { <button type="button" aria-label="Save"></button> }
    <p>Unrelated content</p>
    @if (ready) { <button type="button"></button> }
  `, 'sample.html'));
  const comparison = compareBaseline(current, baseline);

  assert.equal(comparison.added.length, 1);
  assert.equal(comparison.resolved.length, 1);
});

test('uses stable enclosing identities for findings outside control-flow branches', () => {
  const baseline = fingerprintFindings(analyseTemplate(`
    <button type="button"></button>
  `, 'sample.html'));
  const current = fingerprintFindings(analyseTemplate(`
    <section><button type="button"></button></section>
    <button type="button" aria-label="Save"></button>
  `, 'sample.html'));
  const comparison = compareBaseline(current, baseline);

  assert.equal(comparison.added.length, 1);
  assert.equal(comparison.resolved.length, 1);

  const relocated = fingerprintFindings(analyseTemplate(`
    <p>Unrelated sibling</p>
    <button type="button"></button>
  `, 'sample.html'));
  const relocationComparison = compareBaseline(relocated, baseline);
  assert.equal(relocationComparison.added.length, 0);
  assert.equal(relocationComparison.resolved.length, 0);

  const nestedBaseline = fingerprintFindings(analyseTemplate(`
    <section><button type="button"></button></section>
  `, 'sample.html'));
  const nestedRelocated = fingerprintFindings(analyseTemplate(`
    <section><button type="button" aria-label="Save"></button></section>
    <section><button type="button"></button></section>
  `, 'sample.html'));
  const nestedComparison = compareBaseline(nestedRelocated, nestedBaseline);
  assert.equal(nestedComparison.added.length, 0);
  assert.equal(nestedComparison.resolved.length, 0);
});
