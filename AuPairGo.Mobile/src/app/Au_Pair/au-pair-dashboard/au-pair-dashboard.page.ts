import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonHeader, IonTitle, IonToolbar, IonButtons, IonIcon, IonButton, IonMenuButton } from '@ionic/angular';

@Component({
  selector: 'app-au-pair-dashboard',
  templateUrl: './au-pair-dashboard.page.html',
  styleUrls: ['./au-pair-dashboard.page.scss'],
  imports: [IonButton, IonIcon, IonButtons, IonHeader, IonTitle, IonToolbar, CommonModule, FormsModule, IonMenuButton],
})
export class AuPairDashboardPage {
  constructor() {}
}
