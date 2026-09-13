export class FakeSsrHttpStatusService {
  public notFoundCallCount: number = 0;

  setNotFound(): void {
    this.notFoundCallCount += 1;
  }
}
