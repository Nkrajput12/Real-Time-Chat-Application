import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { SignalRService } from './core/services/signalr.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.html'
})
export class App implements OnInit {
  constructor(private auth: AuthService, private signalR: SignalRService) {}

  ngOnInit(): void {
    // Auto-reconnect SignalR if user already logged in
    if (this.auth.isLoggedIn) {
      this.signalR.startConnection();
    }
  }
}
