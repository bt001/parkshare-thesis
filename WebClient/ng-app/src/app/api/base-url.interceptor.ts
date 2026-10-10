import { HttpInterceptorFn } from '@angular/common/http';
import { resolveApiUrl } from './api-request';

/** Resolves relative API requests without rewriting external URLs or assets. */
export const BaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  const url = resolveApiUrl(req.url);
  return next(url === req.url ? req : req.clone({ url }));
};
