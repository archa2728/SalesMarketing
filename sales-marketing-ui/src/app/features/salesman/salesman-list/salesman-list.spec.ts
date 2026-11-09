import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SalesmanList } from './salesman-list';

describe('SalesmanList', () => {
  let component: SalesmanList;
  let fixture: ComponentFixture<SalesmanList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SalesmanList],
    }).compileComponents();

    fixture = TestBed.createComponent(SalesmanList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
