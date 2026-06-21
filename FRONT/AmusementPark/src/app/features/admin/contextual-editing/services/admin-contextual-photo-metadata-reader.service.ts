import { Injectable } from '@angular/core';

import { ImageGeoLocation } from '@app/models/images/image-geo-location';

export type AdminContextualPhotoSourceKind = 'file' | 'remote';
export type AdminContextualPhotoGeoStatus = 'detected' | 'missing' | 'unavailable';

export interface AdminContextualPhotoMetadataPreview {
  readonly sourceKind: AdminContextualPhotoSourceKind;
  readonly fileName: string | null;
  readonly contentType: string | null;
  readonly sizeInBytes: number | null;
  readonly width: number | null;
  readonly height: number | null;
  readonly geoLocation: ImageGeoLocation | null;
  readonly geoStatus: AdminContextualPhotoGeoStatus;
}

@Injectable({
  providedIn: 'root'
})
export class AdminContextualPhotoMetadataReaderService {
  async readFile(file: File): Promise<AdminContextualPhotoMetadataPreview> {
    const dimensions: { width: number | null; height: number | null } = await this.readImageDimensionsFromFile(file);
    const geoLocation: ImageGeoLocation | null = await this.tryReadJpegGeoLocation(file);

    return {
      sourceKind: 'file',
      fileName: file.name,
      contentType: file.type || null,
      sizeInBytes: file.size,
      width: dimensions.width,
      height: dimensions.height,
      geoLocation,
      geoStatus: geoLocation ? 'detected' : 'missing'
    };
  }

  async readRemoteUrl(sourceUrl: string): Promise<AdminContextualPhotoMetadataPreview> {
    const dimensions: { width: number | null; height: number | null } = await this.readImageDimensionsFromUrl(sourceUrl);

    return {
      sourceKind: 'remote',
      fileName: null,
      contentType: null,
      sizeInBytes: null,
      width: dimensions.width,
      height: dimensions.height,
      geoLocation: null,
      geoStatus: 'unavailable'
    };
  }

  private readImageDimensionsFromFile(file: File): Promise<{ width: number | null; height: number | null }> {
    const objectUrl: string = URL.createObjectURL(file);

    return this.readImageDimensionsFromUrl(objectUrl).finally((): void => URL.revokeObjectURL(objectUrl));
  }

  private readImageDimensionsFromUrl(url: string): Promise<{ width: number | null; height: number | null }> {
    return new Promise((resolve: (value: { width: number | null; height: number | null }) => void): void => {
      const image: HTMLImageElement = new Image();

      image.onload = (): void => {
        resolve({
          width: image.naturalWidth > 0 ? image.naturalWidth : null,
          height: image.naturalHeight > 0 ? image.naturalHeight : null
        });
      };
      image.onerror = (): void => resolve({ width: null, height: null });
      image.src = url;
    });
  }

  private async tryReadJpegGeoLocation(file: File): Promise<ImageGeoLocation | null> {
    if (!this.isJpeg(file)) {
      return null;
    }

    try {
      const buffer: ArrayBuffer = await file.arrayBuffer();
      return this.readGpsFromJpeg(buffer);
    } catch {
      return null;
    }
  }

  private isJpeg(file: File): boolean {
    const contentType: string = file.type.toLowerCase();
    const fileName: string = file.name.toLowerCase();

    return contentType === 'image/jpeg' ||
      contentType === 'image/jpg' ||
      fileName.endsWith('.jpg') ||
      fileName.endsWith('.jpeg');
  }

  private readGpsFromJpeg(buffer: ArrayBuffer): ImageGeoLocation | null {
    const view: DataView = new DataView(buffer);
    if (view.byteLength < 4 || view.getUint16(0, false) !== 0xffd8) {
      return null;
    }

    let offset: number = 2;
    while (offset + 4 < view.byteLength) {
      if (view.getUint8(offset) !== 0xff) {
        return null;
      }

      const marker: number = view.getUint8(offset + 1);
      const segmentLength: number = view.getUint16(offset + 2, false);
      if (segmentLength < 2 || offset + 2 + segmentLength > view.byteLength) {
        return null;
      }

      if (marker === 0xe1 && this.hasExifHeader(view, offset + 4)) {
        return this.readGpsFromExif(view, offset + 10, segmentLength - 8);
      }

      offset += 2 + segmentLength;
    }

    return null;
  }

  private hasExifHeader(view: DataView, offset: number): boolean {
    return offset + 6 <= view.byteLength &&
      view.getUint8(offset) === 0x45 &&
      view.getUint8(offset + 1) === 0x78 &&
      view.getUint8(offset + 2) === 0x69 &&
      view.getUint8(offset + 3) === 0x66 &&
      view.getUint8(offset + 4) === 0 &&
      view.getUint8(offset + 5) === 0;
  }

  private readGpsFromExif(view: DataView, tiffOffset: number, tiffLength: number): ImageGeoLocation | null {
    if (tiffOffset + 8 > view.byteLength || tiffLength < 8) {
      return null;
    }

    const littleEndian: boolean | null = this.readEndian(view, tiffOffset);
    if (littleEndian === null || view.getUint16(tiffOffset + 2, littleEndian) !== 42) {
      return null;
    }

    const firstIfdOffset: number = view.getUint32(tiffOffset + 4, littleEndian);
    const gpsIfdOffset: number | null = this.findIfdPointer(view, tiffOffset, tiffLength, firstIfdOffset, 0x8825, littleEndian);
    if (gpsIfdOffset === null) {
      return null;
    }

    return this.readGpsIfd(view, tiffOffset, tiffLength, gpsIfdOffset, littleEndian);
  }

  private readEndian(view: DataView, offset: number): boolean | null {
    const byteOrder: number = view.getUint16(offset, false);
    if (byteOrder === 0x4949) {
      return true;
    }

    if (byteOrder === 0x4d4d) {
      return false;
    }

    return null;
  }

  private findIfdPointer(
    view: DataView,
    tiffOffset: number,
    tiffLength: number,
    ifdOffset: number,
    tagId: number,
    littleEndian: boolean
  ): number | null {
    const ifdAbsoluteOffset: number = tiffOffset + ifdOffset;
    if (ifdAbsoluteOffset + 2 > tiffOffset + tiffLength) {
      return null;
    }

    const entryCount: number = view.getUint16(ifdAbsoluteOffset, littleEndian);
    for (let index: number = 0; index < entryCount; index++) {
      const entryOffset: number = ifdAbsoluteOffset + 2 + index * 12;
      if (entryOffset + 12 > tiffOffset + tiffLength) {
        return null;
      }

      const currentTagId: number = view.getUint16(entryOffset, littleEndian);
      if (currentTagId === tagId) {
        return view.getUint32(entryOffset + 8, littleEndian);
      }
    }

    return null;
  }

  private readGpsIfd(
    view: DataView,
    tiffOffset: number,
    tiffLength: number,
    ifdOffset: number,
    littleEndian: boolean
  ): ImageGeoLocation | null {
    const ifdAbsoluteOffset: number = tiffOffset + ifdOffset;
    if (ifdAbsoluteOffset + 2 > tiffOffset + tiffLength) {
      return null;
    }

    const entryCount: number = view.getUint16(ifdAbsoluteOffset, littleEndian);
    let latitudeRef: string | null = null;
    let longitudeRef: string | null = null;
    let latitudeParts: readonly number[] | null = null;
    let longitudeParts: readonly number[] | null = null;

    for (let index: number = 0; index < entryCount; index++) {
      const entryOffset: number = ifdAbsoluteOffset + 2 + index * 12;
      if (entryOffset + 12 > tiffOffset + tiffLength) {
        return null;
      }

      const tagId: number = view.getUint16(entryOffset, littleEndian);
      if (tagId === 1) {
        latitudeRef = this.readAsciiEntry(view, entryOffset);
      } else if (tagId === 2) {
        latitudeParts = this.readRationalArrayEntry(view, tiffOffset, tiffLength, entryOffset, littleEndian, 3);
      } else if (tagId === 3) {
        longitudeRef = this.readAsciiEntry(view, entryOffset);
      } else if (tagId === 4) {
        longitudeParts = this.readRationalArrayEntry(view, tiffOffset, tiffLength, entryOffset, littleEndian, 3);
      }
    }

    if (!latitudeRef || !longitudeRef || !latitudeParts || !longitudeParts) {
      return null;
    }

    const latitude: number | null = this.convertDmsToDecimal(latitudeParts, latitudeRef);
    const longitude: number | null = this.convertDmsToDecimal(longitudeParts, longitudeRef);
    if (latitude === null || longitude === null) {
      return null;
    }

    return { latitude, longitude };
  }

  private readAsciiEntry(view: DataView, entryOffset: number): string | null {
    const firstByte: number = view.getUint8(entryOffset + 8);
    return firstByte > 0 ? String.fromCharCode(firstByte).toUpperCase() : null;
  }

  private readRationalArrayEntry(
    view: DataView,
    tiffOffset: number,
    tiffLength: number,
    entryOffset: number,
    littleEndian: boolean,
    expectedCount: number
  ): readonly number[] | null {
    const type: number = view.getUint16(entryOffset + 2, littleEndian);
    const count: number = view.getUint32(entryOffset + 4, littleEndian);
    const valueOffset: number = view.getUint32(entryOffset + 8, littleEndian);
    const absoluteOffset: number = tiffOffset + valueOffset;

    if (type !== 5 || count !== expectedCount || absoluteOffset + expectedCount * 8 > tiffOffset + tiffLength) {
      return null;
    }

    const values: number[] = [];
    for (let index: number = 0; index < expectedCount; index++) {
      const rationalOffset: number = absoluteOffset + index * 8;
      const numerator: number = view.getUint32(rationalOffset, littleEndian);
      const denominator: number = view.getUint32(rationalOffset + 4, littleEndian);
      if (denominator === 0) {
        return null;
      }

      values.push(numerator / denominator);
    }

    return values;
  }

  private convertDmsToDecimal(parts: readonly number[], ref: string): number | null {
    if (parts.length !== 3 || !parts.every((value: number) => Number.isFinite(value))) {
      return null;
    }

    const sign: number = ref === 'S' || ref === 'W' ? -1 : 1;
    return sign * (parts[0] + parts[1] / 60 + parts[2] / 3600);
  }
}
