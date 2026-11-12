import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IonButton, IonCardContent, IonCard, IonContent, IonIcon } from '@ionic/angular';

@Component({
  selector: 'app-home',
  templateUrl: 'home.page.html',
  styleUrls: ['home.page.scss'],
  imports: [IonCard, IonCardContent, RouterLink, IonButton, IonContent, IonIcon],
})
export class HomePage {
  constructor() {}
}
