import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent {
  isLoggedIn = false;

  constructor() {
    this.isLoggedIn = localStorage.getItem('user') !== null;
  }

  features = [
    {
      icon: 'heart',
      title: 'Hòa hợp lối sống',
      desc: 'Hệ thống matching thông minh dựa trên thói quen sinh hoạt: giờ giấc, vệ sinh, thú cưng...'
    },
    {
      icon: 'shield',
      title: 'Xác thực an toàn',
      desc: 'Tất cả người dùng đều được xác thực qua thẻ sinh viên hoặc CCCD để đảm bảo an toàn.'
    },
    {
      icon: 'users',
      title: 'Cộng đồng văn minh',
      desc: 'Kết nối với những người bạn cùng trang lứa, cùng trường hoặc cùng sở thích.'
    }
  ];

  trustPoints = [
    '100% người dùng được xác thực danh tính',
    'Đánh giá độ hòa hợp trước khi chat',
    'Thông tin minh bạch, rõ ràng',
    'Hỗ trợ giải quyết mâu thuẫn'
  ];

  getIconClass(iconName: string): string {
    const iconClasses = {
      heart: 'w-7 h-7 text-saco-orange',
      shield: 'w-7 h-7 text-saco-orange',
      users: 'w-7 h-7 text-saco-orange'
    };
    return iconClasses[iconName as keyof typeof iconClasses] || 'w-7 h-7 text-saco-orange';
  }

  getSvgIcon(iconName: string): string {
    const svgIcons = {
      heart: `<svg class="${this.getIconClass(iconName)}" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"></path>
      </svg>`,
      shield: `<svg class="${this.getIconClass(iconName)}" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"></path>
      </svg>`,
      users: `<svg class="${this.getIconClass(iconName)}" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"></path>
      </svg>`
    };
    return svgIcons[iconName as keyof typeof svgIcons] || svgIcons.heart;
  }
}
