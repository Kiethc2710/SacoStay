import { Component, inject, OnInit } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router } from '@angular/router';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzMessageService } from 'ng-zorro-antd/message';
import { AuthService } from '../auth.service';

function confirmPasswordMatch(control: AbstractControl): ValidationErrors | null {
  const password = control.parent?.get('password')?.value;
  const confirm = control?.value;
  if (!confirm) return null;
  return password === confirm ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-auth-page',
  standalone: true,
  imports: [NzFormModule, NzInputModule, NzButtonModule, ReactiveFormsModule],
  template: `
    <div class="auth-page" [class.swapped]="!isLogin">
      <!-- Panel 1: Branding / Hình (tráo với Panel 2 khi đổi mode) -->
      <div class="panel panel-branding">
        @if (isLogin) {
          <div class="panel-inner branding-login">
            <h1 class="auth-logo">SacoStay</h1>
            <p class="auth-tagline">Tìm chỗ ở phù hợp với bạn</p>
            <div class="brand-placeholder">
              <span>Ảnh SacoStay</span>
              <small>(Sau này thêm ảnh vào đây)</small>
            </div>
            <div class="auth-decoration">
              <div class="decoration-circle c1"></div>
              <div class="decoration-circle c2"></div>
              <div class="decoration-circle c3"></div>
            </div>
          </div>
        } @else {
          <div class="panel-inner branding-register">
            <h1 class="auth-logo">Tham gia SacoStay</h1>
            <p class="auth-tagline">Tạo tài khoản và khám phá chỗ ở lý tưởng</p>
            <div class="brand-placeholder register-img">
              <span>Ảnh Đăng ký</span>
              <small>(Hình khác cho màn đăng ký)</small>
            </div>
            <div class="auth-decoration">
              <div class="decoration-circle c1"></div>
              <div class="decoration-circle c2"></div>
              <div class="decoration-circle c3"></div>
            </div>
          </div>
        }
      </div>

      <!-- Panel 2: Form (tráo với Panel 1 khi đổi mode) -->
      <div class="panel panel-form">
        <div class="panel-inner form-inner">
          @if (isLogin) {
            <div class="auth-card">
              <h2 class="auth-title">Đăng nhập</h2>
              <p class="auth-desc">Nhập thông tin để truy cập tài khoản</p>
              <form nz-form [formGroup]="loginForm" (ngSubmit)="submitLogin()" nzLayout="vertical" class="auth-form">
                <nz-form-item>
                  <nz-form-label [nzRequired]="true">Email / SĐT / Tên đăng nhập</nz-form-label>
                  <nz-form-control nzErrorTip="Vui lòng nhập thông tin đăng nhập">
                    <input nz-input formControlName="emailPhoneorUsername" placeholder="email&#64;example.com hoặc 0912345678" size="large" />
                  </nz-form-control>
                </nz-form-item>
                <nz-form-item>
                  <nz-form-label [nzRequired]="true">Mật khẩu</nz-form-label>
                  <nz-form-control nzErrorTip="Vui lòng nhập mật khẩu">
                    <input nz-input type="password" formControlName="password" placeholder="••••••••" size="large" />
                  </nz-form-control>
                </nz-form-item>
                @if (loginError) {
                  <nz-form-item>
                    <div class="form-error">{{ loginError }}</div>
                  </nz-form-item>
                }
                <nz-form-item>
                  <button nz-button nzType="primary" nzBlock nzSize="large" [nzLoading]="loginLoading" type="submit" class="auth-submit-btn">Đăng nhập</button>
                </nz-form-item>
              </form>
              <p class="auth-switch">
                Chưa có tài khoản?
                <a href="javascript:void(0)" (click)="switchToRegister()">Đăng ký ngay</a>
              </p>
            </div>
          } @else {
            <div class="auth-card">
              <h2 class="auth-title">Đăng ký</h2>
              <p class="auth-desc">Tạo tài khoản để bắt đầu sử dụng SacoStay</p>
              <form nz-form [formGroup]="registerForm" (ngSubmit)="submitRegister()" nzLayout="vertical" class="auth-form">
                <nz-form-item>
                  <nz-form-label [nzRequired]="true">Họ và tên</nz-form-label>
                  <nz-form-control nzErrorTip="Vui lòng nhập họ tên">
                    <input nz-input formControlName="fullName" placeholder="Nguyễn Văn A" size="large" />
                  </nz-form-control>
                </nz-form-item>
                <nz-form-item>
                  <nz-form-label [nzRequired]="true">Email</nz-form-label>
                  <nz-form-control nzErrorTip="Vui lòng nhập email hợp lệ">
                    <input nz-input type="email" formControlName="email" placeholder="email&#64;example.com" size="large" />
                  </nz-form-control>
                </nz-form-item>
                <nz-form-item>
                  <nz-form-label [nzRequired]="true">Tên đăng nhập</nz-form-label>
                  <nz-form-control nzErrorTip="Vui lòng nhập tên đăng nhập">
                    <input nz-input formControlName="username" placeholder="username" size="large" />
                  </nz-form-control>
                </nz-form-item>
                <nz-form-item>
                  <nz-form-label [nzRequired]="true">Mật khẩu</nz-form-label>
                  <nz-form-control nzErrorTip="Mật khẩu tối thiểu 6 ký tự">
                    <input nz-input type="password" formControlName="password" placeholder="••••••••" size="large" />
                  </nz-form-control>
                </nz-form-item>
                <nz-form-item>
                  <nz-form-label [nzRequired]="true">Xác nhận mật khẩu</nz-form-label>
                  <nz-form-control [nzErrorTip]="registerForm.get('confirmPassword')?.hasError('passwordMismatch') ? 'Mật khẩu không trùng khớp' : 'Vui lòng xác nhận mật khẩu'">
                    <input nz-input type="password" formControlName="confirmPassword" placeholder="••••••••" size="large" />
                  </nz-form-control>
                </nz-form-item>
                @if (registerSuccess) {
                  <nz-form-item>
                    <div class="form-success">{{ registerSuccess }}</div>
                  </nz-form-item>
                }
                <nz-form-item>
                  <button nz-button nzType="primary" nzBlock nzSize="large" [nzLoading]="registerLoading" type="submit" class="auth-submit-btn">Đăng ký</button>
                </nz-form-item>
              </form>
              <p class="auth-switch">
                Đã có tài khoản?
                <a href="javascript:void(0)" (click)="switchToLogin()">Đăng nhập</a>
              </p>
            </div>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-page {
      position: relative;
      min-height: 100vh;
      width: 100%;
      font-family: var(--auth-font);
      overflow: hidden;
    }
    .panel {
      position: absolute;
      width: 50%;
      top: 0;
      bottom: 0;
      transition: left 0.6s cubic-bezier(0.4, 0, 0.2, 1);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem;
      transform: translateZ(0);
      will-change: left;
    }
    .panel-branding {
      left: 0;
      background: var(--auth-bg-left);
      color: #fff;
    }
    .auth-page.swapped .panel-branding {
      left: 50%;
    }
    .panel-form {
      left: 50%;
      background: #fafafa;
    }
    .auth-page.swapped .panel-form {
      left: 0;
    }
    .panel-inner {
      position: relative;
      z-index: 1;
      width: 100%;
      max-width: 420px;
    }
    @keyframes formContentIn {
      from { opacity: 0; transform: translateX(12px); }
      to { opacity: 1; transform: translateX(0); }
    }
    @keyframes brandingContentIn {
      from { opacity: 0; transform: translateX(-12px); }
      to { opacity: 1; transform: translateX(0); }
    }
    .branding-login,
    .branding-register {
      animation: brandingContentIn 0.45s cubic-bezier(0.4, 0, 0.2, 1) both;
    }
    .branding-login .auth-logo,
    .branding-register .auth-logo {
      font-size: 2.25rem;
      font-weight: 700;
      margin: 0 0 0.5rem 0;
      letter-spacing: -0.02em;
    }
    .auth-tagline {
      font-size: 1.125rem;
      opacity: 0.95;
      margin: 0 0 1.5rem 0;
      font-weight: 500;
    }
    .brand-placeholder {
      background: rgba(255,255,255,0.12);
      border: 2px dashed rgba(255,255,255,0.4);
      border-radius: 12px;
      padding: 3rem 2rem;
      text-align: center;
      margin-top: 1rem;
    }
    .brand-placeholder span {
      display: block;
      font-size: 1rem;
      font-weight: 600;
    }
    .brand-placeholder small {
      font-size: 0.8rem;
      opacity: 0.85;
    }
    .brand-placeholder.register-img {
      background: rgba(255,255,255,0.08);
      border-color: rgba(255,255,255,0.3);
    }
    .auth-decoration {
      position: absolute;
      top: -50%;
      right: -50%;
      width: 100%;
      height: 100%;
      pointer-events: none;
    }
    .decoration-circle {
      position: absolute;
      border-radius: 50%;
      background: rgba(255,255,255,0.08);
    }
    .decoration-circle.c1 { width: 240px; height: 240px; top: 10%; right: 10%; }
    .decoration-circle.c2 { width: 160px; height: 160px; top: 50%; right: 30%; }
    .decoration-circle.c3 { width: 80px; height: 80px; bottom: 20%; right: 60%; }

    .auth-card {
      width: 100%;
      max-width: 420px;
      background: #fff;
      padding: 2.5rem;
      border-radius: 16px;
      box-shadow: var(--auth-card-shadow);
      animation: formContentIn 0.45s cubic-bezier(0.4, 0, 0.2, 1) both;
    }
    .auth-title {
      font-size: 1.5rem;
      font-weight: 700;
      margin: 0 0 0.25rem 0;
      color: #111;
    }
    .auth-desc {
      color: #64748b;
      font-size: 0.9375rem;
      margin: 0 0 1.75rem 0;
    }
    .auth-form .ant-form-item { margin-bottom: 1.25rem; }
    .auth-form .ant-form-item:last-child { margin-bottom: 0; }
    .auth-submit-btn {
      height: 48px !important;
      font-weight: 600;
      border-radius: 10px;
      background: var(--auth-primary) !important;
      border-color: var(--auth-primary) !important;
    }
    .auth-submit-btn:hover {
      background: var(--auth-primary-hover) !important;
      border-color: var(--auth-primary-hover) !important;
    }
    .form-error { color: #dc2626; font-size: 0.875rem; padding: 0.5rem 0; }
    .form-success { color: #059669; font-size: 0.875rem; padding: 0.5rem 0; }
    .auth-switch {
      text-align: center;
      margin: 1.5rem 0 0 0;
      font-size: 0.9375rem;
      color: #64748b;
    }
    .auth-switch a {
      color: var(--auth-primary);
      font-weight: 600;
      text-decoration: none;
      cursor: pointer;
    }
    .auth-switch a:hover { text-decoration: underline; }

    @media (max-width: 900px) {
      .auth-page {
        display: flex;
        flex-direction: column;
      }
      .panel {
        position: relative !important;
        width: 100% !important;
        left: 0 !important;
        min-height: 45vh;
      }
      .panel-branding { order: 1; }
      .panel-form { order: 2; }
      .auth-page.swapped .panel-branding { order: 2; }
      .auth-page.swapped .panel-form { order: 1; }
    }
  `]
})
export class AuthPageComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly message = inject(NzMessageService);

  isLogin = true;
  loginLoading = false;
  loginError = '';
  registerLoading = false;
  registerSuccess = '';

  loginForm = this.fb.nonNullable.group({
    emailPhoneorUsername: ['', [Validators.required]],
    password: ['', [Validators.required]]
  });

  registerForm = this.fb.nonNullable.group(
    {
      fullName: ['', [Validators.required]],
      email: ['', [Validators.required, Validators.email]],
      username: ['', [Validators.required]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required, confirmPasswordMatch]]
    }
  );

  ngOnInit(): void {
    const path = this.router.url.split('?')[0];
    if (path === '/register') this.isLogin = false;
    this.registerForm.get('password')?.valueChanges?.subscribe(() => {
      this.registerForm.get('confirmPassword')?.updateValueAndValidity();
    });
  }

  switchToRegister(): void {
    this.isLogin = false;
    this.router.navigate(['/register'], { replaceUrl: true });
  }

  switchToLogin(): void {
    this.isLogin = true;
    this.router.navigate(['/login'], { replaceUrl: true });
  }

  submitLogin(): void {
    this.loginError = '';
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }
    this.loginLoading = true;
    this.auth.login(this.loginForm.getRawValue()).subscribe({
      next: (res) => {
        this.loginLoading = false;
        if (res?.token) {
          this.message.success('Đăng nhập thành công');
          this.router.navigate(['/']);
        }
      },
      error: (err) => {
        this.loginLoading = false;
        const status = err?.status;
        if (status === 401) {
          this.loginError = 'Email/số điện thoại hoặc mật khẩu không đúng.';
        } else if (status === 0 || err?.message?.includes('Http failure')) {
          this.loginError = 'Không kết nối được API hoặc sai tên đăng nhập/mật khẩu. Nếu đã bật backend, kiểm tra CORS.';
        } else {
          this.loginError = err?.error?.message || 'Đăng nhập thất bại. Thử lại sau.';
        }
        this.message.error(this.loginError);
      }
    });
  }

  submitRegister(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }
    this.registerLoading = true;
    this.registerSuccess = '';
    setTimeout(() => {
      this.registerLoading = false;
      this.registerSuccess = 'Form đã sẵn sàng. Tính năng đăng ký sẽ được kết nối với backend. Hiện tại vui lòng liên hệ admin để tạo tài khoản.';
      this.message.info(this.registerSuccess);
    }, 800);
  }
}
