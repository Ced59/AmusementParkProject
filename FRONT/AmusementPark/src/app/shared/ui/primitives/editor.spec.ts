import { ChangeDetectorRef, ElementRef } from '@angular/core';

import { HtmlSecurityService } from '@shared/utils/security';
import { UrlSecurityService } from '@shared/utils/security/url-security.service';
import { Editor } from './editor';
import type Quill from 'quill';

interface TestableEditor {
  editorElement: ElementRef<HTMLElement>;
  toolbarElement: ElementRef<HTMLElement>;
  managedImageInput: ElementRef<HTMLInputElement>;
  uploadManagedImages(files: File[]): Promise<void>;
  onManagedImagePaste(event: ClipboardEvent): void;
  onManagedImageCopy(event: ClipboardEvent): void;
  changeSelectedManagedImageLayout(layout: 'left' | 'right' | 'center' | 'full'): void;
  changeSelectedManagedImageAlt(): void;
  applyManagedImagePreviewsForDom(value: string): string;
  editor: Quill | null;
}

function createEditor(platformId: 'browser' | 'server' = 'browser'): {
  readonly component: Editor;
  readonly testable: TestableEditor;
  readonly content: HTMLElement;
} {
  const component: Editor = new Editor(
    platformId as unknown as object,
    new HtmlSecurityService(document, new UrlSecurityService()),
    { markForCheck: vi.fn() } as unknown as ChangeDetectorRef
  );
  const content: HTMLElement = document.createElement('div');
  const toolbar: HTMLElement = document.createElement('div');
  const input: HTMLInputElement = document.createElement('input');
  const testable: TestableEditor = component as unknown as TestableEditor;
  testable.editorElement = new ElementRef<HTMLElement>(content);
  testable.toolbarElement = new ElementRef<HTMLElement>(toolbar);
  testable.managedImageInput = new ElementRef<HTMLInputElement>(input);
  return { component, testable, content };
}

describe('Editor managed images', () => {
  it('does not initialize Quill during SSR', async () => {
    const context = createEditor('server');

    context.component.ngAfterViewInit();
    await Promise.resolve();

    expect(context.content.querySelector('.ql-editor')).toBeNull();
  });

  it('renders an existing managed image through the binary pipeline but emits canonical html', async () => {
    const context = createEditor();
    const imageId: string = '0123456789abcdef0123456789abcdef';
    const previewUrl: string = `/api/images/binary/${imageId}?width=1280&v=2`;
    const emittedValues: string[] = [];
    context.component.preserveManagedImages = true;
    context.component.managedImagePreviewUrl = vi.fn().mockReturnValue(previewUrl);
    context.component.registerOnChange((value: string): void => {
      emittedValues.push(value);
    });
    const canonicalHtml: string =
      `<p>Avis existant</p><img src="/images/${imageId}" class="rich-text__image rich-text__image--right" alt="Parc">`;
    const domHtml: string = context.testable.applyManagedImagePreviewsForDom(canonicalHtml);
    const domTemplate: HTMLTemplateElement = document.createElement('template');
    domTemplate.innerHTML = domHtml;
    expect(domTemplate.content.querySelector('img')?.getAttribute('src')).toBe(previewUrl);
    expect(domTemplate.content.querySelector('img')?.getAttribute('data-managed-image-id'))
      .toBe(imageId);
    expect(domHtml).not.toContain(`src="/images/${imageId}"`);
    context.component.writeValue(
      canonicalHtml
    );

    context.component.ngAfterViewInit();
    await vi.waitFor(
      (): void => expect(context.content.querySelector('.ql-editor')).not.toBeNull(),
      { timeout: 3000 }
    );

    const image: HTMLImageElement | null = context.content.querySelector(
      'img.rich-text__image'
    );
    expect(image?.getAttribute('src')).toBe(previewUrl);
    expect(image?.getAttribute('data-managed-image-id')).toBe(imageId);

    if (!context.testable.editor || !image) {
      throw new Error('Expected Quill and the managed image to be initialized.');
    }

    image.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    context.testable.changeSelectedManagedImageLayout('left');

    const updatedImage: HTMLImageElement | null = context.content.querySelector(
      'img.rich-text__image'
    );
    expect(updatedImage?.getAttribute('src')).toBe(previewUrl);
    expect(updatedImage?.classList.contains('rich-text__image--left')).toBe(true);
    expect(emittedValues.at(-1)).toContain(`src="/images/${imageId}"`);
    expect(emittedValues.at(-1)).not.toContain('/api/images/binary/');
    context.component.ngOnDestroy();
  });

  it('uploads pasted image files instead of storing base64 clipboard html', async () => {
    const context = createEditor();
    const upload = vi.fn().mockResolvedValue({ id: '0123456789abcdef0123456789abcdef' });
    context.component.allowManagedImages = true;
    context.component.managedImageUpload = upload;
    const pastedFile: File = new File(['pixels'], 'paste.png', { type: 'image/png' });
    const preventDefault = vi.fn();
    const stopImmediatePropagation = vi.fn();

    context.testable.onManagedImagePaste({
      clipboardData: {
        files: [pastedFile],
        getData: vi.fn().mockReturnValue('<img src="data:image/png;base64,AAAA">')
      },
      preventDefault,
      stopImmediatePropagation
    } as unknown as ClipboardEvent);
    await vi.waitFor((): void => expect(upload).toHaveBeenCalledWith(pastedFile));

    expect(preventDefault).toHaveBeenCalled();
    expect(stopImmediatePropagation).toHaveBeenCalled();
  });

  it('uploads dropped files and blocks dragged hotlink URLs', async () => {
    const context = createEditor();
    const upload = vi.fn().mockResolvedValue({ id: 'abcdef0123456789abcdef0123456789' });
    context.component.allowManagedImages = true;
    context.component.managedImageUpload = upload;
    const droppedFile: File = new File(['pixels'], 'drop.webp', { type: 'image/webp' });
    const preventFileDrop = vi.fn();

    context.component.onDrop({
      dataTransfer: {
        files: [droppedFile],
        items: []
      },
      preventDefault: preventFileDrop,
      stopPropagation: vi.fn()
    } as unknown as DragEvent);
    await vi.waitFor((): void => expect(upload).toHaveBeenCalledWith(droppedFile));
    expect(preventFileDrop).toHaveBeenCalled();

    const preventHotlinkDrop = vi.fn();
    context.component.onDrop({
      dataTransfer: {
        files: [],
        items: [{ kind: 'string', type: 'text/uri-list' }]
      },
      preventDefault: preventHotlinkDrop,
      stopPropagation: vi.fn()
    } as unknown as DragEvent);
    expect(preventHotlinkDrop).toHaveBeenCalled();
    expect(upload).toHaveBeenCalledTimes(1);
  });

  it('blocks Quill file drops when managed images are preserved but uploads are forbidden', () => {
    const context = createEditor();
    const upload = vi.fn().mockResolvedValue({
      id: '0123456789abcdef0123456789abcdef'
    });
    context.component.allowManagedImages = false;
    context.component.preserveManagedImages = true;
    context.component.managedImageUpload = upload;
    const preventDefault = vi.fn();
    const stopPropagation = vi.fn();

    context.component.onDrop({
      dataTransfer: {
        files: [new File(['pixels'], 'forbidden.png', { type: 'image/png' })],
        items: []
      },
      preventDefault,
      stopPropagation
    } as unknown as DragEvent);

    expect(preventDefault).toHaveBeenCalled();
    expect(stopPropagation).toHaveBeenCalled();
    expect(upload).not.toHaveBeenCalled();
  });

  it('blocks dropped html containing an image even when no file or uri-list item is exposed', () => {
    const context = createEditor();
    context.component.allowManagedImages = true;
    const preventDefault = vi.fn();
    const stopPropagation = vi.fn();

    context.component.onDrop({
      dataTransfer: {
        files: [],
        items: [{ kind: 'string', type: 'text/html' }],
        getData: (type: string): string =>
          type === 'text/html' ? '<p>External</p><img src="https://example.test/image.png">' : ''
      },
      preventDefault,
      stopPropagation
    } as unknown as DragEvent);

    expect(preventDefault).toHaveBeenCalled();
    expect(stopPropagation).toHaveBeenCalled();
  });

  it('stores stable managed tags and applies image alignment without replacing the id', async () => {
    const context = createEditor();
    const emittedValues: string[] = [];
    const upload = vi.fn().mockResolvedValue({
      id: '11111111111111111111111111111111',
      alt: 'Roller coaster',
      previewUrl: 'blob:comment-draft'
    });
    context.component.allowManagedImages = true;
    context.component.preserveManagedImages = true;
    context.component.registerOnChange((value: string): void => {
      emittedValues.push(value);
    });
    context.component.managedImageUpload = upload;
    context.component.ngAfterViewInit();
    await vi.waitFor(
      (): void => expect(context.content.querySelector('.ql-editor')).not.toBeNull(),
      { timeout: 3000 }
    );

    await context.testable.uploadManagedImages([
      new File(['pixels'], 'coaster.png', { type: 'image/png' })
    ]);
    const image: HTMLImageElement | null = context.content.querySelector(
      'img.rich-text__image'
    );
    expect(image?.getAttribute('src')).toBe('blob:comment-draft');
    expect(image?.getAttribute('data-managed-image-id')).toBe(
      '11111111111111111111111111111111'
    );
    expect(image?.outerHTML).toContain('rich-text__image--full');
    expect(emittedValues.join('')).toContain(
      'src="/images/11111111111111111111111111111111"'
    );
    expect(emittedValues.join('')).not.toContain('blob:comment-draft');

    image?.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    context.testable.changeSelectedManagedImageLayout('left');

    const alignedImage: HTMLImageElement | null = context.content.querySelector(
      'img.rich-text__image'
    );
    expect(alignedImage?.getAttribute('src')).toBe('blob:comment-draft');
    expect(alignedImage?.classList.contains('rich-text__image--left')).toBe(true);
    expect(alignedImage?.getAttribute('alt')).toBe('Roller coaster');
    expect(alignedImage?.getAttribute('tabindex')).toBe('0');

    if (!context.testable.editor) {
      throw new Error('Expected Quill to be initialized.');
    }
    vi.spyOn(context.testable.editor, 'getSelection').mockReturnValue({
      index: 0,
      length: context.testable.editor.getLength()
    });
    const copied: Record<string, string> = {};
    context.testable.onManagedImageCopy({
      clipboardData: {
        setData: (type: string, value: string): void => {
          copied[type] = value;
        }
      },
      preventDefault: vi.fn()
    } as unknown as ClipboardEvent);
    expect(copied['text/html']).toContain(
      'src="/images/11111111111111111111111111111111"'
    );
    expect(copied['text/html']).not.toContain('blob:comment-draft');

    context.testable.onManagedImagePaste({
      clipboardData: {
        files: [],
        getData: (type: string): string => type === 'text/html' ? copied['text/html'] : ''
      },
      preventDefault: vi.fn(),
      stopImmediatePropagation: vi.fn()
    } as unknown as ClipboardEvent);
    expect(upload).toHaveBeenCalledTimes(1);
    expect(context.content.querySelectorAll(
      'img[data-managed-image-id="11111111111111111111111111111111"]'
    )).toHaveLength(2);
    context.component.ngOnDestroy();
  });

  it('selects a managed image on keyboard focus for layout and alt tools', async () => {
    const context = createEditor();
    context.component.allowManagedImages = true;
    context.component.preserveManagedImages = true;
    context.component.managedImageUpload = vi.fn().mockResolvedValue({
      id: '22222222222222222222222222222222',
      alt: 'Initial alt',
      previewUrl: 'blob:keyboard-draft'
    });
    context.component.managedImageAltPrompt = 'Alternative text';
    context.component.ngAfterViewInit();
    await vi.waitFor(
      (): void => expect(context.content.querySelector('.ql-editor')).not.toBeNull(),
      { timeout: 3000 }
    );
    await context.testable.uploadManagedImages([
      new File(['pixels'], 'keyboard.png', { type: 'image/png' })
    ]);

    const image: HTMLImageElement | null = context.content.querySelector(
      'img.rich-text__image'
    );
    if (!image) {
      throw new Error('Expected a managed image.');
    }

    image.focus();
    context.testable.changeSelectedManagedImageLayout('right');
    const promptSpy = vi.spyOn(globalThis, 'prompt').mockReturnValue('Keyboard alt');
    context.testable.changeSelectedManagedImageAlt();

    const updatedImage: HTMLImageElement | null = context.content.querySelector(
      'img.rich-text__image'
    );
    expect(updatedImage?.classList.contains('rich-text__image--right')).toBe(true);
    expect(updatedImage?.getAttribute('alt')).toBe('Keyboard alt');
    expect(promptSpy).toHaveBeenCalledWith('Alternative text', 'Initial alt');
    context.component.ngOnDestroy();
  });

  it('keeps a managed image reversible through delete and undo without cleanup callbacks', async () => {
    const context = createEditor();
    context.component.allowManagedImages = true;
    context.component.preserveManagedImages = true;
    context.component.managedImageUpload = vi.fn().mockResolvedValue({
      id: '44444444444444444444444444444444',
      previewUrl: 'blob:undo-draft'
    });
    context.component.ngAfterViewInit();
    await vi.waitFor(
      (): void => expect(context.content.querySelector('.ql-editor')).not.toBeNull(),
      { timeout: 3000 }
    );
    await context.testable.uploadManagedImages([
      new File(['pixels'], 'undo.png', { type: 'image/png' })
    ]);
    if (!context.testable.editor) {
      throw new Error('Expected Quill to be initialized.');
    }

    const history = context.testable.editor.getModule('history') as {
      cutoff(): void;
      undo(): void;
    };
    history.cutoff();
    context.testable.editor.deleteText(0, 1, 'user');
    expect(context.content.querySelector('img.rich-text__image')).toBeNull();

    history.undo();

    expect(context.content.querySelector(
      'img[data-managed-image-id="44444444444444444444444444444444"]'
    )).not.toBeNull();
    context.component.ngOnDestroy();
  });

  it('does not insert an upload result after editor destruction', async () => {
    const context = createEditor();
    const uploadControl: {
      resolve?: (image: { id: string; previewUrl: string }) => void;
    } = {};
    context.component.allowManagedImages = true;
    context.component.managedImageUpload = vi.fn().mockReturnValue(
      new Promise<{ id: string; previewUrl: string }>((resolve): void => {
        uploadControl.resolve = resolve;
      })
    );
    context.component.ngAfterViewInit();
    await vi.waitFor(
      (): void => expect(context.content.querySelector('.ql-editor')).not.toBeNull(),
      { timeout: 3000 }
    );

    const upload: Promise<void> = context.testable.uploadManagedImages([
      new File(['pixels'], 'destroyed.png', { type: 'image/png' })
    ]);
    context.component.ngOnDestroy();
    uploadControl.resolve?.({
      id: '33333333333333333333333333333333',
      previewUrl: 'blob:destroyed-draft'
    });
    await upload;

    expect(context.content.querySelector('img.rich-text__image')).toBeNull();
  });
});
