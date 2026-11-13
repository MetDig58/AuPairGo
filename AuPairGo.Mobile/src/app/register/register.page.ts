import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import type { FormGroup } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  IonContent,
  IonHeader,
  IonToolbar,
  IonButtons,
  IonBackButton,
  IonButton,
  IonCheckbox,
  IonIcon,
  IonInput,
  IonCardContent,
  IonCard,
} from '@ionic/angular';

@Component({
  selector: 'app-register',
  templateUrl: './register.page.html',
  styleUrls: ['./register.page.scss'],
  imports: [
    IonCard,
    IonCardContent,
    IonInput,
    IonIcon,
    IonCheckbox,
    IonButton,
    IonBackButton,
    IonButtons,
    IonContent,
    IonHeader,
    IonToolbar,
    CommonModule,
    FormsModule,
    RouterLink,
  ],
})
export class RegisterPage {
  showPassword: boolean = false;
  showConfirmPassword: boolean = false;
  registerForm!: FormGroup;

  togglePassword(): boolean {
    return (this.showPassword = !this.showPassword);
  }

  toggleConfirmPassword(): boolean {
    return (this.showConfirmPassword = !this.showConfirmPassword);
  }

  constructor() {}
}
