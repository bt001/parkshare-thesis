import { Component, ElementRef, EventEmitter, Input, OnChanges, Output, SimpleChanges, ViewChild } from '@angular/core';

/** A labelled position on the clock face in its 240-unit coordinate system. */
interface ClockLabel {
  /** Numeric hour or minute selected at this position. */
  value: number;
  /** Horizontal position of the label. */
  x: number;
  /** Vertical position of the label. */
  y: number;
}

/** A 24-hour analog time control with exact minutes and release-to-commit gestures. */
@Component({
  selector: 'app-clock-time', standalone: true,
  templateUrl: './clock-time.component.html', styleUrl: './clock-time.component.scss'
})
export class ClockTimeComponent implements OnChanges {
  /** Selected time in zero-padded 24-hour HH:mm format. */
  @Input() value = '00:00';
  /** Prevents pointer and keyboard changes when the containing form is disabled. */
  @Input() disabled = false;
  /** Emits a completed time selection without requiring confirmation. */
  @Output() timeChange = new EventEmitter<string>();
  /** Focusable, pointer-capturing clock surface. */
  @ViewChild('dial') private dial?: ElementRef<HTMLDivElement>;
  /** Active unit, independently selectable through the digital time display. */
  stage: 'hour' | 'minute' = 'hour';
  /** Distinguishes the parent's acknowledgement from an external replacement during a new gesture. */
  private publishedValue?: string;
  /** Gesture preview; the parent value remains unchanged until pointer release. */
  private gesture?: { pointerId: number; value: number };
  /** Minute tick positions, including every individual minute. */
  readonly ticks = Array.from({ length: 60 }, (_, value) => ({
    value, start: this.point(value * 6, value % 5 === 0 ? 105 : 109), end: this.point(value * 6, 112)
  }));
  /** Cancels a gesture if its external value or disabled state changes. */
  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['disabled'] && this.disabled) || (changes['value'] && this.value !== this.publishedValue)) this.cancelPointer();
    if (changes['value']) this.publishedValue = undefined;
  }
  /** Parses a valid external time without exposing invalid numeric state. */
  private get parts(): number[] {
    return /^(?:[01]\d|2[0-3]):[0-5]\d$/.test(this.value) ? this.value.split(':').map(Number) : [0, 0];
  }
  /** Committed hour with an optional active gesture preview. */
  get hour(): number { return this.stage === 'hour' && this.gesture ? this.gesture.value : this.parts[0]; }
  /** Committed minute with an optional active gesture preview. */
  get minute(): number { return this.stage === 'minute' && this.gesture ? this.gesture.value : this.parts[1]; }
  /** Currently displayed slider value. */
  get selected(): number { return this.stage === 'hour' ? this.hour : this.minute; }
  /** Maximum slider value for the active unit. */
  get maximum(): number { return this.stage === 'hour' ? 23 : 59; }
  /** Selected time read by assistive technology. */
  get accessibleTime(): string { return `${this.pad(this.hour)}:${this.pad(this.minute)}, Athens time`; }
  /** Labels for both hour rings or the twelve five-minute positions. */
  get labels(): ClockLabel[] {
    const values = this.stage === 'hour' ? Array.from({ length: 24 }, (_, i) => i) : Array.from({ length: 12 }, (_, i) => i * 5);
    return values.map(value => ({ value, ...this.point(value * (this.stage === 'hour' ? 30 : 6), this.stage === 'hour' && (value === 0 || value > 12) ? 64 : 94) }));
  }
  /** Position of the current clock hand and selected-value marker. */
  get hand(): { x: number; y: number } {
    return this.point(this.selected * (this.stage === 'hour' ? 30 : 6), this.stage === 'hour' && (this.selected === 0 || this.selected > 12) ? 64 : 94);
  }
  /** Converts an angle and radius into SVG coordinates. */
  private point(degrees: number, radius: number): { x: number; y: number } {
    const radians = degrees * Math.PI / 180;
    return { x: 120 + Math.sin(radians) * radius, y: 120 - Math.cos(radians) * radius };
  }
  /** Formats a numeric time part for the digital display and minute labels. */
  pad(value: number): string { return String(value).padStart(2, '0'); }
  /** Switches the selected unit and moves keyboard focus to the clock. */
  selectStage(stage: 'hour' | 'minute'): void {
    if (this.disabled) return;
    this.cancelPointer(); this.stage = stage; this.dial?.nativeElement.focus();
  }
  /** Converts a pointer position into an exact clock value. */
  private pointerValue(event: PointerEvent): number | null {
    const bounds = this.dial!.nativeElement.getBoundingClientRect();
    if (!bounds.width || !bounds.height) return null;
    const x = (event.clientX - bounds.left) * 240 / bounds.width - 120;
    const y = (event.clientY - bounds.top) * 240 / bounds.height - 120;
    const radius = Math.hypot(x, y);
    if (radius < 24) return null;
    const degrees = (Math.atan2(x, -y) * 180 / Math.PI + 360) % 360;
    if (this.stage === 'minute') return Math.round(degrees / 6) % 60;
    const hour = Math.round(degrees / 30) % 12;
    return radius < 79 ? (hour === 0 ? 0 : hour + 12) : (hour === 0 ? 12 : hour);
  }
  /** Starts a captured gesture without changing the committed time. */
  pointerDown(event: PointerEvent): void {
    if (this.disabled || event.button !== 0 || this.gesture) return;
    const value = this.pointerValue(event);
    if (value === null) return;
    event.preventDefault();
    this.gesture = { pointerId: event.pointerId, value };
    this.dial!.nativeElement.setPointerCapture(event.pointerId);
    this.dial!.nativeElement.focus();
  }
  /** Previews the nearest hour or individual minute while dragging. */
  pointerMove(event: PointerEvent): void {
    if (this.gesture?.pointerId !== event.pointerId) return;
    const value = this.pointerValue(event);
    if (value !== null) this.gesture.value = value;
  }
  /** Commits on release, advancing an hour selection to the minute dial. */
  pointerUp(event: PointerEvent): void {
    if (this.gesture?.pointerId !== event.pointerId) return;
    this.pointerMove(event);
    const value = this.gesture.value;
    this.cancelPointer();
    if (this.disabled) return;
    this.publish(value);
    if (this.stage === 'hour') this.stage = 'minute';
  }
  /** Cancels a gesture without emitting a partial time selection. */
  cancelPointer(event?: PointerEvent): void {
    if (!this.gesture || (event && event.pointerId !== this.gesture.pointerId)) return;
    const pointerId = this.gesture.pointerId;
    this.gesture = undefined;
    if (this.dial?.nativeElement.hasPointerCapture(pointerId)) this.dial.nativeElement.releasePointerCapture(pointerId);
  }
  /** Adjusts the active unit with wrapping arrows and Home/End limits. */
  keyDown(event: KeyboardEvent): void {
    if (this.disabled) return;
    const step: Record<string, number> = { ArrowUp: 1, ArrowRight: 1, ArrowDown: -1, ArrowLeft: -1 };
    if (event.key === 'Enter') { event.preventDefault(); event.stopPropagation(); this.selectStage('minute'); return; }
    const delta = step[event.key];
    if (delta === undefined && event.key !== 'Home' && event.key !== 'End') return;
    event.preventDefault(); event.stopPropagation(); this.cancelPointer();
    const value = event.key === 'Home' ? 0 : event.key === 'End' ? this.maximum : (this.selected + delta + this.maximum + 1) % (this.maximum + 1);
    this.publish(value);
  }
  /** Emits one complete 24-hour time value for the active unit. */
  private publish(value: number): void {
    const [hour, minute] = this.parts;
    this.value = this.stage === 'hour' ? `${this.pad(value)}:${this.pad(minute)}` : `${this.pad(hour)}:${this.pad(value)}`;
    this.publishedValue = this.value;
    this.timeChange.emit(this.value);
  }
}
