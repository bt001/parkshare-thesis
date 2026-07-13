import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../api/auth/auth.service';
import { ListingService } from '../api/listings/listing.service';
import { OwnerListingFormComponent } from './owner-listing-form.component';

describe('OwnerListingFormComponent', () => {
  let component: OwnerListingFormComponent;
  let fixture: ComponentFixture<OwnerListingFormComponent>;
  let router: Router;
  const listingService = jasmine.createSpyObj<ListingService>('ListingService', ['create', 'update', 'getById']);
  const authService = jasmine.createSpyObj<AuthService>('AuthService', ['refresh']);

  beforeEach(async () => {
    listingService.create.calls.reset();
    authService.refresh.calls.reset();
    listingService.create.and.returnValue(of({ listingId: 2, isActive: true }));
    authService.refresh.and.returnValue(of({ userId: 1, userName: 'owner', email: 'owner@test', role: 'Renter,Owner', roles: ['Renter', 'Owner'], name: 'Owner', accessToken: 'token', accessTokenExpiresAt: '2099-01-01', tokenType: 'Bearer', refreshToken: 'refresh', refreshTokenExpiresAt: '2099-01-02' }));
    await TestBed.configureTestingModule({ imports: [OwnerListingFormComponent], providers: [provideRouter([]), { provide: ListingService, useValue: listingService }, { provide: AuthService, useValue: authService }, { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => null } } } }] }).compileComponents();
    router = TestBed.inject(Router); spyOn(router, 'navigateByUrl').and.resolveTo(true);
    fixture = TestBed.createComponent(OwnerListingFormComponent); component = fixture.componentInstance; fixture.detectChanges();
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
