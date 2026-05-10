import { Component, OnInit, OnDestroy, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { MessageService } from '../../../core/services/message.service';
import { RoomService } from '../../../core/services/room.service';
import { MediaService } from '../../../core/services/media.service';
import { Message, ChatRoom, RoomMember, AuthResponse, SignalRMessage, User } from '../../../core/models';
import { UiStateService } from '../../../core/services/ui-state.service';
import { UserProfileModalComponent } from '../user-profile-modal.component';

@Component({
  selector: 'app-room-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, UserProfileModalComponent],
  styleUrl: './room-chat.component.css',
  templateUrl: './room-chat.component.html'
})
export class RoomChatComponent implements OnInit, OnDestroy {
  @ViewChild('msgContainer') msgContainer!: ElementRef;
  @ViewChild('fileInput') fileInput!: ElementRef;

  roomId!: number;
  currentUser: AuthResponse | null = null;
  room: ChatRoom | null = null;
  members: RoomMember[] = [];
  messages: Message[] = [];
  onlineSet = new Set<number>();
  loading = true;
  sending = false;
  messageText = '';
  showMembers = true;
  showAddMember = false;
  showSettings = false;
  addMemberQuery = '';
  addMemberResults: any[] = [];
  typingUsers = new Map<number, any>();
  editRoom = { roomName: '', description: '', roomType: 'PUBLIC' as any, maxMembers: 100 };
  selectedUser: User | null = null;

  private destroy$ = new Subject<void>();
  private typingTimer: any;

  constructor(
    private route: ActivatedRoute,
    private auth: AuthService,
    private signalR: SignalRService,
    private messageService: MessageService,
    private roomService: RoomService,
    private mediaService: MediaService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    public uiState: UiStateService
  ) {}

  get isRoomAdmin(): boolean {
    const me = this.members.find(m => m.userId === this.currentUser?.userId);
    return me?.role === 'ADMIN' || me?.role === 'MODERATOR';
  }

  ngOnInit(): void {
    this.currentUser = this.auth.currentUser;
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.roomId = +params['roomId'];
      this.loadRoom();
    });

    this.signalR.roomMessageReceived$.pipe(takeUntil(this.destroy$)).subscribe((msg: SignalRMessage) => {
      if (msg.roomId === this.roomId) {
        const type = (msg.messageType as any) || 'TEXT';
        this.messages.push({
          messageId: 0, senderId: msg.senderId, roomId: msg.roomId,
          content: msg.content, messageType: type, isRead: true,
          mediaUrl: msg.mediaUrl,
          isDeleted: false, isEdited: false, sentAt: msg.timestamp || new Date().toISOString()
        });
        this.cdr.detectChanges();
        this.scrollToBottom();
      }
    });

    this.signalR.userTyping$.pipe(takeUntil(this.destroy$)).subscribe(e => {
      if (e.senderId !== this.currentUser?.userId) {
        if (e.isTyping) {
          this.typingUsers.set(e.senderId, setTimeout(() => { this.typingUsers.delete(e.senderId); this.cdr.detectChanges(); }, 4000));
        } else {
          clearTimeout(this.typingUsers.get(e.senderId));
          this.typingUsers.delete(e.senderId);
        }
        this.cdr.detectChanges();
      }
    });

    this.signalR.userOnline$.pipe(takeUntil(this.destroy$)).subscribe(id => { this.onlineSet.add(id); this.cdr.detectChanges(); });
    this.signalR.userOffline$.pipe(takeUntil(this.destroy$)).subscribe(id => { this.onlineSet.delete(id); this.cdr.detectChanges(); });
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  loadRoom(): void {
    this.loading = true;
    this.loadRoomData();
    this.messageService.getRoomMessages(this.roomId).subscribe({
      next: msgs => {
        this.messages = msgs.sort((a, b) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime());
        this.loading = false; this.cdr.detectChanges();
        setTimeout(() => this.scrollToBottom(), 50);
      }, error: () => { this.loading = false; this.cdr.detectChanges(); }
    });
    this.signalR.joinRoom(this.roomId);
  }

  loadRoomData(): void {
    this.roomService.getRoomById(this.roomId).subscribe({ 
      next: r => { 
        this.room = r; 
        this.editRoom = { roomName: r.roomName, description: r.description || '', roomType: r.roomType, maxMembers: r.maxMembers }; 
        this.cdr.detectChanges(); 
      } 
    });
    this.loadMembers();
  }

  loadMembers(): void {
    this.roomService.getMembers(this.roomId).subscribe({ 
      next: m => { 
        this.members = m; 
        this.members.forEach(member => {
          if (!member.displayName || member.displayName === `User ${member.userId}`) {
            this.auth.getUserById(member.userId).subscribe({
              next: u => {
                member.displayName = u.displayName;
                member.userName = u.userName;
                member.avatarUrl = u.avatarUrl;
                this.cdr.detectChanges();
              },
              error: () => {
                // Fallback if user not found
                if (!member.displayName) member.displayName = `User ${member.userId}`;
              }
            });
          }
        });
        this.cdr.detectChanges(); 
      } 
    });
  }

  sendMessage(): void {
    const txt = this.messageText.trim();
    if (!txt || this.sending) return;
    this.sending = true;
    this.messageText = '';
    this.signalR.sendRoomMessage(this.roomId, txt).then(() => { this.sending = false; this.cdr.detectChanges(); }).catch(() => { this.sending = false; });
  }

  onKeyDown(e: KeyboardEvent): void { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); this.sendMessage(); } }

  onInputChange(): void {
    this.signalR.sendTypingIndicator(this.roomId, true);
    clearTimeout(this.typingTimer);
    this.typingTimer = setTimeout(() => this.signalR.sendTypingIndicator(this.roomId, false), 2000);
  }

  onFileSelected(e: Event): void {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.sending = true;
    this.mediaService.uploadFile(file, this.roomId).subscribe({ 
      next: m => { 
        const isImage = file.type.startsWith('image/');
        const type = isImage ? 'IMAGE' : 'FILE';
        const content = `Sent a ${isImage ? 'photo' : 'file'}: ${m.fileName}`;
        this.signalR.sendRoomMessage(this.roomId, content, type, m.blobUrl).then(() => {
          this.sending = false;
          this.cdr.detectChanges();
        }); 
      },
      error: (err) => { 
        alert('Failed to upload media. Please try again.');
        this.sending = false; 
        this.cdr.detectChanges(); 
      }
    });
  }

  searchAddUser(): void {
    if (this.addMemberQuery.length < 2) { this.addMemberResults = []; return; }
    this.auth.searchUsers(this.addMemberQuery).subscribe({ next: u => { this.addMemberResults = u.filter(x => !this.members.some(m => m.userId === x.userId)); this.cdr.detectChanges(); } });
  }

  addMember(userId: number): void {
    this.roomService.addMember(this.roomId, userId).subscribe({ 
      next: () => { 
        this.showAddMember = false; 
        this.addMemberQuery = '';
        this.addMemberResults = [];
        this.loadMembers(); 
      },
      error: () => alert('Failed to add member.')
    });
  }

  updateRole(m: RoomMember, newRole: string): void {
    if (m.userId === this.currentUser?.userId) return;
    this.roomService.updateMemberRole(this.roomId, m.userId, newRole).subscribe({
      next: () => {
        m.role = newRole as any;
        this.cdr.detectChanges();
      },
      error: () => alert('Failed to update role. Ensure you have admin permissions.')
    });
  }

  removeMember(m: RoomMember): void {
    if (!confirm(`Remove ${m.displayName || m.userName}?`)) return;
    this.roomService.removeMember(this.roomId, m.userId).subscribe({ next: () => { this.members = this.members.filter(x => x.userId !== m.userId); this.cdr.detectChanges(); } });
  }

  saveSettings(): void {
    this.roomService.updateRoom(this.roomId, this.editRoom).subscribe({ next: () => { this.showSettings = false; this.loadRoom(); } });
  }

  deleteRoom(): void {
    if (!confirm('Permanently delete this room?')) return;
    this.roomService.deleteRoom(this.roomId).subscribe({ next: () => { this.showSettings = false; } });
  }

  getSenderName(id: number): string {
    const m = this.members.find(x => x.userId === id);
    return m?.displayName || m?.userName || `User ${id}`;
  }

  getTypingText(): string {
    const ids = Array.from(this.typingUsers.keys());
    if (ids.length === 0) return '';
    const names = ids.map(id => this.getSenderName(id));
    return names.length === 1 ? `${names[0]} is typing...` : `${names.join(', ')} are typing...`;
  }

  showDateDivider(i: number): boolean {
    if (i === 0) return true;
    return new Date(this.messages[i].sentAt).toDateString() !== new Date(this.messages[i - 1].sentAt).toDateString();
  }

  scrollToBottom(): void {
    setTimeout(() => { if (this.msgContainer) this.msgContainer.nativeElement.scrollTop = this.msgContainer.nativeElement.scrollHeight; }, 50);
  }

  getInitials(n: string): string { if (!n) return '?'; return n.split(' ').map(x => x[0]).join('').toUpperCase().slice(0, 2); }
  formatTime(d: string): string { return new Date(d).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }); }
  formatDate(d: string): string { const dt = new Date(d); const now = new Date(); if (dt.toDateString() === now.toDateString()) return 'Today'; return dt.toLocaleDateString([], { weekday: 'long', month: 'short', day: 'numeric' }); }
  renderContent(c: string, deleted: boolean): string { if (deleted) return '<em>deleted</em>'; return c.replace(/@(\w+)/g, '<span style="color:var(--accent-secondary);font-weight:600">@$1</span>'); }

  viewUser(userId: number): void {
    this.auth.getUserById(userId).subscribe(user => {
      this.selectedUser = user;
      this.cdr.detectChanges();
    });
  }

  onMessageUser(userId: number): void {
    this.router.navigate(['/chat/dm', userId]);
  }

  deleteMessage(msg: Message): void {
    if (!confirm('Are you sure you want to delete this message?')) return;
    this.messageService.deleteMessage(msg.messageId).subscribe({
      next: () => {
        msg.isDeleted = true;
        msg.content = '';
        this.cdr.detectChanges();
      },
      error: () => alert('Failed to delete message. Permissions denied.')
    });
  }

  toggleSidebar(): void {
    this.uiState.toggleSidebar();
  }
}
