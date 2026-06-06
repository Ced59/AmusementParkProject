import { DOCUMENT } from '@angular/common';
import { Inject, Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class JsonLdService {
  private readonly managedSelector: string = 'script[type="application/ld+json"][data-managed-by="amusementpark-seo"]';

  constructor(@Inject(DOCUMENT) private readonly document: Document) {
  }

  setJsonLd(documents: unknown[]): void {
    this.document.head.querySelectorAll<HTMLScriptElement>(this.managedSelector)
      .forEach((element: HTMLScriptElement): void => element.remove());

    documents
      .filter((document: unknown): boolean => !!document)
      .forEach((document: unknown, index: number): void => {
        const scriptElement: HTMLScriptElement = this.document.createElement('script');
        scriptElement.type = 'application/ld+json';
        scriptElement.setAttribute('data-managed-by', 'amusementpark-seo');
        scriptElement.setAttribute('data-json-ld-index', index.toString());
        scriptElement.text = JSON.stringify(document);
        this.document.head.appendChild(scriptElement);
      });
  }
}
