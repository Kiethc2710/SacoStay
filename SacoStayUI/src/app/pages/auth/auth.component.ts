import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-auth',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './auth.component.html',
  styleUrls: ['./auth.component.css']
})
export class AuthComponent implements OnInit {
  currentMode: 'login' | 'register' = 'login';
  
  loginForm!: FormGroup;
  registerForm!: FormGroup;
  
  loginLoading = false;
  registerLoading = false;
  loginError = '';
  registerError = '';
  
  selectedRole: 'tenant' | 'landlord' = 'tenant';

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.initForms();
    // Lấy mode từ URL query params
    const urlParams = new URLSearchParams(window.location.search);
    this.currentMode = (urlParams.get('mode') as 'login' | 'register') || 'login';
  }

  private initForms(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });

    this.registerForm = this.fb.group({
      username: ['', Validators.required],
      fullName: ['', Validators.required],
      phone: ['', [Validators.required, Validators.pattern('^[0-9]{10,11}$')]],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });
  }

  passwordMatchValidator(form: FormGroup): { [key: string]: boolean } | null {
    const password = form.get('password')?.value;
    const confirmPassword = form.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  switchMode(mode: 'login' | 'register'): void {
    this.currentMode = mode;
    // Cập nhật URL mà không reload
    const url = new URL(window.location.href);
    if (mode === 'login') {
      url.searchParams.delete('mode');
    } else {
      url.searchParams.set('mode', 'register');
    }
    window.history.replaceState({}, '', url.toString());
  }

  selectRole(role: 'tenant' | 'landlord'): void {
    this.selectedRole = role;
  }

  submitLogin(): void {
    if (this.loginForm.invalid) {
      Object.values(this.loginForm.controls).forEach(control => {
        control.markAsDirty();
        control.updateValueAndValidity();
      });
      return;
    }

    this.loginLoading = true;
    this.loginError = '';

    const loginData = {
      emailPhoneorUsername: this.loginForm.value.email,
      password: this.loginForm.value.password
    };

    this.authService.login(loginData).subscribe({
      next: (response: any) => {
        this.loginLoading = false;
        localStorage.setItem('token', response.token);
        if (response.user) {
          localStorage.setItem('user', JSON.stringify(response.user));
        }
        alert('Đăng nhập thành công');
        this.router.navigate(['/']);
      },
      error: (err: any) => {
        this.loginLoading = false;
        const status = err?.status;
        if (status === 401) {
          this.loginError = 'Email/số điện thoại hoặc mật khẩu không đúng.';
        } else if (status === 0 || err?.message?.includes('Http failure')) {
          this.loginError = 'Không kết nối được API hoặc sai tên đăng nhập/mật khẩu. Nếu đã bật backend, kiểm tra CORS.';
        } else {
          this.loginError = err?.error?.message || 'Đăng nhập thất bại. Thử lại sau.';
        }
        alert(this.loginError);
      }
    });
  }

  submitRegister(): void {
    if (this.registerForm.invalid) {
      Object.values(this.registerForm.controls).forEach(control => {
        control.markAsDirty();
        control.updateValueAndValidity();
      });
      return;
    }

    this.registerLoading = true;
    this.registerError = '';

    const registerData = {
      username: this.registerForm.value.username,
      email: this.registerForm.value.email,
      password: this.registerForm.value.password,
      firstName: this.registerForm.value.fullName.split(' ')[0],
      lastName: this.registerForm.value.fullName.split(' ').slice(1).join(' '),
      phoneNumber: this.registerForm.value.phone,
      role: this.selectedRole
    };

    this.authService.register(registerData).subscribe({
      next: (response: any) => {
        this.registerLoading = false;
        
        // Store temp data for OTP verification
        localStorage.setItem('temp_email', registerData.email);
        localStorage.setItem('temp_name', registerData.firstName + ' ' + registerData.lastName);
        localStorage.setItem('user_role', registerData.role);
        
        // Navigate to OTP verification
        this.router.navigate(['/otp-verification']);
      },
      error: (err: any) => {
        this.registerLoading = false;
        this.registerError = err?.error?.message || 'Đăng ký thất bại. Thử lại sau.';
        alert(this.registerError);
      }
    });
  }
}
