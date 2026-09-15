import type { Server } from 'node:http';

export interface ShutdownSignals {
  on(event: 'SIGTERM' | 'SIGINT', listener: () => void): unknown;
  exit(code: number): unknown;
}

/** Called only after the edge has stopped routing to this front and drained. */
export function installGracefulShutdown(
  server: Pick<Server, 'close'>,
  flush: () => void,
  signals: ShutdownSignals = process,
  deadlineMilliseconds: number = 180_000,
): void {
  let closing = false;
  const stop = (): void => {
    if (closing) {
      return;
    }
    closing = true;
    const deadline = setTimeout(() => signals.exit(1), deadlineMilliseconds);
    server.close((error?: Error): void => {
      clearTimeout(deadline);
      try {
        flush();
      } finally {
        signals.exit(error ? 1 : 0);
      }
    });
  };
  signals.on('SIGTERM', stop);
  signals.on('SIGINT', stop);
}
