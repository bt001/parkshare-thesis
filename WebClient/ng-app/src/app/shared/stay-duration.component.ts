import { Component, ElementRef, forwardRef, HostListener, inject, Input } from '@angular/core';
import { ControlValueAccessor, FormsModule, NG_VALIDATORS, NG_VALUE_ACCESSOR, ValidationErrors, Validator } from '@angular/forms';

/** Reusable stay-length control with quick presets and precise custom durations. */
@Component({
  selector: 'app-stay-duration', standalone: true, host: { '[class.compact]': 'compact' }, imports: [FormsModule],
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => StayDurationComponent), multi: true },
    { provide: NG_VALIDATORS, useExisting: forwardRef(() => StayDurationComponent), multi: true }
  ],
  template: `
    @if (compact) { <svg class="duration-icon" aria-hidden="true" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></svg> }
    <select [class.custom-selected]="compact && custom" [id]="inputId" aria-label="Length of stay" [disabled]="disabled" [ngModel]="custom ? 'custom' : (value ?? '')" [ngModelOptions]="{standalone: true}" (ngModelChange)="select($event)">
      <option value="">Choose length</option>
      @for (minutes of presets; track minutes) { <option [value]="minutes">{{ minutes === 30 ? '30 minutes' : minutes / 60 + (minutes === 60 ? ' hour' : ' hours') }}</option> }
      <option value="custom">{{ custom && value ? (hours + ' hr ' + minutes + ' min') : 'Custom duration' }}</option>
    </select>
    @if (compact && custom && !customOpen) { <button type="button" class="edit-custom" aria-label="Edit custom duration" (click)="customOpen = true">Edit</button> }
    @if (custom && (!compact || customOpen)) {
      <div class="custom-panel" [style]="compact ? customPanelStyle : null"><div class="custom-duration">
        <label>Hours<input type="number" min="0" step="1" [disabled]="disabled" [value]="hours" (input)="setCustom('hours', $any($event.target).value)" /></label>
        <label>Minutes<input type="number" min="0" max="59" step="1" [disabled]="disabled" [value]="minutes" (input)="setCustom('minutes', $any($event.target).value)" /></label>
      </div>
      @if (!value) { <p class="error" role="status">Enter whole hours and 0–59 minutes, with at least 1 minute in total.</p> }
      @if (compact) { <button type="button" class="close-custom" (click)="customOpen = false">Done</button> }</div>
    }
  `,
  styles: [`
    :host { display: block; min-width: 0; }
    select, input { width: 100%; min-width: 0; min-height: 2.85rem; padding: .65rem .8rem; font: inherit; border: 1px solid #a9b9c4; border-radius: .65rem; background: var(--surface); color: var(--ink); }
    .custom-duration { display: grid; grid-template-columns: 1fr 1fr; gap: .6rem; margin-top: .6rem; }
    label { display: grid; gap: .25rem; color: var(--muted); font-size: .8rem; }
    :host(.compact) { position: relative; }
    :host(.compact) > select { border: 0; min-height: 32px; padding: .15rem 0 .15rem 1.65rem; border-radius: .25rem; font-size: .9rem; }
    .duration-icon { position: absolute; left: 0; top: 7px; color: var(--accent); pointer-events: none; }
    :host(.compact) .custom-panel { position: absolute; top: calc(100% + 12px); right: -.5rem; width: min(240px, calc(100vw - 48px)); padding: .8rem; border: 1px solid var(--line); border-radius: .75rem; background: var(--surface); box-shadow: 0 8px 28px #16324f22; z-index: 20; }
    :host(.compact) .custom-duration { margin-top: 0; }
    :host(.compact) .custom-selected { padding-right: 3.75rem; }
    .edit-custom { position: absolute; right: 1.3rem; top: 2px; min-height: 28px; padding: .2rem .35rem; border: 0; border-radius: .25rem; background: var(--soft); color: var(--accent); cursor: pointer; font-size: .75rem; }
    .close-custom { display: block; margin: .6rem 0 0 auto; border: 0; border-radius: .4rem; padding: .4rem .7rem; background: var(--accent); color: white; cursor: pointer; }
    .error { color: var(--danger); font-size: .8rem; margin: .4rem 0 0; }
  `]
})
export class StayDurationComponent implements ControlValueAccessor, Validator {
  /** Fits the optional custom editor inside the visible viewport. */
  get customPanelStyle(): Record<string, string> {
    const bounds = this.element.nativeElement.getBoundingClientRect();
    const viewport = window.visualViewport;
    const left = viewport?.offsetLeft ?? 0, top = viewport?.offsetTop ?? 0;
    const width = viewport?.width ?? window.innerWidth, height = viewport?.height ?? window.innerHeight;
    const below = top + height - bounds.bottom - 20, above = bounds.top - top - 20;
    const upwards = below < 180 && above > below;
    const panelWidth = Math.min(240, width - 24);
    return { position: 'fixed', width: `${panelWidth}px`, right: 'auto',
      left: `${Math.max(left + 12, Math.min(bounds.right - panelWidth, left + width - panelWidth - 12))}px`,
      top: upwards ? 'auto' : `${Math.max(top + 12, bounds.bottom + 8)}px`,
      bottom: upwards ? `${window.innerHeight - bounds.top + 8}px` : 'auto',
      'max-height': `${Math.max(0, Math.min(352, upwards ? above : below))}px`, 'overflow-y': 'auto' };
  }
  /** Re-evaluates floating placement after a viewport change. */
  @HostListener('window:resize')
  @HostListener('window:scroll')
  updatePlacement(): void {}
  /** Limits outside dismissal to this duration editor. */
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
  /** Whether the compact custom editor is expanded. */
  customOpen = false;
  /** Dismisses the compact editor without changing its immediately saved values. */
  @HostListener('document:pointerdown', ['$event'])
  dismissOutside(event: PointerEvent): void {
    if (this.compact && !this.element.nativeElement.contains(event.target as Node)) this.customOpen = false;
  }
  /** Supports keyboard dismissal of the floating duration editor. */
  @HostListener('keydown.escape', ['$event'])
  dismissCustom(event: Event): void {
    if (this.compact && this.customOpen) { event.preventDefault(); event.stopPropagation(); this.customOpen = false; }
  }
  /** Uses a borderless select and floating custom editor in the discovery bar. */
  @Input() compact = false;
  /** Unique identifier supplied by the containing form. */
  @Input() inputId = 'stay-duration';
  /** Common parking durations in minutes. */
  readonly presets = [30, 60, 120, 180, 240, 480, 720];
  /** Current valid total minutes, or an incomplete selection. */
  value: number | null = null;
  /** Whether custom fields are displayed. */
  custom = false;
  /** Editable whole hours, including multi-day stays. */
  hours: number | null = 1;
  /** Editable minute remainder. */
  minutes: number | null = 0;
  /** Disabled state supplied by reactive forms. */
  disabled = false;
  /** Reports committed duration changes. */
  private change: (value: number | null) => void = () => {};
  /** Reports interaction with the duration control. */
  private touched: () => void = () => {};
  /** Restores a duration, revealing custom fields for non-preset values. */
  writeValue(value: number | null): void {
    this.value = value;
    this.customOpen = false;
    this.custom = value !== null && !this.presets.includes(value);
    if (value !== null) { this.hours = Math.floor(value / 60); this.minutes = value % 60; }
  }
  /** Keeps invalid custom input distinct from an optional empty duration. */
  validate(): ValidationErrors | null { return this.custom && this.value === null ? { stayDuration: true } : null; }
  /** Registers the reactive-forms change callback. */
  registerOnChange(fn: (value: number | null) => void): void { this.change = fn; }
  /** Registers the reactive-forms touched callback. */
  registerOnTouched(fn: () => void): void { this.touched = fn; }
  /** Updates disabled state. */
  setDisabledState(disabled: boolean): void { this.disabled = disabled; }
  /** Selects a preset or opens custom fields using the existing length. */
  select(raw: string): void {
    this.custom = raw === 'custom';
    this.customOpen = this.custom;
    if (this.custom) { const value = this.value ?? 60; this.hours = Math.floor(value / 60); this.minutes = value % 60; this.publish(value); }
    else this.publish(raw ? Number(raw) : null);
  }
  /** Validates custom parts before publishing their total. */
  setCustom(part: 'hours' | 'minutes', raw: string): void {
    this[part] = raw.trim() ? Number(raw) : null;
    const hours = this.hours, minutes = this.minutes;
    const total = hours! * 60 + minutes!;
    this.publish(hours !== null && minutes !== null && Number.isSafeInteger(hours) && hours >= 0
      && Number.isInteger(minutes) && minutes >= 0 && minutes <= 59 && Number.isSafeInteger(total) && total > 0 ? total : null);
  }
  /** Publishes a validated length and marks the control touched. */
  private publish(value: number | null): void { this.value = value; this.change(value); this.touched(); }
}
