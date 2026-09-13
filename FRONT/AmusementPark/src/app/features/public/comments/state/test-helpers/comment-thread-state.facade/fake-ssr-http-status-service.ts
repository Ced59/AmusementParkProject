export class FakeSsrHttpStatusService {
  readonly statuses: number[] = [];
  notFoundCallCount: number = 0;

  setNotFound(): void {
    this.notFoundCallCount += 1;
  }

  setStatus(status: number): void {
    this.statuses.push(status);
  }
}
