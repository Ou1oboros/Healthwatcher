import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  PagedResult,
  PreviewTarget,
  Target,
  TargetHistoryEntry,
  TargetUptime,
} from '../models/target.model';

// Relative on purpose: the dev-server proxy forwards it in dev, nginx does in the cluster,
// so the browser never needs the API's real host or port.
const API_BASE_URL = '/api';

@Injectable({ providedIn: 'root' })
export class TargetService {
  constructor(private http: HttpClient) {}

  getTargets(pageIndex = 1, pageSize = 20): Observable<PagedResult<PreviewTarget>> {
    const params = new HttpParams()
      .set('pageIndex', pageIndex)
      .set('pageSize', pageSize);

    return this.http.get<PagedResult<PreviewTarget>>(`${API_BASE_URL}/targets`, { params });
  }

  getTargetById(id: string): Observable<Target> {
    return this.http.get<Target>(`${API_BASE_URL}/targets/${id}`);
  }

  getTargetHistory(
    id: string,
    pageIndex = 1,
    pageSize = 20,
    withinHours?: number
  ): Observable<PagedResult<TargetHistoryEntry>> {
    let params = new HttpParams().set('pageIndex', pageIndex).set('pageSize', pageSize);
    if (withinHours) {
      params = params.set('withinHours', withinHours);
    }

    return this.http.get<PagedResult<TargetHistoryEntry>>(
      `${API_BASE_URL}/targets/${id}/history`,
      { params }
    );
  }

  getTargetUptime(id: string, windowHours = 24): Observable<TargetUptime> {
    const params = new HttpParams().set('windowHours', windowHours);

    return this.http.get<TargetUptime>(`${API_BASE_URL}/targets/${id}/uptime`, { params });
  }

  addTarget(url: string): Observable<PreviewTarget> {
    return this.http.post<PreviewTarget>(`${API_BASE_URL}/targets`, { url });
  }

  renameTarget(id: string, name: string): Observable<void> {
    return this.http.patch<void>(`${API_BASE_URL}/targets/${id}`, { name });
  }

  deleteTarget(id: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/targets/${id}`);
  }
}
