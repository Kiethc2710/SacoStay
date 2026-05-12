import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loading',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './loading.component.html',
  styleUrls: ['./loading.component.css']
})
export class LoadingComponent {
  @Input() size: 'small' | 'medium' | 'large' = 'medium';
  @Input() color: 'primary' | 'secondary' = 'primary';

  get spinnerClass(): string {
    const sizeClass = {
      small: 'w-4 h-4',
      medium: 'w-6 h-6',
      large: 'w-8 h-8'
    }[this.size];

    const colorClass = {
      primary: 'text-saco-orange',
      secondary: 'text-gray-500'
    }[this.color];

    return `${sizeClass} ${colorClass}`;
  }
}
