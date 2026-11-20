import { Pipe, PipeTransform } from '@angular/core';

/** Parses an offset-free booking value as a display-only wall clock. */
function wallDate(value: string): Date | null {
  if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2}(\.\d+)?)?$/.test(value)) return null;
  const date = new Date(value + 'Z');
  return Number.isFinite(date.getTime()) && date.toISOString().slice(0, 16) === value.slice(0, 16) ? date : null;
}

/** Formats a calendar date without depending on the browser timezone. */
function day(date: Date): string {
  return new Intl.DateTimeFormat('en-GB', { timeZone: 'UTC', weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' }).format(date);
}

/** Formats a minute-precision wall-clock time. */
function time(date: Date): string {
  return new Intl.DateTimeFormat('en-GB', { timeZone: 'UTC', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' }).format(date);
}

/** Presents Athens booking wall clocks without converting them to browser time. */
@Pipe({ name: 'parkingPeriod', standalone: true })
export class ParkingPeriodPipe implements PipeTransform {
  /** Condenses same-day stays and retains both dates for overnight stays. */
  transform(start: string | null | undefined, end?: string | null): string {
    const first = start ? wallDate(start) : null;
    if (!first) return 'Time unavailable';
    if (!end) return `${day(first)} · ${time(first)}`;
    const last = wallDate(end);
    if (!last) return 'Time unavailable';
    return start!.slice(0, 10) === end.slice(0, 10)
      ? `${day(first)} · ${time(first)}–${time(last)}`
      : `${day(first)} · ${time(first)} – ${day(last)} · ${time(last)}`;
  }
}

/** Presents event instants in Athens rather than the viewer's browser timezone. */
@Pipe({ name: 'athensTimestamp', standalone: true })
export class AthensTimestampPipe implements PipeTransform {
  /** Formats event timestamps; offset-free persisted UTC values retain UTC semantics. */
  transform(value: string | null | undefined): string {
    if (!value || !/^\d{4}-\d{2}-\d{2}T/.test(value)) return 'Time unavailable';
    const date = new Date(/(Z|[+-]\d{2}:\d{2})$/i.test(value) ? value : value + 'Z');
    return Number.isFinite(date.getTime()) ? new Intl.DateTimeFormat('en-GB', {
      timeZone: 'Europe/Athens', day: 'numeric', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit', hourCycle: 'h23'
    }).format(date) : 'Time unavailable';
  }
}
