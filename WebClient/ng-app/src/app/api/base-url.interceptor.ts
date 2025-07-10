import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../environments/environment';

export const BaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  // Only prepend baseUrl for relative API calls
  if (!req.url.startsWith('https')) {
    const apiReq = req.clone({
      url: environment.apiUrl + req.url.replace(/^\/+/, '')
    });
    return next(apiReq);
  }
  return next(req);
};
