import { Observable, of } from 'rxjs';

import { ImageDto } from '@app/models/images/image-dto';

import { ParkItemsPageStateImagesApiServicePort } from '../../park-items-page-state-data.ports';

export class FakeImagesPort implements ParkItemsPageStateImagesApiServicePort {
  getImages(): Observable<ImageDto[]> {
    return of([]);
  }

  buildImageUrl(): string {
    return '';
  }

  buildImageSrcSet(): string {
    return '';
  }
}
