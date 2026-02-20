export interface TimelineEntry {
  id: string | number;
  label: string;
  timestamp: Date | string;
  user?: string;
  version: number;
  description: string;
}
