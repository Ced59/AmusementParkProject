// @vitest-environment node

describe('Editor SSR loading', () => {
  it('loads the editor component module without evaluating Quill in Node', async () => {
    expect(globalThis.document).toBeUndefined();

    const editorModule: typeof import('./editor') = await import('./editor');

    expect(editorModule.Editor).toBeDefined();
  });
});
