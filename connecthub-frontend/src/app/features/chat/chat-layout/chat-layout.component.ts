import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Observable, Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { MessageService } from '../../../core/services/message.service';
import { RoomService } from '../../../core/services/room.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ThemeService } from '../../../core/services/theme.service';
import { AuthResponse, ChatRoom, RecentChat, SignalRMessage } from '../../../core/models';

@Component({
  selector: 'app-chat-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, FormsModule],
  styleUrl: './chat-layout.component.css',
  templateUrl: './chat-layout.component.html'
})
export class ChatLayoutComponent implements OnInit, OnDestroy {
  currentUser: AuthResponse | null = null;
  recentChats: RecentChat[] = [];
  myRooms: ChatRoom[] = [];
  onlineUserIds = new Set<number>();
  searchQuery = '';
  searchResults: any[] = [];
  unreadNotifs$!: Observable<number>;
  connState: string = 'disconnected';
  activeRoute = '';

  showCreateRoom = false;
  creatingRoom = false;
  createRoomError = '';
  newRoom = { roomName: '', description: '', roomType: 'PUBLIC' as 'PUBLIC' | 'PRIVATE', maxMembers: 100 };
  memberSearchQuery = '';
  memberSearchResults: any[] = [];
  selectedMembers = new Map<number, any>();

  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();

  constructor(
    private auth: AuthService,
    private signalR: SignalRService,
    private messageService: MessageService,
    private roomService: RoomService,
    private notifService: NotificationService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    public themeService: ThemeService
  ) {
    this.unreadNotifs$ = this.notifService.unreadCount$;
  }

  get isAdmin(): boolean {
    return this.currentUser?.role?.toLowerCase() === 'admin';
  }

  ngOnInit(): void {
    this.currentUser = this.auth.currentUser;
    this.activeRoute = this.router.url;
    this.router.events.pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.activeRoute = this.router.url;
    });

    // Load data
    this.loadRecentChats();
    this.loadMyRooms();
    if (this.currentUser) {
      this.notifService.getUnreadCount(this.currentUser.userId).subscribe();
    }

    // SignalR connection state
    this.signalR.connectionState$.pipe(takeUntil(this.destroy$)).subscribe(state => {
      this.connState = state;
      this.cdr.detectChanges();
    });

    // Real-time: new DM received → refresh and sort recent chats
    this.signalR.messageReceived$.pipe(takeUntil(this.destroy$)).subscribe(msg => {
      this.handleIncomingMessage(msg);
    });

    // Real-time: room message
    this.signalR.roomMessageReceived$.pipe(takeUntil(this.destroy$)).subscribe(() => {
      // could refresh room badges
    });

    // Presence: Unified state subscription
    this.signalR.onlineUserIds$.pipe(takeUntil(this.destroy$)).subscribe(ids => {
      this.onlineUserIds = ids;
      this.cdr.detectChanges();
    });

    // Notification count from SignalR
    this.signalR.notificationCount$.pipe(takeUntil(this.destroy$)).subscribe(count => {
      this.notifService.setUnreadCount(count);
    });

    // Search debounce
    this.searchSubject.pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$)).subscribe(q => {
      if (q.trim().length > 1) {
        this.auth.searchUsers(q).subscribe(users => {
          this.searchResults = users;
          this.cdr.detectChanges();
        });
      } else {
        this.searchResults = [];
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadRecentChats(): void {
    this.messageService.getRecentChats().subscribe(chats => {
      this.recentChats = chats.sort((a, b) => new Date(b.lastMessageAt).getTime() - new Date(a.lastMessageAt).getTime());
      this.fetchMissingChatDetails();
      this.cdr.detectChanges();
    });
  }

  private fetchMissingChatDetails(): void {
    this.recentChats.forEach(chat => {
      if (!chat.displayName || chat.displayName === 'User') {
        this.auth.getUserById(chat.userId).subscribe(user => {
          chat.displayName = user.displayName;
          chat.userName = user.userName;
          chat.avatarUrl = user.avatarUrl;
          this.cdr.detectChanges();
        });
      }
    });
  }

  private handleIncomingMessage(msg: SignalRMessage): void {
    const existing = this.recentChats.find(c => c.userId === msg.senderId || (msg.senderId === this.currentUser?.userId && c.userId === msg.receiverId));
    
    if (existing) {
      existing.lastMessage = msg.content;
      existing.lastMessageAt = msg.timestamp;
      if (msg.senderId !== this.currentUser?.userId) existing.unreadCount++;
      // Move to top
      this.recentChats = [existing, ...this.recentChats.filter(c => c !== existing)];
    } else {
      // New conversation
      this.loadRecentChats();
    }
    this.cdr.detectChanges();
  }

  loadMyRooms(): void {
    this.roomService.getMyRooms().subscribe(rooms => {
      this.myRooms = rooms;
      rooms.forEach(r => this.signalR.joinRoom(r.roomId));
      this.cdr.detectChanges();
    });
  }

  onSearch(): void { this.searchSubject.next(this.searchQuery); }
  clearSearch(): void { this.searchQuery = ''; this.searchResults = []; }

  openDM(userId: number): void {
    this.searchResults = [];
    this.searchQuery = '';
    this.router.navigate(['/chat/dm', userId]);
  }

  openRoom(roomId: number): void { this.router.navigate(['/chat/room', roomId]); }

  isOnline(userId: number): boolean { return this.onlineUserIds.has(userId); }

  getInitials(name: string): string {
    if (!name) return '?';
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  formatTime(dateStr: string): string {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    const now = new Date();
    if (d.toDateString() === now.toDateString()) {
      return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }
    return d.toLocaleDateString([], { month: 'short', day: 'numeric' });
  }

  createRoom(): void {
    if (!this.newRoom.roomName.trim()) { this.createRoomError = 'Room name is required'; return; }
    this.creatingRoom = true; this.createRoomError = '';
    this.roomService.createRoom(this.newRoom).subscribe({
      next: res => {
        const memberIds = Array.from(this.selectedMembers.keys());
        if (memberIds.length > 0) {
          // Add members one by one
          const addTasks = memberIds.map(id => this.roomService.addMember(res.id, id).toPromise());
          Promise.all(addTasks).finally(() => {
            this.finishCreateRoom(res.id);
          });
        } else {
          this.finishCreateRoom(res.id);
        }
      },
      error: () => { this.creatingRoom = false; this.createRoomError = 'Failed to create room'; }
    });
  }

  private finishCreateRoom(roomId: number): void {
    this.creatingRoom = false;
    this.showCreateRoom = false;
    this.newRoom = { roomName: '', description: '', roomType: 'PUBLIC', maxMembers: 100 };
    this.selectedMembers.clear();
    this.memberSearchQuery = '';
    this.memberSearchResults = [];
    this.loadMyRooms();
    this.router.navigate(['/chat/room', roomId]);
  }

  onMemberSearch(): void {
    const q = this.memberSearchQuery.trim();
    if (q.length < 2) { this.memberSearchResults = []; return; }
    this.auth.searchUsers(q).subscribe(users => {
      this.memberSearchResults = users.filter(u => u.userId !== this.currentUser?.userId);
      this.cdr.detectChanges();
    });
  }

  toggleUserSelection(user: any): void {
    if (this.selectedMembers.has(user.userId)) {
      this.selectedMembers.delete(user.userId);
    } else {
      this.selectedMembers.set(user.userId, user);
    }
    this.memberSearchQuery = '';
    this.memberSearchResults = [];
  }

  isUserSelected(userId: number): boolean { return this.selectedMembers.has(userId); }
  getSelectedMembersList(): any[] { return Array.from(this.selectedMembers.values()); }

  logout(): void {
    this.signalR.stopConnection();
    this.auth.logout();
  }
}
