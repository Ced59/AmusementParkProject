import { BindingType, parseTemplate } from '@angular/compiler';

// Presentation may move, but its existing expressions, events, accessible labels,
// form constraints and deferred triggers must not disappear. This is a source
// contract comparison, not a substitute for browser or permission acceptance.
export function templateContracts(source, filename = 'review.html') {
  const parsed = parseTemplate(source, filename, { preserveWhitespaces: false });
  if (parsed.errors?.length) throw new Error(`Cannot parse template contract: ${filename}: ${parsed.errors[0]}`);
  const contracts = [];
  const add = (kind, ...values) => contracts.push(JSON.stringify([kind, ...values]));
  const expression = value => typeof value === 'string' ? value.trim() : value?.source?.trim();
  const imagePresentation = new Set(['sizes', 'srcWidth', 'responsiveWidths', 'imgClass', 'placeholderClass', 'placeholderIconClass']);
  const semanticAttribute = /^(?:id|name|type|href|routerLink|role|tabindex|aria-(?!hidden$)[\w-]+|formControlName|formGroupName|autocomplete|autofocus|required|disabled|readonly|multiple|min|max|step|pattern|minlength|maxlength|accept|loading|fetchPriority|target|rel|download)$/i;
  function walk(node) {
    const kind = node.constructor.name;
    const tag = node.name ?? node.tagName ?? kind;
    for (const attribute of node.attributes ?? []) {
      if (semanticAttribute.test(attribute.name)) add('attribute', tag, attribute.name, attribute.value);
    }
    for (const input of node.inputs ?? []) {
      if ([BindingType.Class, BindingType.Style, BindingType.Animation].includes(input.type)
        || ['class', 'style', 'ngClass', 'ngStyle', 'iconClass', 'tone'].includes(input.name)
        || (tag === 'app-image-display' && imagePresentation.has(input.name))) continue;
      add('input', tag, input.type, input.name, expression(input.value));
    }
    for (const output of node.outputs ?? []) add('event', tag, output.name, output.type, expression(output.handler));
    for (const attribute of node.templateAttrs ?? []) add('template', tag, attribute.name, expression(attribute.value));
    for (const reference of node.references ?? []) add('reference', tag, reference.name, reference.value);
    for (const variable of node.variables ?? []) add('variable', tag, variable.name, variable.value);
    if (kind === 'BoundText') add('text', expression(node.value));
    if (node.expression) add('expression', kind, expression(node.expression));
    if (node.expressionAlias) add('alias', kind, node.expressionAlias.name);
    if (node.trackBy) add('trackBy', expression(node.trackBy));
    if (node.item) add('loopItem', node.item.name);
    for (const variable of node.contextVariables ?? []) add('loopContext', variable.name, variable.value);
    for (const key of ['triggers', 'prefetchTriggers', 'hydrateTriggers']) {
      for (const [name, trigger] of Object.entries(node[key] ?? {})) {
        add('defer', key, name, trigger.constructor.name, trigger.reference ?? null, trigger.delay ?? null, expression(trigger.value) ?? null);
      }
    }
    for (const key of ['minimumTime', 'afterTime']) if (node[key] !== undefined) add('deferTiming', kind, key, node[key]);
    for (const key of ['children', 'branches', 'cases']) for (const child of node[key] ?? []) walk(child);
    for (const key of ['placeholder', 'loading', 'error', 'empty']) if (node[key]) walk(node[key]);
  }
  parsed.nodes.forEach(walk);
  return contracts.sort();
}

export function compareTemplateContracts(before, after) {
  const remaining = [...after];
  const missing = [];
  for (const contract of before) {
    const index = remaining.indexOf(contract);
    if (index < 0) missing.push(JSON.parse(contract));
    else remaining.splice(index, 1);
  }
  return { beforeCount: before.length, afterCount: after.length, missing, added: remaining.map(value => JSON.parse(value)) };
}
