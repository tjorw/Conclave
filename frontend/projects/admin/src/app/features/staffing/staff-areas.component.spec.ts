import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import {
  ConventionService,
  StaffScheduleDto,
  StaffService,
} from 'shared';
import { StaffAreasComponent } from './staff-areas.component';
import { EditionContextService } from '../../services/edition-context.service';

describe('StaffAreasComponent', () => {
  let component: StaffAreasComponent;

  const activeEdition = signal({
    id: 'edition-1',
    name: 'Konvent 2027',
    start: '2027-03-01',
    end: '2027-03-03',
    status: 'Published',
  });

  const schedule: StaffScheduleDto = {
    editionId: 'edition-1',
    staffAreaFilterId: null,
    scheduleDays: [
      { date: '2027-03-01', startTime: '08:00', endTime: '18:00' },
      { date: '2027-03-02', startTime: '09:00', endTime: '17:00' },
    ],
    staffAreas: [
      {
        staffAreaId: 'area-1',
        name: 'Reception',
        description: null,
        responsibleId: 'resp-1',
        responsibleName: 'Ansvarig 1',
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
                status: 'Planned',
                staffingStatus: 'Unstaffed',
              },
            ],
          },
        ],
      },
      {
        staffAreaId: 'area-2',
        name: 'Info',
        description: null,
        responsibleId: 'resp-2',
        responsibleName: 'Ansvarig 2',
        stations: [
          {
            stationId: 'station-2',
            name: 'Info Desk',
            description: null,
            shifts: [
              {
                shiftId: 'shift-2',
                stationId: 'station-2',
                responsibleId: 'resp-2',
                responsibleName: 'Bertil',
                start: '2027-03-01T12:00:00',
                end: '2027-03-01T14:00:00',
                minPersons: 2,
                maxPersons: 3,
                activeAssignmentCount: 1,
                confirmedAssignmentCount: 0,
                status: 'Planned',
                staffingStatus: 'UnderMin',
              },
              {
                shiftId: 'shift-3',
                stationId: 'station-2',
                responsibleId: 'resp-3',
                responsibleName: 'Carin',
                start: '2027-03-02T10:00:00',
                end: '2027-03-02T12:00:00',
                minPersons: 2,
                maxPersons: 2,
                activeAssignmentCount: 2,
                confirmedAssignmentCount: 2,
                status: 'Planned',
                staffingStatus: 'Full',
              },
            ],
          },
        ],
      },
    ],
  };

  beforeEach(() => {
    const staffServiceStub = {
      getStaffSchedule: vi.fn(() => of(schedule)),
      getShift: vi.fn((shiftId: string) => of({
        id: shiftId,
        stationId: shiftId === 'shift-1' ? 'station-1' : 'station-2',
        responsibleId: shiftId === 'shift-1' ? 'resp-1' : 'resp-2',
        responsibleName: shiftId === 'shift-1' ? 'Anna' : 'Bertil',
        start: shiftId === 'shift-1' ? '2027-03-01T09:00:00' : '2027-03-01T12:00:00',
        end: shiftId === 'shift-1' ? '2027-03-01T11:00:00' : '2027-03-01T14:00:00',
        minPersons: shiftId === 'shift-1' ? 1 : 2,
        maxPersons: shiftId === 'shift-1' ? 2 : 3,
        status: 'Planned',
        assignments: shiftId === 'shift-2'
          ? [{
              id: 'assignment-1',
              personId: 'staff-1',
              personName: 'Funktionär 1',
              status: 'Confirmed',
              assignedAt: '2027-03-01T08:00:00',
            }]
          : [],
      })),
      listStaffApplications: vi.fn(() => of([])),
      createShift: vi.fn(),
      updateShift: vi.fn(),
      cancelShift: vi.fn(),
      assignPerson: vi.fn(),
      confirmAssignment: vi.fn(),
      rejectAssignment: vi.fn(),
      cancelAssignment: vi.fn(),
    };

    const conventionServiceStub = {
      listEditionStaff: vi.fn(() => of([])),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: StaffService, useValue: staffServiceStub },
        { provide: ConventionService, useValue: conventionServiceStub },
        { provide: EditionContextService, useValue: { activeEdition } },
        { provide: Router, useValue: { navigate: vi.fn() } },
      ],
    });

    component = TestBed.runInInjectionContext(() => new StaffAreasComponent());
    component.schedule.set(schedule);
    component.selectedDay.set('2027-03-01');
    component.areaFilter.set('all');
    component.stationFilter.set('all');
    component.staffingFilter.set('all');
  });

  it('filters table rows by selected area and staffing status', () => {
    expect(component.tableRows().map(row => row.shift.shiftId)).toEqual(['shift-1', 'shift-2']);

    component.onAreaFilterChange('area-2');
    component.onStaffingFilterChange('UnderMin');

    expect(component.tableRows().map(row => row.shift.shiftId)).toEqual(['shift-2']);
  });

  it('updates station options when the area filter changes', () => {
    component.onAreaFilterChange('area-2');

    expect(component.stationOptions()).toEqual([
      { id: 'station-2', name: 'Info Desk' },
    ]);
  });

  it('returns localized staffing labels for the filtered rows', () => {
    component.onAreaFilterChange('area-1');

    const statuses = component.tableRows().map(row => component.staffingStatusLabel(row.shift.staffingStatus));

    expect(statuses).toEqual(['Obemannat']);
  });

  it('includes the responsible person in the selected shift person timeline', () => {
    component.selectedShiftId.set('shift-2');
    component.selectedShiftDetail.set({
      id: 'shift-2',
      stationId: 'station-2',
      responsibleId: 'resp-2',
      responsibleName: 'Bertil',
      start: '2027-03-01T12:00:00',
      end: '2027-03-01T14:00:00',
      minPersons: 2,
      maxPersons: 3,
      status: 'Planned',
      assignments: [{
        id: 'assignment-1',
        personId: 'staff-1',
        personName: 'Funktionär 1',
        status: 'Confirmed',
        assignedAt: '2027-03-01T08:00:00',
      }],
    });

    const rows = component.personTimelineRows();

    expect(rows.map(row => ({ personId: row.personId, role: row.role }))).toEqual([
      { personId: 'resp-2', role: 'responsible' },
      { personId: 'staff-1', role: 'assigned' },
    ]);
  });
});
