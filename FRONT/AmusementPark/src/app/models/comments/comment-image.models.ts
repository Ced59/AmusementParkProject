export interface CommentImageUpload {
  readonly id: string;
  readonly url: string;
}

export interface ManagedRichTextImage {
  readonly id: string;
  readonly alt?: string;
  readonly previewUrl?: string;
}
