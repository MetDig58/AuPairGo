import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular';

@Component({
  selector: 'app-au-pair-dashboard',
  templateUrl: './au-pair-dashboard.page.html',
  styleUrls: ['./au-pair-dashboard.page.scss'],
  imports: [IonContent, IonHeader, IonTitle, IonToolbar, CommonModule, FormsModule],
})
export class AuPairDashboardPage implements OnInit {
  constructor() {}

  ngOnInit() {}
}
