import { Injectable } from '@angular/core';
import {MatSnackBar} from "@angular/material/snack-bar";

@Injectable({
  providedIn: 'root'
})
export class SnackbarService {

  constructor(private snackbar: MatSnackBar) {

  }

  success(message: string): void {
    this.snackbar.open(message, 'Close', {
      duration: 3000,
      panelClass: 'success-snackbar',
    });
  }

  error(message: string, action: string = 'Close', duration: number = 5000) {
    this.snackbar.open(message, action, {
      duration,
      panelClass: ['error-snackbar'],
    });
  }
}
