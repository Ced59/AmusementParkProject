export class SsrRenderQueueFullError extends Error {
  constructor() {
    super('SSR render queue is full.');
    this.name = 'SsrRenderQueueFullError';
  }
}
