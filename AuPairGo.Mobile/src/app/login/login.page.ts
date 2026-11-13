import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  IonContent,
  IonInput,
  IonCard,
  IonCardContent,
  IonCheckbox,
  IonButton,
  IonIcon,
  IonHeader,
  IonButtons,
  IonToolbar,
  IonBackButton,
  IonRouterLink,
  ToastController,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { eyeOutline, eyeOffOutline } from 'ionicons/icons';

import { AuthService } from '../Core/auth/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.page.html',
  standalone: true,
  styleUrls: ['./login.page.scss'],
  imports: [
    IonBackButton,
    IonToolbar,
    IonButtons,
    IonHeader,
    IonIcon,
    IonButton,
    IonCardContent,
    IonContent,
    CommonModule,
    FormsModule,
    IonInput,
    IonCard,
    IonCheckbox,
    IonRouterLink,
    RouterLink,
  ],
})
export class LoginPage {
  email = '';
  password = '';
  rememberMe = false;

  showPassword = false;
  isLoading = false;

  private auth = inject(AuthService);
  private router = inject(Router);
  private toastCtrl = inject(ToastController);

  constructor() {
    addIcons({ eyeOutline, eyeOffOutline });
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  async login(): Promise<void> {
    if (!this.email || !this.password) {
      await this.showToast('Please enter both email and password', 'warning');
      return;
    }

    this.isLoading = true;
    try {
      await this.auth.login(this.email, this.password);
      await this.router.navigateByUrl('/home', { replaceUrl: true });
    } catch (err: any) {
      console.error('Login error:', err);
      const msg = err.error?.title || err.error?.message || err.message || 'Login failed';
      await this.showToast(msg, 'danger');
    } finally {
      this.isLoading = false;
    }
  }

  private async showToast(message: string, color: string) {
    const toast = await this.toastCtrl.create({
      message,
      color,
      duration: 3000,
      position: 'bottom',
    });
    await toast.present();
  }
}
