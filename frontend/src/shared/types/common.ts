/** Generic paginated response wrapper matching the backend PagedResult<T>. */
export interface PagedList<T> {
  data: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/** Alias kept for backwards-compatibility with code that refers to PagedResult. */
export type PagedResult<T> = PagedList<T>;

/** RFC 7807 Problem Details returned by the API on errors. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}
