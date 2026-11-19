import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { of, Subject, throwError } from 'rxjs';
import { ListingService } from '../api/listings/listing.service';
import { ListingLocation, StreetPlace } from '../api/listings/listing.models';
import { ListingLocationPickerComponent } from './listing-location-picker.component';
import { LocationMapComponent } from './location-map.component';

@Component({ selector: 'app-location-map', standalone: true, template: '' })
class MapStub {
  @Input() point: unknown;
  @Input() disabled = false;
  @Input() editable = true;
  @Output() pointChanged = new EventEmitter();
  @Output() available = new EventEmitter();
}

describe('ListingLocationPickerComponent', () => {
  let fixture: ComponentFixture<ListingLocationPickerComponent>;
  let component: ListingLocationPickerComponent;
  let service: jasmine.SpyObj<ListingService>;
  const street: StreetPlace = { label: 'Ευτυχίδου, Αθήνα, Greece', street: 'Ευτυχίδου', city: 'Αθήνα', postcode: '11634', latitude: 37.969, longitude: 23.748 };
  const exact = { ...street, label: 'Ευτυχίδου 12A, Αθήνα', houseNumber: '12A', latitude: 37.9694 };
  let changes: jasmine.Spy;
  beforeEach(async () => {
    service = jasmine.createSpyObj('ListingService', ['searchStreets', 'resolveAddress']);
    service.searchStreets.and.returnValue(of({ items: [street] }));
    service.resolveAddress.and.returnValue(of({ items: [exact] }));
    await TestBed.configureTestingModule({ imports: [ListingLocationPickerComponent], providers: [{ provide: ListingService, useValue: service }] })
      .overrideComponent(ListingLocationPickerComponent, { remove: { imports: [LocationMapComponent] }, add: { imports: [MapStub] } }).compileComponents();
    fixture = TestBed.createComponent(ListingLocationPickerComponent); component = fixture.componentInstance;
    changes = jasmine.createSpy('changes'); component.locationChanged.subscribe(changes); fixture.detectChanges();
  });

  function locate(): void { component.selectStreet(street); component.numberChanged('12A'); component.locate(); }

  it('debounces suggestions and searches only after two characters', fakeAsync(() => {
    component.streetChanged('E'); tick(500); expect(service.searchStreets).not.toHaveBeenCalled();
    component.streetChanged('Eu'); tick(399); expect(service.searchStreets).not.toHaveBeenCalled();
    component.streetChanged('Eut'); tick(400); expect(service.searchStreets).toHaveBeenCalledOnceWith('Eut');
    expect(component.suggestions).toEqual([street]);
  }));
  it('cancels obsolete street responses immediately', fakeAsync(() => {
    const old = new Subject<{ items: StreetPlace[] }>(); service.searchStreets.and.returnValue(old);
    component.streetChanged('Old'); tick(400); component.streetChanged('New');
    old.next({ items: [street] }); expect(component.suggestions).toEqual([]); expect(old.observed).toBeFalse();
    fixture.destroy(); tick(400);
  }));
  it('supports keyboard selection with locality labels and no form submit', fakeAsync(() => {
    const duplicate = { ...street, city: 'Other city', label: 'Ευτυχίδου, Other city' };
    service.searchStreets.and.returnValue(of({ items: [street, duplicate] }));
    component.streetChanged('Ευ'); tick(400); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Other city');
    component.streetKey(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    component.streetKey(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    const enter = new KeyboardEvent('keydown', { key: 'Enter', cancelable: true }); component.streetKey(enter);
    expect(enter.defaultPrevented).toBeTrue(); expect(component.selected).toEqual(duplicate);
  }));
  it('shows empty results and retries failures without losing the number', fakeAsync(() => {
    component.numberChanged('12'); service.searchStreets.and.returnValue(throwError(() => new Error('offline')));
    component.streetChanged('Test'); tick(400); expect(component.error).toContain('Retry'); expect(component.houseNumber).toBe('12');
    service.searchStreets.and.returnValue(of({ items: [] })); component.streetChanged('Test'); tick(400);
    expect(component.empty).toBeTrue(); expect(component.error).toBe('');
  }));
  it('requires a selected street and nonblank number of at most 20 characters', () => {
    component.locate(); component.selectStreet(street); component.numberChanged('  '); component.locate();
    component.numberChanged('x'.repeat(21)); component.locate(); expect(service.resolveAddress).not.toHaveBeenCalled();
  });
  it('accepts suffixes and ranges as text', () => {
    component.selectStreet(street); component.numberChanged('12–14'); component.locate();
    expect(service.resolveAddress).toHaveBeenCalledWith(street, '12–14');
  });
  it('requires map readiness and confirmation even for an exact address', () => {
    locate(); expect(component.draft?.latitude).toBe(exact.latitude); expect(component.approximate).toBeFalse();
    component.confirm(); expect(component.confirmed).toBeFalse();
    component.mapAvailability(true); component.confirm();
    expect(changes).toHaveBeenCalledWith(jasmine.objectContaining({ address: exact.label, latitude: exact.latitude }));
  });
  it('uses the selected street and entered number for approximate fallback', () => {
    service.resolveAddress.and.returnValue(of({ items: [{ ...exact, houseNumber: '99' }] })); locate();
    expect(component.approximate).toBeTrue(); expect(component.draft?.latitude).toBe(street.latitude);
    expect(component.draft?.address).toContain('12A'); component.mapAvailability(true); component.confirm(); expect(component.confirmed).toBeFalse();
    component.movePin({ latitude: 37.9698, longitude: 23.7482 }); component.confirm(); expect(component.confirmed).toBeTrue();
  });
  it('allows explicit placement when resolution has no candidates', () => {
    service.resolveAddress.and.returnValue(of({ items: [] })); locate(); expect(component.approximate).toBeTrue(); expect(component.draft).not.toBeNull();
  });
  it('moving a confirmed pin requires reconfirmation without changing the address', () => {
    locate(); component.mapAvailability(true); component.confirm();
    component.movePin({ latitude: 37.97, longitude: 23.75 });
    expect(component.confirmed).toBeFalse(); expect(changes).toHaveBeenCalledWith(null); expect(component.draft?.address).toBe(exact.label);
  });
  it('editing either address input invalidates confirmed coordinates immediately', () => {
    locate(); component.mapAvailability(true); component.confirm(); component.numberChanged('13');
    expect(component.draft).toBeNull(); expect(component.confirmed).toBeFalse();
    component.locate(); component.mapAvailability(true); component.movePin(street); component.confirm(); component.streetChanged('E');
    expect(component.selected).toBeNull(); expect(component.draft).toBeNull(); expect(changes.calls.mostRecent().args).toEqual([null]);
  });
  it('cancels in-flight resolution when the number changes', () => {
    const pending = new Subject<{ items: StreetPlace[] }>(); service.resolveAddress.and.returnValue(pending); locate();
    component.numberChanged('13'); pending.next({ items: [exact] }); expect(component.draft).toBeNull(); expect(pending.observed).toBeFalse();
  });
  it('retains the street and number on provider failure and permits retry', () => {
    service.resolveAddress.and.returnValue(throwError(() => new Error('offline'))); locate();
    expect(component.error).toContain('retry'); expect(component.selected).toEqual(street); expect(component.houseNumber).toBe('12A');
    service.resolveAddress.and.returnValue(of({ items: [exact] })); component.locate(); expect(component.draft).not.toBeNull();
  });
  it('blocks overly long addresses and invalid coordinates', () => {
    service.resolveAddress.and.returnValue(of({ items: [{ ...exact, label: 'x'.repeat(201) }] })); locate();
    expect(component.draft).toBeNull(); expect(component.error).toContain('200');
    service.resolveAddress.and.returnValue(of({ items: [{ ...exact, latitude: NaN }] })); component.locate();
    expect(component.approximate).toBeTrue();
  });
  it('invalidates confirmation after map failure', () => {
    locate(); component.mapAvailability(true); component.confirm(); component.mapAvailability(false);
    expect(component.confirmed).toBeFalse(); component.confirm(); expect(component.confirmed).toBeFalse();
  });
  it('accepts legacy saved locations without parsing or lookup and cancels edits', () => {
    const saved: ListingLocation = { address: 'Legacy address', latitude: 37.97, longitude: 23.75 };
    fixture.componentRef.setInput('saved', saved); fixture.detectChanges(); expect(component.editing).toBeFalse();
    expect(service.resolveAddress).not.toHaveBeenCalled(); component.changeLocation();
    fixture.componentRef.setInput('showErrors', true); fixture.detectChanges(); expect(component.editing).toBeTrue();
    locate(); component.cancelChange(); expect(component.editing).toBeFalse(); expect(changes).toHaveBeenCalledWith(saved);
  });
  it('prevents draft and pin changes while saving', () => {
    locate(); component.mapAvailability(true); component.confirm(); component.disabled = true;
    component.numberChanged('99'); component.movePin(street); expect(component.houseNumber).toBe('12A'); expect(component.confirmed).toBeTrue();
  });
});
