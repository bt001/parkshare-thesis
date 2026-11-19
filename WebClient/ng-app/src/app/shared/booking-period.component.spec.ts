import { TestBed } from '@angular/core/testing';
import { BookingPeriodComponent } from './booking-period.component';
import { StayDurationComponent } from './stay-duration.component';
import { ArrivalPickerComponent } from './arrival-picker.component';

describe('BookingPeriodComponent', () => {
  it('displays a restored preset on the first rendered dropdown', async () => {
    const fixture = TestBed.createComponent(BookingPeriodComponent);
    fixture.componentRef.setInput('startDateTime', '2099-01-15T10:00');
    fixture.componentRef.setInput('endDateTime', '2099-01-15T12:00');
    fixture.detectChanges(); await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-stay-duration select').value).toBe('120');
  });
  it('suggests one hour only when the user commits a new arrival', () => {
    const fixture = TestBed.createComponent(BookingPeriodComponent);
    const component = fixture.componentInstance;
    const emit = spyOn(component.periodChange, 'emit');
    fixture.detectChanges();
    expect(component.arrival.value).toBe('');
    expect(component.duration.value).toBeNull();
    component.arrival.setValue('2099-01-15T23:45');
    expect(component.duration.value).toBe(60);
    expect(emit).toHaveBeenCalledWith({ startDateTime: '2099-01-15T23:45', endDateTime: '2099-01-16T00:45', incomplete: false });
  });
  it('retains duration-first intent and translates restored custom periods', () => {
    const fixture = TestBed.createComponent(BookingPeriodComponent);
    const component = fixture.componentInstance;
    const emit = spyOn(component.periodChange, 'emit');
    fixture.detectChanges(); component.duration.setValue(120);
    expect(emit).toHaveBeenCalledWith({ startDateTime: '', endDateTime: '', incomplete: true });
    component.arrival.setValue('2099-01-15T10:00');
    expect(component.departure).toBe('2099-01-15T12:00');
    fixture.componentRef.setInput('startDateTime', '2099-01-15T10:00');
    fixture.componentRef.setInput('endDateTime', '2099-01-16T11:01');
    fixture.detectChanges();
    expect(component.duration.value).toBe(1501);
    fixture.componentRef.setInput('startDateTime', ''); fixture.componentRef.setInput('endDateTime', ''); fixture.componentRef.setInput('revision', 1); fixture.detectChanges();
    expect(component.duration.value).toBeNull();
  });
  it('preserves invalid and expired saved values for correction', () => {
    const fixture = TestBed.createComponent(BookingPeriodComponent);
    fixture.componentRef.setInput('startDateTime', 'bad');
    fixture.componentRef.setInput('endDateTime', 'saved end');
    fixture.detectChanges();
    expect(fixture.componentInstance.arrival.value).toBe('bad');
    expect(fixture.nativeElement.textContent).toContain('saved end');
    fixture.componentRef.setInput('startDateTime', '2020-01-15T10:00');
    fixture.componentRef.setInput('endDateTime', '2020-01-15T12:00'); fixture.detectChanges();
    expect(fixture.componentInstance.arrival.value).toBe('2020-01-15T10:00');
    expect(fixture.componentInstance.duration.value).toBe(120);
  });
  it('does not overwrite an invalid custom draft on its parent echo', () => {
    const fixture = TestBed.createComponent(BookingPeriodComponent);
    fixture.componentRef.setInput('startDateTime', '2099-01-15T10:00');
    fixture.componentRef.setInput('endDateTime', '2099-01-15T12:00'); fixture.detectChanges();
    fixture.componentInstance.duration.setValue(null);
    fixture.componentRef.setInput('endDateTime', ''); fixture.detectChanges();
    expect(fixture.componentInstance.duration.value).toBeNull();
    expect(fixture.componentInstance.arrival.value).toBe('2099-01-15T10:00');
  });
});

describe('StayDurationComponent', () => {
  it('round-trips non-preset lengths and rejects invalid custom parts', () => {
    const component = TestBed.createComponent(StayDurationComponent).componentInstance; const change = jasmine.createSpy(); component.registerOnChange(change);
    component.writeValue(1501); expect(component.custom).toBeTrue(); expect(component.hours).toBe(25); expect(component.minutes).toBe(1);
    component.setCustom('hours', '1.5'); expect(change).toHaveBeenCalledWith(null); expect(component.validate()).toEqual({stayDuration: true});
    component.setCustom('hours', '25'); component.setCustom('minutes', '60'); expect(component.value).toBeNull();
    component.setCustom('minutes', '1'); expect(component.value).toBe(1501);
    component.setCustom('hours', '0'); component.setCustom('minutes', '0'); expect(component.value).toBeNull();
    component.setCustom('minutes', '1'); expect(component.value).toBe(1);
  });
});

describe('ArrivalPickerComponent', () => {
  it('selects a fresh Athens default on opening and saves time edits without Apply', () => {
    jasmine.clock().install(); jasmine.clock().mockDate(new Date('2026-11-19T12:34:30Z'));
    try {
      const fixture = TestBed.createComponent(ArrivalPickerComponent); fixture.detectChanges();
      const component = fixture.componentInstance; const change = jasmine.createSpy(); component.registerOnChange(change);
      component.open(); expect(component.day).toBe('2026-11-19'); expect(component.hour).toBe('14'); expect(component.minute).toBe('35');
      expect(change).toHaveBeenCalledWith('2026-11-19T14:35');
      component.selectClockTime('16:35'); expect(change).toHaveBeenCalledWith('2026-11-19T16:35');
      component.close(); expect(component.value).toBe('2026-11-19T16:35');
      component.open(); expect(component.hour).toBe('16');
      component.chooseDay('2026-11-20'); expect(change).toHaveBeenCalledWith('2026-11-20T16:35');
      component.clear(); expect(change).toHaveBeenCalledWith('');
      expect(document.activeElement).toBe(component.trigger.nativeElement);
      fixture.destroy();
    } finally { jasmine.clock().uninstall(); }
  });
  it('switches date/time views, preserves selections and starts on Date when reopened', () => {
    const fixture = TestBed.createComponent(ArrivalPickerComponent); fixture.detectChanges();
    const component = fixture.componentInstance; component.writeValue('2099-01-15T23:59'); component.open();
    component.switchView('time'); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('app-clock-time')).toBeTruthy();
    expect(fixture.nativeElement.querySelector('select')).toBeNull();
    component.selectClockTime('00:01'); component.switchView('date'); expect(component.value).toBe('2099-01-15T00:01');
    component.close(); component.open(); expect(component.view).toBe('date'); expect(component.hour).toBe('00'); expect(component.minute).toBe('01');
    component.close();
  });
  it('keeps its measured panel inside a short viewport', () => {
    const fixture = TestBed.createComponent(ArrivalPickerComponent); fixture.detectChanges();
    const component = fixture.componentInstance;
    component.open();
    const panel = component.dialog.nativeElement.getBoundingClientRect();
    expect(panel.top).toBeGreaterThanOrEqual(0); expect(panel.bottom).toBeLessThanOrEqual(window.innerHeight);
    component.close(); fixture.destroy();
  });
  it('selects across month boundaries by keyboard without Apply', () => {
    const fixture = TestBed.createComponent(ArrivalPickerComponent); fixture.detectChanges();
    const component = fixture.componentInstance; component.writeValue('2099-01-31T10:00'); component.open();
    component.navigate(new KeyboardEvent('keydown', { key: 'ArrowRight' }), '2099-01-31');
    expect(component.day).toBe('2099-02-01'); expect(component.value).toBe('2099-02-01T10:00');
    component.close();
  });
});
