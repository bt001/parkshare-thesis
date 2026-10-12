import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { AuthService } from '../api/auth/auth.service';
import { ListingService } from '../api/listings/listing.service';
import { OwnerListingFormComponent } from './owner-listing-form.component';

describe('OwnerListingFormComponent', () => {
  let component: OwnerListingFormComponent;
  let fixture: ComponentFixture<OwnerListingFormComponent>;
  let router: Router;
  const listingService = jasmine.createSpyObj<ListingService>('ListingService', ['create', 'update', 'getById', 'getAmenities']);
  const authService = jasmine.createSpyObj<AuthService>('AuthService', ['refresh']);

  beforeEach(async () => {
    listingService.create.calls.reset();
    listingService.getAmenities.and.returnValue(of({ items: [{ id: 1, name: 'Lighting' }, { id: 2, name: 'CCTV' }] }));
    authService.refresh.calls.reset();
    listingService.create.and.returnValue(of({ listingId: 2, isActive: true }));
    authService.refresh.and.returnValue(of({ userId: 1, userName: 'owner', email: 'owner@test', role: 'Renter,Owner', roles: ['Renter', 'Owner'], name: 'Owner', accessToken: 'token', accessTokenExpiresAt: '2099-01-01', tokenType: 'Bearer', refreshToken: 'refresh', refreshTokenExpiresAt: '2099-01-02' }));
    await TestBed.configureTestingModule({ imports: [OwnerListingFormComponent], providers: [provideRouter([]), { provide: ListingService, useValue: listingService }, { provide: AuthService, useValue: authService }, { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => null } } } }] }).compileComponents();
    router = TestBed.inject(Router); spyOn(router, 'navigateByUrl').and.resolveTo(true);
    fixture = TestBed.createComponent(OwnerListingFormComponent); component = fixture.componentInstance; fixture.detectChanges();
  });

  it('blocks saving on catalog failure and supports retry', () => {
    listingService.getAmenities.and.returnValue(throwError(() => new Error('offline')));
    component.loadAmenities();
    expect(component.isCatalogReady).toBeFalse();
    expect(component.catalogError).toContain('retry');
    component.submit();
    expect(listingService.create).not.toHaveBeenCalled();
    listingService.getAmenities.and.returnValue(of({ items: [] }));
    component.loadAmenities();
    expect(component.isCatalogReady).toBeTrue();
  });

  it('retains selected amenities after a failed save', () => {
    component.form.setValue({ title: 'Space', description: 'Secure space', address: 'Main St', latitude: 44.4, longitude: 26.1, pricePerHour: 12.5, size: '', isAlwaysAvailable: true });
    component.toggleAmenity(1, true); component.toggleAmenity(2, true); component.toggleAmenity(2, false);
    listingService.create.and.returnValue(throwError(() => new Error('failed')));
    component.submit();
    expect(listingService.create).toHaveBeenCalledWith(jasmine.objectContaining({ amenityIds: [1] }));
    expect(component.selectedAmenityIds).toEqual([1]);
    expect(component.isSubmitting).toBeFalse();
  });

  it('preselects saved amenities on edit', () => {
    const route = TestBed.inject(ActivatedRoute);
    spyOn(route.snapshot.paramMap, 'get').and.returnValue('7');
    listingService.getById.and.returnValue(of({ id: 7, ownerId: 1, title: 'Space', description: 'Secure space', latitude: 44.4, longitude: 26.1, pricePerHour: 12, isActive: false, isAlwaysAvailable: true, createdAt: '', amenities: [{ id: 2, name: 'CCTV' }] }));
    component.ngOnInit();
    expect(component.selectedAmenityIds).toEqual([2]);
    fixture.detectChanges();
    expect((fixture.nativeElement.querySelectorAll('.amenity-options input')[1] as HTMLInputElement).checked).toBeTrue();
  });

  it('rejects invalid values without submitting', () => {
    component.submit();
    expect(listingService.create).not.toHaveBeenCalled();
  });

  it('creates a scheduled listing and refreshes the owner session', () => {
    component.form.setValue({ title: 'Space', description: 'Secure space', address: 'Main St', latitude: 44.4, longitude: 26.1, pricePerHour: 12.5, size: 'Medium', isAlwaysAvailable: false });
    component.submit();
    expect(listingService.create).toHaveBeenCalledWith(jasmine.objectContaining({ isAlwaysAvailable: false, size: 'Medium' }));
    expect(authService.refresh).toHaveBeenCalled();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/owner/listings');
  });
});
