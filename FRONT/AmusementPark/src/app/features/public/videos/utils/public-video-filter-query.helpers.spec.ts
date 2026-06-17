import { convertToParamMap } from '@angular/router';

import { VideoType } from '@app/models/videos/video-type';
import {
  buildPublicVideoFilterKey,
  buildPublicVideoFilterQueryParams,
  parsePublicVideoFilters
} from './public-video-filter-query.helpers';

describe('public video filter query helpers', () => {
  it('parses supported filters from query params and rejects unknown types', () => {
    const filters = parsePublicVideoFilters(convertToParamMap({
      type: VideoType.ON_RIDE,
      tagId: 'official',
      creatorName: ' Creator '
    }));

    expect(filters).toEqual({
      type: VideoType.ON_RIDE,
      tagId: 'official',
      creatorName: 'Creator'
    });

    expect(parsePublicVideoFilters(convertToParamMap({ type: 'BAD' })).type).toBeNull();
  });

  it('builds nullable query params and stable filter keys', () => {
    const filters = {
      type: null,
      tagId: 'official',
      creatorName: ''
    };

    expect(buildPublicVideoFilterQueryParams(filters)).toEqual({
      type: null,
      tagId: 'official',
      creatorName: null
    });
    expect(buildPublicVideoFilterKey(filters)).toBe('|official|');
  });
});
