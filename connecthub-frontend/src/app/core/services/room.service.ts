import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ChatRoom, RoomMember, CreateRoomDto } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class RoomService {
  private apiUrl = `${environment.apiUrl}/api/rooms`;

  constructor(private http: HttpClient) {}

  createRoom(dto: CreateRoomDto): Observable<{ id: number }> {
    const payload = {
      ...dto,
      roomType: dto.roomType === 'PUBLIC' ? 0 : dto.roomType === 'PRIVATE' ? 1 : 2
    };
    return this.http.post<{ id: number }>(`${this.apiUrl}/Create`, payload);
  }

  getMyRooms(): Observable<ChatRoom[]> {
    return this.http.get<any[]>(`${this.apiUrl}/my-rooms`).pipe(
      map(rooms => rooms.map(r => ({ 
        ...r, 
        roomType: typeof r.roomType === 'string' ? r.roomType : (r.roomType === 0 ? 'PUBLIC' : r.roomType === 1 ? 'PRIVATE' : 'DIRECT') 
      })))
    );
  }

  getRoomById(id: number): Observable<ChatRoom> {
    return this.http.get<any>(`${this.apiUrl}/GetByID/${id}`).pipe(
      map(r => ({ 
        ...r, 
        roomType: typeof r.roomType === 'string' ? r.roomType : (r.roomType === 0 ? 'PUBLIC' : r.roomType === 1 ? 'PRIVATE' : 'DIRECT') 
      }))
    );
  }

  getPublicRooms(): Observable<ChatRoom[]> {
    return this.http.get<any[]>(`${environment.apiUrl}/api/rooms/public`).pipe(
      map(rooms => rooms.map(r => ({ 
        ...r, 
        roomType: typeof r.roomType === 'string' ? r.roomType : (r.roomType === 0 ? 'PUBLIC' : r.roomType === 1 ? 'PRIVATE' : 'DIRECT') 
      })))
    );
  }

  getMembers(roomId: number): Observable<RoomMember[]> {
    return this.http.get<any[]>(`${this.apiUrl}/Getallmembers/${roomId}`).pipe(
      map(members => members.map(m => ({ 
        ...m, 
        role: typeof m.role === 'string' ? m.role : (m.role === 0 ? 'ADMIN' : m.role === 1 ? 'MODERATOR' : 'MEMBER') 
      })))
    );
  }

  addMember(roomId: number, userId: number): Observable<any> {
    return this.http.post(`${this.apiUrl}/${roomId}/addMember/${userId}`, {}, { responseType: 'text' });
  }

  removeMember(roomId: number, userId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${roomId}/removeMember/${userId}`, { responseType: 'text' });
  }

  leaveRoom(roomId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/leave/${roomId}`, { responseType: 'text' });
  }

  updateRoom(id: number, dto: CreateRoomDto): Observable<any> {
    const payload = {
      ...dto,
      roomType: dto.roomType === 'PUBLIC' ? 0 : dto.roomType === 'PRIVATE' ? 1 : 2
    };
    return this.http.put(`${this.apiUrl}/UpdateRoom/${id}`, payload, { responseType: 'text' });
  }

  deleteRoom(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/DeleteRoom/${id}`, { responseType: 'text' });
  }

  updateMemberRole(roomId: number, userId: number, role: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/${roomId}/members/${userId}/role`, JSON.stringify(role), {
      headers: { 'Content-Type': 'application/json' },
      responseType: 'text' as 'json'
    });
  }
}
