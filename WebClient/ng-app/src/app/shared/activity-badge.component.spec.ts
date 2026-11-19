import { TestBed } from '@angular/core/testing';
import { ActivityBadgeComponent } from './activity-badge.component';

describe('ActivityBadgeComponent', () => {
  it('hides zero, caps visual counts and preserves accessible totals and stale feedback', () => {
    const fixture = TestBed.createComponent(ActivityBadgeComponent);
    fixture.componentRef.setInput('count', 0); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.activity-badge')).toBeNull();
    fixture.componentRef.setInput('count', 150);
    fixture.componentRef.setInput('description', 'unread messages'); fixture.detectChanges();
    const badge = fixture.nativeElement.querySelector('.activity-badge') as HTMLElement;
    expect(badge.textContent?.trim()).toBe('99+');
    expect(badge.getAttribute('aria-label')).toBe('150 unread messages');
    fixture.componentRef.setInput('stale', true); fixture.detectChanges();
    expect(badge.getAttribute('aria-label')).toContain('temporarily unavailable');
    expect(badge.textContent?.trim()).toBe('99+'); fixture.destroy();
  });
});
