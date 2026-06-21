export type AdminPublicViewMode = 'anonymousVisitor' | 'userVisitor' | 'moderatorVisitor' | 'adminPreview';

export interface AdminPublicViewModeDefinition {
  readonly value: AdminPublicViewMode;
  readonly labelKey: string;
  readonly shortLabelKey: string;
  readonly ariaLabelKey: string;
  readonly icon: string;
}

export const DEFAULT_ADMIN_PUBLIC_VIEW_MODE: AdminPublicViewMode = 'anonymousVisitor';

export const ADMIN_PUBLIC_VIEW_MODE_DEFINITIONS: readonly AdminPublicViewModeDefinition[] = [
  {
    value: 'anonymousVisitor',
    labelKey: 'admin.publicViewToolbar.modes.anonymous.label',
    shortLabelKey: 'admin.publicViewToolbar.modes.anonymous.short',
    ariaLabelKey: 'admin.publicViewToolbar.modes.anonymous.aria',
    icon: 'pi-eye'
  },
  {
    value: 'userVisitor',
    labelKey: 'admin.publicViewToolbar.modes.user.label',
    shortLabelKey: 'admin.publicViewToolbar.modes.user.short',
    ariaLabelKey: 'admin.publicViewToolbar.modes.user.aria',
    icon: 'pi-user'
  },
  {
    value: 'moderatorVisitor',
    labelKey: 'admin.publicViewToolbar.modes.moderator.label',
    shortLabelKey: 'admin.publicViewToolbar.modes.moderator.short',
    ariaLabelKey: 'admin.publicViewToolbar.modes.moderator.aria',
    icon: 'pi-user-edit'
  },
  {
    value: 'adminPreview',
    labelKey: 'admin.publicViewToolbar.modes.admin.label',
    shortLabelKey: 'admin.publicViewToolbar.modes.admin.short',
    ariaLabelKey: 'admin.publicViewToolbar.modes.admin.aria',
    icon: 'pi-shield'
  }
];

const ADMIN_PUBLIC_VIEW_MODE_VALUES = new Set<AdminPublicViewMode>(
  ADMIN_PUBLIC_VIEW_MODE_DEFINITIONS.map((definition: AdminPublicViewModeDefinition) => definition.value)
);

export function isAdminPublicViewMode(value: unknown): value is AdminPublicViewMode {
  return typeof value === 'string' && ADMIN_PUBLIC_VIEW_MODE_VALUES.has(value as AdminPublicViewMode);
}
