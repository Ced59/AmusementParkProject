import {
  ManagedCommentImageAltMaxLength,
  extractManagedCommentImageId,
  normalizeManagedCommentImageId
} from '@shared/utils/comments/managed-comment-image.helpers';

export type ManagedImageLayout = 'left' | 'right' | 'center' | 'full';

export interface ManagedImageBlotValue {
  readonly id: string;
  readonly alt: string;
  readonly layout: ManagedImageLayout;
  readonly previewUrl?: string;
}

export const ManagedImageIdAttribute: string = 'data-managed-image-id';

export function registerManagedImageBlot(quillConstructor: typeof import('quill').default): void {
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

  const ManagedImageBlotClass = class ManagedImageBlot extends BlockEmbed {
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
  };

  quillConstructor.register('formats/managedImage', ManagedImageBlotClass, true);
}

function applyManagedImageValue(node: HTMLElement, value: ManagedImageBlotValue): void {
  node.setAttribute(ManagedImageIdAttribute, value.id);
  node.setAttribute('src', value.previewUrl ?? `/images/${value.id}`);
  node.setAttribute(
    'class',
    `rich-text__image rich-text__image--${normalizeManagedImageLayout(value.layout)}`
  );
  node.setAttribute('alt', value.alt.trim().slice(0, ManagedCommentImageAltMaxLength));
  node.setAttribute('loading', 'lazy');
  node.setAttribute('decoding', 'async');
  node.setAttribute('tabindex', '0');
}

export function managedImageValueFromNode(node: Element): ManagedImageBlotValue | null {
  const managedImageId: string | null = normalizeManagedCommentImageId(
    node.getAttribute(ManagedImageIdAttribute)
  );
  const source: string = node.getAttribute('src') ?? '';
  const imageId: string | null = managedImageId ?? extractManagedCommentImageId(source);
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
    previewUrl: managedImageId !== null && extractManagedCommentImageId(source) === null
      ? source || undefined
      : undefined
  };
}

export function normalizeManagedImageLayout(value: string): ManagedImageLayout {
  const normalizedValue: string = value.trim().toLowerCase();
  return normalizedValue === 'left'
    || normalizedValue === 'right'
    || normalizedValue === 'center'
    || normalizedValue === 'full'
    ? normalizedValue
    : 'full';
}
