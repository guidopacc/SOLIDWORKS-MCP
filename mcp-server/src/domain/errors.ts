export type SolidWorksMcpErrorCode =
  | 'precondition_failed'
  | 'not_found'
  | 'invalid_input'
  | 'unsupported_operation'
  | 'internal_error';

export interface SolidWorksMcpErrorDetails {
  [key: string]: unknown;
}

export class SolidWorksMcpError extends Error {
  override readonly name = 'SolidWorksMcpError';

  constructor(
    readonly code: SolidWorksMcpErrorCode,
    message: string,
    readonly details: SolidWorksMcpErrorDetails = {},
    readonly retryable = false,
  ) {
    super(message);
  }
}

export function isSolidWorksMcpError(
  error: unknown,
): error is SolidWorksMcpError {
  return error instanceof SolidWorksMcpError;
}

export function preconditionFailed(
  message: string,
  details: SolidWorksMcpErrorDetails = {},
): SolidWorksMcpError {
  return new SolidWorksMcpError('precondition_failed', message, details);
}

export function notFound(
  message: string,
  details: SolidWorksMcpErrorDetails = {},
): SolidWorksMcpError {
  return new SolidWorksMcpError('not_found', message, details);
}

export function invalidInput(
  message: string,
  details: SolidWorksMcpErrorDetails = {},
): SolidWorksMcpError {
  return new SolidWorksMcpError('invalid_input', message, details);
}

export function unsupportedOperation(
  message: string,
  details: SolidWorksMcpErrorDetails = {},
): SolidWorksMcpError {
  return new SolidWorksMcpError('unsupported_operation', message, details);
}

export function internalError(
  message: string,
  details: SolidWorksMcpErrorDetails = {},
): SolidWorksMcpError {
  return new SolidWorksMcpError('internal_error', message, details, true);
}

export function formatToolErrorEnvelope(error: unknown): {
  ok: false;
  error: {
    code: SolidWorksMcpErrorCode;
    message: string;
    retryable: boolean;
    details: SolidWorksMcpErrorDetails;
  };
} {
  if (isSolidWorksMcpError(error)) {
    return {
      ok: false,
      error: {
        code: error.code,
        message: error.message,
        retryable: error.retryable,
        details: error.details,
      },
    };
  }

  const fallbackMessage =
    error instanceof Error ? error.message : 'Unexpected internal error';

  return {
    ok: false,
    error: {
      code: 'internal_error',
      message: fallbackMessage,
      retryable: true,
      details: {},
    },
  };
}
