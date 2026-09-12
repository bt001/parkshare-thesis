import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  { path: 'bookings', renderMode: RenderMode.Client },
  {
    path: 'listings/:id',
    renderMode: RenderMode.Client
  },
  {
    path: 'owner/listings/:id/edit',
    renderMode: RenderMode.Client
  },
  {
    path: 'owner/listings/:id/availability',
    renderMode: RenderMode.Client
  },
  {
    path: '**',
    renderMode: RenderMode.Prerender
  }
];
