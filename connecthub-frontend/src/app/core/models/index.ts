export interface User {
  userId: number;
  userName: string;
  displayName: string;
  email: string;
  avatarUrl?: string;
  bio?: string;
  isOnline: boolean;
  lastSeen?: string;
  createdAt: string;
  isActive: boolean;
  role?: string;
}

export interface AuthResponse {
  token: string;
  userId: number;
  userName: string;
  displayName: string;
  email: string;
  avatarUrl?: string;
  role?: string;
}

export interface LoginRequest {
  emailOrUsername: string;
  password: string;
}

export interface RegisterRequest {
  userName: string;
  displayName: string;
  email: string;
  password: string;
}

export interface UpdateProfileRequest {
  displayName?: string;
  bio?: string;
  avatarUrl?: string;
}

export interface Message {
  messageId: number;
  senderId: number;
  receiverId?: number;
  roomId?: number;
  content: string;
  messageType: 'TEXT' | 'IMAGE' | 'FILE' | 'AUDIO';
  isRead: boolean;
  isDeleted: boolean;
  isEdited: boolean;
  sentAt: string;
  readAt?: string;
  editedAt?: string;
  mediaUrl?: string;
  replyToMessageId?: number;
  senderName?: string;
  senderAvatar?: string;
}

export interface RecentChat {
  userId: number;
  userName: string;
  displayName: string;
  avatarUrl?: string;
  lastMessage: string;
  lastMessageAt: string;
  unreadCount: number;
  isOnline: boolean;
}

export interface ChatRoom {
  roomId: number;
  roomName: string;
  description?: string;
  roomType: 'PUBLIC' | 'PRIVATE' | 'DIRECT';
  avatarUrl?: string;
  createdBy: number;
  createdAt: string;
  isActive: boolean;
  maxMembers: number;
  memberCount?: number;
}

export interface RoomMember {
  memberId: number;
  roomId: number;
  userId: number;
  userName?: string;
  displayName?: string;
  avatarUrl?: string;
  role: 'ADMIN' | 'MODERATOR' | 'MEMBER';
  joinedAt: string;
  isActive: boolean;
  isOnline?: boolean;
}

export interface CreateRoomDto {
  roomName: string;
  description?: string;
  roomType: 'PUBLIC' | 'PRIVATE' | 'DIRECT';
  avatarUrl?: string;
  maxMembers: number;
}

export interface Notification {
  notificationId: number;
  recipientId: number;
  senderId?: number;
  type: 'MESSAGE' | 'MENTION' | 'ROOM_INVITE' | 'ROLE_CHANGE' | 'PLATFORM';
  title: string;
  message: string;
  relatedId?: number;
  relatedType?: string;
  isRead: boolean;
  sentAt: string;
  senderName?: string;
  senderAvatar?: string;
}

export interface MediaFile {
  fileId: string;
  uploadedBy: number;
  fileName: string;
  contentType: string;
  fileSizeKb: number;
  blobUrl: string;
  thumbnailUrl?: string;
  messageId?: number;
  roomId?: number;
  uploadedAt: string;
  expiresAt?: string;
}

export interface TypingEvent {
  senderId: number;
  roomId?: number;
  isTyping: boolean;
}

export interface PresenceEvent {
  userId: number;
  isOnline: boolean;
}

export interface SignalRMessage {
  senderId: number;
  receiverId?: number;
  roomId?: number;
  content: string;
  timestamp: string;
  messageType: 'TEXT' | 'IMAGE' | 'FILE' | 'AUDIO';
  mediaUrl?: string;
  replyToMessageId?: number;
}
