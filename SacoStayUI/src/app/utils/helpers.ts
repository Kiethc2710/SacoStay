import { APP_CONSTANTS } from './constants';

export class Helpers {
  // Storage helpers
  static setToken(token: string): void {
    localStorage.setItem(APP_CONSTANTS.TOKEN_KEY, token);
  }

  static getToken(): string | null {
    return localStorage.getItem(APP_CONSTANTS.TOKEN_KEY);
  }

  static removeToken(): void {
    localStorage.removeItem(APP_CONSTANTS.TOKEN_KEY);
  }

  static setUser(user: any): void {
    localStorage.setItem(APP_CONSTANTS.USER_KEY, JSON.stringify(user));
  }

  static getUser(): any | null {
    const userStr = localStorage.getItem(APP_CONSTANTS.USER_KEY);
    return userStr ? JSON.parse(userStr) : null;
  }

  static removeUser(): void {
    localStorage.removeItem(APP_CONSTANTS.USER_KEY);
  }

  // Validation helpers
  static isValidEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  static isValidPhone(phone: string): boolean {
    const phoneRegex = /^[0-9]{10,11}$/;
    return phoneRegex.test(phone);
  }

  static isValidPassword(password: string): boolean {
    return password.length >= APP_CONSTANTS.PASSWORD_MIN_LENGTH;
  }

  // Format helpers
  static formatPhone(phone: string): string {
    return phone.replace(/(\d{3})(\d{3})(\d{4})/, '$1-$2-$3');
  }

  static formatCurrency(amount: number): string {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND'
    }).format(amount);
  }

  static formatDate(date: Date | string): string {
    const d = new Date(date);
    return d.toLocaleDateString('vi-VN');
  }

  // URL helpers
  static buildUrl(base: string, params: Record<string, any>): string {
    const url = new URL(base);
    Object.entries(params).forEach(([key, value]) => {
      if (value !== null && value !== undefined) {
        url.searchParams.set(key, String(value));
      }
    });
    return url.toString();
  }

  // Auth helpers
  static isAuthenticated(): boolean {
    return !!this.getToken();
  }

  static hasRole(role: string): boolean {
    const user = this.getUser();
    return user?.roles?.includes(role);
  }

  // Common helpers
  static generateId(): string {
    return Math.random().toString(36).substr(2, 9);
  }

  static debounce<T extends (...args: any[]) => any>(
    func: T,
    wait: number
  ): (...args: Parameters<T>) => void {
    let timeout: any;
    return (...args: Parameters<T>) => {
      clearTimeout(timeout);
      timeout = setTimeout(() => func.apply(this, args), wait);
    };
  }
}
