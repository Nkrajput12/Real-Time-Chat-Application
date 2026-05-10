import { Routes } from '@angular/router';
import { AuthGuard, GuestGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'chat', pathMatch: 'full' },
  {
    path: 'auth',
    canActivate: [GuestGuard],
    children: [
      {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
      },
      {
        path: 'register',
        loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent)
      },
      { path: '', redirectTo: 'login', pathMatch: 'full' }
    ]
  },
  {
    path: '',
    canActivate: [AuthGuard],
    loadComponent: () => import('./features/chat/chat-layout/chat-layout.component').then(m => m.ChatLayoutComponent),
    children: [
      { path: 'chat', loadComponent: () => import('./features/chat/chat-welcome/chat-welcome.component').then(m => m.ChatWelcomeComponent) },
      { path: 'chat/dm/:userId', loadComponent: () => import('./features/chat/direct-message/direct-message.component').then(m => m.DirectMessageComponent) },
      { path: 'chat/room/:roomId', loadComponent: () => import('./features/chat/room-chat/room-chat.component').then(m => m.RoomChatComponent) },
      { path: 'rooms', loadComponent: () => import('./features/rooms/browse-rooms/browse-rooms.component').then(m => m.BrowseRoomsComponent) },
      { path: 'notifications', loadComponent: () => import('./features/notifications/notifications.component').then(m => m.NotificationsComponent) },
      { path: 'profile', loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent) },
      { path: 'admin', loadComponent: () => import('./features/admin/admin.component').then(m => m.AdminComponent) },
      { path: '', redirectTo: 'chat', pathMatch: 'full' }
    ]
  },
  { path: '**', redirectTo: 'chat' }
];
