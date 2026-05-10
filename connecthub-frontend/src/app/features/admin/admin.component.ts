import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../core/services/admin.service';
import { NotificationService } from '../../core/services/notification.service';
import { AuthService } from '../../core/services/auth.service';
import { SignalRService } from '../../core/services/signalr.service';
import { User } from '../../core/models';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrl: './admin.component.css',
  templateUrl: './admin.component.html'
})
export class AdminComponent implements OnInit {
  tab = 'overview';
  analytics: any = null;
  analyticsLoading = false;
  connCount = 0;
  users: User[] = [];
  filteredUsers: User[] = [];
  usersLoading = false;
  userSearch = '';
  broadcastMsg = '';
  broadcasting = false;
  broadcastResult: { type: string; text: string } | null = null;
  currentUserId: number;

  constructor(
    private adminService: AdminService,
    private notifService: NotificationService,
    private auth: AuthService,
    private signalR: SignalRService,
    private cdr: ChangeDetectorRef
  ) {
    this.currentUserId = this.auth.currentUser?.userId || 0;
  }

  ngOnInit(): void { this.loadAnalytics(); }

  loadAnalytics(): void {
    this.analyticsLoading = true;
    this.adminService.getAnalytics().subscribe(a => { this.analytics = a; this.analyticsLoading = false; this.cdr.detectChanges(); });
  }

  loadUsers(): void {
    if (this.users.length > 0) return;
    this.usersLoading = true;
    this.adminService.getAllUsers().subscribe(u => { this.users = u; this.filteredUsers = u; this.usersLoading = false; this.cdr.detectChanges(); });
  }

  filterUsers(): void {
    const q = this.userSearch.toLowerCase();
    this.filteredUsers = q ? this.users.filter(u => u.displayName?.toLowerCase().includes(q) || u.userName?.toLowerCase().includes(q) || u.email?.toLowerCase().includes(q)) : [...this.users];
  }

  suspendUser(u: User): void {
    this.adminService.suspendUser(u.userId).subscribe({
      next: () => { u.isActive = !u.isActive; this.cdr.detectChanges(); },
      error: () => alert('Action failed — check admin permissions.')
    });
  }

  deleteUser(u: User): void {
    if (!confirm(`Permanently delete @${u.userName}? This cannot be undone.`)) return;
    this.adminService.deleteUser(u.userId).subscribe({
      next: () => { this.users = this.users.filter(x => x.userId !== u.userId); this.filterUsers(); this.cdr.detectChanges(); },
      error: () => alert('Delete failed.')
    });
  }

  broadcast(): void {
    this.broadcasting = true; this.broadcastResult = null;
    this.notifService.broadcast(this.broadcastMsg).subscribe({
      next: () => {
        this.broadcasting = false;
        this.broadcastResult = { type: 'success', text: '✅ Notification sent to all users!' };
        this.broadcastMsg = '';
        setTimeout(() => { this.broadcastResult = null; this.cdr.detectChanges(); }, 4000);
        this.cdr.detectChanges();
      },
      error: () => {
        this.broadcasting = false;
        this.broadcastResult = { type: 'error', text: '❌ Broadcast failed.' };
        this.cdr.detectChanges();
      }
    });
  }

  getInitials(n: string): string {
    if (!n) return '?';
    return n.split(' ').map(x => x[0]).join('').toUpperCase().slice(0, 2);
  }
}
