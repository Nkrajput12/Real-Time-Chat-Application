import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { AuthResponse } from '../../core/models';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrl: './profile.component.css',
  templateUrl: './profile.component.html'
})
export class ProfileComponent implements OnInit {
  currentUser: AuthResponse | null = null;
  form = { displayName: '', bio: '', avatarUrl: '' };
  pwForm = { old: '', new: '', confirm: '' };
  saving = false;
  changingPw = false;
  profileMsg: { type: string; text: string } | null = null;
  pwMsg: { type: string; text: string } | null = null;
  avatarPreview: string | null = null;

  constructor(private auth: AuthService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.currentUser = this.auth.currentUser;
    this.auth.getMyProfile().subscribe({
      next: u => {
        this.currentUser = { ...this.auth.currentUser!, ...u };
        this.form = { displayName: u.displayName || '', bio: u.bio || '', avatarUrl: u.avatarUrl || '' };
        this.cdr.detectChanges();
      }
    });
  }

  saveProfile(): void {
    this.saving = true; this.profileMsg = null;
    this.auth.updateProfile(this.form).subscribe({
      next: () => {
        this.saving = false;
        this.profileMsg = { type: 'success', text: '✅ Profile updated successfully!' };
        setTimeout(() => { this.profileMsg = null; this.cdr.detectChanges(); }, 3000);
        this.cdr.detectChanges();
      },
      error: () => {
        this.saving = false;
        this.profileMsg = { type: 'error', text: '❌ Failed to update profile.' };
        this.cdr.detectChanges();
      }
    });
  }

  changePassword(): void {
    if (!this.pwForm.old || !this.pwForm.new) { this.pwMsg = { type: 'error', text: 'All fields required' }; return; }
    if (this.pwForm.new !== this.pwForm.confirm) { this.pwMsg = { type: 'error', text: 'Passwords do not match' }; return; }
    this.changingPw = true; this.pwMsg = null;
    this.auth.changePassword(this.pwForm.old, this.pwForm.new).subscribe({
      next: () => {
        this.changingPw = false;
        this.pwMsg = { type: 'success', text: '✅ Password changed successfully!' };
        this.pwForm = { old: '', new: '', confirm: '' };
        setTimeout(() => { this.pwMsg = null; this.cdr.detectChanges(); }, 3000);
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.changingPw = false;
        this.pwMsg = { type: 'error', text: err?.error || 'Incorrect current password.' };
        this.cdr.detectChanges();
      }
    });
  }

  onAvatarChange(e: Event): void {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => {
      this.avatarPreview = reader.result as string;
      this.form.avatarUrl = this.avatarPreview;
      this.cdr.detectChanges();
    };
    reader.readAsDataURL(file);
  }

  getInitials(n: string): string {
    if (!n) return '?';
    return n.split(' ').map(x => x[0]).join('').toUpperCase().slice(0, 2);
  }
}
