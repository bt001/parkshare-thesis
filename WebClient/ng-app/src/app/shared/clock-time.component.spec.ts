import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ClockTimeComponent } from './clock-time.component';

/** Creates a controlled clock with deterministic dial geometry for gesture checks. */
function createClock(value = '23:59'): ComponentFixture<ClockTimeComponent> {
  const fixture = TestBed.createComponent(ClockTimeComponent);
  fixture.componentRef.setInput('value', value); fixture.detectChanges();
  fixture.componentInstance.timeChange.subscribe(time => fixture.componentRef.setInput('value', time));
  const dial = fixture.nativeElement.querySelector('.clock-dial') as HTMLDivElement;
  spyOn(dial, 'getBoundingClientRect').and.returnValue({ left: 0, top: 0, width: 240, height: 240 } as DOMRect);
  spyOn(dial, 'setPointerCapture');
  return fixture;
}

/** Creates a primary pointer event at a logical clock position. */
function pointer(type: string, x: number, y: number, id = 1): PointerEvent {
  return new PointerEvent(type, { pointerId: id, button: 0, clientX: x, clientY: y });
}

describe('ClockTimeComponent', () => {
  it('distinguishes midnight/noon and keeps all 24 hours on two rings', () => {
    const fixture = createClock('00:01'), clock = fixture.componentInstance;
    expect(clock.labels.length).toBe(24);
    expect(clock.labels.find(label => label.value === 0)?.y).toBe(56);
    expect(clock.labels.find(label => label.value === 12)?.y).toBe(26);
    expect(clock.accessibleTime).toBe('00:01, Athens time');
    const emit = spyOn(clock.timeChange, 'emit').and.callThrough();
    clock.pointerDown(pointer('pointerdown', 120, 26)); expect(emit).not.toHaveBeenCalled();
    clock.pointerUp(pointer('pointerup', 120, 26)); fixture.detectChanges();
    expect(clock.value).toBe('12:01'); expect(clock.stage).toBe('minute');
    clock.selectStage('hour'); clock.pointerDown(pointer('pointerdown', 120, 56)); clock.pointerUp(pointer('pointerup', 120, 56)); fixture.detectChanges();
    expect(clock.value).toBe('00:01');
  });

  it('selects an afternoon inner-ring hour and advances to minutes without confirmation', () => {
    const fixture = createClock('12:45'), clock = fixture.componentInstance;
    clock.pointerDown(pointer('pointerdown', 56, 120)); clock.pointerUp(pointer('pointerup', 56, 120)); fixture.detectChanges();
    expect(clock.value).toBe('21:45'); expect(clock.stage).toBe('minute');
    expect(fixture.nativeElement.querySelector('[role=slider]').getAttribute('aria-valuemax')).toBe('59');
  });

  it('selects every minute, including between the five-minute labels', () => {
    const fixture = createClock('23:00'), clock = fixture.componentInstance;
    clock.selectStage('minute');
    clock.pointerDown(pointer('pointerdown', 129.83, 26.51)); clock.pointerUp(pointer('pointerup', 129.83, 26.51)); fixture.detectChanges();
    expect(clock.value).toBe('23:01'); expect(clock.stage).toBe('minute');
    clock.pointerDown(pointer('pointerdown', 110.17, 26.51)); clock.pointerUp(pointer('pointerup', 110.17, 26.51)); fixture.detectChanges();
    expect(clock.value).toBe('23:59'); expect(clock.labels.length).toBe(12); expect(clock.ticks.length).toBe(60);
  });

  it('previews dragging without emissions and commits only its final release', () => {
    const fixture = createClock('01:45'), clock = fixture.componentInstance;
    const emit = spyOn(clock.timeChange, 'emit').and.callThrough();
    clock.pointerDown(pointer('pointerdown', 214, 120)); clock.pointerMove(pointer('pointermove', 120, 214)); fixture.detectChanges();
    expect(clock.hour).toBe(6); expect(clock.value).toBe('01:45'); expect(emit).not.toHaveBeenCalled();
    clock.pointerUp(pointer('pointerup', 120, 214)); fixture.detectChanges();
    expect(emit).toHaveBeenCalledOnceWith('06:45'); expect(clock.value).toBe('06:45');
  });

  it('ignores cancelled/lost gestures, other pointers and the centre pin', () => {
    const fixture = createClock('10:15'), clock = fixture.componentInstance;
    const emit = spyOn(clock.timeChange, 'emit');
    clock.pointerDown(pointer('pointerdown', 120, 120)); clock.pointerUp(pointer('pointerup', 120, 26)); expect(emit).not.toHaveBeenCalled();
    clock.pointerDown(pointer('pointerdown', 214, 120)); clock.pointerUp(pointer('pointerup', 120, 214, 2)); expect(emit).not.toHaveBeenCalled();
    clock.cancelPointer(pointer('pointercancel', 214, 120)); clock.pointerUp(pointer('pointerup', 120, 214)); expect(emit).not.toHaveBeenCalled();
    clock.pointerDown(pointer('pointerdown', 214, 120)); clock.cancelPointer(pointer('lostpointercapture', 214, 120));
    expect(clock.hour).toBe(10); expect(clock.value).toBe('10:15');
  });

  it('wraps each keyboard field independently and supports Home/End and Enter', () => {
    const fixture = createClock(), clock = fixture.componentInstance;
    clock.keyDown(new KeyboardEvent('keydown', { key: 'ArrowRight' })); fixture.detectChanges(); expect(clock.value).toBe('00:59');
    clock.keyDown(new KeyboardEvent('keydown', { key: 'End' })); fixture.detectChanges(); expect(clock.value).toBe('23:59');
    clock.keyDown(new KeyboardEvent('keydown', { key: 'Enter' })); fixture.detectChanges(); expect(clock.stage).toBe('minute');
    clock.keyDown(new KeyboardEvent('keydown', { key: 'ArrowUp' })); fixture.detectChanges(); expect(clock.value).toBe('23:00');
    clock.keyDown(new KeyboardEvent('keydown', { key: 'ArrowLeft' })); fixture.detectChanges(); expect(clock.value).toBe('23:59');
    clock.keyDown(new KeyboardEvent('keydown', { key: 'Home' })); fixture.detectChanges(); expect(clock.value).toBe('23:00');
  });

  it('does not cancel a new gesture when the parent acknowledges the preceding selection', () => {
    const fixture = createClock('10:00'), clock = fixture.componentInstance;
    clock.pointerDown(pointer('pointerdown', 214, 120)); clock.pointerUp(pointer('pointerup', 214, 120));
    clock.pointerDown(pointer('pointerdown', 129.83, 26.51)); fixture.detectChanges();
    expect(clock.minute).toBe(1);
    clock.pointerUp(pointer('pointerup', 129.83, 26.51)); fixture.detectChanges();
    expect(clock.value).toBe('03:01');
    fixture.componentRef.setInput('value', '16:45'); fixture.detectChanges();
    clock.pointerDown(pointer('pointerdown', 120, 214));
    fixture.componentRef.setInput('value', '03:01'); fixture.detectChanges();
    const emit = spyOn(clock.timeChange, 'emit');
    clock.pointerUp(pointer('pointerup', 120, 214)); expect(emit).not.toHaveBeenCalled();
  });

  it('retains every rapid keyboard adjustment before the parent view renders', () => {
    const fixture = createClock('23:01'), clock = fixture.componentInstance;
    for (let i = 0; i < 7; i++) clock.keyDown(new KeyboardEvent('keydown', { key: 'ArrowLeft' }));
    expect(clock.value).toBe('16:01'); fixture.detectChanges(); expect(clock.value).toBe('16:01');
  });

  it('cancels gestures on external updates and respects disabled state', () => {
    const fixture = createClock(), clock = fixture.componentInstance;
    const emit = spyOn(clock.timeChange, 'emit');
    clock.pointerDown(pointer('pointerdown', 214, 120)); fixture.componentRef.setInput('value', '16:01'); fixture.detectChanges();
    clock.pointerUp(pointer('pointerup', 214, 120)); expect(emit).not.toHaveBeenCalled();
    fixture.componentRef.setInput('disabled', true); fixture.detectChanges();
    clock.pointerDown(pointer('pointerdown', 214, 120)); clock.pointerUp(pointer('pointerup', 214, 120)); clock.keyDown(new KeyboardEvent('keydown', { key: 'End' }));
    expect(emit).not.toHaveBeenCalled(); expect(fixture.nativeElement.querySelector('[role=slider]').tabIndex).toBe(-1);
  });
});
