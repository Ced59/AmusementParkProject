#!/usr/bin/env bash

is_static_seo_snapshot_enabled() {
  [ "${SSR_SEO_STATIC_SNAPSHOT_ENABLED:-true}" = "true" ]
}
