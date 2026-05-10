import { Injectable, NgZone } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';
import { Message, SignalRMessage, TypingEvent, PresenceEvent } from '../models';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hubConnection!: signalR.HubConnection;

  // Subjects for real-time events
  messageReceived$ = new Subject<SignalRMessage>();
  roomMessageReceived$ = new Subject<SignalRMessage>();
  userTyping$ = new Subject<{ senderId: number; roomId?: number; recipientId?: number; isTyping: boolean }>();
  userOnline$ = new Subject<number>();
  userOffline$ = new Subject<number>();
  notificationCount$ = new Subject<number>();
  readReceipt$ = new Subject<{ messageId: number; readAt: string }>();
  onlineUsersReceived$ = new Subject<number[]>();

  connectionState$ = new BehaviorSubject<'connected' | 'disconnected' | 'connecting'>('disconnected');
  onlineUserIds$ = new BehaviorSubject<Set<number>>(new Set<number>());
  private onlineUserIds = new Set<number>();

  private joinedRooms = new Set<number>();

  constructor(private authService: AuthService, private zone: NgZone) {}

  startConnection(): void {
    const token = this.authService.getToken();
    if (!token) return;

    // Avoid duplicate connections
    if (this.hubConnection && this.hubConnection.state !== signalR.HubConnectionState.Disconnected) {
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl, {
        accessTokenFactory: () => token,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.registerHandlers();

    this.connectionState$.next('connecting');
    this.hubConnection
      .start()
      .then(() => {
        this.connectionState$.next('connected');
        console.log('[SignalR] Connected');
        // Re-join rooms after connect
        this.joinedRooms.forEach(id => this.joinRoom(id));
      })
      .catch(err => {
        console.error('[SignalR] Connection failed:', err);
        this.connectionState$.next('disconnected');
      });

    this.hubConnection.onreconnected(() => {
      this.connectionState$.next('connected');
      this.joinedRooms.forEach(id => this.joinRoom(id));
    });

    this.hubConnection.onreconnecting(() => this.connectionState$.next('connecting'));
    this.hubConnection.onclose(() => this.connectionState$.next('disconnected'));
  }

  stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
    }
  }

  private registerHandlers(): void {
    this.hubConnection.on('ReceiveMessage', (msg: SignalRMessage) => {
      this.zone.run(() => this.messageReceived$.next(msg));
    });

    this.hubConnection.on('ReceiveRoomMessage', (msg: SignalRMessage) => {
      this.zone.run(() => this.roomMessageReceived$.next(msg));
    });

    this.hubConnection.on('UserTyping', (senderId: number, isTyping: boolean) => {
      this.zone.run(() => this.userTyping$.next({ senderId, isTyping }));
    });

    this.hubConnection.on('UserOnline', (userId: number) => {
      this.zone.run(() => {
        this.onlineUserIds.add(userId);
        this.onlineUserIds$.next(new Set(this.onlineUserIds));
        this.userOnline$.next(userId);
      });
    });

    this.hubConnection.on('UserOffline', (userId: number) => {
      this.zone.run(() => {
        this.onlineUserIds.delete(userId);
        this.onlineUserIds$.next(new Set(this.onlineUserIds));
        this.userOffline$.next(userId);
      });
    });

    this.hubConnection.on('NotificationCount', (count: number) => {
      this.zone.run(() => this.notificationCount$.next(count));
    });

    this.hubConnection.on('MessageRead', (messageId: number, readAt: string) => {
      this.zone.run(() => this.readReceipt$.next({ messageId, readAt }));
    });

    this.hubConnection.on('OnlineUsers', (userIds: number[]) => {
      this.zone.run(() => {
        this.onlineUserIds = new Set(userIds);
        this.onlineUserIds$.next(new Set(this.onlineUserIds));
        this.onlineUsersReceived$.next(userIds);
      });
    });
  }

  async sendDirectMessage(receiverId: number, content: string, recipientEmail?: string, type: string = 'TEXT', mediaUrl?: string): Promise<void> {
    if (this.hubConnection?.state !== signalR.HubConnectionState.Connected) return;
    await this.hubConnection.invoke('SendDirectMessage', receiverId, content, recipientEmail ?? null, type, mediaUrl ?? null);
  }

  async sendRoomMessage(roomId: number, content: string, type: string = 'TEXT', mediaUrl?: string): Promise<void> {
    if (this.hubConnection?.state !== signalR.HubConnectionState.Connected) return;
    await this.hubConnection.invoke('SendRoomMessage', roomId, content, type, mediaUrl ?? null);
  }

  async sendTypingIndicator(roomId: number, isTyping: boolean): Promise<void> {
    if (this.hubConnection?.state !== signalR.HubConnectionState.Connected) return;
    await this.hubConnection.invoke('TypingIndicator', roomId, isTyping).catch(() => {});
  }

  async joinRoom(roomId: number): Promise<void> {
    if (this.hubConnection?.state !== signalR.HubConnectionState.Connected) return;
    this.joinedRooms.add(roomId);
    await this.hubConnection.invoke('JoinRoom', roomId).catch(() => {});
  }

  async leaveRoom(roomId: number): Promise<void> {
    this.joinedRooms.delete(roomId);
  }

  get isConnected(): boolean {
    return this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }
}
