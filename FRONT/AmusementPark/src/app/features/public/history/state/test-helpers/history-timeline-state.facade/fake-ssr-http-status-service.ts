export class FakeSsrHttpStatusService {
  public notFoundCallCount = 0;
  public readonly statusCodes: number[] = [];

  setNotFound(): void {
    this.notFoundCallCount += 1;
  }

  setStatus(statusCode: number): void {
    this.statusCodes.push(statusCode);
  }
}
