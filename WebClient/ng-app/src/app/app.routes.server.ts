import { RenderMode, ServerRoute } from '@angular/ssr';

/** Browser-local authentication is restored only after client bootstrap. */
export const serverRoutes: ServerRoute[] = [
  { path: 'login', renderMode: RenderMode.Prerender },
  { path: 'register', renderMode: RenderMode.Prerender },
  { path: '**', renderMode: RenderMode.Client }
];
