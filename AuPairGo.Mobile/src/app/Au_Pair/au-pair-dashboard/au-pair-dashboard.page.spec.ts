import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AuPairDashboardPage } from './au-pair-dashboard.page';

describe('AuPairDashboardPage', () => {
  let component: AuPairDashboardPage;
  let fixture: ComponentFixture<AuPairDashboardPage>;

  beforeEach(() => {
    fixture = TestBed.createComponent(AuPairDashboardPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
