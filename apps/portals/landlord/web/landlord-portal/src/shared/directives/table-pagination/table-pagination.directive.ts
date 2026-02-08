import { Directive, input, output, computed, signal } from '@angular/core';

/**
 * Headless pagination directive
 *
 * Provides pagination logic and state management while letting developers
 * control the UI completely. Use with template variable to access navigation
 * methods and computed metadata.
 *
 * @example
 * ```html
 * <div
 *   appTablePagination
 *   [totalCount]="totalCount()"
 *   [pageSize]="pagination().pageSize"
 *   [currentPage]="pagination().page"
 *   (pageChange)="onPageChange($event)"
 *   #pager="appTablePagination">
 *
 *   <button (click)="pager.previous()" [disabled]="!pager.hasPrevious()">
 *     Previous
 *   </button>
 *   <span>Page {{ pager.currentPage() }} of {{ pager.totalPages() }}</span>
 *   <button (click)="pager.next()" [disabled]="!pager.hasNext()">
 *     Next
 *   </button>
 * </div>
 * ```
 */
@Directive({
  selector: '[appTablePagination]',
  exportAs: 'appTablePagination'
})
export class TablePaginationDirective {
  // Inputs
  totalCount = input.required<number>();
  pageSize = input.required<number>();
  currentPage = input.required<number>();

  // Outputs
  pageChange = output<number>();

  // Computed pagination metadata
  totalPages = computed(() => {
    const total = this.totalCount();
    const size = this.pageSize();
    return size > 0 ? Math.ceil(total / size) : 0;
  });

  hasNext = computed(() => {
    return this.currentPage() < this.totalPages();
  });

  hasPrevious = computed(() => {
    return this.currentPage() > 1;
  });

  // Navigation methods
  next(): void {
    if (this.hasNext()) {
      this.pageChange.emit(this.currentPage() + 1);
    }
  }

  previous(): void {
    if (this.hasPrevious()) {
      this.pageChange.emit(this.currentPage() - 1);
    }
  }

  goToPage(page: number): void {
    const targetPage = Math.max(1, Math.min(page, this.totalPages()));
    if (targetPage !== this.currentPage()) {
      this.pageChange.emit(targetPage);
    }
  }

  goToFirst(): void {
    if (this.currentPage() !== 1) {
      this.pageChange.emit(1);
    }
  }

  goToLast(): void {
    const last = this.totalPages();
    if (this.currentPage() !== last) {
      this.pageChange.emit(last);
    }
  }
}
