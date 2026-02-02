import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTypographyModule } from 'ng-zorro-antd/typography';
import { AuthService } from '../auth/auth.service';
import type { UserProfile } from '../auth/models/auth.models';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [NzLayoutModule, NzButtonModule, NzCardModule, NzTypographyModule],
  template: `
    <nz-layout class="layout">
      <nz-header class="header">
        <span class="logo">SacoStay</span>
        <div class="header-right">
          @if (profile) {
            <span class="user-info">{{ profile.userName }} ({{ profile.email }})</span>
          }
          <button nz-button nzType="default" (click)="logout()">Đăng xuất</button>
        </div>
      </nz-header>
      <nz-content class="content">
        <div class="inner">
          <nz-card nzTitle="Chào mừng">
            @if (profile) {
              <p><strong>User ID:</strong> {{ profile.id }}</p>
              <p><strong>Tên đăng nhập:</strong> {{ profile.userName }}</p>
              <p><strong>Email:</strong> {{ profile.email }}</p>
              <p><strong>Số điện thoại:</strong> {{ profile.phoneNumber || '—' }}</p>
              <p><strong>Vai trò:</strong> {{ profile.roles?.length ? profile.roles.join(', ') : '—' }}</p>
            } @else if (loading) {
              <p>Đang tải thông tin...</p>
            } @else {
              <p>Không tải được thông tin người dùng.</p>
            }
          </nz-card>
        </div>
      </nz-content>
    </nz-layout>
  `,
  styles: [`
    .layout { min-height: 100vh; }
    .header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      background: #001529;
      color: #fff;
      padding: 0 24px;
    }
    .logo { font-size: 1.25rem; font-weight: 600; }
    .header-right { display: flex; align-items: center; gap: 1rem; }
    .user-info { font-size: 0.875rem; opacity: 0.9; }
    .content { padding: 24px; }
    .inner { max-width: 800px; margin: 0 auto; }
  `]
})
export class HomeComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  profile: UserProfile | null = null;
  loading = true;

  ngOnInit(): void {
    this.auth.getProfile().subscribe({
      next: (p) => {
        this.profile = p ?? null;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  logout(): void {
    this.auth.logout();
  }
}
