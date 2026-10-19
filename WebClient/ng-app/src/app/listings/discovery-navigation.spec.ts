import { convertToParamMap } from '@angular/router';
import { discoveryParamsKey, readDiscoveryNavigation, writeDiscoveryNavigation } from './discovery-navigation';

describe('Discovery navigation', () => {
  it('round trips state and omits defaults', () => {
    const state = { view: 'map' as const, page: 2, selectedId: 42, viewport: { latitude: 38, longitude: 24, zoom: 14 } };
    expect(readDiscoveryNavigation(convertToParamMap(writeDiscoveryNavigation(state)))).toEqual(state);
    expect(writeDiscoveryNavigation(readDiscoveryNavigation(convertToParamMap({})))).toEqual({});
  });

  it('discards malformed navigation and incomplete cameras', () => {
    for (const params of [
      { page: '-1', selectedId: '1.5', mapLat: '38', mapLng: '24' },
      { page: 'Infinity', selectedId: 'NaN', mapLat: '91', mapLng: '24', mapZoom: '12' },
      { page: '1.5', selectedId: '-1', mapLat: '38', mapLng: '24', mapZoom: '20' },
      { page: '2147483647', selectedId: '', mapLat: '', mapLng: '24', mapZoom: '0' }
    ]) {
      expect(readDiscoveryNavigation(convertToParamMap(params))).toEqual({ view: 'list', page: 1, selectedId: null, viewport: null });
    }
    expect(readDiscoveryNavigation(convertToParamMap({ mapLat: '0', mapLng: '0', mapZoom: '0' })).viewport)
      .toEqual({ latitude: 0, longitude: 0, zoom: 0 });
  });

  it('compares router strings and typed values including repeated parameters', () => {
    expect(discoveryParamsKey(convertToParamMap({ page: 2, amenityIds: [1, 2] })))
      .toBe(discoveryParamsKey(convertToParamMap({ amenityIds: ['1', '2'], page: '2' })));
  });
});
