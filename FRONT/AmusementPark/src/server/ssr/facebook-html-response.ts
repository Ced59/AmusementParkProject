import {
  prepareRobotHtmlForResponse,
  RobotHtmlPreparationOptions,
  RobotHtmlPreparationResult,
} from '../../app/core/ssr/robot-html-optimizer';
import {
  injectFacebookAppIdMeta,
  injectFacebookImageOverrideMeta,
} from './facebook-open-graph-meta';

export function prepareFacebookHtmlResponse(
  html: string,
  requestUrl: string,
  publicUrl: string,
  facebookAppId: string | null,
  robotOptions: RobotHtmlPreparationOptions,
): RobotHtmlPreparationResult {
  const preparationResult: RobotHtmlPreparationResult = prepareRobotHtmlForResponse(
    html,
    robotOptions,
  );
  const htmlWithFacebookAppId: string = injectFacebookAppIdMeta(
    preparationResult.html,
    facebookAppId,
  );
  const finalHtml: string = injectFacebookImageOverrideMeta(
    htmlWithFacebookAppId,
    requestUrl,
    publicUrl,
  );

  return {
    ...preparationResult,
    html: finalHtml,
  };
}
