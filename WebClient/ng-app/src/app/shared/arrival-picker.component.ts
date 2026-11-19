import { DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, ElementRef, forwardRef, HostListener, inject, Input, ViewChild } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { departureForStay, nextAthensMinute } from '../services/booking-time';

/** Accessible Athens arrival picker with immediate date and time selection. */
@Component({
  selector: 'app-arrival-picker', standalone: true, host: { '[class.compact]': 'compact' }, imports: [DatePipe],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => ArrivalPickerComponent), multi: true }],
  templateUrl: './arrival-picker.component.html', styleUrl: './arrival-picker.component.scss'
})
export class ArrivalPickerComponent implements ControlValueAccessor {
  /** Renders the selected month before modal focus is assigned. */
  private readonly changeDetector = inject(ChangeDetectorRef);
  /** Removes the separate input border inside a segmented discovery bar. */
  @Input() compact = false;
  /** Unique control identifier supplied by the containing form. */
  @Input() inputId = 'arrival';
  /** Calendar dialog rendered in the browser top layer. */
  @ViewChild('dialog') dialog!: ElementRef<HTMLDialogElement>;
  /** Trigger receiving focus when the dialog closes. */
  @ViewChild('trigger') trigger!: ElementRef<HTMLButtonElement>;
  /** Committed offset-free Athens value. */
  value = '';
  /** Selected calendar day. */
  day = '';
  /** Selected hour. */
  hour = '00';
  /** Selected minute. */
  minute = '00';
  /** First day of the visible calendar month. */
  month = new Date();
  /** Current Athens date used to identify dates before today. */
  minimumDay = nextAthensMinute().slice(0, 10);
  /** Whether the containing form disables this control. */
  disabled = false;
  /** Hour options. */
  readonly hours = Array.from({ length: 24 }, (_, i) => String(i).padStart(2, '0'));
  /** Minute options, retaining exact next-minute suggestions. */
  readonly minutes = Array.from({ length: 60 }, (_, i) => String(i).padStart(2, '0'));
  /** Monday-first calendar headings. */
  readonly weekdays = ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su'];
  /** Propagates committed form values. */
  private change: (value: string) => void = () => {};
  /** Reports form interaction. */
  private touched: () => void = () => {};
  /** Receives external form values without rewriting invalid saved input. */
  writeValue(value: string | null): void { this.value = value ?? ''; }
  /** Registers the forms value callback. */
  registerOnChange(fn: (value: string) => void): void { this.change = fn; }
  /** Registers the forms touched callback. */
  registerOnTouched(fn: () => void): void { this.touched = fn; }
  /** Reflects disabled form state. */
  setDisabledState(disabled: boolean): void { this.disabled = disabled; }
  /** Formats the selected arrival without native input placeholders. */
  get label(): string {
    const date = new Date(`${this.value}:00Z`);
    return departureForStay(this.value, 1)
      ? new Intl.DateTimeFormat('en-GB', { timeZone: 'UTC', weekday: 'short', day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).format(date)
      : this.value || 'Choose arrival';
  }
  /** Builds only the weeks needed for the visible month without browser-timezone conversions. */
  get days(): string[] {
    const first = new Date(this.month);
    first.setUTCDate(1 - (first.getUTCDay() + 6) % 7);
    const last = new Date(this.month); last.setUTCMonth(last.getUTCMonth() + 1, 0);
    const count = Math.ceil(((this.month.getUTCDay() + 6) % 7 + last.getUTCDate()) / 7) * 7;
    return Array.from({ length: count }, (_, i) => new Date(first.getTime() + i * 86400000).toISOString().slice(0, 10));
  }
  /** Groups calendar cells into accessible Monday-first rows. */
  get weeks(): string[][] { const days = this.days; return Array.from({ length: days.length / 7 }, (_, i) => days.slice(i * 7, i * 7 + 7)); }
  /** Opens at the existing selection, defaulting an empty arrival to the next Athens minute. */
  open(): void {
    if (this.disabled) return;
    this.minimumDay = nextAthensMinute().slice(0, 10);
    this.setDraft(departureForStay(this.value, 1) ? this.value : nextAthensMinute());
    if (!this.value) this.saveSelection();
    this.changeDetector.detectChanges();
    const panel = this.dialog.nativeElement;
    panel.showModal();
    this.position();
    (panel.querySelector<HTMLButtonElement>('[aria-selected="true"]:not(:disabled)') ?? panel.querySelector<HTMLButtonElement>('button'))?.focus();
  }
  /** Fits the measured picker above or below its trigger within the visual viewport. */
  @HostListener('window:resize')
  @HostListener('window:scroll')
  position(): void {
    const panel = this.dialog?.nativeElement;
    if (!panel?.open) return;
    const viewport = window.visualViewport;
    const top = viewport?.offsetTop ?? 0;
    const left = viewport?.offsetLeft ?? 0;
    const width = viewport?.width ?? window.innerWidth;
    const height = viewport?.height ?? window.innerHeight;
    const margin = 12;
    panel.style.maxHeight = `${Math.max(0, height - margin * 2)}px`;
    panel.style.maxWidth = `${Math.max(0, width - margin * 2)}px`;
    const bounds = this.trigger.nativeElement.getBoundingClientRect();
    const size = panel.getBoundingClientRect();
    const below = bounds.bottom + 8;
    const above = bounds.top - size.height - 8;
    const preferred = below + size.height <= top + height - margin ? below : above;
    panel.style.setProperty('--picker-left', `${Math.max(left + margin, Math.min(bounds.left, left + width - size.width - margin))}px`);
    panel.style.setProperty('--picker-top', `${Math.max(top + margin, Math.min(preferred, top + height - size.height - margin))}px`);
  }
  /** Positions the calendar and time together. */
  private setDraft(value: string): void {
    this.day = value.slice(0, 10); this.hour = value.slice(11, 13); this.minute = value.slice(14, 16);
    this.month = new Date(`${this.day.slice(0, 7)}-01T00:00:00Z`);
  }
  /** Selects today's next future Athens minute immediately. */
  today(): void { this.setDraft(nextAthensMinute()); this.saveSelection(); }
  /** Selects a calendar day immediately while leaving time controls available. */
  chooseDay(day: string): void { this.day = day; this.saveSelection(); }
  /** Saves an hour or minute edit without a separate Apply action. */
  changeTime(part: 'hour' | 'minute', value: string): void { this[part] = value; this.saveSelection(); }
  /** Changes the displayed calendar month. */
  moveMonth(delta: number): void {
    const next = new Date(this.month); next.setUTCMonth(next.getUTCMonth() + delta);
    if (next.getUTCFullYear() >= 1 && next.getUTCFullYear() <= 9999) this.month = next;
  }
  /** Publishes each date/time selection without closing the picker. */
  private saveSelection(): void { this.value = `${this.day}T${this.hour}:${this.minute}`; this.change(this.value); this.touched(); }
  /** Clears arrival and closes the picker. */
  clear(): void { this.value = ''; this.change(''); this.close(); }
  /** Closes the picker and restores focus; selections are already saved. */
  close(): void { this.dialog.nativeElement.close(); this.touched(); this.trigger.nativeElement.focus(); }
  /** Supports arrow, Home and End navigation within calendar days. */
  navigate(event: KeyboardEvent, day: string): void {
    const offset: Record<string, number> = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -7, ArrowDown: 7 };
    const date = new Date(`${day}T00:00:00Z`);
    const weekday = (date.getUTCDay() + 6) % 7;
    const delta = event.key === 'Home' ? -weekday : event.key === 'End' ? 6 - weekday : offset[event.key];
    if (delta === undefined) return;
    event.preventDefault(); date.setUTCDate(date.getUTCDate() + delta);
    this.day = date.toISOString().slice(0, 10);
    if (this.day < this.minimumDay) this.day = this.minimumDay;
    this.saveSelection();
    this.month = new Date(`${this.day.slice(0, 7)}-01T00:00:00Z`);
    setTimeout(() => this.dialog.nativeElement.querySelector<HTMLButtonElement>('[aria-selected="true"]')?.focus());
  }
  /** Dismisses only pointer clicks outside the dialog, preserving interior whitespace. */
  dismissBackdrop(event: MouseEvent): void {
    if (event.target !== this.dialog.nativeElement) return;
    const bounds = this.dialog.nativeElement.getBoundingClientRect();
    if (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom) this.close();
  }
  /** Contains keyboard focus inside the modal calendar. */
  containFocus(event: KeyboardEvent): void {
    if (event.key !== 'Tab') return;
    const controls = Array.from(this.dialog.nativeElement.querySelectorAll<HTMLElement>('button, select')).filter(el => el.tabIndex >= 0 && !el.matches(':disabled'));
    const first = controls[0], last = controls[controls.length - 1];
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
  }
}
