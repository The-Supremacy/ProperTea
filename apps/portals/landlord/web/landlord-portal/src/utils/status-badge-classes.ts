import { cva } from 'class-variance-authority';

export const statusBadgeClasses = cva(
  'inline-flex rounded-full px-2 py-1 text-xs font-semibold',
  {
    variants: {
      status: {
        active: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200',
        inactive: 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-200',
      },
    },
    defaultVariants: { status: 'inactive' },
  },
);
