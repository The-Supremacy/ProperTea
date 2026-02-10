export interface EntityDetailsAction {
  label: string;
  icon?: string;
  variant?: 'default' | 'outline' | 'destructive';
  handler: () => void | Promise<void>;
  separatorBefore?: boolean;
  disabled?: boolean;
}

export interface EntityDetailsConfig {
  title: string;
  subtitle?: string;
  showBackButton?: boolean;
  showRefresh?: boolean;
  primaryActions?: EntityDetailsAction[];
  secondaryActions?: EntityDetailsAction[];
}
