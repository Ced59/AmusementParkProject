function createMediaQueryList(query: string): MediaQueryList {
  return {
    matches: false,
    media: query,
    onchange: null,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    dispatchEvent: () => false,
    addListener: () => undefined,
    removeListener: () => undefined,
  };
}

Object.defineProperty(window, 'matchMedia', {
  configurable: true,
  writable: true,
  value: createMediaQueryList,
});

Object.defineProperty(window, 'scrollTo', {
  configurable: true,
  writable: true,
  value: () => undefined,
});

Object.defineProperty(HTMLElement.prototype, 'scrollIntoView', {
  configurable: true,
  writable: true,
  value: () => undefined,
});

Object.defineProperty(Document.prototype, 'execCommand', {
  configurable: true,
  writable: true,
  value: () => false,
});

if (!File.prototype.text) {
  Object.defineProperty(File.prototype, 'text', {
    configurable: true,
    writable: true,
    value: function text(this: File): Promise<string> {
      return new Promise<string>((resolve, reject) => {
        const reader: FileReader = new FileReader();
        reader.onload = () => resolve(String(reader.result ?? ''));
        reader.onerror = () =>
          reject(reader.error ?? new Error('Unable to read the test file.'));
        reader.readAsText(this);
      });
    },
  });
}
