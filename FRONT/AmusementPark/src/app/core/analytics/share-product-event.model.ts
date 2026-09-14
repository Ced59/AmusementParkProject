export type ShareProductRecapType =
  | 'visit-recap'
  | 'year-recap'
  | 'passport-profile'
  | 'personal-ranking'
  | 'profile-comparison';

export type ShareProductEventType =
  | 'share_activation_started'
  | 'share_preview_created'
  | 'share_published'
  | 'share_revoked'
  | 'share_rotated'
  | 'share_opened'
  | 'share_cta_passport_started'
  | 'share_render_failed';

export interface ShareProductEvent {
  readonly type: ShareProductEventType;
  readonly recapType: ShareProductRecapType;
}
