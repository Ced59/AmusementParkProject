export interface ApiProblemDetails {
  type?: string | null;
  title?: string | null;
  status?: number | null;
  detail?: string | null;
  instance?: string | null;
  traceId?: string | null;
  errorCode?: string | null;
  errors?: Record<string, readonly string[]> | null;
}

export type ApiError = ApiProblemDetails;
