import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ParentDashboardPage } from './parent-dashboard.page';

describe('ParentDashboardPage', () => {
  let component: ParentDashboardPage;
  let fixture: ComponentFixture<ParentDashboardPage>;

  beforeEach(() => {
    fixture = TestBed.createComponent(ParentDashboardPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
