import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const userResolver = () => {
  const authService = inject(AuthService);

  if(authService.currentUserSig())
    return authService.currentUserSig()!;

  return authService.getCurrentUser();
};
