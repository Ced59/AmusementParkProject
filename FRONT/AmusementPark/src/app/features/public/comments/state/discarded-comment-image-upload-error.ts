export class DiscardedCommentImageUploadError extends Error {
  constructor() {
    super('The comment image draft was discarded.');
  }
}
