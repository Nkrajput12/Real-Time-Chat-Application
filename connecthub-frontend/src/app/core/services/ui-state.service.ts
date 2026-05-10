import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UiStateService {
  private sidebarVisible = new BehaviorSubject<boolean>(window.innerWidth > 768);
  sidebarVisible$ = this.sidebarVisible.asObservable();

  toggleSidebar(): void {
    this.sidebarVisible.next(!this.sidebarVisible.value);
  }

  setSidebarVisible(visible: boolean): void {
    this.sidebarVisible.next(visible);
  }

  get isSidebarVisible(): boolean {
    return this.sidebarVisible.value;
  }
}
