import { ShareModerationReason } from '@app/models/sharing/share-moderation.models';

export interface ShareModerationReasonOption {
  readonly labelKey: string;
  readonly value: ShareModerationReason;
}
