import { ParamMap, Params } from '@angular/router';

/** A settled map camera, independent of the applied search area. */
export interface MapViewport {
  /** Centre latitude in degrees. */
  latitude: number;
  /** Centre longitude in degrees. */
  longitude: number;
  /** Leaflet zoom level. */
  zoom: number;
}

/** Restorable presentation state for discovery. */
export interface DiscoveryNavigation {
  /** Active result presentation. */
  view: 'list' | 'map';
  /** Requested list page. */
  page: number;
  /** Selected result, resolved against fresh map results. */
  selectedId: number | null;
  /** Saved camera, or automatic fitting for a new search. */
  viewport: MapViewport | null;
}

/** Reads optional numbers without treating missing or blank values as zero. */
function numberParam(params: ParamMap, key: string): number {
  const value = params.get(key);
  return String(value ?? '').trim() ? Number(value) : NaN;
}

/** Parses navigation independently from API criteria, discarding malformed values. */
export function readDiscoveryNavigation(params: ParamMap): DiscoveryNavigation {
  // Keep the 12-row page offset within the server integer range.
  const page = numberParam(params, 'page');
  const selectedId = numberParam(params, 'selectedId');
  const latitude = numberParam(params, 'mapLat');
  const longitude = numberParam(params, 'mapLng');
  const zoom = numberParam(params, 'mapZoom');
  return {
    view: params.get('view') === 'map' ? 'map' : 'list',
    page: Number.isInteger(page) && page > 0 && page <= 178956970 ? page : 1,
    selectedId: Number.isSafeInteger(selectedId) && selectedId > 0 ? selectedId : null,
    viewport: Number.isFinite(latitude) && Math.abs(latitude) <= 90
      && Number.isFinite(longitude) && Math.abs(longitude) <= 180
      && Number.isFinite(zoom) && zoom >= 0 && zoom <= 19 ? { latitude, longitude, zoom } : null
  };
}

/** Serializes presentation state without default values or API search fields. */
export function writeDiscoveryNavigation(state: DiscoveryNavigation): Params {
  return {
    ...(state.view === 'map' ? { view: 'map' } : {}),
    ...(state.page > 1 ? { page: state.page } : {}),
    ...(state.selectedId !== null ? { selectedId: state.selectedId } : {}),
    ...(state.viewport ? { mapLat: state.viewport.latitude, mapLng: state.viewport.longitude, mapZoom: state.viewport.zoom } : {})
  };
}

/** Stable comparison of router parameters, including repeated amenities. */
export function discoveryParamsKey(params: ParamMap): string {
  return JSON.stringify([...params.keys].sort().map(key => [key, params.getAll(key).map(String)]));
}
