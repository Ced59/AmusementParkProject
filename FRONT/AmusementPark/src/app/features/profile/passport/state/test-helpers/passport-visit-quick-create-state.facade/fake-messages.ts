export class FakeMessages {
  readonly details: string[] = [];

  add(_severity: 'success' | 'info' | 'warn' | 'error', _summary: string, detail: string): void {
    this.details.push(detail);
  }
}
