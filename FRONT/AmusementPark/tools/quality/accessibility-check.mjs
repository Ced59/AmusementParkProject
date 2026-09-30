import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { BindingType, parseTemplate } from '@angular/compiler';
import ts from 'typescript';

const TOOL_DIRECTORY = path.dirname(fileURLToPath(import.meta.url));
const FRONTEND_ROOT = path.resolve(TOOL_DIRECTORY, '..', '..');
const APPLICATION_ROOT = path.join(FRONTEND_ROOT, 'src', 'app');
const BASELINE_PATH = path.join(TOOL_DIRECTORY, 'accessibility-baseline.json');

const INTERACTIVE_NATIVE_ELEMENTS = new Set(['button', 'input', 'select', 'textarea', 'summary']);
const BARE_KEYBOARD_EVENT_NAMES = new Set(['keydown', 'keyup', 'keypress']);
const ACTIVATION_KEYBOARD_EVENT_NAMES = new Set([
  'keydown.enter',
  'keyup.enter',
  'keypress.enter',
  'keydown.space',
  'keyup.space',
  'keypress.space',
  'keydown.spacebar',
  'keyup.spacebar',
  'keypress.spacebar'
]);
const INTERACTIVE_ROLE_REQUIRED_KEYS = new Map([
  ['button', [['enter'], ['space']]],
  ['checkbox', [['space']]],
  ['link', [['enter']]],
  ['menuitem', [['enter', 'space']]],
  ['menuitemcheckbox', [['enter', 'space']]],
  ['menuitemradio', [['enter', 'space']]],
  ['radio', [['space']]],
  ['switch', [['space']]],
  ['tab', [['enter', 'space']]],
  ['treeitem', [['enter']]]
]);

function walkFiles(directory, predicate) {
  const files = [];

  for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
    const fullPath = path.join(directory, entry.name);

    if (entry.isDirectory()) {
      files.push(...walkFiles(fullPath, predicate));
      continue;
    }

    if (predicate(fullPath)) {
      files.push(fullPath);
    }
  }

  return files.sort((left, right) => left.localeCompare(right, 'en'));
}

function isAttributeBinding(input) {
  return input.type === BindingType.Property || input.type === BindingType.Attribute;
}

function staticAttributes(node) {
  if (Array.isArray(node.attributes)) {
    return node.attributes;
  }

  return Object.entries(node.attrs ?? {}).map(([name, value]) => ({ name, value }));
}

function attributeNames(node) {
  return new Set([
    ...staticAttributes(node).map((attribute) => attribute.name.toLowerCase()),
    ...(node.inputs ?? [])
      .filter((input) => isAttributeBinding(input))
      .map((input) => input.name.toLowerCase())
  ]);
}

function outputNames(node) {
  return new Set((node.outputs ?? []).map((output) => output.name.toLowerCase()));
}

function elementName(node) {
  const name = typeof node.name === 'string'
    ? node.name
    : typeof node.tag === 'string'
      ? node.tag
      : node.tagName;
  return typeof name === 'string' ? name.toLowerCase() : '';
}

function attributeValue(node, name) {
  const normalizedName = name.toLowerCase();
  const attribute = staticAttributes(node).find((candidate) => candidate.name.toLowerCase() === normalizedName);
  return attribute?.value ?? null;
}

function normalizedAttributeName(name) {
  return typeof name === 'string' ? name.toLowerCase().replaceAll('-', '') : '';
}

function reachableStaticValues(expression) {
  if (expression?.constructor?.name === 'ASTWithSource') {
    return reachableStaticValues(expression.ast);
  }

  if (expression?.constructor?.name === 'ParenthesizedExpression') {
    return reachableStaticValues(expression.expression);
  }

  if (expression?.constructor?.name === 'LiteralPrimitive') {
    return { values: [expression.value], hasUnknown: false };
  }

  if (
    expression?.constructor?.name === 'PropertyRead'
    && expression.name === 'undefined'
    && expression.receiver?.constructor?.name === 'ImplicitReceiver'
  ) {
    return { values: [undefined], hasUnknown: false };
  }

  if (expression?.constructor?.name === 'PrefixNot') {
    const operand = reachableStaticValues(expression.expression);
    return {
      values: operand.values.map((value) => !value),
      hasUnknown: operand.hasUnknown
    };
  }

  if (expression?.constructor?.name === 'Unary' && (expression.operator === '-' || expression.operator === '+')) {
    const operand = reachableStaticValues(expression.expr);
    return {
      values: operand.values.map((value) => (
        expression.operator === '-' ? -Number(value) : Number(value)
      )),
      hasUnknown: operand.hasUnknown
    };
  }

  if (expression?.constructor?.name === 'Conditional') {
    const condition = reachableStaticValues(expression.condition);
    const whenTrue = reachableStaticValues(expression.trueExp);
    const whenFalse = reachableStaticValues(expression.falseExp);
    const canBeTrue = condition.hasUnknown || condition.values.some((value) => Boolean(value));
    const canBeFalse = condition.hasUnknown || condition.values.some((value) => !value);
    return {
      values: [
        ...(canBeTrue ? whenTrue.values : []),
        ...(canBeFalse ? whenFalse.values : [])
      ],
      hasUnknown: (canBeTrue && whenTrue.hasUnknown) || (canBeFalse && whenFalse.hasUnknown)
    };
  }

  if (
    expression?.constructor?.name === 'Binary'
    && ['||', '&&', '??'].includes(expression.operation)
  ) {
    const left = reachableStaticValues(expression.left);
    const right = reachableStaticValues(expression.right);
    const values = [];
    let hasUnknown = left.hasUnknown;

    for (const value of left.values) {
      const usesRight = expression.operation === '||'
        ? !value
        : expression.operation === '&&'
          ? Boolean(value)
          : value === null || value === undefined;

      if (usesRight) {
        values.push(...right.values);
        hasUnknown ||= right.hasUnknown;
      } else {
        values.push(value);
      }
    }

    if (left.hasUnknown) {
      values.push(...right.values);

      if (expression.operation === '||') {
        values.push('__unknown_truthy__', true, 1);
        hasUnknown = right.hasUnknown;
      } else if (expression.operation === '&&') {
        values.push('', false, 0, null, undefined);
        hasUnknown = right.hasUnknown;
      } else {
        values.push('', false, -1, '__unknown_non_nullish__');
        hasUnknown = right.hasUnknown;
      }
    }

    return { values, hasUnknown };
  }

  if (
    expression?.constructor?.name === 'Binary'
    && ['===', '!==', '==', '!=', '<', '<=', '>', '>=', '+', '-', '*', '/', '%', '**'].includes(expression.operation)
  ) {
    const left = reachableStaticValues(expression.left);
    const right = reachableStaticValues(expression.right);
    if (left.hasUnknown || right.hasUnknown || left.values.length === 0 || right.values.length === 0) {
      return { values: [], hasUnknown: true };
    }

    const values = left.values.flatMap((leftValue) => right.values.map((rightValue) => {
      switch (expression.operation) {
        case '===': return leftValue === rightValue;
        case '!==': return leftValue !== rightValue;
        case '==': return leftValue == rightValue;
        case '!=': return leftValue != rightValue;
        case '<': return leftValue < rightValue;
        case '<=': return leftValue <= rightValue;
        case '>': return leftValue > rightValue;
        case '>=': return leftValue >= rightValue;
        case '+': return leftValue + rightValue;
        case '-': return Number(leftValue) - Number(rightValue);
        case '*': return Number(leftValue) * Number(rightValue);
        case '/': return Number(leftValue) / Number(rightValue);
        case '%': return Number(leftValue) % Number(rightValue);
        case '**': return Number(leftValue) ** Number(rightValue);
        default: return undefined;
      }
    }));

    return { values, hasUnknown: false };
  }

  if (expression?.constructor?.name === 'Interpolation') {
    let values = [expression.strings?.[0] ?? ''];
    let hasUnknown = false;

    for (const [index, candidate] of (expression.expressions ?? []).entries()) {
      const outcome = reachableStaticValues(candidate);
      hasUnknown ||= outcome.hasUnknown;

      if (outcome.values.length === 0) {
        values = [];
        continue;
      }

      values = values.flatMap((prefix) => outcome.values.map((value) => (
        `${prefix}${value === null || value === undefined ? '' : String(value)}${expression.strings?.[index + 1] ?? ''}`
      )));
    }

    return { values, hasUnknown };
  }

  return { values: [], hasUnknown: true };
}

function boundReachableStaticValues(input) {
  if (input?.value?.source?.trim() === 'undefined') {
    return { values: [undefined], hasUnknown: false };
  }

  return reachableStaticValues(input?.value?.ast);
}

function everyReachableStaticValueMatches(input, predicate) {
  const outcome = boundReachableStaticValues(input);
  return outcome.values.every((value) => predicate(value));
}

function hasNonEmptyOrBoundValue(node, candidateNames) {
  const normalizedNames = new Set(candidateNames.map((name) => name.toLowerCase()));
  const boundValues = (node.inputs ?? []).filter((input) => (
    isAttributeBinding(input) && normalizedNames.has(input.name.toLowerCase())
  ));

  for (const input of boundValues) {
    if (everyReachableStaticValueMatches(input, (value) => (
      typeof value === 'string'
        ? value.trim().length > 0
        : value !== null && value !== undefined
    ))) {
      return true;
    }
  }

  return staticAttributes(node).some((attribute) => (
    normalizedNames.has(attribute.name.toLowerCase())
    && typeof attribute.value === 'string'
    && attribute.value.trim().length > 0
  ));
}

function normalizedCssValue(value) {
  return typeof value === 'string'
    ? value.trim().toLowerCase().replace(/\s*!important\s*$/, '').trim()
    : null;
}

function inlineStyleHidesContent(value) {
  if (typeof value !== 'string') {
    return false;
  }

  const declarations = new Map();
  for (const declaration of value.split(';')) {
    const parts = declaration.split(':', 2).map((part) => part.trim().toLowerCase());
    if (parts.length !== 2 || parts[0].length === 0) {
      continue;
    }

    const important = /\s*!important\s*$/.test(parts[1]);
    const current = declarations.get(parts[0]);
    if (!current || important || !current.important) {
      declarations.set(parts[0], { value: parts[1], important });
    }
  }

  const display = normalizedCssValue(declarations.get('display')?.value);
  const visibility = normalizedCssValue(declarations.get('visibility')?.value);

  return display === 'none'
    || visibility === 'hidden'
    || visibility === 'collapse';
}

function booleanAttributeCanExclude(node, name) {
  const normalizedName = name.toLowerCase();
  if (staticAttributes(node).some((attribute) => attribute.name.toLowerCase() === normalizedName)) {
    return true;
  }

  const binding = (node.inputs ?? []).find((input) => (
    isAttributeBinding(input) && input.name.toLowerCase() === normalizedName
  ));
  if (!binding) {
    return false;
  }

  const outcome = boundReachableStaticValues(binding);
  if (binding.type === BindingType.Attribute) {
    return outcome.hasUnknown
      || outcome.values.some((value) => value !== null && value !== undefined);
  }

  return outcome.hasUnknown || outcome.values.some((value) => Boolean(value));
}

function isAccessibilityHidden(node) {
  const names = attributeNames(node);

  if (booleanAttributeCanExclude(node, 'hidden') || booleanAttributeCanExclude(node, 'inert')) {
    return true;
  }

  if (inlineStyleHidesContent(attributeValue(node, 'style'))) {
    return true;
  }

  const boundInlineStyle = (node.inputs ?? []).find((input) => (
    isAttributeBinding(input) && input.name.toLowerCase() === 'style'
  ));
  if (boundInlineStyle) {
    const boundInlineStyleValues = boundReachableStaticValues(boundInlineStyle);
    if (
      boundInlineStyleValues.hasUnknown
      || boundInlineStyleValues.values.some((value) => inlineStyleHidesContent(value))
    ) {
      return true;
    }
  }

  const hiddenStyleBinding = (node.inputs ?? []).find((input) => {
    if (input.type !== BindingType.Style) {
      return false;
    }

    const property = input.name.toLowerCase();
    const outcome = boundReachableStaticValues(input);
    if (['display', 'visibility'].includes(property) && outcome.hasUnknown) {
      return true;
    }

    return outcome.values.some((candidate) => {
      const value = normalizedCssValue(candidate);
      return (property === 'display' && value === 'none')
        || (property === 'visibility' && (value === 'hidden' || value === 'collapse'));
    });
  });
  if (hiddenStyleBinding) {
    return true;
  }

  const staticValue = attributeValue(node, 'aria-hidden') ?? attributeValue(node, 'ariahidden');

  if (typeof staticValue === 'string' && staticValue.trim().toLowerCase() === 'true') {
    return true;
  }

  const boundAriaHidden = (node.inputs ?? []).find((input) => (
    isAttributeBinding(input) && input.name.toLowerCase().replaceAll('-', '') === 'ariahidden'
  ));
  if (!boundAriaHidden) {
    return false;
  }

  const ariaHiddenValues = boundReachableStaticValues(boundAriaHidden);
  return ariaHiddenValues.hasUnknown
    || ariaHiddenValues.values.some((value) => (
      value === true
      || (typeof value === 'string' && value.trim().toLowerCase() === 'true')
    ));
}

function reachableIfBranches(node) {
  const reachable = [];
  let canFallThrough = true;

  for (const branch of node.branches ?? []) {
    if (!canFallThrough) {
      break;
    }

    if (branch.expression === null) {
      reachable.push(branch);
      canFallThrough = false;
      break;
    }

    const condition = reachableStaticValues(branch.expression);
    const canBeTrue = condition.hasUnknown || condition.values.some((value) => Boolean(value));
    const canBeFalse = condition.hasUnknown || condition.values.some((value) => !value);
    if (canBeTrue) {
      reachable.push(branch);
    }

    canFallThrough = canBeFalse;
  }

  return { reachable, canFallThrough };
}

function reachableSwitchGroups(node) {
  const selector = reachableStaticValues(node.expression);
  if (selector.hasUnknown || selector.values.length === 0) {
    return null;
  }

  const groups = node.groups ?? [];
  const defaultGroup = groups.find((group) => (
    (group.cases ?? []).some((candidate) => candidate.expression === null)
  ));
  const caseGroups = [];

  for (const group of groups) {
    const values = [];
    for (const candidate of group.cases ?? []) {
      if (candidate.expression === null) {
        continue;
      }

      const outcome = reachableStaticValues(candidate.expression);
      if (outcome.hasUnknown || outcome.values.length === 0) {
        return null;
      }

      values.push(...outcome.values);
    }

    if (values.length > 0) {
      caseGroups.push({ group, values });
    }
  }

  const reachable = new Set();
  let canFallThrough = false;
  for (const selectorValue of selector.values) {
    const matched = caseGroups.find((candidate) => (
      candidate.values.some((caseValue) => Object.is(caseValue, selectorValue))
    ));

    if (matched) {
      reachable.add(matched.group);
    } else if (defaultGroup) {
      reachable.add(defaultGroup);
    } else {
      canFallThrough = true;
    }
  }

  return { reachable: [...reachable], canFallThrough };
}

function reachabilityContextsAreCompatible(left, right) {
  for (const [owner, branch] of left ?? []) {
    if (right?.has(owner) && right.get(owner) !== branch) {
      return false;
    }
  }

  return true;
}

function templateScopesAreCompatible(source, target, context) {
  const sourceScope = context.templateScopeByNode.get(source) ?? null;
  const targetScope = context.templateScopeByNode.get(target) ?? null;
  return targetScope === null || targetScope === sourceScope;
}

function labelledByValueResolves(value, node, context, resolving) {
  if (typeof value !== 'string') {
    return false;
  }

  const referencedIds = value.trim().split(/\s+/).filter((candidate) => candidate.length > 0);
  return referencedIds.some((id) => {
    const sourceReachability = context.reachabilityByNode.get(node) ?? new Map();
    const targets = (context.nodesById.get(id) ?? []).filter((target) => (
      reachabilityContextsAreCompatible(
        sourceReachability,
        context.reachabilityByNode.get(target) ?? new Map()
      )
      && templateScopesAreCompatible(node, target, context)
    ));
    return targets.length > 0
      && targets.every((target) => hasAccessibleName(target, context, resolving));
  });
}

function hasUsableAriaLabelledBy(node, context, resolving) {
  const staticReference = staticAttributes(node).find((attribute) => (
    normalizedAttributeName(attribute.name) === 'arialabelledby'
  ));
  if (staticReference) {
    return labelledByValueResolves(staticReference.value, node, context, resolving);
  }

  const boundReference = (node.inputs ?? []).find((input) => (
    isAttributeBinding(input) && normalizedAttributeName(input.name) === 'arialabelledby'
  ));
  if (!boundReference) {
    return false;
  }

  const outcome = boundReachableStaticValues(boundReference);
  if (outcome.values.length === 0) {
    return outcome.hasUnknown;
  }

  return outcome.values.every((value) => labelledByValueResolves(value, node, context, resolving));
}

function hasUsableNativeLabel(node, context, resolving) {
  if (!['button', 'input', 'select', 'textarea'].includes(elementName(node))) {
    return false;
  }

  const labels = new Set();
  for (const id of context.idsByNode.get(node) ?? []) {
    for (const label of context.labelsByTargetId.get(id.trim()) ?? []) {
      labels.add(label);
    }
  }

  let ancestor = context.parentByNode.get(node);
  while (ancestor) {
    if (elementName(ancestor) === 'label') {
      labels.add(ancestor);
    }

    ancestor = context.parentByNode.get(ancestor);
  }

  const sourceReachability = context.reachabilityByNode.get(node) ?? new Map();
  const compatibleLabels = [...labels].filter((label) => (
    reachabilityContextsAreCompatible(
      sourceReachability,
      context.reachabilityByNode.get(label) ?? new Map()
    )
    && templateScopesAreCompatible(node, label, context)
  ));
  const namedLabels = compatibleLabels.filter((label) => (
    hasAccessibleName(label, context, resolving)
  ));
  return compatibleLabels.length > 0
    && namedLabels.length > 0
    && compatibleLabels.every((label) => (
      namedLabels.includes(label)
      || namedLabels.some((namedLabel) => (
        reachabilityContextsAreCompatible(
          context.reachabilityByNode.get(label) ?? new Map(),
          context.reachabilityByNode.get(namedLabel) ?? new Map()
        )
      ))
    ));
}

function hasAccessibleName(node, context, resolving = new Set()) {
  if (resolving.has(node)) {
    return false;
  }

  const nestedResolving = new Set(resolving);
  nestedResolving.add(node);

  if (hasNonEmptyOrBoundValue(node, [
    'aria-label',
    'ariaLabel'
  ])) {
    return true;
  }

  if (hasUsableAriaLabelledBy(node, context, nestedResolving)) {
    return true;
  }

  if (hasUsableNativeLabel(node, context, nestedResolving)) {
    return true;
  }

  if (elementName(node) === 'img' && hasNonEmptyOrBoundValue(node, ['alt'])) {
    return true;
  }

  if (attributeNames(node).has('appuibutton') && hasNonEmptyOrBoundValue(node, ['label'])) {
    return true;
  }

  if (elementName(node) === 'input') {
    const typeCandidates = inputTypeCandidates(node);
    if (typeCandidates.hasUnknown) {
      return false;
    }

    return typeCandidates.types.every((type) => {
      if ((type === 'submit' || type === 'reset') && !attributeNames(node).has('value')) {
        return true;
      }

      if (type === 'button' || type === 'submit' || type === 'reset') {
        return hasNonEmptyOrBoundValue(node, ['value']);
      }

      if (type === 'image') {
        return hasNonEmptyOrBoundValue(node, ['alt']);
      }

      return true;
    });
  }

  return nestedNodeCollections(node).some((collection) => (
    collection.some((child) => containsAccessibleContent(child, context, nestedResolving))
  ));
}

function templateBinding(node, name) {
  const normalizedName = name.toLowerCase();
  return (node.templateAttrs ?? []).find((attribute) => attribute.name?.toLowerCase() === normalizedName);
}

function reachableTemplateReferenceNames(expression) {
  if (expression?.constructor?.name === 'ASTWithSource') {
    return reachableTemplateReferenceNames(expression.ast);
  }

  if (expression?.constructor?.name === 'ParenthesizedExpression') {
    return reachableTemplateReferenceNames(expression.expression);
  }

  if (
    expression?.constructor?.name === 'PropertyRead'
    && expression.receiver?.constructor?.name === 'ImplicitReceiver'
  ) {
    return { names: [expression.name], hasUnknown: false };
  }

  if (expression?.constructor?.name === 'LiteralPrimitive' && typeof expression.value === 'string') {
    return { names: [expression.value], hasUnknown: false };
  }

  if (expression?.constructor?.name === 'Conditional') {
    const condition = reachableStaticValues(expression.condition);
    const canBeTrue = condition.hasUnknown || condition.values.some((value) => Boolean(value));
    const canBeFalse = condition.hasUnknown || condition.values.some((value) => !value);
    const whenTrue = reachableTemplateReferenceNames(expression.trueExp);
    const whenFalse = reachableTemplateReferenceNames(expression.falseExp);
    return {
      names: [
        ...(canBeTrue ? whenTrue.names : []),
        ...(canBeFalse ? whenFalse.names : [])
      ],
      hasUnknown: (canBeTrue && whenTrue.hasUnknown) || (canBeFalse && whenFalse.hasUnknown)
    };
  }

  return { names: [], hasUnknown: true };
}

function referencedTemplateContainsAccessibleContent(binding, outlet, context, resolving) {
  const references = reachableTemplateReferenceNames(binding?.value?.ast);
  const outletReachability = context.reachabilityByNode.get(outlet) ?? new Map();
  return !references.hasUnknown
    && references.names.length > 0
    && references.names.every((referenceName) => {
      const referencedTemplates = (context.templatesByReference.get(referenceName) ?? []).filter((candidate) => (
        reachabilityContextsAreCompatible(
          outletReachability,
          context.reachabilityByNode.get(candidate) ?? new Map()
        )
      ));
      return referencedTemplates.length > 0
        && referencedTemplates.every((referencedTemplate) => (
          collectionContainsAccessibleContent(referencedTemplate.children ?? [], context, resolving)
        ));
    });
}

function templateOutletContainsAccessibleContent(node, context, resolving) {
  const outletBinding = templateBinding(node, 'ngTemplateOutlet')
    ?? (node.inputs ?? []).find((input) => input.name?.toLowerCase() === 'ngtemplateoutlet');
  return outletBinding
    ? referencedTemplateContainsAccessibleContent(outletBinding, node, context, resolving)
    : null;
}

function boundRenderedTextContainsAccessibleContent(node) {
  const bindings = (node.inputs ?? []).filter((input) => (
    input.type === BindingType.Property
    && ['innertext', 'textcontent'].includes(normalizedAttributeName(input.name))
  ));

  return bindings.some((binding) => {
    const outcome = boundReachableStaticValues(binding);
    if (outcome.values.length === 0) {
      return outcome.hasUnknown;
    }

    return outcome.values.every((value) => (
      value !== null && value !== undefined && String(value).trim().length > 0
    ));
  });
}

function legacyIfContainsAccessibleContent(node, context, resolving) {
  const conditionBinding = templateBinding(node, 'ngIf');
  if (!conditionBinding) {
    return null;
  }

  const condition = boundReachableStaticValues(conditionBinding);
  const canBeTrue = condition.hasUnknown || condition.values.some((value) => Boolean(value));
  const canBeFalse = condition.hasUnknown || condition.values.some((value) => !value);
  const thenBinding = templateBinding(node, 'ngIfThen');
  const elseBinding = templateBinding(node, 'ngIfElse');

  const trueBranchHasContent = thenBinding
    ? referencedTemplateContainsAccessibleContent(thenBinding, node, context, resolving)
    : collectionContainsAccessibleContent(node.children ?? [], context, resolving);
  const falseBranchHasContent = elseBinding
    ? referencedTemplateContainsAccessibleContent(elseBinding, node, context, resolving)
    : false;

  return (!canBeTrue || trueBranchHasContent) && (!canBeFalse || falseBranchHasContent);
}

function containsAccessibleContent(node, context, resolving = new Set(), includeHiddenRoot = false) {
  const nodeType = node.constructor?.name ?? '';

  if (resolving.has(node)) {
    return false;
  }

  const nestedResolving = new Set(resolving);
  nestedResolving.add(node);

  if (!includeHiddenRoot && isAccessibilityHidden(node)) {
    return false;
  }

  if (nodeType === 'Template') {
    return legacyIfContainsAccessibleContent(node, context, nestedResolving)
      ?? templateOutletContainsAccessibleContent(node, context, nestedResolving)
      ?? false;
  }

  const templateOutletHasContent = templateOutletContainsAccessibleContent(node, context, nestedResolving);
  if (templateOutletHasContent !== null) {
    return templateOutletHasContent;
  }

  if (boundRenderedTextContainsAccessibleContent(node)) {
    return true;
  }

  if (nodeType === 'IfBlock') {
    const branches = reachableIfBranches(node);
    return !branches.canFallThrough
      && branches.reachable.length > 0
      && branches.reachable.every((branch) => (
        collectionContainsAccessibleContent(branch.children ?? [], context, nestedResolving)
      ));
  }

  if (nodeType === 'SwitchBlock') {
    const reachable = reachableSwitchGroups(node);
    if (reachable) {
      return !reachable.canFallThrough
        && reachable.reachable.length > 0
        && reachable.reachable.every((group) => (
          collectionContainsAccessibleContent(group.children ?? [], context, nestedResolving)
        ));
    }

    const groups = node.groups ?? [];
    const hasDefault = groups.some((group) => (
      (group.cases ?? []).some((candidate) => candidate.expression === null)
    ));
    return hasDefault
      && groups.length > 0
      && groups.every((group) => (
        collectionContainsAccessibleContent(group.children ?? [], context, nestedResolving)
      ));
  }

  if (nodeType === 'ForLoopBlock') {
    return collectionContainsAccessibleContent(node.children ?? [], context, nestedResolving)
      && Array.isArray(node.empty?.children)
      && collectionContainsAccessibleContent(node.empty.children, context, nestedResolving);
  }

  if (nodeType === 'DeferredBlock') {
    return [{ children: node.children }, node.placeholder, node.loading, node.error]
      .every((block) => (
        Array.isArray(block?.children)
        && collectionContainsAccessibleContent(block.children, context, nestedResolving)
      ));
  }

  if (nodeType === 'Icu') {
    const icuNodes = node.cases
      ? [node]
      : (node.i18n?.nodes ?? []).filter((candidate) => candidate?.constructor?.name === 'Icu');

    return icuNodes.length > 0 && icuNodes.every((icu) => {
      const cases = Object.values(icu.cases ?? {});
      return cases.length > 0 && cases.every((candidate) => (
        collectionContainsAccessibleContent(candidate.children ?? [], context, nestedResolving)
      ));
    });
  }

  if (nodeType === 'BoundText') {
    const interpolation = node.value?.ast;

    if (interpolation?.constructor?.name !== 'Interpolation') {
      return true;
    }

    const outcome = reachableStaticValues(interpolation);
    return outcome.values.every((value) => (
      value !== null && value !== undefined && String(value).trim().length > 0
    ));
  }

  if (nodeType === 'Content') {
    return true;
  }

  if (nodeType === 'Text' && typeof node.value === 'string' && node.value.trim().length > 0) {
    return true;
  }

  if (
    hasNonEmptyOrBoundValue(node, ['aria-label', 'ariaLabel'])
    || hasUsableAriaLabelledBy(node, context, nestedResolving)
  ) {
    return true;
  }

  if (elementName(node) === 'img' && hasNonEmptyOrBoundValue(node, ['alt'])) {
    return true;
  }

  return nestedNodeCollections(node).some((collection) => (
    collectionContainsAccessibleContent(collection, context, nestedResolving)
  ));
}

function collectionContainsAccessibleContent(nodes, context, resolving) {
  return nodes.some((child) => containsAccessibleContent(child, context, resolving));
}

function hasUsableLinkTarget(node, name) {
  const normalizedName = name.toLowerCase();
  if (staticAttributes(node).some((attribute) => attribute.name.toLowerCase() === normalizedName)) {
    return true;
  }

  return (node.inputs ?? []).some((input) => {
    if (!isAttributeBinding(input) || input.name.toLowerCase() !== normalizedName) {
      return false;
    }

    return everyReachableStaticValueMatches(
      input,
      (value) => value !== null && value !== undefined
    );
  });
}

function hasPotentiallyUsableLinkTarget(node, name) {
  const normalizedName = name.toLowerCase();
  if (staticAttributes(node).some((attribute) => attribute.name.toLowerCase() === normalizedName)) {
    return true;
  }

  return (node.inputs ?? []).some((input) => {
    if (!isAttributeBinding(input) || input.name.toLowerCase() !== normalizedName) {
      return false;
    }

    const outcome = boundReachableStaticValues(input);
    return outcome.hasUnknown
      || outcome.values.some((value) => value !== null && value !== undefined);
  });
}

function isNativeInteractive(node) {
  const name = elementName(node);
  if (INTERACTIVE_NATIVE_ELEMENTS.has(name)) {
    return true;
  }

  if (name === 'a') {
    return hasUsableLinkTarget(node, 'href') || hasUsableLinkTarget(node, 'routerlink');
  }

  return false;
}

function inputTypeCandidates(node) {
  const staticType = attributeValue(node, 'type');
  if (typeof staticType === 'string') {
    return { types: [staticType.trim().toLowerCase()], hasUnknown: false };
  }

  const boundType = (node.inputs ?? []).find((input) => (
    isAttributeBinding(input) && input.name.toLowerCase() === 'type'
  ));
  if (!boundType) {
    return { types: ['text'], hasUnknown: false };
  }

  const outcome = boundReachableStaticValues(boundType);
  return {
    types: outcome.values
      .filter((value) => typeof value === 'string')
      .map((value) => value.trim().toLowerCase()),
    hasUnknown: outcome.hasUnknown
      || outcome.values.some((value) => typeof value !== 'string')
  };
}

function startTagSource(node, template) {
  const span = node.startSourceSpan ?? node.sourceSpan;
  const start = span?.start?.offset ?? 0;
  const end = span?.end?.offset ?? start;
  return template.slice(start, end).replace(/\s+/g, ' ').trim();
}

function expressionSignature(expression, ancestors = new Set()) {
  if (expression?.constructor?.name === 'ASTWithSource') {
    return expressionSignature(expression.ast, ancestors);
  }

  if (expression?.constructor?.name === 'ParenthesizedExpression') {
    return expressionSignature(expression.expression, ancestors);
  }

  if (expression === null || typeof expression !== 'object') {
    return JSON.stringify(expression);
  }

  if (ancestors.has(expression)) {
    return '[cycle]';
  }

  const nestedAncestors = new Set(ancestors);
  nestedAncestors.add(expression);
  if (Array.isArray(expression)) {
    return `[${expression.map((value) => expressionSignature(value, nestedAncestors)).join(',')}]`;
  }

  const ignoredFields = new Set([
    'span',
    'sourceSpan',
    'nameSpan',
    'argumentSpan',
    'source',
    'location',
    'errors'
  ]);
  const fields = Object.entries(expression)
    .filter(([key, value]) => !ignoredFields.has(key) && typeof value !== 'function')
    .sort(([left], [right]) => left.localeCompare(right, 'en'))
    .map(([key, value]) => `${key}:${expressionSignature(value, nestedAncestors)}`);

  return `${expression.constructor?.name ?? 'Object'}{${fields.join(',')}}`;
}

function topLevelActions(handler) {
  const root = handler?.ast ?? handler;
  return root?.constructor?.name === 'Chain'
    ? root.expressions ?? []
    : [root];
}

function topLevelActionSignatures(handler) {
  return topLevelActions(handler).filter(Boolean).map((action) => expressionSignature(action));
}

function containsEventReference(expression, visited = new Set()) {
  if (!expression || typeof expression !== 'object' || visited.has(expression)) {
    return false;
  }

  visited.add(expression);
  if (
    expression.constructor?.name === 'PropertyRead'
    && expression.name === '$event'
    && expression.receiver?.constructor?.name === 'ImplicitReceiver'
  ) {
    return true;
  }

  return Object.entries(expression).some(([key, value]) => {
    if (['span', 'sourceSpan', 'nameSpan', 'argumentSpan'].includes(key)) {
      return false;
    }

    return Array.isArray(value)
      ? value.some((candidate) => containsEventReference(candidate, visited))
      : containsEventReference(value, visited);
  });
}

function isDelegatedEventHandler(handler) {
  const root = handler?.ast ?? handler;
  const actions = root?.constructor?.name === 'Chain'
    ? root.expressions ?? []
    : [root];
  const action = actions.length === 1 ? actions[0] : null;

  return (action?.constructor?.name === 'Call' || action?.constructor?.name === 'SafeCall')
    && (action.args ?? []).some((argument) => containsEventReference(argument));
}

function isEventControlAction(expression) {
  const unwrapped = expression?.constructor?.name === 'ParenthesizedExpression'
    ? expression.expression
    : expression;
  if (unwrapped?.constructor?.name !== 'Call') {
    return false;
  }

  const receiver = unwrapped.receiver;
  return receiver?.constructor?.name === 'PropertyRead'
    && ['preventDefault', 'stopPropagation', 'stopImmediatePropagation'].includes(receiver.name)
    && receiver.receiver?.constructor?.name === 'PropertyRead'
    && receiver.receiver.name === '$event'
    && receiver.receiver.receiver?.constructor?.name === 'ImplicitReceiver';
}

function actionSequencesAreEqual(left, right) {
  return left.length === right.length && left.every((value, index) => value === right[index]);
}

function activationKeyForEvent(eventName) {
  if (eventName.endsWith('.enter')) {
    return 'enter';
  }

  if (eventName.endsWith('.space') || eventName.endsWith('.spacebar')) {
    return 'space';
  }

  return null;
}

function hasKeyboardActivation(node) {
  const outputs = node.outputs ?? [];
  const hasRouterLink = attributeNames(node).has('routerlink');
  const clickActions = outputs
    .filter((output) => output.name.toLowerCase() === 'click')
    .flatMap((output) => topLevelActions(output.handler))
    .filter((action) => !isEventControlAction(action))
    .map((action) => expressionSignature(action));
  const keyboardOutputs = outputs.filter((output) => {
    const eventName = output.name.toLowerCase();
    return ACTIVATION_KEYBOARD_EVENT_NAMES.has(eventName) || BARE_KEYBOARD_EVENT_NAMES.has(eventName);
  });

  const validKeyboardHandlers = keyboardOutputs.length > 0 && keyboardOutputs.every((output) => {
    const eventName = output.name.toLowerCase();

    if (BARE_KEYBOARD_EVENT_NAMES.has(eventName)) {
      return isDelegatedEventHandler(output.handler);
    }

    const handlerSource = output?.handler?.source?.trim() ?? '';
    const keyboardActions = topLevelActions(output.handler);
    const keyboardActionSignatures = keyboardActions
      .filter((action) => !isEventControlAction(action))
      .map((action) => expressionSignature(action));
    if (handlerSource.length === 0 || keyboardActions.length === 0) {
      return false;
    }

    if (actionSequencesAreEqual(keyboardActionSignatures, clickActions)) {
      return clickActions.length > 0 || !hasRouterLink;
    }

    if (hasRouterLink && clickActions.length === 0 && keyboardActionSignatures.length > 0) {
      return true;
    }

    return isDelegatedEventHandler(output.handler);
  });
  if (!validKeyboardHandlers) {
    return false;
  }

  const hasDelegatedBareHandler = keyboardOutputs.some((output) => (
    BARE_KEYBOARD_EVENT_NAMES.has(output.name.toLowerCase())
    && isDelegatedEventHandler(output.handler)
  ));
  if (hasDelegatedBareHandler) {
    return true;
  }

  const coveredKeys = new Set(keyboardOutputs
    .map((output) => activationKeyForEvent(output.name.toLowerCase()))
    .filter(Boolean));
  const roles = interactiveRoleCandidates(node);
  if (roles.hasUnknown || roles.roles.length === 0) {
    return false;
  }

  return roles.roles.every((role) => {
    const requiredKeyGroups = INTERACTIVE_ROLE_REQUIRED_KEYS.get(role);
    return requiredKeyGroups
      ? requiredKeyGroups.every((alternatives) => (
        alternatives.some((key) => coveredKeys.has(key))
      ))
      : false;
  });
}

function hasActionableClick(node) {
  return (node.outputs ?? [])
    .filter((output) => output.name.toLowerCase() === 'click')
    .some((output) => topLevelActions(output.handler).some((action) => !isEventControlAction(action)));
}

function hasDeclaredImageAlternative(node) {
  if (staticAttributes(node).some((attribute) => attribute.name.toLowerCase() === 'alt')) {
    return true;
  }

  return (node.inputs ?? []).some((input) => {
    if (!isAttributeBinding(input) || input.name.toLowerCase() !== 'alt') {
      return false;
    }

    return everyReachableStaticValueMatches(
      input,
      (value) => value !== null && value !== undefined
    );
  });
}

function hasReachableTabIndex(node) {
  const staticTabIndex = attributeValue(node, 'tabindex');

  if (staticTabIndex !== null) {
    const parsedTabIndex = Number(staticTabIndex.trim());
    return staticTabIndex.trim().length > 0 && Number.isInteger(parsedTabIndex) && parsedTabIndex >= 0;
  }

  const boundTabIndex = (node.inputs ?? []).find((input) => (
    isAttributeBinding(input) && input.name.toLowerCase() === 'tabindex'
  ));
  if (!boundTabIndex) {
    return false;
  }

  return everyReachableStaticValueMatches(boundTabIndex, (value) => {
    const parsedTabIndex = Number(value);
    return Number.isInteger(parsedTabIndex) && parsedTabIndex >= 0;
  });
}

function hasInteractiveRole(node) {
  const candidates = interactiveRoleCandidates(node);
  return !candidates.hasUnknown
    && candidates.roles.length > 0
    && candidates.roles.every((role) => INTERACTIVE_ROLE_REQUIRED_KEYS.has(role));
}

function interactiveRoleCandidates(node) {
  const staticRole = attributeValue(node, 'role');
  if (typeof staticRole === 'string') {
    return { roles: [staticRole.trim().toLowerCase()], hasUnknown: false };
  }

  const boundRole = (node.inputs ?? []).find((input) => (
    isAttributeBinding(input) && input.name.toLowerCase() === 'role'
  ));
  if (!boundRole) {
    return { roles: [], hasUnknown: true };
  }

  const outcome = boundReachableStaticValues(boundRole);
  return {
    roles: outcome.values
      .filter((value) => typeof value === 'string')
      .map((value) => value.trim().toLowerCase()),
    hasUnknown: outcome.hasUnknown || outcome.values.some((value) => typeof value !== 'string')
  };
}

function controlFlowScopeIdentity(node, context) {
  const reachability = context.reachabilityByNode.get(node) ?? new Map();
  return [...reachability].map(([owner, branch]) => {
    const ownerHeader = context.controlFlowIdentityByOwner.get(owner)
      ?? owner.startSourceSpan?.toString?.()
      ?? owner.expression?.source
      ?? owner.constructor?.name
      ?? 'control-flow';
    const branchHeader = branch.startSourceSpan?.toString?.()
      ?? branch.expression?.source
      ?? (branch.expression === null ? 'default' : branch.constructor?.name)
      ?? 'branch';
    return `${ownerHeader}::${branchHeader}`.replace(/\s+/g, ' ').trim();
  }).join(' > ');
}

function enclosingElementScopeIdentity(node, context) {
  const identities = [];
  let ancestor = context.parentByNode.get(node);
  while (ancestor) {
    const identity = context.structuralIdentityByNode.get(ancestor);
    if (identity) {
      identities.unshift(identity);
    }

    ancestor = context.parentByNode.get(ancestor);
  }

  return identities.join(' > ');
}

function findingScopeIdentity(node, context) {
  return [
    enclosingElementScopeIdentity(node, context),
    controlFlowScopeIdentity(node, context)
  ].filter((identity) => identity.length > 0).join(' > ');
}

function createFinding(rule, message, node, template, source, lineOffset, locator, context) {
  const sourceLine = node.sourceSpan?.start?.line ?? 0;
  const sourceStart = node.sourceSpan?.start?.offset ?? 0;
  const sourceEnd = node.sourceSpan?.end?.offset ?? sourceStart;

  return {
    rule,
    message,
    path: source,
    line: sourceLine + 1 + lineOffset,
    element: node.name ?? node.tag ?? 'template',
    context: startTagSource(node, template),
    identity: template.slice(sourceStart, sourceEnd).replace(/\s+/g, ' ').trim(),
    scopeIdentity: findingScopeIdentity(node, context),
    locator
  };
}

function analyseElement(node, template, source, lineOffset, locator, context) {
  const findings = [];
  const name = elementName(node);
  const names = attributeNames(node);
  const outputs = outputNames(node);
  const hasInteractiveSemantics = hasInteractiveRole(node);

  if (name === 'img' && !hasDeclaredImageAlternative(node)) {
    findings.push(createFinding(
      'image-alt',
      'Every image must declare an alt attribute, including an empty alt for decorative images.',
      node,
      template,
      source,
      lineOffset,
      locator,
      context
    ));
  }

  const isNamedInteractive = name === 'button'
    || (name === 'a' && (
      hasUsableLinkTarget(node, 'href')
      || hasUsableLinkTarget(node, 'routerlink')
      || hasActionableClick(node)
    ))
    || (name === 'input' && (() => {
      const candidates = inputTypeCandidates(node);
      return candidates.hasUnknown
        || candidates.types.some((type) => ['button', 'submit', 'reset', 'image'].includes(type));
    })())
    || hasInteractiveSemantics;

  if (isNamedInteractive && !hasAccessibleName(node, context)) {
    findings.push(createFinding(
      'interactive-name',
      'Interactive elements must expose visible text, projected text or an ARIA name.',
      node,
      template,
      source,
      lineOffset,
      locator,
      context
    ));
  }

  const isCustomElement = name.includes('-');
  const hasPointerInteraction = hasActionableClick(node)
    || hasPotentiallyUsableLinkTarget(node, 'routerlink');
  const hasNativePointerInteraction = isNativeInteractive(node)
    || (
      name === 'a'
      && !hasActionableClick(node)
      && hasPotentiallyUsableLinkTarget(node, 'routerlink')
    );
  if (hasPointerInteraction && !hasNativePointerInteraction && !isCustomElement) {
    const hasKeyboardHandler = hasKeyboardActivation(node);
    const hasKeyboardFocus = hasReachableTabIndex(node);

    if (!hasKeyboardHandler || !hasInteractiveSemantics || !hasKeyboardFocus) {
      findings.push(createFinding(
        'click-keyboard',
        'Non-native click targets must provide keyboard handling and interactive semantics.',
        node,
        template,
        source,
        lineOffset,
        locator,
        context
      ));
    }
  }

  return findings;
}

function nestedNodeCollections(node) {
  const collections = [];

  if (Array.isArray(node.children)) {
    collections.push(node.children);
  }

  for (const branch of node.branches ?? []) {
    if (Array.isArray(branch.children)) {
      collections.push(branch.children);
    }
  }

  for (const group of node.groups ?? []) {
    if (Array.isArray(group.children)) {
      collections.push(group.children);
    }
  }

  const icuNodes = node.cases
    ? [node]
    : (node.i18n?.nodes ?? []).filter((candidate) => candidate?.constructor?.name === 'Icu');
  for (const icu of icuNodes) {
    for (const candidate of Object.values(icu.cases ?? {})) {
      if (Array.isArray(candidate.children)) {
        collections.push(candidate.children);
      }
    }
  }

  if (Array.isArray(node.empty?.children)) {
    collections.push(node.empty.children);
  }

  for (const block of [node.mainBlock, node.placeholder, node.loading, node.error]) {
    if (Array.isArray(block?.children)) {
      collections.push(block.children);
    }
  }

  return collections;
}

function reachableNestedNodeCollections(node) {
  const nodeType = node.constructor?.name ?? '';
  if (nodeType === 'IfBlock') {
    return reachableIfBranches(node).reachable
      .map((branch) => branch.children)
      .filter(Array.isArray);
  }

  if (nodeType === 'SwitchBlock') {
    const reachable = reachableSwitchGroups(node);
    if (reachable) {
      return reachable.reachable
        .map((group) => group.children)
        .filter(Array.isArray);
    }
  }

  return nestedNodeCollections(node);
}

function reachableScopedNodeCollections(node) {
  const nodeType = node.constructor?.name ?? '';
  if (nodeType === 'IfBlock') {
    return reachableIfBranches(node).reachable
      .filter((branch) => Array.isArray(branch.children))
      .map((branch) => ({ collection: branch.children, owner: node, branch }));
  }

  if (nodeType === 'SwitchBlock') {
    const reachable = reachableSwitchGroups(node);
    if (reachable) {
      return reachable.reachable
        .filter((group) => Array.isArray(group.children))
        .map((group) => ({ collection: group.children, owner: node, branch: group }));
    }
  }

  if (nodeType === 'ForLoopBlock') {
    return [
      ...(Array.isArray(node.children)
        ? [{ collection: node.children, owner: node, branch: node }]
        : []),
      ...(Array.isArray(node.empty?.children)
        ? [{ collection: node.empty.children, owner: node, branch: node.empty }]
        : [])
    ];
  }

  if (nodeType === 'DeferredBlock') {
    return [
      ...(Array.isArray(node.children)
        ? [{ collection: node.children, owner: node, branch: node }]
        : []),
      ...[node.placeholder, node.loading, node.error]
        .filter((block) => Array.isArray(block?.children))
        .map((block) => ({ collection: block.children, owner: node, branch: block }))
    ];
  }

  return nestedNodeCollections(node).map((collection) => ({ collection }));
}

export function analyseTemplate(template, source = 'inline-template', lineOffset = 0) {
  const parsed = parseTemplate(template, source, { preserveWhitespaces: true });
  const findings = [];
  const context = {
    controlFlowIdentityByOwner: new Map(),
    nodesById: new Map(),
    idsByNode: new Map(),
    labelsByTargetId: new Map(),
    parentByNode: new Map(),
    structuralIdentityByNode: new Map(),
    templateScopeByNode: new Map(),
    templatesByReference: new Map(),
    reachabilityByNode: new Map()
  };
  const controlFlowHeaderOccurrences = new Map();
  const structuralHeaderOccurrences = new Map();

  function indexNodes(
    nodes,
    indexed = new Set(),
    reachability = new Map(),
    parent = null,
    templateScope = null
  ) {
    for (const node of nodes) {
      if (indexed.has(node)) {
        continue;
      }

      indexed.add(node);
      context.reachabilityByNode.set(node, reachability);
      context.templateScopeByNode.set(node, templateScope);
      if (parent) {
        context.parentByNode.set(node, parent);
      }

      const nodeType = node.constructor?.name ?? '';
      if (['IfBlock', 'SwitchBlock', 'ForLoopBlock', 'DeferredBlock'].includes(nodeType)) {
        const header = (node.startSourceSpan?.toString?.()
          ?? node.expression?.source
          ?? nodeType).replace(/\s+/g, ' ').trim();
        const occurrence = (controlFlowHeaderOccurrences.get(header) ?? 0) + 1;
        controlFlowHeaderOccurrences.set(header, occurrence);
        context.controlFlowIdentityByOwner.set(node, `${header}#${occurrence}`);
      }

      if (elementName(node).length > 0) {
        const header = startTagSource(node, template);
        const occurrence = (structuralHeaderOccurrences.get(header) ?? 0) + 1;
        structuralHeaderOccurrences.set(header, occurrence);
        context.structuralIdentityByNode.set(node, `${header}#${occurrence}`);
      }

      const ids = new Set();
      const staticId = attributeValue(node, 'id');
      if (typeof staticId === 'string') {
        ids.add(staticId);
      }

      context.idsByNode.set(node, ids);

      for (const input of node.inputs ?? []) {
        if (isAttributeBinding(input) && input.name.toLowerCase() === 'id') {
          const outcome = boundReachableStaticValues(input);
          for (const value of outcome.values) {
            if (typeof value === 'string') {
              ids.add(value);
            }
          }
        }
      }

      for (const id of ids) {
        const normalizedId = id.trim();
        if (normalizedId.length === 0) {
          continue;
        }

        const indexedNodes = context.nodesById.get(normalizedId) ?? [];
        indexedNodes.push(node);
        context.nodesById.set(normalizedId, indexedNodes);
      }

      if (elementName(node) === 'label') {
        const targets = new Set();
        const staticTarget = attributeValue(node, 'for');
        if (typeof staticTarget === 'string') {
          targets.add(staticTarget);
        }

        for (const input of node.inputs ?? []) {
          if (
            isAttributeBinding(input)
            && ['for', 'htmlfor'].includes(input.name.toLowerCase())
          ) {
            const outcome = boundReachableStaticValues(input);
            for (const value of outcome.values) {
              if (typeof value === 'string') {
                targets.add(value);
              }
            }
          }
        }

        for (const target of targets) {
          const normalizedTarget = target.trim();
          if (normalizedTarget.length === 0) {
            continue;
          }

          const indexedLabels = context.labelsByTargetId.get(normalizedTarget) ?? [];
          indexedLabels.push(node);
          context.labelsByTargetId.set(normalizedTarget, indexedLabels);
        }
      }

      for (const reference of node.references ?? []) {
        if (typeof reference.name === 'string' && reference.name.length > 0) {
          const referencedTemplates = context.templatesByReference.get(reference.name) ?? [];
          referencedTemplates.push(node);
          context.templatesByReference.set(reference.name, referencedTemplates);
        }
      }

      for (const scoped of reachableScopedNodeCollections(node)) {
        const nestedReachability = new Map(reachability);
        if (scoped.owner) {
          nestedReachability.set(scoped.owner, scoped.branch);
        }

        const nestedTemplateScope = nodeType === 'Template' && elementName(node) === 'ng-template'
          ? node
          : templateScope;
        indexNodes(scoped.collection, indexed, nestedReachability, node, nestedTemplateScope);
      }
    }
  }

  indexNodes(parsed.nodes);

  for (const error of parsed.errors ?? []) {
    findings.push({
      rule: 'template-parse',
      message: error.msg ?? String(error),
      path: source,
      line: (error.span?.start?.line ?? 0) + 1 + lineOffset,
      element: 'template',
      context: error.span?.toString?.() ?? 'parse-error'
    });
  }

  function visit(nodes, parentLocator = 'root') {
    let structuralIndex = 0;

    for (const node of nodes) {
      const nodeType = node.constructor?.name ?? 'Node';
      const isStructural = nodeType !== 'Text' && nodeType !== 'BoundText' && nodeType !== 'Content';
      const locator = isStructural ? `${parentLocator}/${nodeType}:${structuralIndex}` : parentLocator;
      if (isStructural) {
        structuralIndex += 1;
      }

      if (elementName(node).length > 0) {
        findings.push(...analyseElement(node, template, source, lineOffset, locator, context));
      }

      for (const [collectionIndex, collection] of reachableNestedNodeCollections(node).entries()) {
        visit(collection, `${locator}/children:${collectionIndex}`);
      }
    }
  }

  visit(parsed.nodes);
  return findings;
}

function staticStringValue(node, constants, resolving = new Set()) {
  if (ts.isStringLiteral(node) || ts.isNoSubstitutionTemplateLiteral(node)) {
    return node.text;
  }

  if (ts.isParenthesizedExpression(node)) {
    return staticStringValue(node.expression, constants, resolving);
  }

  if (ts.isBinaryExpression(node) && node.operatorToken.kind === ts.SyntaxKind.PlusToken) {
    const left = staticStringValue(node.left, constants, resolving);
    const right = staticStringValue(node.right, constants, resolving);
    return left === null || right === null ? null : `${left}${right}`;
  }

  if (ts.isTemplateExpression(node)) {
    let value = node.head.text;

    for (const span of node.templateSpans) {
      const expression = staticStringValue(span.expression, constants, resolving);
      if (expression === null) {
        return null;
      }

      value += expression;
      value += span.literal.text;
    }

    return value;
  }

  if (ts.isIdentifier(node)) {
    const name = node.text;
    if (resolving.has(name) || !constants.has(name)) {
      return null;
    }

    resolving.add(name);
    const value = staticStringValue(constants.get(name), constants, resolving);
    resolving.delete(name);
    return value;
  }

  if (
    ts.isAsExpression(node)
    || ts.isTypeAssertionExpression(node)
    || (typeof ts.isSatisfiesExpression === 'function' && ts.isSatisfiesExpression(node))
  ) {
    return staticStringValue(node.expression, constants, resolving);
  }

  return null;
}

function componentInlineTemplates(filePath) {
  const sourceText = fs.readFileSync(filePath, 'utf8');
  const sourceFile = ts.createSourceFile(filePath, sourceText, ts.ScriptTarget.Latest, true, ts.ScriptKind.TS);
  const templates = [];
  const unsupported = [];
  const constants = new Map();
  const componentDecoratorNames = new Set();
  const angularCoreNamespaces = new Set();

  for (const statement of sourceFile.statements) {
    if (
      ts.isImportDeclaration(statement)
      && ts.isStringLiteral(statement.moduleSpecifier)
      && statement.moduleSpecifier.text === '@angular/core'
    ) {
      const namedBindings = statement.importClause?.namedBindings;

      if (namedBindings && ts.isNamedImports(namedBindings)) {
        for (const element of namedBindings.elements) {
          const importedName = element.propertyName?.text ?? element.name.text;
          if (importedName === 'Component') {
            componentDecoratorNames.add(element.name.text);
          }
        }
      } else if (namedBindings && ts.isNamespaceImport(namedBindings)) {
        angularCoreNamespaces.add(namedBindings.name.text);
      }
    }

    if (!ts.isVariableStatement(statement) || (statement.declarationList.flags & ts.NodeFlags.Const) === 0) {
      continue;
    }

    for (const declaration of statement.declarationList.declarations) {
      if (ts.isIdentifier(declaration.name) && declaration.initializer) {
        constants.set(declaration.name.text, declaration.initializer);
      }
    }
  }

  function isAngularComponentDecorator(node) {
    if (!ts.isCallExpression(node)) {
      return false;
    }

    if (ts.isIdentifier(node.expression)) {
      return componentDecoratorNames.has(node.expression.text);
    }

    return ts.isPropertyAccessExpression(node.expression)
      && node.expression.name.text === 'Component'
      && ts.isIdentifier(node.expression.expression)
      && angularCoreNamespaces.has(node.expression.expression.text);
  }

  function staticPropertyName(name) {
    if (ts.isIdentifier(name) || ts.isStringLiteral(name) || ts.isNumericLiteral(name)) {
      return name.text;
    }

    if (ts.isComputedPropertyName(name)) {
      return staticStringValue(name.expression, constants);
    }

    return null;
  }

  function staticObjectLiteral(node, resolving = new Set()) {
    if (ts.isObjectLiteralExpression(node)) {
      return node;
    }

    if (ts.isParenthesizedExpression(node) || ts.isAsExpression(node)) {
      return staticObjectLiteral(node.expression, resolving);
    }

    if (ts.isIdentifier(node) && constants.has(node.text) && !resolving.has(node.text)) {
      resolving.add(node.text);
      const value = staticObjectLiteral(constants.get(node.text), resolving);
      resolving.delete(node.text);
      return value;
    }

    return null;
  }

  function reportUnsupported(node) {
    const start = sourceFile.getLineAndCharacterOfPosition(node.getStart(sourceFile));
    unsupported.push({
      line: start.line + 1,
      context: node.getText(sourceFile).replace(/\s+/g, ' ').trim()
    });
  }

  function visit(node) {
    if (isAngularComponentDecorator(node)) {
      const metadataArgument = node.arguments[0];
      const metadata = metadataArgument ? staticObjectLiteral(metadataArgument) : null;

      if (!metadata && metadataArgument) {
        reportUnsupported(metadataArgument);
      } else if (metadata) {
        for (const property of metadata.properties) {
          if (ts.isSpreadAssignment(property)) {
            reportUnsupported(property);
            continue;
          }

          if (ts.isShorthandPropertyAssignment(property) && property.name.text === 'template') {
            const initializer = constants.get(property.name.text);
            const start = sourceFile.getLineAndCharacterOfPosition(property.getStart(sourceFile));
            const template = initializer ? staticStringValue(initializer, constants) : null;

            if (template === null) {
              reportUnsupported(property);
            } else {
              templates.push({ template, lineOffset: start.line });
            }
            continue;
          }

          if (!ts.isPropertyAssignment(property)) {
            continue;
          }

          const propertyName = staticPropertyName(property.name);
          if (propertyName === null) {
            reportUnsupported(property.name);
            continue;
          }

          if (propertyName !== 'template') {
            continue;
          }

          const start = sourceFile.getLineAndCharacterOfPosition(property.initializer.getStart(sourceFile));
          const template = staticStringValue(property.initializer, constants);

          if (template === null) {
            reportUnsupported(property);
          } else {
            templates.push({ template, lineOffset: start.line });
          }
        }
      }
    }

    ts.forEachChild(node, visit);
  }

  visit(sourceFile);
  return { templates, unsupported };
}

export function fingerprintFindings(findings) {
  const occurrences = new Map();

  return findings.map((finding) => {
    const normalizedIdentity = (finding.identity ?? finding.context).replace(/\s+/g, ' ').trim();
    const digest = crypto.createHash('sha256').update(normalizedIdentity).digest('hex').slice(0, 12);
    const normalizedScope = finding.scopeIdentity?.replace(/\s+/g, ' ').trim() ?? '';
    const scopeKey = normalizedScope.length > 0
      ? `|${crypto.createHash('sha256').update(normalizedScope).digest('hex').slice(0, 12)}`
      : '';
    const occurrenceKey = `${finding.rule}|${finding.path}${scopeKey}|${digest}`;
    const occurrence = (occurrences.get(occurrenceKey) ?? 0) + 1;
    occurrences.set(occurrenceKey, occurrence);

    return {
      fingerprint: `${occurrenceKey}|${occurrence}`,
      rule: finding.rule,
      path: finding.path,
      line: finding.line,
      message: finding.message
    };
  }).sort((left, right) => left.fingerprint.localeCompare(right.fingerprint, 'en'));
}

export function compareBaseline(currentFindings, baselineFindings) {
  const currentByFingerprint = new Map(currentFindings.map((finding) => [finding.fingerprint, finding]));
  const baselineByFingerprint = new Map(baselineFindings.map((finding) => [finding.fingerprint, finding]));

  return {
    added: currentFindings.filter((finding) => !baselineByFingerprint.has(finding.fingerprint)),
    resolved: baselineFindings.filter((finding) => !currentByFingerprint.has(finding.fingerprint))
  };
}

export function scanApplication(applicationRoot = APPLICATION_ROOT, frontendRoot = FRONTEND_ROOT) {
  const findings = [];
  const htmlFiles = walkFiles(
    applicationRoot,
    (filePath) => filePath.endsWith('.html')
      && !filePath.endsWith('.spec.html')
      && !filePath.includes(`${path.sep}test-helpers${path.sep}`)
  );
  const componentFiles = walkFiles(
    applicationRoot,
    (filePath) => filePath.endsWith('.ts')
      && !filePath.endsWith('.spec.ts')
      && !filePath.includes(`${path.sep}test-helpers${path.sep}`)
  );

  for (const filePath of htmlFiles) {
    const relativePath = path.relative(frontendRoot, filePath).replaceAll('\\', '/');
    findings.push(...analyseTemplate(fs.readFileSync(filePath, 'utf8'), relativePath));
  }

  for (const filePath of componentFiles) {
    const relativePath = path.relative(frontendRoot, filePath).replaceAll('\\', '/');
    const inlineTemplates = componentInlineTemplates(filePath);

    for (const [index, candidate] of inlineTemplates.templates.entries()) {
      findings.push(...analyseTemplate(
        candidate.template,
        `${relativePath}#inline-${index + 1}`,
        candidate.lineOffset
      ));
    }

    for (const [index, candidate] of inlineTemplates.unsupported.entries()) {
      findings.push({
        rule: 'inline-template-analysis',
        path: relativePath,
        line: candidate.line,
        context: candidate.context,
        identity: candidate.context,
        locator: `inline-template:${index + 1}`,
        message: 'Inline Angular template expression cannot be analysed statically.'
      });
    }
  }

  return fingerprintFindings(findings);
}

function run() {
  const currentFindings = scanApplication();

  if (process.argv.includes('--update-baseline')) {
    const baseline = { version: 3, findings: currentFindings };
    fs.writeFileSync(BASELINE_PATH, `${JSON.stringify(baseline, null, 2)}\n`, 'utf8');
    console.log(`Accessibility baseline updated with ${currentFindings.length} known findings.`);
    return;
  }

  const baseline = JSON.parse(fs.readFileSync(BASELINE_PATH, 'utf8'));
  const comparison = compareBaseline(currentFindings, baseline.findings ?? []);

  for (const finding of comparison.added) {
    console.error(`[accessibility:new] ${finding.path}:${finding.line} ${finding.rule} — ${finding.message}`);
  }

  for (const finding of comparison.resolved) {
    console.error(`[accessibility:baseline] Resolved finding still present in baseline: ${finding.fingerprint}`);
  }

  if (comparison.added.length > 0 || comparison.resolved.length > 0) {
    console.error('Accessibility baseline changed. Fix new regressions and refresh the baseline only for intentional debt reduction.');
    process.exit(1);
  }

  console.log(`Accessibility check passed with ${currentFindings.length} tracked legacy findings and no regression.`);
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  run();
}
