import {
  AfterContentInit,
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ContentChildren,
  ElementRef,
  forwardRef,
  Inject,
  Input,
  OnDestroy,
  PLATFORM_ID,
  QueryList,
  TemplateRef,
  ViewChild
} from '@angular/core';
import { isPlatformBrowser, NgIf, NgStyle, NgTemplateOutlet } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import type Quill from 'quill';

import { ManagedRichTextImage } from '@app/models/comments/comment-image.models';
import { HtmlSecurityService } from '@shared/utils/security';
import {
  ManagedCommentImageAltMaxLength,
  extractManagedCommentImageId,
  normalizeManagedCommentImageId
} from '@shared/utils/comments/managed-comment-image.helpers';
import { UiTemplate } from './api';

export type ManagedImageUploadHandler = (file: File) => Promise<ManagedRichTextImage>;
export type ManagedImageRemovalHandler = (imageId: string) => void;
export type ManagedImagePreviewResolver = (imageId: string) => string | null;

type ManagedImageLayout = 'left' | 'right' | 'center' | 'full';

interface ManagedImageBlotValue {
  readonly id: string;
  readonly alt: string;
  readonly layout: ManagedImageLayout;
  readonly previewUrl?: string;
}

const ManagedImageIdAttribute: string = 'data-managed-image-id';

@Component({
  selector: 'app-ui-editor',
  standalone: true,
  imports: [NgIf, NgStyle, NgTemplateOutlet],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => Editor), multi: true }],
  template: `
    <div
      class="p-editor-container"
      [class.p-editor-container--uploading]="managedImageUploadCount > 0"
      (dragover)="onDragOver($event)"
      (drop)="onDrop($event)">
      <div #toolbar class="p-editor-toolbar">
        <ng-container *ngIf="template('header') as headerTemplate"><ng-container *ngTemplateOutlet="headerTemplate"></ng-container></ng-container>
      </div>
      <div #editor class="p-editor-content" [ngStyle]="style"></div>
      <div *ngIf="managedImageUploadCount > 0" class="p-editor-upload-status" role="status" aria-live="polite">
        {{ managedImageUploadingLabel }}
      </div>
      <input
        #managedImageInput
        class="p-editor-managed-image-input"
        type="file"
        accept="image/*"
        multiple
        tabindex="-1"
        aria-hidden="true"
        (change)="onManagedImageSelection($event)" />
    </div>
  `,
  styles: [`
    .p-editor-container { position: relative; }
    .p-editor-managed-image-input { display: none; }
    .p-editor-upload-status {
      align-items: center;
      background: color-mix(in srgb, var(--surface) 92%, transparent);
      bottom: .6rem;
      display: flex;
      font-size: .82rem;
      font-weight: 700;
      gap: .45rem;
      left: .75rem;
      padding: .35rem .55rem;
      position: absolute;
      z-index: 2;
    }
    .p-editor-upload-status::before {
      animation: p-editor-spin .8s linear infinite;
      border: 2px solid color-mix(in srgb, var(--c-sky) 30%, transparent);
      border-radius: 50%;
      border-top-color: var(--c-sky);
      content: '';
      height: .8rem;
      width: .8rem;
    }
    @keyframes p-editor-spin { to { transform: rotate(360deg); } }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class Editor implements AfterViewInit, AfterContentInit, OnDestroy, ControlValueAccessor {
  @Input() readonly: boolean = false;
  @Input() style: Record<string, string> | null = null;
  @Input() placeholder: string | null = null;
  @Input() allowManagedImages: boolean = false;
  @Input() preserveManagedImages: boolean = false;
  @Input() managedImageUpload: ManagedImageUploadHandler | null = null;
  @Input() managedImageRemoved: ManagedImageRemovalHandler | null = null;
  @Input() managedImagePreviewUrl: ManagedImagePreviewResolver | null = null;
  @Input() managedImageUploadingLabel: string = '';
  @Input() managedImageAltPrompt: string = '';
  @ContentChildren(UiTemplate) templates!: QueryList<UiTemplate>;
  @ViewChild('editor') private editorElement?: ElementRef<HTMLElement>;
  @ViewChild('toolbar') private toolbarElement?: ElementRef<HTMLElement>;
  @ViewChild('managedImageInput') private managedImageInput?: ElementRef<HTMLInputElement>;

  managedImageUploadCount: number = 0;

  private editor: Quill | null = null;
  private quillConstructor: typeof import('quill').default | null = null;
  private pendingValue: string = '';
  private destroyed: boolean = false;
  private selectedManagedImage: HTMLImageElement | null = null;
  private managedImageIds: Set<string> = new Set<string>();
  private readonly removeRootListeners: Array<() => void> = [];
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  constructor(
    @Inject(PLATFORM_ID) private readonly platformId: object,
    private readonly htmlSecurityService: HtmlSecurityService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  ngAfterContentInit(): void {
  }

  ngAfterViewInit(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    void this.initializeEditor();
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    for (const removeListener of this.removeRootListeners) {
      removeListener();
    }
    this.editor = null;
    this.quillConstructor = null;
  }

  onManagedImageSelection(event: Event): void {
    const input: HTMLInputElement = event.target as HTMLInputElement;
    const files: File[] = Array.from(input.files ?? []);
    input.value = '';
    void this.uploadManagedImages(files);
  }

  onDragOver(event: DragEvent): void {
    if (this.readonly
      || (!this.preserveManagedImages && !this.allowManagedImages)
      || !this.hasImageFiles(event.dataTransfer?.files)) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = this.allowManagedImages ? 'copy' : 'none';
    }
  }

  onDrop(event: DragEvent): void {
    if (this.readonly || (!this.preserveManagedImages && !this.allowManagedImages)) {
      return;
    }

    const files: File[] = this.imageFiles(event.dataTransfer?.files);
    const containsExternalImageUrl: boolean = Array.from(event.dataTransfer?.items ?? []).some(
      (item: DataTransferItem) => item.kind === 'string' && item.type === 'text/uri-list'
    );
    if (files.length === 0 && !containsExternalImageUrl) {
      return;
    }

    event.preventDefault();
    event.stopPropagation();
    if (this.allowManagedImages && files.length > 0) {
      void this.uploadManagedImages(files);
    }
  }

  private async initializeEditor(): Promise<void> {
    const quillModule: typeof import('quill') = await import('quill');
    if (this.destroyed || !this.editorElement || !this.toolbarElement) {
      return;
    }

    this.quillConstructor = quillModule.default;
    if (this.preserveManagedImages || this.allowManagedImages) {
      registerManagedImageBlot(quillModule.default);
    }

    this.editor = new quillModule.default(this.editorElement.nativeElement, {
      modules: {
        toolbar: {
          container: this.toolbarElement.nativeElement,
          handlers: this.allowManagedImages
            ? {
              managedImage: (): void => this.openManagedImagePicker(),
              managedImageLayout: (layout: string): void =>
                this.changeSelectedManagedImageLayout(normalizeManagedImageLayout(layout)),
              managedImageAlt: (): void => this.changeSelectedManagedImageAlt()
            }
            : {}
        }
      },
      placeholder: this.placeholder ?? '',
      readOnly: this.readonly,
      theme: 'snow'
    });

    this.writeEditorHtml(this.pendingValue);
    this.editor.on('text-change', (): void => {
      this.notifyRemovedManagedImages();
      this.onChange(this.sanitizeEditorHtml(this.editor?.root.innerHTML ?? ''));
      this.onTouched();
    });

    if (this.preserveManagedImages || this.allowManagedImages) {
      this.installManagedImageListeners();
    }
  }

  writeValue(value: string | null): void {
    this.pendingValue = value ?? '';
    if (this.editor) {
      this.writeEditorHtml(this.pendingValue);
    }
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.readonly = isDisabled;
    this.editor?.enable(!isDisabled);
  }

  template(name: string): TemplateRef<unknown> | null {
    return this.templates?.find((template: UiTemplate) => template.name === name)?.template ?? null;
  }

  private installManagedImageListeners(): void {
    if (!this.editor) {
      return;
    }

    const root: HTMLElement = this.editor.root;
    const container: HTMLElement = this.editorElement?.nativeElement ?? root;
    const clickHandler = (event: Event): void => {
      const target: EventTarget | null = event.target;
      this.selectedManagedImage = target instanceof HTMLImageElement
        && managedImageValueFromNode(target) !== null
        ? target
        : null;
    };
    const pasteHandler = (event: ClipboardEvent): void => this.onManagedImagePaste(event);
    const copyHandler = (event: ClipboardEvent): void => this.onManagedImageCopy(event);
    const dragOverHandler = (event: DragEvent): void => this.onDragOver(event);
    const dropHandler = (event: DragEvent): void => this.onDrop(event);
    root.addEventListener('click', clickHandler);
    root.addEventListener('copy', copyHandler, true);
    container.addEventListener('paste', pasteHandler, true);
    container.addEventListener('dragover', dragOverHandler, true);
    container.addEventListener('drop', dropHandler, true);
    this.removeRootListeners.push(
      (): void => root.removeEventListener('click', clickHandler),
      (): void => root.removeEventListener('copy', copyHandler, true),
      (): void => container.removeEventListener('paste', pasteHandler, true),
      (): void => container.removeEventListener('dragover', dragOverHandler, true),
      (): void => container.removeEventListener('drop', dropHandler, true)
    );
  }

  private onManagedImagePaste(event: ClipboardEvent): void {
    if (this.readonly || !event.clipboardData) {
      return;
    }

    const files: File[] = this.imageFiles(event.clipboardData.files);
    if (files.length > 0) {
      event.preventDefault();
      event.stopImmediatePropagation();
      void this.uploadManagedImages(files);
      return;
    }

    const html: string = event.clipboardData.getData('text/html');
    if (!/<img[\s>]/i.test(html)) {
      return;
    }

    event.preventDefault();
    event.stopImmediatePropagation();
    const safeHtml: string = this.allowManagedImages
      ? this.htmlSecurityService.sanitizeManagedImageRichHtml(html)
      : this.removeImagesFromHtml(this.htmlSecurityService.sanitizeManagedImageRichHtml(html));
    const insertionIndex: number = this.editor?.getSelection()?.index
      ?? Math.max(0, (this.editor?.getLength() ?? 1) - 1);
    this.editor?.clipboard.dangerouslyPasteHTML(insertionIndex, safeHtml, 'user');
    this.hydrateManagedImagePreviews();
  }

  private onManagedImageCopy(event: ClipboardEvent): void {
    if (!this.editor || !event.clipboardData) {
      return;
    }

    const range = this.editor.getSelection();
    if (!range || range.length === 0) {
      return;
    }

    const selectedHtml: string = this.editor.getSemanticHTML(range);
    if (!selectedHtml.includes(ManagedImageIdAttribute) && !/<img[\s>]/i.test(selectedHtml)) {
      return;
    }

    event.preventDefault();
    event.clipboardData.setData('text/html', this.sanitizeEditorHtml(selectedHtml));
    event.clipboardData.setData('text/plain', this.editor.getText(range));
  }

  private openManagedImagePicker(): void {
    if (!this.readonly && this.allowManagedImages && this.managedImageUpload) {
      this.managedImageInput?.nativeElement.click();
    }
  }

  private async uploadManagedImages(files: File[]): Promise<void> {
    if (!this.allowManagedImages || this.readonly || !this.managedImageUpload) {
      return;
    }

    for (const file of files) {
      if (!file.type.toLowerCase().startsWith('image/')) {
        continue;
      }

      this.managedImageUploadCount += 1;
      this.changeDetectorRef.markForCheck();
      try {
        const uploaded: ManagedRichTextImage = await this.managedImageUpload(file);
        if (!this.destroyed) {
          this.insertManagedImage(uploaded);
        }
      } catch {
        // The page facade exposes the localized upload error.
      } finally {
        this.managedImageUploadCount = Math.max(0, this.managedImageUploadCount - 1);
        this.changeDetectorRef.markForCheck();
      }
    }
  }

  private insertManagedImage(image: ManagedRichTextImage): void {
    if (!this.editor || !this.htmlSecurityService.extractManagedImageId(`/images/${image.id}`)) {
      return;
    }

    const index: number = this.editor.getSelection()?.index
      ?? Math.max(0, this.editor.getLength() - 1);
    const value: ManagedImageBlotValue = {
      id: image.id,
      alt: (image.alt ?? '').trim().slice(0, ManagedCommentImageAltMaxLength),
      layout: 'full',
      previewUrl: image.previewUrl
    };
    this.editor.insertEmbed(index, 'managedImage', value, 'user');
    this.editor.insertText(index + 1, '\n', 'user');
    this.editor.setSelection(index + 2, 0, 'silent');
    this.selectedManagedImage = this.resolveManagedImageAt(index);
  }

  private changeSelectedManagedImageLayout(layout: ManagedImageLayout): void {
    this.replaceSelectedManagedImage({ layout });
  }

  private changeSelectedManagedImageAlt(): void {
    if (!this.selectedManagedImage || !isPlatformBrowser(this.platformId)) {
      return;
    }

    const currentAlt: string = this.selectedManagedImage.getAttribute('alt') ?? '';
    const nextAlt: string | null = globalThis.prompt(this.managedImageAltPrompt, currentAlt);
    if (nextAlt !== null) {
      this.replaceSelectedManagedImage({
        alt: nextAlt.trim().slice(0, ManagedCommentImageAltMaxLength)
      });
    }
  }

  private replaceSelectedManagedImage(
    changes: Partial<Pick<ManagedImageBlotValue, 'alt' | 'layout'>>
  ): void {
    if (!this.editor || !this.quillConstructor || !this.selectedManagedImage) {
      return;
    }

    const currentValue: ManagedImageBlotValue | null = managedImageValueFromNode(
      this.selectedManagedImage
    );
    const blot: ReturnType<typeof this.quillConstructor.find> =
      this.quillConstructor.find(this.selectedManagedImage);
    if (!currentValue || !blot || blot === this.editor) {
      return;
    }

    const index: number = this.editor.getIndex(
      blot as Parameters<Quill['getIndex']>[0]
    );
    const nextValue: ManagedImageBlotValue = { ...currentValue, ...changes };
    this.editor.deleteText(index, 1, 'silent');
    this.editor.insertEmbed(index, 'managedImage', nextValue, 'user');
    this.editor.setSelection(index + 1, 0, 'silent');
    this.selectedManagedImage = this.resolveManagedImageAt(index);
  }

  private writeEditorHtml(value: string): void {
    if (!this.editor) {
      return;
    }

    const html: string = this.sanitizeEditorHtml(value);
    this.editor.clipboard.dangerouslyPasteHTML(html, 'silent');
    this.hydrateManagedImagePreviews();
    this.managedImageIds = collectManagedImageIds(this.editor.root, this.htmlSecurityService);
    this.selectedManagedImage = null;
  }

  private resolveManagedImageAt(index: number): HTMLImageElement | null {
    const leaf = this.editor?.getLeaf(index)[0] as { domNode?: Node } | null | undefined;
    return leaf?.domNode instanceof HTMLImageElement
      && managedImageValueFromNode(leaf.domNode) !== null
      ? leaf.domNode
      : null;
  }

  private sanitizeEditorHtml(value: string): string {
    const canonicalHtml: string = this.canonicalizeManagedImageHtml(value);
    return this.preserveManagedImages || this.allowManagedImages
      ? this.htmlSecurityService.sanitizeManagedImageRichHtml(canonicalHtml)
      : this.htmlSecurityService.sanitizeRichHtml(value);
  }

  private canonicalizeManagedImageHtml(value: string): string {
    const documentRef: Document = this.editorElement?.nativeElement.ownerDocument ?? document;
    const template: HTMLTemplateElement = documentRef.createElement('template');
    template.innerHTML = value;
    const images: HTMLImageElement[] = Array.from(template.content.querySelectorAll('img'));
    for (const image of images) {
      const imageId: string | null = normalizeManagedCommentImageId(
        image.getAttribute(ManagedImageIdAttribute)
      ) ?? extractManagedCommentImageId(image.getAttribute('src'));
      if (imageId) {
        image.setAttribute('src', `/images/${imageId}`);
      }
      image.removeAttribute(ManagedImageIdAttribute);
    }
    return template.innerHTML;
  }

  private hydrateManagedImagePreviews(): void {
    if (!this.editor || !this.managedImagePreviewUrl) {
      return;
    }

    const images: HTMLImageElement[] = Array.from(
      this.editor.root.querySelectorAll('img.rich-text__image')
    );
    for (const image of images) {
      const imageId: string | null = extractManagedCommentImageId(image.getAttribute('src'));
      if (!imageId) {
        continue;
      }

      const previewUrl: string | null = this.managedImagePreviewUrl(imageId);
      if (previewUrl?.startsWith('blob:')) {
        image.setAttribute(ManagedImageIdAttribute, imageId);
        image.setAttribute('src', previewUrl);
      }
    }
  }

  private removeImagesFromHtml(value: string): string {
    const documentRef: Document = this.editorElement?.nativeElement.ownerDocument ?? document;
    const template: HTMLTemplateElement = documentRef.createElement('template');
    template.innerHTML = value;
    for (const image of Array.from(template.content.querySelectorAll('img'))) {
      image.remove();
    }
    return template.innerHTML;
  }

  private notifyRemovedManagedImages(): void {
    if (!this.editor || !this.allowManagedImages) {
      return;
    }

    const currentIds: Set<string> = collectManagedImageIds(
      this.editor.root,
      this.htmlSecurityService
    );
    for (const previousId of this.managedImageIds) {
      if (!currentIds.has(previousId)) {
        this.managedImageRemoved?.(previousId);
      }
    }
    this.managedImageIds = currentIds;
  }

  private imageFiles(fileList: FileList | null | undefined): File[] {
    return Array.from(fileList ?? []).filter(
      (file: File) => file.type.toLowerCase().startsWith('image/')
    );
  }

  private hasImageFiles(fileList: FileList | null | undefined): boolean {
    return this.imageFiles(fileList).length > 0;
  }
}

function registerManagedImageBlot(quillConstructor: typeof import('quill').default): void {
  if (quillConstructor.imports['formats/managedImage']) {
    return;
  }

  const BlockEmbed = quillConstructor.import('blots/block/embed') as {
    new(...parameters: unknown[]): {
      domNode: HTMLElement;
      format(name: string, value: unknown): void;
    };
    create(value: unknown): HTMLElement;
  };

  class ManagedImageBlot extends BlockEmbed {
    static blotName: string = 'managedImage';
    static className: string = 'rich-text__image';
    static tagName: string = 'IMG';

    static override create(value: ManagedImageBlotValue): HTMLElement {
      const node: HTMLElement = super.create(value);
      applyManagedImageValue(node, value);
      return node;
    }

    static value(node: HTMLElement): ManagedImageBlotValue {
      return managedImageValueFromNode(node) ?? {
        id: '',
        alt: '',
        layout: 'full'
      };
    }

    override format(name: string, value: unknown): void {
      if (name === 'managedImageLayout') {
        const currentValue: ManagedImageBlotValue | null = managedImageValueFromNode(this.domNode);
        if (currentValue) {
          applyManagedImageValue(this.domNode, {
            ...currentValue,
            layout: normalizeManagedImageLayout(String(value))
          });
        }
        return;
      }

      if (name === 'managedImageAlt') {
        this.domNode.setAttribute(
          'alt',
          String(value).trim().slice(0, ManagedCommentImageAltMaxLength)
        );
        return;
      }

      super.format(name, value);
    }
  }

  quillConstructor.register('formats/managedImage', ManagedImageBlot, true);
}

function applyManagedImageValue(node: HTMLElement, value: ManagedImageBlotValue): void {
  node.setAttribute(ManagedImageIdAttribute, value.id);
  node.setAttribute('src', value.previewUrl?.startsWith('blob:') ? value.previewUrl : `/images/${value.id}`);
  node.setAttribute(
    'class',
    `rich-text__image rich-text__image--${normalizeManagedImageLayout(value.layout)}`
  );
  node.setAttribute('alt', value.alt.trim().slice(0, ManagedCommentImageAltMaxLength));
  node.setAttribute('loading', 'lazy');
  node.setAttribute('decoding', 'async');
}

function managedImageValueFromNode(node: Element): ManagedImageBlotValue | null {
  const imageId: string | null = normalizeManagedCommentImageId(
    node.getAttribute(ManagedImageIdAttribute)
  ) ?? extractManagedCommentImageId(node.getAttribute('src'));
  if (imageId === null) {
    return null;
  }

  const layoutClass: string | undefined = Array.from(node.classList).find(
    (className: string) => /^rich-text__image--(left|right|center|full)$/i.test(className)
  );
  return {
    id: imageId,
    alt: (node.getAttribute('alt') ?? '').trim().slice(0, ManagedCommentImageAltMaxLength),
    layout: normalizeManagedImageLayout(layoutClass?.replace('rich-text__image--', '') ?? 'full'),
    previewUrl: (node.getAttribute('src') ?? '').startsWith('blob:')
      ? node.getAttribute('src') ?? undefined
      : undefined
  };
}

function normalizeManagedImageLayout(value: string): ManagedImageLayout {
  const normalizedValue: string = value.trim().toLowerCase();
  return normalizedValue === 'left'
    || normalizedValue === 'right'
    || normalizedValue === 'center'
    || normalizedValue === 'full'
    ? normalizedValue
    : 'full';
}

function collectManagedImageIds(
  root: ParentNode,
  htmlSecurityService: HtmlSecurityService
): Set<string> {
  const imageIds: Set<string> = new Set<string>();
  const images: HTMLImageElement[] = Array.from(root.querySelectorAll('img'));
  for (const image of images) {
    const imageId: string | null = normalizeManagedCommentImageId(
      image.getAttribute(ManagedImageIdAttribute)
    ) ?? htmlSecurityService.extractManagedImageId(image.getAttribute('src'));
    if (imageId) {
      imageIds.add(imageId);
    }
  }
  return imageIds;
}
