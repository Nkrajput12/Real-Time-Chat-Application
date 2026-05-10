import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private darkModeSubject = new BehaviorSubject<boolean>(true);
  isDarkMode$ = this.darkModeSubject.asObservable();

  constructor() {
    this.initTheme();
  }

  private initTheme(): void {
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme) {
      const isDark = savedTheme === 'dark';
      this.darkModeSubject.next(isDark);
      this.updateBodyClass(isDark);
    } else {
      // Default to dark mode or system preference
      const prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
      this.darkModeSubject.next(prefersDark);
      this.updateBodyClass(prefersDark);
    }
  }

  toggleTheme(): void {
    const isDark = !this.darkModeSubject.value;
    this.darkModeSubject.next(isDark);
    localStorage.setItem('theme', isDark ? 'dark' : 'light');
    this.updateBodyClass(isDark);
  }

  private updateBodyClass(isDark: boolean): void {
    if (isDark) {
      document.body.classList.remove('light-mode');
      document.body.classList.add('dark-mode');
    } else {
      document.body.classList.remove('dark-mode');
      document.body.classList.add('light-mode');
    }
  }
}
