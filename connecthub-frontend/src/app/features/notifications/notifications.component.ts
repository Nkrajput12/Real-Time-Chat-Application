import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';
import { NotificationService } from '../../core/services/notification.service';
import { SignalRService } from '../../core/services/signalr.service';
import { AuthService } from '../../core/services/auth.service';
import { Notification } from '../../core/models';
import { Router } from '@angular/router';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule],
  styleUrl: './notifications.component.css',
  templateUrl: './notifications.component.html'
})
export class NotificationsComponent implements OnInit, OnDestroy {
  notifications: Notification[] = [];
  filtered: Notification[] = [];
  loading = true;
  tab = 'all';
  unreadCount = 0;
  private destroy$ = new Subject<void>();

  constructor(
    private notifService: NotificationService,
    private signalR: SignalRService,
    private auth: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.load();
    this.notifService.unreadCount$.pipe(takeUntil(this.destroy$)).subscribe(c => {
      this.unreadCount = c; this.cdr.detectChanges();
    });
    this.signalR.notificationCount$.pipe(takeUntil(this.destroy$)).subscribe(c => {
      this.notifService.setUnreadCount(c);
      this.load(); // reload on new notification
    });
  }

  setTab(t: string): void { this.tab = t; this.applyTab(); }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  load(): void {
    const uid = this.auth.currentUser?.userId;
    if (!uid) return;
    this.notifService.getNotifications(uid).subscribe({
      next: n => {
        this.notifications = n.sort((a, b) => new Date(b.sentAt).getTime() - new Date(a.sentAt).getTime());
        this.unreadCount = n.filter(x => !x.isRead).length;
        this.applyTab();
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => { this.loading = false; this.cdr.detectChanges(); }
    });
  }

  applyTab(): void {
    switch (this.tab) {
      case 'unread': this.filtered = this.notifications.filter(n => !n.isRead); break;
      case 'messages': this.filtered = this.notifications.filter(n => n.type === 'MESSAGE'); break;
      case 'mentions': this.filtered = this.notifications.filter(n => n.type === 'MENTION'); break;
      default: this.filtered = [...this.notifications];
    }
  }

  markAllRead(): void {
    this.notifService.markAllRead().subscribe({
      next: () => {
        this.notifications.forEach(n => n.isRead = true);
        this.unreadCount = 0;
        this.applyTab();
        this.cdr.detectChanges();
      }
    });
  }

  handleClick(n: Notification): void {
    if (!n.isRead) {
      this.notifService.markRead(n.notificationId).subscribe(() => {
        n.isRead = true;
        this.unreadCount = Math.max(0, this.unreadCount - 1);
        this.notifService.setUnreadCount(this.unreadCount);
        this.cdr.detectChanges();
      });
    }
    // Navigate based on type
    if (n.type === 'MESSAGE' && n.relatedId) this.router.navigate(['/chat/dm', n.relatedId]);
    if ((n.type === 'MENTION' || n.type === 'ROOM_INVITE') && n.relatedId) this.router.navigate(['/chat/room', n.relatedId]);
  }

  getIcon(type: string): string {
    const map: Record<string, string> = { MESSAGE: '💬', MENTION: '@', ROOM_INVITE: '🚪', ROLE_CHANGE: '⭐', PLATFORM: '📢' };
    return map[type] || '🔔';
  }

  getIconBg(type: string): string {
    const map: Record<string, string> = {
      MESSAGE: 'rgba(108,99,255,0.15)', MENTION: 'rgba(245,158,11,0.15)',
      ROOM_INVITE: 'rgba(59,130,246,0.15)', ROLE_CHANGE: 'rgba(239,68,68,0.15)', PLATFORM: 'rgba(16,185,129,0.15)'
    };
    return map[type] || 'rgba(108,99,255,0.15)';
  }

  formatTime(d: string): string {
    const dt = new Date(d); const now = new Date();
    const diff = now.getTime() - dt.getTime();
    if (diff < 60000) return 'just now';
    if (diff < 3600000) return `${Math.floor(diff / 60000)}m ago`;
    if (diff < 86400000) return `${Math.floor(diff / 3600000)}h ago`;
    return dt.toLocaleDateString([], { month: 'short', day: 'numeric' });
  }
}
