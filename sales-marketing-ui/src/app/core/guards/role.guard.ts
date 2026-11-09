import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const roleGuard: CanActivateFn = (route) => {
  const router = inject(Router);

  const token = localStorage.getItem('token');
  const adminRole = Number(localStorage.getItem('adminRole'));

  // Check whether the user is logged in.
  if (!token) {
    return router.createUrlTree(['/login']);
  }

  // Get the role required by the route.
  const expectedRole = Number(route.data?.['role']);

  // Check whether the user's role matches.
  if (adminRole === expectedRole) {
    return true;
  }

  // Redirect users who don't have permission.
  if (adminRole === 1) {
    return router.createUrlTree(['/salesman']);
  }

  if (adminRole === 2) {
    return router.createUrlTree(['/marketing-team']);
  }

  return router.createUrlTree(['/login']);
};