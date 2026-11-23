import { Component } from '@angular/core';
import {
  IonHeader,
  IonFooter,
  IonToolbar,
  IonTitle,
  IonButtons,
  IonMenuButton,
  IonIcon,
  IonButton,
  IonContent,
  IonCard,
  IonCardContent,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import {
  bookOutline,
  carSportOutline,
  notificationsOutline,
  alertCircle,
  timeOutline,
  pinSharp,
  locationSharp,
} from 'ionicons/icons';

@Component({
  selector: 'app-au-pair-dashboard',
  templateUrl: './au-pair-dashboard.page.html',
  styleUrls: ['./au-pair-dashboard.page.scss'],
  imports: [
    IonButton,
    IonIcon,
    IonButtons,
    IonTitle,
    IonToolbar,
    IonFooter,
    IonHeader,
    IonMenuButton,
    IonContent,
    IonCard,
    IonCardContent,
  ],
})
export class AuPairDashboardPage {
  constructor() {
    addIcons({ notificationsOutline, carSportOutline, bookOutline, alertCircle, timeOutline, pinSharp, locationSharp });
  }
}
