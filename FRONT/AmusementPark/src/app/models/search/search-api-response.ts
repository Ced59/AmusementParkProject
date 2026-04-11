import { CollectionResponse } from '@shared/models/contracts';

import { SearchResultItem } from './search-result-item';

export type SearchApiResponse = CollectionResponse<SearchResultItem>;
