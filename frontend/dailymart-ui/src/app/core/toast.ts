import { Injectable, signal } from '@angular/core';

export interface ToastMessage {
  id: number;
  text: string;
  type: 'info' | 'success' | 'error';
}

/** Replaces MatSnackBar (there is no Material in this app) - a tiny signal-backed queue rendered by
 * ToastContainerComponent, mounted once in the app shell. */
@Injectable({ providedIn: 'root' })
export class Toast {
  private nextId = 0;
  readonly messages = signal<ToastMessage[]>([]);

  show(text: string, type: ToastMessage['type'] = 'info', durationMs = 3000): void {
    const id = this.nextId++;
    this.messages.update((list) => [...list, { id, text, type }]);
    setTimeout(() => this.dismiss(id), durationMs);
  }

  success(text: string, durationMs = 3000): void {
    this.show(text, 'success', durationMs);
  }

  error(text: string, durationMs = 4000): void {
    this.show(text, 'error', durationMs);
  }

  dismiss(id: number): void {
    this.messages.update((list) => list.filter((m) => m.id !== id));
  }
}
