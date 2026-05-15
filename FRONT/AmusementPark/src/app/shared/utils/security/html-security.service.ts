import { DOCUMENT } from '@angular/common';
import { Inject, Injectable } from '@angular/core';

import { UrlSecurityService } from './url-security.service';

const ALLOWED_ELEMENTS: Set<string> = new Set<string>([
  'a', 'b', 'blockquote', 'br', 'code', 'div', 'em', 'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'hr', 'i',
  'img', 'li', 'ol', 'p', 'pre', 's', 'span', 'strong', 'sub', 'sup', 'table', 'tbody', 'td', 'th',
  'thead', 'tr', 'u', 'ul'
]);

const DROP_WITH_CONTENT_ELEMENTS: Set<string> = new Set<string>([
  'base', 'button', 'embed', 'form', 'iframe', 'input', 'link', 'meta', 'object', 'script', 'style', 'svg', 'textarea'
]);

const ALLOWED_CLASS_PATTERN: RegExp = /^(ql-(align|indent|size|direction|font|color|background)(-[a-z0-9_-]+)?|rich-text__[a-z0-9_-]+)$/i;
const COLOR_PATTERN: RegExp = /^(#[0-9a-f]{3,8}|rgba?\(\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}(\s*,\s*(0|1|0?\.\d+))?\s*\)|hsla?\(\s*\d{1,3}\s*,\s*\d{1,3}%\s*,\s*\d{1,3}%(\s*,\s*(0|1|0?\.\d+))?\s*\))$/i;
const SIZE_PATTERN: RegExp = /^\d{1,3}(\.\d{1,2})?(px|em|rem|%)$/i;
const SPACING_PATTERN: RegExp = /^\d{1,3}(\.\d{1,2})?(px|em|rem|%)$/i;

@Injectable({
  providedIn: 'root'
})
export class HtmlSecurityService {
  constructor(
    @Inject(DOCUMENT) private readonly documentRef: Document,
    private readonly urlSecurityService: UrlSecurityService
  ) {
  }

  sanitizeRichHtml(value: string | null | undefined): string {
    const rawValue: string = value?.trim() ?? '';
    if (!rawValue) {
      return '';
    }

    const workingDocument: Document = this.createWorkingDocument();
    const template: HTMLTemplateElement = workingDocument.createElement('template');
    template.innerHTML = rawValue;

    this.sanitizeChildren(template.content);
    return template.innerHTML;
  }

  private createWorkingDocument(): Document {
    return this.documentRef.implementation?.createHTMLDocument('rich-html-sanitizer') ?? this.documentRef;
  }

  private sanitizeChildren(parent: Node): void {
    const childNodes: Node[] = Array.from(parent.childNodes);
    for (const childNode of childNodes) {
      this.sanitizeNode(parent, childNode);
    }
  }

  private sanitizeNode(parent: Node, node: Node): void {
    if (node.nodeType === 8) {
      parent.removeChild(node);
      return;
    }

    if (node.nodeType === 3) {
      return;
    }

    if (node.nodeType !== 1) {
      parent.removeChild(node);
      return;
    }

    const element: HTMLElement = node as HTMLElement;
    const tagName: string = element.tagName.toLowerCase();

    if (DROP_WITH_CONTENT_ELEMENTS.has(tagName)) {
      parent.removeChild(element);
      return;
    }

    if (!ALLOWED_ELEMENTS.has(tagName)) {
      this.unwrapElement(parent, element);
      return;
    }

    this.sanitizeElementAttributes(element, tagName);
    this.sanitizeChildren(element);
  }

  private unwrapElement(parent: Node, element: HTMLElement): void {
    const childNodes: Node[] = Array.from(element.childNodes);
    for (const childNode of childNodes) {
      parent.insertBefore(childNode, element);
      this.sanitizeNode(parent, childNode);
    }

    if (element.parentNode === parent) {
      parent.removeChild(element);
    }
  }

  private sanitizeElementAttributes(element: HTMLElement, tagName: string): void {
    const attributes: Attr[] = Array.from(element.attributes);
    for (const attribute of attributes) {
      const attributeName: string = attribute.name.toLowerCase();
      const attributeValue: string = attribute.value;

      if (attributeName.startsWith('on')) {
        element.removeAttribute(attribute.name);
        continue;
      }

      if (attributeName === 'class') {
        this.sanitizeClassAttribute(element, attributeValue);
        continue;
      }

      if (attributeName === 'style') {
        this.sanitizeStyleAttribute(element, attributeValue);
        continue;
      }

      if (tagName === 'a' && attributeName === 'href') {
        this.sanitizeAnchorHref(element as HTMLAnchorElement, attributeValue);
        continue;
      }

      if (tagName === 'img' && attributeName === 'src') {
        this.sanitizeImageSource(element as HTMLImageElement, attributeValue);
        continue;
      }

      if (this.isAllowedSpecificAttribute(tagName, attributeName)) {
        continue;
      }

      element.removeAttribute(attribute.name);
    }

    if (tagName === 'a' && element.hasAttribute('href')) {
      element.setAttribute('target', '_blank');
      element.setAttribute('rel', 'noopener noreferrer nofollow');
    }

    if (tagName === 'img') {
      element.setAttribute('loading', 'lazy');
      element.setAttribute('decoding', 'async');
    }
  }

  private sanitizeAnchorHref(element: HTMLAnchorElement, value: string): void {
    const safeUrl: string | null = this.urlSecurityService.sanitizeRichHtmlUrl(value);
    if (safeUrl === null) {
      element.removeAttribute('href');
      return;
    }

    element.setAttribute('href', safeUrl);
  }

  private sanitizeImageSource(element: HTMLImageElement, value: string): void {
    const safeUrl: string | null = this.urlSecurityService.sanitizeRichHtmlUrl(value, true);
    if (safeUrl === null) {
      element.removeAttribute('src');
      return;
    }

    element.setAttribute('src', safeUrl);
  }

  private sanitizeClassAttribute(element: HTMLElement, value: string): void {
    const safeClasses: string[] = value
      .split(/\s+/g)
      .map((className: string) => className.trim())
      .filter((className: string) => className.length > 0 && ALLOWED_CLASS_PATTERN.test(className));

    if (safeClasses.length === 0) {
      element.removeAttribute('class');
      return;
    }

    element.setAttribute('class', safeClasses.join(' '));
  }

  private sanitizeStyleAttribute(element: HTMLElement, value: string): void {
    const safeDeclarations: string[] = value
      .split(';')
      .map((declaration: string) => declaration.trim())
      .map((declaration: string) => this.sanitizeStyleDeclaration(declaration))
      .filter((declaration: string | null): declaration is string => declaration !== null);

    if (safeDeclarations.length === 0) {
      element.removeAttribute('style');
      return;
    }

    element.setAttribute('style', safeDeclarations.join('; '));
  }

  private sanitizeStyleDeclaration(declaration: string): string | null {
    const separatorIndex: number = declaration.indexOf(':');
    if (separatorIndex <= 0) {
      return null;
    }

    const propertyName: string = declaration.substring(0, separatorIndex).trim().toLowerCase();
    const propertyValue: string = declaration.substring(separatorIndex + 1).trim();
    const lowerValue: string = propertyValue.toLowerCase();

    if (!propertyValue || lowerValue.includes('url(') || lowerValue.includes('expression(') || lowerValue.includes('javascript:')) {
      return null;
    }

    if ((propertyName === 'color' || propertyName === 'background-color') && COLOR_PATTERN.test(propertyValue)) {
      return `${propertyName}: ${propertyValue}`;
    }

    if (propertyName === 'text-align' && /^(left|right|center|justify|start|end)$/i.test(propertyValue)) {
      return `${propertyName}: ${propertyValue.toLowerCase()}`;
    }

    if (propertyName === 'font-weight' && /^(normal|bold|bolder|lighter|[1-9]00)$/i.test(propertyValue)) {
      return `${propertyName}: ${propertyValue.toLowerCase()}`;
    }

    if (propertyName === 'font-style' && /^(normal|italic|oblique)$/i.test(propertyValue)) {
      return `${propertyName}: ${propertyValue.toLowerCase()}`;
    }

    if (propertyName === 'text-decoration' && /^(none|underline|line-through)$/i.test(propertyValue)) {
      return `${propertyName}: ${propertyValue.toLowerCase()}`;
    }

    if (propertyName === 'font-size' && SIZE_PATTERN.test(propertyValue)) {
      return `${propertyName}: ${propertyValue}`;
    }

    if ((propertyName === 'margin-left' || propertyName === 'padding-left') && SPACING_PATTERN.test(propertyValue)) {
      return `${propertyName}: ${propertyValue}`;
    }

    if (propertyName === 'vertical-align' && /^(baseline|sub|super)$/i.test(propertyValue)) {
      return `${propertyName}: ${propertyValue.toLowerCase()}`;
    }

    return null;
  }

  private isAllowedSpecificAttribute(tagName: string, attributeName: string): boolean {
    if (attributeName === 'title' || attributeName === 'aria-label') {
      return true;
    }

    if (tagName === 'a') {
      return attributeName === 'target' || attributeName === 'rel';
    }

    if (tagName === 'img') {
      return ['alt', 'width', 'height', 'loading', 'decoding'].includes(attributeName);
    }

    if (tagName === 'th' || tagName === 'td') {
      return attributeName === 'colspan' || attributeName === 'rowspan';
    }

    return false;
  }
}
