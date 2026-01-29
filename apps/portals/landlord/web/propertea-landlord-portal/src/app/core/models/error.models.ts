/**
 * Represents a validation error for a specific field with error code for translation
 */
export interface ValidationError {
  field: string;
  errorCode: string;
  parameters?: Record<string, unknown>;
}

/**
 * RFC 9457 ProblemDetails response structure from backend
 */
export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail: string;
  errorCode?: string;
  parameters?: Record<string, unknown>;
}
