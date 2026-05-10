import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { RoomService } from '../../../core/services/room.service';
import { SignalRService } from '../../../core/services/signalr.service';
import { AuthService } from '../../../core/services/auth.service';
import { ChatRoom, CreateRoomDto } from '../../../core/models';

@Component({
  selector: 'app-browse-rooms',
  standalone: true,
  imports: [CommonModule, FormsModule],
  styleUrl: './browse-rooms.component.css',
  templateUrl: './browse-rooms.component.html'
})
export class BrowseRoomsComponent implements OnInit {
  rooms: ChatRoom[] = [];
  myRooms: ChatRoom[] = [];
  filtered: ChatRoom[] = [];
  loading = true;
  searchQuery = '';
  filter = 'all';
  showCreate = false;
  creating = false;
  createError = '';
  newRoom: CreateRoomDto = { roomName: '', description: '', roomType: 'PUBLIC', maxMembers: 100 };

  constructor(private roomService: RoomService, private signalR: SignalRService, private auth: AuthService, private router: Router, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.roomService.getPublicRooms().subscribe(r => { this.rooms = r; this.applyFilter(); this.loading = false; this.cdr.detectChanges(); });
    this.roomService.getMyRooms().subscribe({ next: r => { this.myRooms = r; this.applyFilter(); this.cdr.detectChanges(); } });
  }

  applyFilter(): void {
    let list = [...this.rooms];
    if (this.searchQuery) list = list.filter(r => r.roomName.toLowerCase().includes(this.searchQuery.toLowerCase()) || (r.description || '').toLowerCase().includes(this.searchQuery.toLowerCase()));
    if (this.filter === 'joined') list = list.filter(r => this.isJoined(r.roomId));
    this.filtered = list;
  }

  setFilter(f: string): void { this.filter = f; this.applyFilter(); }
  isJoined(id: number): boolean { return this.myRooms.some(r => r.roomId === id); }

  openRoom(room: ChatRoom): void {
    if (this.isJoined(room.roomId)) { this.router.navigate(['/chat/room', room.roomId]); }
  }

  joinRoom(e: Event, room: ChatRoom): void {
    e.stopPropagation();
    if (this.isJoined(room.roomId)) { this.router.navigate(['/chat/room', room.roomId]); return; }
    this.roomService.addMember(room.roomId, this.auth.currentUser!.userId).subscribe({
      next: () => {
        this.myRooms.push(room);
        this.signalR.joinRoom(room.roomId);
        this.applyFilter();
        this.router.navigate(['/chat/room', room.roomId]);
        this.cdr.detectChanges();
      }
    });
  }

  createRoom(): void {
    if (!this.newRoom.roomName.trim()) { this.createError = 'Name required'; return; }
    this.creating = true; this.createError = '';
    this.roomService.createRoom(this.newRoom).subscribe({
      next: res => {
        this.creating = false; this.showCreate = false;
        this.newRoom = { roomName: '', description: '', roomType: 'PUBLIC', maxMembers: 100 };
        this.load();
        this.router.navigate(['/chat/room', res.id]);
      },
      error: () => { this.creating = false; this.createError = 'Failed to create room'; }
    });
  }
}
