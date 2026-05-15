import { ApiError } from './api-error.model';

export interface OperationSuccess<TData> {
  succeeded: true;
  data: TData;
  error: null;
}

export interface OperationFailure<TError = ApiError> {
  succeeded: false;
  data: null;
  error: TError;
}

export type OperationResult<TData, TError = ApiError> = OperationSuccess<TData> | OperationFailure<TError>;
