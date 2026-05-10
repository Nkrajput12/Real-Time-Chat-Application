import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Message, RecentChat } from '../models';
import { environment } from '../../../environments/environment';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class MessageService {
  private apiUrl = `${environment.apiUrl}/api/messages`;

  constructor(private http: HttpClient) {}

  getDirectMessages(receiverId: number): Observable<Message[]> {
    return this.http.get<any[]>(`${this.apiUrl}/direct/${receiverId}`).pipe(
      map(msgs => msgs.map(m => this.mapMessage(m)))
    );
  }

  getRoomMessages(roomId: number): Observable<Message[]> {
    return this.http.get<any[]>(`${this.apiUrl}/room/${roomId}`).pipe(
      map(msgs => msgs.map(m => this.mapMessage(m)))
    );
  }

  getRecentChats(): Observable<RecentChat[]> {
    return this.http.get<any[]>(`${this.apiUrl}/recent`).pipe(
      map(msgs => msgs.filter(m => m.receiverId != null).map(m => {
        const rawUser = localStorage.getItem('ch_user');
        const currentUserId = rawUser ? JSON.parse(rawUser).userId : 0;
        const otherUserId = m.senderId === currentUserId ? m.receiverId : m.senderId;
        return {
          userId: otherUserId,
          userName: '',
          displayName: '',
          lastMessage: m.content,
          lastMessageAt: m.sentAt,
          unreadCount: (m.receiverId === currentUserId && !m.isRead) ? 1 : 0,
          isOnline: false
        } as RecentChat;
      }))
    );
  }

  getUnread(): Observable<Message[]> {
    return this.http.get<any[]>(`${this.apiUrl}/unread`).pipe(
      map(msgs => msgs.map(m => this.mapMessage(m)))
    );
  }

  searchMessages(query: string): Observable<Message[]> {
    return this.http.get<any[]>(`${this.apiUrl}/search?query=${encodeURIComponent(query)}`).pipe(
      map(msgs => msgs.map(m => this.mapMessage(m)))
    );
  }

  markRead(id: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/markRead/${id}`, {});
  }

  editMessage(id: number, content: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/edit/${id}`, JSON.stringify(content), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  deleteMessage(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  private mapMessage(m: any): Message {
    return {
      ...m,
      messageType: typeof m.messageType === 'number' 
        ? (m.messageType === 0 ? 'TEXT' : m.messageType === 1 ? 'IMAGE' : m.messageType === 2 ? 'FILE' : 'AUDIO')
        : m.messageType
    };
  }
}
