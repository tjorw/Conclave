import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StaffTimelineComponent } from './staff-timeline.component';
import { StaffScheduleDto } from 'shared';

describe('StaffTimelineComponent', () => {
  let fixture: ComponentFixture<StaffTimelineComponent>;
  let component: StaffTimelineComponent;

  const schedule: StaffScheduleDto = {
    editionId: 'edition-1',
    staffAreaFilterId: null,
    scheduleDays: [
      { date: '2027-03-01', startTime: '08:00', endTime: '18:00' },
    ],
    staffAreas: [
      {
        staffAreaId: 'area-1',
        name: 'Reception',
        description: null,
        responsibleId: 'resp-1',
        responsibleName: 'Ansvarig',
        stations: [
          {
            stationId: 'station-1',
            name: 'Desk',
            description: null,
            shifts: [
              {
                shiftId: 'shift-1',
                stationId: 'station-1',
                responsibleId: 'resp-1',
                responsibleName: 'Anna',
                start: '2027-03-01T09:00:00',
                end: '2027-03-01T11:00:00',
                minPersons: 1,
                maxPersons: 2,
                activeAssignmentCount: 0,
                confirmedAssignmentCount: 0,
                staffingStatus: 'Unstaffed',
              },
              {
                shiftId: 'shift-2',
                stationId: 'station-1',
                responsibleId: 'resp-2',
                responsibleName: 'Bertil',
                start: '2027-03-01T11:30:00',
                end: '2027-03-01T13:30:00',
                minPersons: 2,
                maxPersons: 3,
                activeAssignmentCount: 1,
                confirmedAssignmentCount: 0,
                staffingStatus: 'UnderMin',
              },
              {
                shiftId: 'shift-3',
                stationId: 'station-1',
                responsibleId: 'resp-3',
                responsibleName: 'Carin',
                start: '2027-03-01T14:00:00',
                end: '2027-03-01T16:00:00',
                minPersons: 2,
                maxPersons: 2,
                activeAssignmentCount: 2,
                confirmedAssignmentCount: 2,
                staffingStatus: 'Full',
              },
            ],
          },
        ],
      },
    ],
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StaffTimelineComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(StaffTimelineComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('schedule', schedule);
    fixture.componentRef.setInput('editionStart', '2027-03-01');
    fixture.componentRef.setInput('editionEnd', '2027-03-01');
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('renders staffing status classes for shift blocks', () => {
    const blocks = Array.from(fixture.nativeElement.querySelectorAll('.st-block')) as HTMLElement[];

    expect(blocks).toHaveLength(3);
    expect(blocks[0].classList.contains('is-unstaffed')).toBe(true);
    expect(blocks[1].classList.contains('is-under-min')).toBe(true);
    expect(blocks[2].classList.contains('is-full')).toBe(true);
  });

  it('emits the selected shift id when a block is clicked', () => {
    const emitSpy = vi.spyOn(component.shiftSelected, 'emit');
    const firstBlock = fixture.nativeElement.querySelector('.st-block') as HTMLElement;

    firstBlock.click();

    expect(emitSpy).toHaveBeenCalledWith('shift-1');
  });
});
