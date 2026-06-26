import { Injectable } from '@angular/core';

import { AdminFieldModePosition } from '../models/admin-field-mode.model';

interface GpsIfdValues {
  latitudeRef?: string;
  latitude?: [number, number, number];
  longitudeRef?: string;
  longitude?: [number, number, number];
}

@Injectable({
  providedIn: 'root'
})
export class AdminFieldModePhotoGpsService {
  async readPosition(file: File): Promise<AdminFieldModePosition | null> {
    if (!file.type.toLowerCase().includes('jpeg') && !file.name.toLowerCase().match(/\.(jpe?g)$/)) {
      return null;
    }

    const buffer: ArrayBuffer = await file.arrayBuffer();
    const view = new DataView(buffer);
    const tiffStart: number | null = this.findExifTiffStart(view);
    if (tiffStart === null) {
      return null;
    }

    const littleEndian: boolean | null = this.readEndian(view, tiffStart);
    if (littleEndian === null) {
      return null;
    }

    const firstIfdOffset: number = view.getUint32(tiffStart + 4, littleEndian);
    const gpsIfdOffset: number | null = this.findGpsIfdOffset(view, tiffStart, firstIfdOffset, littleEndian);
    if (gpsIfdOffset === null) {
      return null;
    }

    const gpsValues: GpsIfdValues = this.readGpsIfd(view, tiffStart, gpsIfdOffset, littleEndian);
    if (!gpsValues.latitude || !gpsValues.longitude) {
      return null;
    }

    const latitude: number = this.convertDmsToDecimal(gpsValues.latitude, gpsValues.latitudeRef);
    const longitude: number = this.convertDmsToDecimal(gpsValues.longitude, gpsValues.longitudeRef);

    if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) {
      return null;
    }

    return {
      latitude,
      longitude,
      accuracy: null,
      capturedAt: Date.now()
    };
  }

  private findExifTiffStart(view: DataView): number | null {
    if (view.byteLength < 4 || view.getUint16(0, false) !== 0xffd8) {
      return null;
    }

    let offset = 2;
    while (offset + 4 <= view.byteLength) {
      if (view.getUint8(offset) !== 0xff) {
        return null;
      }

      const marker: number = view.getUint8(offset + 1);
      const size: number = view.getUint16(offset + 2, false);
      if (size < 2 || offset + 2 + size > view.byteLength) {
        return null;
      }

      if (marker === 0xe1 && size >= 8 && this.hasExifHeader(view, offset + 4)) {
        return offset + 10;
      }

      offset += 2 + size;
    }

    return null;
  }

  private hasExifHeader(view: DataView, offset: number): boolean {
    return offset + 6 <= view.byteLength &&
      view.getUint8(offset) === 0x45 &&
      view.getUint8(offset + 1) === 0x78 &&
      view.getUint8(offset + 2) === 0x69 &&
      view.getUint8(offset + 3) === 0x66 &&
      view.getUint8(offset + 4) === 0x00 &&
      view.getUint8(offset + 5) === 0x00;
  }

  private readEndian(view: DataView, tiffStart: number): boolean | null {
    if (tiffStart + 8 > view.byteLength) {
      return null;
    }

    const endianMarker: number = view.getUint16(tiffStart, false);
    if (endianMarker === 0x4949) {
      return true;
    }

    if (endianMarker === 0x4d4d) {
      return false;
    }

    return null;
  }

  private findGpsIfdOffset(view: DataView, tiffStart: number, ifdOffset: number, littleEndian: boolean): number | null {
    const absoluteIfdOffset: number = tiffStart + ifdOffset;
    if (absoluteIfdOffset + 2 > view.byteLength) {
      return null;
    }

    const entryCount: number = view.getUint16(absoluteIfdOffset, littleEndian);
    for (let index = 0; index < entryCount; index += 1) {
      const entryOffset: number = absoluteIfdOffset + 2 + index * 12;
      if (entryOffset + 12 > view.byteLength) {
        return null;
      }

      const tag: number = view.getUint16(entryOffset, littleEndian);
      if (tag === 0x8825) {
        const gpsOffset: number = view.getUint32(entryOffset + 8, littleEndian);
        return gpsOffset;
      }
    }

    return null;
  }

  private readGpsIfd(view: DataView, tiffStart: number, gpsIfdOffset: number, littleEndian: boolean): GpsIfdValues {
    const result: GpsIfdValues = {};
    const absoluteIfdOffset: number = tiffStart + gpsIfdOffset;
    if (absoluteIfdOffset + 2 > view.byteLength) {
      return result;
    }

    const entryCount: number = view.getUint16(absoluteIfdOffset, littleEndian);
    for (let index = 0; index < entryCount; index += 1) {
      const entryOffset: number = absoluteIfdOffset + 2 + index * 12;
      if (entryOffset + 12 > view.byteLength) {
        return result;
      }

      const tag: number = view.getUint16(entryOffset, littleEndian);
      if (tag === 0x0001) {
        result.latitudeRef = this.readAscii(view, entryOffset + 8, 2);
      } else if (tag === 0x0002) {
        result.latitude = this.readRationalDms(view, tiffStart, entryOffset, littleEndian);
      } else if (tag === 0x0003) {
        result.longitudeRef = this.readAscii(view, entryOffset + 8, 2);
      } else if (tag === 0x0004) {
        result.longitude = this.readRationalDms(view, tiffStart, entryOffset, littleEndian);
      }
    }

    return result;
  }

  private readAscii(view: DataView, offset: number, maxLength: number): string {
    let value = '';
    for (let index = 0; index < maxLength && offset + index < view.byteLength; index += 1) {
      const charCode: number = view.getUint8(offset + index);
      if (charCode === 0) {
        break;
      }
      value += String.fromCharCode(charCode);
    }

    return value;
  }

  private readRationalDms(view: DataView, tiffStart: number, entryOffset: number, littleEndian: boolean): [number, number, number] | undefined {
    const type: number = view.getUint16(entryOffset + 2, littleEndian);
    const count: number = view.getUint32(entryOffset + 4, littleEndian);
    const valueOffset: number = view.getUint32(entryOffset + 8, littleEndian);

    if (type !== 5 || count < 3) {
      return undefined;
    }

    const absoluteValueOffset: number = tiffStart + valueOffset;
    if (absoluteValueOffset + 24 > view.byteLength) {
      return undefined;
    }

    return [
      this.readRational(view, absoluteValueOffset, littleEndian),
      this.readRational(view, absoluteValueOffset + 8, littleEndian),
      this.readRational(view, absoluteValueOffset + 16, littleEndian)
    ];
  }

  private readRational(view: DataView, offset: number, littleEndian: boolean): number {
    const numerator: number = view.getUint32(offset, littleEndian);
    const denominator: number = view.getUint32(offset + 4, littleEndian);
    return denominator === 0 ? Number.NaN : numerator / denominator;
  }

  private convertDmsToDecimal(dms: [number, number, number], ref: string | undefined): number {
    const absoluteValue: number = dms[0] + dms[1] / 60 + dms[2] / 3600;
    const normalizedRef: string = (ref ?? '').toUpperCase();

    return normalizedRef === 'S' || normalizedRef === 'W'
      ? -absoluteValue
      : absoluteValue;
  }
}
