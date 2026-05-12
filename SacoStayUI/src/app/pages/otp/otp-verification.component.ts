import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-otp-verification',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './otp-verification.component.html',
  styleUrls: ['./otp-verification.component.css']
})
export class OtpVerificationComponent implements OnInit, OnDestroy {
  otp: string[] = ['', '', '', '', '', ''];
  isLoading = false;
  countdown = 60;
  email = '';
  private countdownTimer: any;

  constructor(private router: Router) {
    this.email = localStorage.getItem('temp_email') || 'your-email@example.com';
  }

  ngOnInit(): void {
    this.startCountdown();
  }

  ngOnDestroy(): void {
    if (this.countdownTimer) {
      clearTimeout(this.countdownTimer);
    }
  }

  startCountdown(): void {
    if (this.countdown > 0) {
      this.countdownTimer = setTimeout(() => {
        this.countdown--;
        this.startCountdown();
      }, 1000);
    }
  }

  handleChange(index: number, value: string): void {
    if (value.length > 1) {
      value = value.slice(0, 1);
    }
    
    if (!/^\d*$/.test(value)) {
      return;
    }

    const newOtp = [...this.otp];
    newOtp[index] = value;
    this.otp = newOtp;

    // Move to next input
    if (value !== '' && index < 5) {
      const nextInput = document.getElementById(`otp-${index + 1}`) as HTMLInputElement;
      nextInput?.focus();
    }
  }

  handleKeyDown(index: number, event: KeyboardEvent): void {
    if (event.key === 'Backspace' && this.otp[index] === '' && index > 0) {
      const prevInput = document.getElementById(`otp-${index - 1}`) as HTMLInputElement;
      prevInput?.focus();
    }
  }

  handleVerify(): void {
    const otpValue = this.otp.join('');
    if (otpValue.length !== 6) {
      return;
    }

    this.isLoading = true;

    setTimeout(() => {
      this.isLoading = false;
      
      // Mock success - create user account and go to home
      const tempName = localStorage.getItem('temp_name') || 'Người dùng mới';
      const tempEmail = localStorage.getItem('temp_email') || '';
      
      localStorage.setItem('user', JSON.stringify({
        id: 'me',
        name: tempName,
        email: tempEmail,
        avatar: `https://ui-avatars.com/api/?name=${encodeURIComponent(tempName)}&background=FF6B6B&color=fff`,
        vipTier: 'free'
      }));

      localStorage.removeItem('temp_email');
      localStorage.removeItem('temp_name');

      const userRole = localStorage.getItem('user_role');
      if (userRole === 'landlord') {
        this.router.navigate(['/identity-verification']);
      } else {
        this.router.navigate(['/']);
      }
    }, 1000);
  }

  handleResend(): void {
    this.countdown = 60;
    this.startCountdown();
    // Mock resend logic
  }

  get isOtpComplete(): boolean {
    return this.otp.join('').length === 6;
  }

  get canResend(): boolean {
    return this.countdown === 0;
  }
}
