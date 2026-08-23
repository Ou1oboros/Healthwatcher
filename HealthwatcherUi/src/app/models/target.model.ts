// Mirrors HealthwatcherApi/Shared/Enums.cs
export enum ConnectionStatus {
  Unknown = 0,
  Up = 1,
  Down = 2,
}

export interface HealthCheckRecord {
  responseTimeMs: number | null;
  status: ConnectionStatus;
  statusCode: number | null;
  error: string | null;
}

// One dashboard row - matches PreviewTargetDto
export interface PreviewTarget {
  id: string;
  name: string;
  url: string;
  status: ConnectionStatus;
  isEnabled: boolean;
  checkedAt: string | null;
  responseTimeMs: number | null;
  statusCode: number | null;
}

// Full target - matches TargetDto
export interface Target {
  id: string;
  createdAt: string;
  updatedAt: string;
  name: string;
  url: string;
  checkedAt: string | null;
  isEnabled: boolean;
  healthCheckRecord: HealthCheckRecord;
}

export interface TargetHistoryEntry {
  id: number;
  checkedAt: string | null;
  healthCheckRecord: HealthCheckRecord;
}

export interface TargetUptime {
  targetId: string;
  windowHours: number;
  totalChecks: number;
  upChecks: number;
  uptimePercentage: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
