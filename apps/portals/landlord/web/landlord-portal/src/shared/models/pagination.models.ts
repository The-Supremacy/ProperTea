export interface PaginationQuery {
  page: number;
  pageSize: number;
}

export interface SortQuery {
  field?: string;
  descending?: boolean;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface PaginationMetadata {
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export function getPaginationMetadata(result: PagedResult<unknown>): PaginationMetadata {
  const totalPages = Math.ceil(result.totalCount / result.pageSize);
  return {
    totalPages,
    hasNextPage: result.page < totalPages,
    hasPreviousPage: result.page > 1
  };
}
