import { Component, inject } from '@angular/core';
import { Toast } from '../../core/toast';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  template: `
    <div class="toast-stack">
      @for (message of toast.messages(); track message.id) {
        <div class="toast" [class.success]="message.type === 'success'" [class.error]="message.type === 'error'" (click)="toast.dismiss(message.id)">
          {{ message.text }}
        </div>
      }
    </div>
  `
})
export class ToastContainerComponent {
  protected readonly toast = inject(Toast);
}
