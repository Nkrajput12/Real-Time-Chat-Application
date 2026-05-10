import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { User } from '../../core/models';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-user-profile-modal',
  standalone: true,
  imports: [CommonModule],
  styleUrl: './user-profile-modal.component.css',
  templateUrl: './user-profile-modal.component.html'
})
export class UserProfileModalComponent {
  @Input() user!: User;
  @Input() isMe: boolean = false;
  @Output() close = new EventEmitter<void>();
  @Output() message = new EventEmitter<number>();

  getInitials(name: string): string {
    if (!name) return '?';
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  formatDate(date: string): string {
    if (!date) return 'Unknown';
    return new Date(date).toLocaleDateString([], { month: 'short', year: 'numeric' });
  }

  onMessage(): void {
    this.message.emit(this.user.userId);
    this.close.emit();
  }
}
