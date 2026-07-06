export interface PublicSitemapLoadOptions {
  readonly includeDescendants: boolean;
  readonly loadDescendantsInInitialRequest: boolean;
}

export function resolvePublicSitemapLoadOptions(isServerSideRender: boolean): PublicSitemapLoadOptions {
  if (isServerSideRender) {
    return {
      includeDescendants: false,
      loadDescendantsInInitialRequest: false
    };
  }

  return {
    includeDescendants: true,
    loadDescendantsInInitialRequest: false
  };
}
