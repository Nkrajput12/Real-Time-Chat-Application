import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { Notification } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private apiUrl = `${environment.apiUrl}/api/notifications`;
  unreadCount$ = new BehaviorSubject<number>(0);

  constructor(private http: HttpClient) {}

  getNotifications(userId: number, page = 1, size = 50): Observable<Notification[]> {
    return this.http.get<Notification[]>(`${this.apiUrl}/${userId}?page=${page}&size=${size}`);
  }

  getUnreadCount(userId: number): Observable<{ count: number }> {
    return this.http.get<{ count: number }>(`${this.apiUrl}/unread-count/${userId}`).pipe(
      tap(res => this.unreadCount$.next(res.count))
    );
  }

  markRead(id: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}/read`, {});
  }

  markAllRead(): Observable<any> {
    return this.http.put(`${this.apiUrl}/read-all`, {}).pipe(
      tap(() => this.unreadCount$.next(0))
    );
  }

  broadcast(message: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/broadcast`, JSON.stringify(message), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  setUnreadCount(count: number): void {
    this.unreadCount$.next(count);
  }

  decrementUnread(): void {
    const current = this.unreadCount$.value;
    this.unreadCount$.next(Math.max(0, current - 1));
  }
}
