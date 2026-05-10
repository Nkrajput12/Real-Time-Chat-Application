import { Component, OnInit, OnDestroy, ViewChild, ElementRef, ChangeDetectorRef, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { MessageService } from '../../../core/services/message.service';
import { MediaService } from '../../../core/services/media.service';
import { Message, AuthResponse, SignalRMessage, User } from '../../../core/models';
import { UiStateService } from '../../../core/services/ui-state.service';
import { UserProfileModalComponent } from '../user-profile-modal.component';

@Component({
  selector: 'app-direct-message',
  standalone: true,
  imports: [CommonModule, FormsModule, UserProfileModalComponent],
  styleUrl: './direct-message.component.css',
  templateUrl: './direct-message.component.html'
})
export class DirectMessageComponent implements OnInit, OnDestroy {
  @ViewChild('msgContainer') msgContainer!: ElementRef;
  @ViewChild('fileInput') fileInput!: ElementRef;

  receiverId!: number;
  currentUser: AuthResponse | null = null;
  recipient: any = null;
  messages: Message[] = [];
  filteredMessages: Message[] = [];
  loading = true;
  sending = false;
  messageText = '';
  isTyping = false;
  recipientOnline = false;
  searchMode = false;
  searchTerm = '';
  editingMsg: Message | null = null;
  selectedFile: File | null = null;
  filePreview: string | null = null;
  uploading = false;
  lightboxSrc: string | null = null;
  selectedUser: User | null = null;

  private destroy$ = new Subject<void>();
  private typingTimer: any;
  private wasTyping = false;

  constructor(
    private route: ActivatedRoute,
    private auth: AuthService,
    private signalR: SignalRService,
    private messageService: MessageService,
    private mediaService: MediaService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    public uiState: UiStateService
  ) {}

  ngOnInit(): void {
    this.currentUser = this.auth.currentUser;
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      this.receiverId = +params['userId'];
      this.loadConversation();
    });

    // Real-time: incoming direct messages
    this.signalR.messageReceived$.pipe(takeUntil(this.destroy$)).subscribe((msg: SignalRMessage) => {
      const isMine = msg.senderId === this.currentUser?.userId;
      const isForThisConv =
        (msg.senderId === this.receiverId && msg.receiverId === this.currentUser?.userId) ||
        (isMine && msg.receiverId === this.receiverId);
      if (isForThisConv) {
        this.appendMessage(msg);
        this.scrollToBottom();
        this.cdr.detectChanges();
      }
    });

    // Typing indicator
    this.signalR.userTyping$.pipe(takeUntil(this.destroy$)).subscribe(e => {
      if (e.senderId === this.receiverId) {
        this.isTyping = e.isTyping;
        this.cdr.detectChanges();
        if (e.isTyping) {
          clearTimeout(this.typingTimer);
          this.typingTimer = setTimeout(() => { this.isTyping = false; this.cdr.detectChanges(); }, 4000);
        }
      }
    });

    // Presence: Unified state subscription
    this.signalR.onlineUserIds$.pipe(takeUntil(this.destroy$)).subscribe(ids => {
      this.recipientOnline = ids.has(this.receiverId);
      this.cdr.detectChanges();
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    clearTimeout(this.typingTimer);
  }

  loadConversation(): void {
    this.loading = true;
    this.recipient = null;
    this.messages = [];

    // Load recipient info by ID
    this.auth.getUserById(this.receiverId).subscribe({
      next: user => {
        this.recipient = user;
        this.recipientOnline = user.isOnline || false;
      },
      error: () => {
        this.recipient = { userId: this.receiverId, displayName: 'User', userName: 'user', isOnline: false, email: '' };
      }
    });

    this.messageService.getDirectMessages(this.receiverId).subscribe({
      next: msgs => {
        this.messages = msgs.sort((a, b) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime());
        this.filteredMessages = [...this.messages];
        this.loading = false;
        this.cdr.detectChanges();
        setTimeout(() => this.scrollToBottom(), 50);
      },
      error: () => { this.loading = false; this.cdr.detectChanges(); }
    });
  }

  appendMessage(msg: SignalRMessage): void {
    const type = (msg.messageType as any) || 'TEXT';
    const m: Message = {
      messageId: 0, senderId: msg.senderId, receiverId: msg.receiverId,
      content: msg.content, messageType: type, isRead: false,
      mediaUrl: msg.mediaUrl,
      isDeleted: false, isEdited: false, sentAt: msg.timestamp || new Date().toISOString()
    };
    this.messages.push(m);
    this.filteredMessages = this.searchTerm ? this.filteredMessages.filter(x => x.content.toLowerCase().includes(this.searchTerm.toLowerCase())) : [...this.messages];
  }

  searchMessages(): void {
    if (!this.searchTerm.trim()) { this.filteredMessages = [...this.messages]; return; }
    const q = this.searchTerm.toLowerCase();
    this.filteredMessages = this.messages.filter(m => m.content.toLowerCase().includes(q));
  }

  sendMessage(): void {
    if (this.editingMsg) { this.saveEdit(); return; }
    if (this.selectedFile) { this.uploadAndSend(); return; }
    const txt = this.messageText.trim();
    if (!txt || this.sending) return;
    this.sending = true;
    const text = this.messageText;
    this.messageText = '';
    this.signalR.sendDirectMessage(this.receiverId, text, this.recipient?.email).then(() => {
      this.sending = false;
      this.cdr.detectChanges();
    }).catch(() => { this.sending = false; this.messageText = text; this.cdr.detectChanges(); });
  }

  uploadAndSend(): void {
    if (!this.selectedFile) return;
    this.uploading = true;
    this.mediaService.uploadFile(this.selectedFile).subscribe({
      next: media => {
        const isImage = this.selectedFile?.type.startsWith('image/');
        const type = isImage ? 'IMAGE' : 'FILE';
        const content = this.messageText.trim() || `Sent a ${isImage ? 'photo' : 'file'}: ${media.fileName}`;
        
        this.signalR.sendDirectMessage(this.receiverId, content, this.recipient?.email, type, media.blobUrl);
        
        this.uploading = false; this.selectedFile = null; this.filePreview = null; this.messageText = '';
        this.cdr.detectChanges();
      },
      error: (err) => { 
        alert('Failed to upload media. Please try again.');
        this.uploading = false; 
        this.cdr.detectChanges(); 
      }
    });
  }

  startEdit(msg: Message): void {
    this.editingMsg = msg;
    this.messageText = msg.content;
  }

  cancelEdit(): void { this.editingMsg = null; this.messageText = ''; }

  saveEdit(): void {
    if (!this.editingMsg || !this.messageText.trim()) return;
    this.messageService.editMessage(this.editingMsg.messageId, this.messageText.trim()).subscribe({
      next: () => {
        const m = this.messages.find(x => x.messageId === this.editingMsg!.messageId);
        if (m) { m.content = this.messageText.trim(); m.isEdited = true; }
        this.filteredMessages = [...this.messages];
        this.editingMsg = null; this.messageText = '';
        this.cdr.detectChanges();
      }
    });
  }

  deleteMsg(msg: Message): void {
    if (!confirm('Delete this message?')) return;
    this.messageService.deleteMessage(msg.messageId).subscribe({
      next: () => {
        msg.isDeleted = true; msg.content = 'This message was deleted';
        this.cdr.detectChanges();
      }
    });
  }

  onKeyDown(e: KeyboardEvent): void {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); this.sendMessage(); }
    if (e.key === 'Escape' && this.editingMsg) this.cancelEdit();
  }

  onInputChange(): void {
    if (!this.wasTyping) {
      this.signalR.sendTypingIndicator(this.receiverId, true);
      this.wasTyping = true;
    }
    clearTimeout(this.typingTimer);
    this.typingTimer = setTimeout(() => {
      this.signalR.sendTypingIndicator(this.receiverId, false);
      this.wasTyping = false;
    }, 2000);
  }

  onFileSelected(e: Event): void {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.selectedFile = file;
    if (file.type.startsWith('image/')) {
      const reader = new FileReader();
      reader.onload = () => { this.filePreview = reader.result as string; this.cdr.detectChanges(); };
      reader.readAsDataURL(file);
    } else { this.filePreview = null; }
  }

  removeFile(): void { this.selectedFile = null; this.filePreview = null; }

  openMedia(url: string): void { this.lightboxSrc = url; }

  scrollToBottom(): void {
    setTimeout(() => {
      if (this.msgContainer) {
        this.msgContainer.nativeElement.scrollTop = this.msgContainer.nativeElement.scrollHeight;
      }
    }, 50);
  }

  showDateDivider(i: number): boolean {
    if (i === 0) return true;
    const curr = new Date(this.filteredMessages[i].sentAt).toDateString();
    const prev = new Date(this.filteredMessages[i - 1].sentAt).toDateString();
    return curr !== prev;
  }

  getInitials(name: string): string {
    if (!name) return '?';
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  formatTime(d: string): string {
    return new Date(d).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  formatDate(d: string): string {
    const dt = new Date(d);
    const now = new Date();
    if (dt.toDateString() === now.toDateString()) return 'Today';
    const yesterday = new Date(now);
    yesterday.setDate(now.getDate() - 1);
    if (dt.toDateString() === yesterday.toDateString()) return 'Yesterday';
    return dt.toLocaleDateString([], { weekday: 'long', month: 'short', day: 'numeric' });
  }

  renderContent(content: string, deleted: boolean): string {
    if (deleted) return '<em>This message was deleted</em>';
    // Render @mentions
    return content.replace(/@(\w+)/g, '<span style="color:var(--accent-secondary);font-weight:600">@$1</span>');
  }

  viewUser(userId: number): void {
    this.auth.getUserById(userId).subscribe(user => {
      this.selectedUser = user;
      this.cdr.detectChanges();
    });
  }

  onMessageUser(userId: number): void {
    if (userId === this.receiverId) return;
    this.router.navigate(['/chat/dm', userId]);
  }

  toggleSidebar(): void {
    this.uiState.toggleSidebar();
  }
}
