import {
  buildPublicParkItemRouteCommands,
  buildPublicParkRouteCommands,
  PublicParkItemRouteTarget,
  PublicParkRouteTarget
} from '@shared/utils/routing/public-detail-route.helpers';

export type ParkMapDetailTarget = PublicParkRouteTarget;
export type ParkItemMapDetailTarget = PublicParkItemRouteTarget;

export function buildParkMapDetailRouteCommands(target: ParkMapDetailTarget): string[] | null {
  return buildPublicParkRouteCommands(target);
}

export function buildParkItemMapDetailRouteCommands(target: ParkItemMapDetailTarget): string[] | null {
  return buildPublicParkItemRouteCommands(target);
}
