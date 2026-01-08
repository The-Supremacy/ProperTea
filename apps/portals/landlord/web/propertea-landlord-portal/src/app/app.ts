import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '@core';
import { LayoutComponent } from './layout/layout.component';
import { LoginPageComponent } from "@features/auth/pages/login/login-page.component";
import { ProgressSpinnerModule } from 'primeng/progressspinner';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule, LayoutComponent, LoginPageComponent, ProgressSpinnerModule],
  templateUrl: './app.html',
  styleUrls: ['./app.scss']
})
export class App {
  protected readonly authService = inject(AuthService);
}
