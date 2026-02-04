import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-shell',
  template: `
    <div class="flex h-screen items-center justify-center bg-background text-foreground">
      <h1 class="text-4xl font-bold">Hello World</h1>
    </div>
  `
})
export class AppShellComponent {}
