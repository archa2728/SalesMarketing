
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet
  ],
  templateUrl: './main-layout.html',
  styleUrl: './main-layout.scss'
})
export class MainLayout {

  constructor(private router: Router) {}

  get adminRole(): number {
    return Number(localStorage.getItem('adminRole'));
  }

  get localStorageUsername(): string {
    return localStorage.getItem('username') || 'Admin';
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('username');
    localStorage.removeItem('adminRole');

    this.router.navigate(['/login']);
  }
}
