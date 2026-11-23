import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular';

@Component({
  selector: 'app-parent-dashboard',
  templateUrl: './parent-dashboard.page.html',
  styleUrls: ['./parent-dashboard.page.scss'],
  imports: [IonContent, IonHeader, IonTitle, IonToolbar, CommonModule, FormsModule],
})
export class ParentDashboardPage implements OnInit {
  constructor() {}

  ngOnInit() {}
}
