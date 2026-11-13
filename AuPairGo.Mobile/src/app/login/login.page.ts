import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
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
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { eyeOutline, eyeOffOutline } from 'ionicons/icons';

@Component({
  selector: 'app-login',
  templateUrl: './login.page.html',
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
  ],
})
export class LoginPage {
  showPassword: boolean = false;
  private router = inject(Router);

  constructor() {
    addIcons({ eyeOutline, eyeOffOutline });
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  goToForgotPassword(): void {
    this.router.navigateByUrl('/forgot-password');
  }
}
