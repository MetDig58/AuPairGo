import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  IonHeader,
  IonTitle,
  IonToolbar,
  IonIcon,
  IonFooter,
  IonButton,
  IonButtons,
  IonMenuButton,
  IonContent,
  IonCardContent,
  IonCard,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { gridOutline, mapOutline, notificationsOutline } from 'ionicons/icons';

@Component({
  selector: 'app-parent-dashboard',
  templateUrl: './parent-dashboard.page.html',
  styleUrls: ['./parent-dashboard.page.scss'],
  imports: [
    IonCard,
    IonCardContent,
    IonButtons,
    IonButton,
    IonFooter,
    IonIcon,
    IonHeader,
    IonTitle,
    IonToolbar,
    CommonModule,
    FormsModule,
    IonMenuButton,
    IonContent,
  ],
})
export class ParentDashboardPage {
  constructor() {
    addIcons({ notificationsOutline, mapOutline, gridOutline });
  }
}
