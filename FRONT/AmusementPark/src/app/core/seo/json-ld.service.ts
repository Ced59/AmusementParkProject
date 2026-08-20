import { DOCUMENT } from '@angular/common';
import { Inject, Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class JsonLdService {
  private readonly managedSelector: string = 'script[type="application/ld+json"][data-managed-by="amusementpark-seo"]';
  private documents: unknown[] = [];

  constructor(@Inject(DOCUMENT) private readonly document: Document) {
  }

  setJsonLd(documents: unknown[]): void {
    this.documents = documents.filter((document: unknown): boolean => !!document);
    this.render();
  }

  replaceJsonLdByType(type: string, document: unknown): void {
    const normalizedType: string = type.trim();
    if (!normalizedType || !document) {
      return;
    }

    let replaced: boolean = false;
    this.documents = this.documents.map((currentDocument: unknown): unknown => {
      if (!replaced && this.resolveJsonLdType(currentDocument) === normalizedType) {
        replaced = true;
        return document;
      }

      return currentDocument;
    });

    if (!replaced) {
      this.documents = [...this.documents, document];
    }

    this.render();
  }

  private render(): void {
    this.document.head.querySelectorAll<HTMLScriptElement>(this.managedSelector)
      .forEach((element: HTMLScriptElement): void => element.remove());

    this.documents.forEach((document: unknown, index: number): void => {
      const scriptElement: HTMLScriptElement = this.document.createElement('script');
      scriptElement.type = 'application/ld+json';
      scriptElement.setAttribute('data-managed-by', 'amusementpark-seo');
      scriptElement.setAttribute('data-json-ld-index', index.toString());
      scriptElement.text = JSON.stringify(document);
      this.document.head.appendChild(scriptElement);
    });
  }

  private resolveJsonLdType(document: unknown): string | null {
    if (typeof document !== 'object' || document === null || !('@type' in document)) {
      return null;
    }

    const type: unknown = (document as { '@type'?: unknown })['@type'];
    return typeof type === 'string' ? type : null;
  }
}
