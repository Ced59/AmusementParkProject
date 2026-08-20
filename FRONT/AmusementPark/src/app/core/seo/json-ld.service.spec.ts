import { JsonLdService } from './json-ld.service';

describe('JsonLdService', () => {
  let service: JsonLdService;
  let testDocument: Document;

  beforeEach(() => {
    testDocument = document.implementation.createHTMLDocument('json-ld-test');
    service = new JsonLdService(testDocument);
  });

  it('adds managed json-ld script tags for truthy documents', () => {
    service.setJsonLd([{ '@type': 'Thing', name: 'A' }, null, { '@type': 'Thing', name: 'B' }]);

    const scripts: NodeListOf<HTMLScriptElement> = testDocument.head.querySelectorAll<HTMLScriptElement>('script[type="application/ld+json"]');

    expect(scripts.length).toBe(2);
    expect(scripts[0].getAttribute('data-managed-by')).toBe('amusementpark-seo');
    expect(scripts[0].getAttribute('data-json-ld-index')).toBe('0');
    expect(JSON.parse(scripts[0].text).name).toBe('A');
  });

  it('removes previous managed scripts before adding new ones', () => {
    service.setJsonLd([{ name: 'Old' }]);
    service.setJsonLd([{ name: 'New' }]);

    const scripts: NodeListOf<HTMLScriptElement> = testDocument.head.querySelectorAll<HTMLScriptElement>('script[data-managed-by="amusementpark-seo"]');

    expect(scripts.length).toBe(1);
    expect(JSON.parse(scripts[0].text).name).toBe('New');
  });

  it('does not remove unmanaged json-ld scripts', () => {
    const unmanagedScript: HTMLScriptElement = testDocument.createElement('script');
    unmanagedScript.type = 'application/ld+json';
    unmanagedScript.text = '{"name":"External"}';
    testDocument.head.appendChild(unmanagedScript);

    service.setJsonLd([]);

    expect(testDocument.head.querySelectorAll('script[type="application/ld+json"]').length).toBe(1);
    expect(testDocument.head.textContent).toContain('External');
  });

  it('replaces one managed json-ld document by type without dropping the others', () => {
    service.setJsonLd([
      { '@context': 'https://schema.org', '@type': 'BreadcrumbList', marker: 'old' },
      { '@context': 'https://schema.org', '@type': 'ItemList', marker: 'timeline' }
    ]);

    service.replaceJsonLdByType('BreadcrumbList', {
      '@context': 'https://schema.org',
      '@type': 'BreadcrumbList',
      marker: 'standalone'
    });

    const documents: Array<Record<string, unknown>> = Array.from(
      testDocument.head.querySelectorAll<HTMLScriptElement>('script[data-managed-by="amusementpark-seo"]')
    ).map((element: HTMLScriptElement): Record<string, unknown> => JSON.parse(element.text) as Record<string, unknown>);

    expect(documents).toHaveLength(2);
    expect(documents).toContainEqual(expect.objectContaining({ '@type': 'BreadcrumbList', marker: 'standalone' }));
    expect(documents).toContainEqual(expect.objectContaining({ '@type': 'ItemList', marker: 'timeline' }));
    expect(documents).not.toContainEqual(expect.objectContaining({ marker: 'old' }));
  });
});
