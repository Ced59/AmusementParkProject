import { CollectionResponse } from '@shared/models/contracts';

import { UserDto } from './user_dto';

export type UsersApiResponse = CollectionResponse<UserDto>;
