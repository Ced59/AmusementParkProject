import { Observable } from 'rxjs';

import { PublicParkNavigationTreeVideosApiServicePort } from '../../public-park-navigation-tree-data.ports';

import { VideoDto } from '@app/models/videos/video-dto';

export class FakeVideosPort implements PublicParkNavigationTreeVideosApiServicePort {
  getVideoById(): Observable<VideoDto> {
    throw new Error('Video should not be loaded for park detail navigation.');
  }
}
