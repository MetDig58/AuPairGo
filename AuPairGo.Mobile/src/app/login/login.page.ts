import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
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
    IonRouterLink,
    RouterLink,
  ],
})
export class LoginPage {
  showPassword: boolean = false;

  constructor() {
    addIcons({ eyeOutline, eyeOffOutline });
  }

  togglePassword(): boolean {
    return (this.showPassword = !this.showPassword);
  }
}
