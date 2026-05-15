export interface ApiError {
  message: string;
  code?: string | null;
  status?: number | null;
  details?: unknown;
  validationErrors?: Record<string, readonly string[]> | null;
}
