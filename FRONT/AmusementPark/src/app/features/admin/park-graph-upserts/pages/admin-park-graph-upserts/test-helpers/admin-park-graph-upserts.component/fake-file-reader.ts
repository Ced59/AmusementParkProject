export function createFakeFileReader(result: string): typeof FileReader {
  return class FakeFileReader {
    public readonly result: string = result;
    public onload: ((this: FileReader, event: ProgressEvent<FileReader>) => void) | null = null;
    public onerror: ((this: FileReader, event: ProgressEvent<FileReader>) => void) | null = null;

    public readAsText(): void {
      this.onload?.call(
        this as unknown as FileReader,
        new ProgressEvent('load') as ProgressEvent<FileReader>,
      );
    }
  } as unknown as typeof FileReader;
}
