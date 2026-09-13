export class FakeSsrRuntimeService {
  public useMinimalPublicData = false;

  shouldUseMinimalPublicData(): boolean {
    return this.useMinimalPublicData;
  }
}
