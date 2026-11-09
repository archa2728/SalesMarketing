import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MarketingTeamList } from './marketing-team-list';

describe('MarketingTeamList', () => {
  let component: MarketingTeamList;
  let fixture: ComponentFixture<MarketingTeamList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MarketingTeamList],
    }).compileComponents();

    fixture = TestBed.createComponent(MarketingTeamList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
