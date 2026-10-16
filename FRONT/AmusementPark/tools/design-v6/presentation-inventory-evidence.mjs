import ts from 'typescript';
import { parseTemplate } from '@angular/compiler';

export function isProductionPresentationFile(filename) {
  const normalized = filename.replaceAll('\\', '/');
  return normalized.endsWith('.ts') && !normalized.endsWith('.spec.ts')
    && !/\/(?:test-helpers|testing|__tests__|fixtures)\//.test(normalized);
}

export function componentPresentations(source, filename = 'review.ts') {
  const parsed = ts.createSourceFile(filename, source, ts.ScriptTarget.Latest, true, ts.ScriptKind.TS);
  if (parsed.parseDiagnostics.length) throw new Error(`Cannot parse component source: ${filename}`);
  const result = [];
  const literal = node => {
    if (!ts.isStringLiteral(node) && !ts.isNoSubstitutionTemplateLiteral(node)) throw new Error(`Dynamic component presentation: ${filename}`);
    return node.text;
  };
  for (const declaration of parsed.statements.filter(ts.isClassDeclaration)) {
    for (const decorator of ts.getDecorators(declaration) ?? []) {
      const call = decorator.expression;
      if (!ts.isCallExpression(call) || !ts.isIdentifier(call.expression) || call.expression.text !== 'Component') continue;
      const metadata = call.arguments[0];
      if (!metadata || !ts.isObjectLiteralExpression(metadata)) throw new Error(`Dynamic component metadata: ${filename}`);
      const property = name => metadata.properties.find(value => ts.isPropertyAssignment(value)
        && (ts.isIdentifier(value.name) || ts.isStringLiteral(value.name)) && value.name.text === name)?.initializer;
      const string = name => property(name) ? literal(property(name)) : null;
      const strings = name => {
        const value = property(name);
        if (!value) return [];
        return ts.isArrayLiteralExpression(value) ? value.elements.map(literal) : [literal(value)];
      };
      const template = property('template');
      if (template && !ts.isStringLiteral(template) && !ts.isNoSubstitutionTemplateLiteral(template)) throw new Error(`Dynamic inline component template: ${filename}`);
      result.push({ name: declaration.name?.text ?? 'anonymous', selector: string('selector'),
        template: template ? literal(template) : null, templateUrl: string('templateUrl'),
        styleUrls: [...strings('styleUrls'), ...strings('styleUrl')], inlineStyles: strings('styles') });
    }
  }
  return result;
}

export function presentationInteractions(source, filename = 'review.html') {
  const parsed = parseTemplate(source, filename, { preserveWhitespaces: false });
  if (parsed.errors?.length) throw new Error(`Cannot inventory template: ${filename}: ${parsed.errors[0]}`);
  const rows = [];
  const expression = value => (typeof value === 'string' ? value : value?.source ?? '').replace(/\s+/g, ' ').trim();
  const textContent = node => ['Text', 'BoundText'].includes(node.constructor.name) ? expression(node.value)
    : ['children', 'branches', 'cases'].flatMap(key => (node[key] ?? []).map(textContent)).join(' ');
  function walk(node, inherited = []) {
    const kind = node.constructor.name;
    const desugared = kind === 'Template' && node.tagName !== 'ng-template';
    const tag = node.name ?? node.tagName;
    const conditions = [...inherited];
    if (node.expression) conditions.push(`${kind}: ${expression(node.expression) || 'else'}`);
    if (kind === 'IfBlockBranch' && !node.expression) conditions.push('else');
    for (const attribute of node.templateAttrs ?? []) {
      if (attribute.name !== 'ngIf' || expression(attribute.value)) conditions.push(`${attribute.name}: ${expression(attribute.value)}`);
    }
    const attributes = desugared ? [] : node.attributes ?? [];
    const inputs = desugared ? [] : node.inputs ?? [];
    const outputs = desugared ? [] : node.outputs ?? [];
    const get = name => {
      const input = inputs.find(value => value.name === name);
      const attribute = attributes.find(value => value.name === name);
      return !input && attribute && name === 'disabled' && attribute.value === '' ? 'true' : expression(input?.value ?? attribute?.value);
    };
    if (!desugared && (['button', 'a', 'form', 'input', 'select', 'textarea', 'app-ui-button'].includes(tag) || outputs.length || get('routerLink'))) {
      rows.push({ type: tag, label: get('aria-label') || get('label') || get('placeholder') || get('title') || get('name') || textContent(node).replace(/\s+/g, ' ').trim().slice(0, 180),
        click: expression(outputs.find(value => value.name === 'click')?.handler),
        submit: expression(outputs.find(value => value.name === 'ngSubmit')?.handler),
        routerLink: get('routerLink'), href: get('href'), disabled: get('disabled'),
        visibility: [...conditions, ...(get('hidden') ? [`hidden: ${get('hidden')}`] : [])].join(' | '),
        events: JSON.stringify(outputs.map(value => ({ name: value.name, handler: expression(value.handler) }))),
        bindings: JSON.stringify([...attributes.map(value => ({ kind: 'attribute', name: value.name, value: expression(value.value) })),
          ...inputs.map(value => ({ kind: 'input', name: value.name, value: expression(value.value) }))]),
        line: node.sourceSpan?.start.line + 1 });
    }
    for (const key of ['children', 'branches', 'cases']) for (const child of node[key] ?? []) walk(child, conditions);
    for (const key of ['placeholder', 'loading', 'error', 'empty']) if (node[key]) walk(node[key], [...conditions, key]);
  }
  parsed.nodes.forEach(node => walk(node));
  return rows;
}
