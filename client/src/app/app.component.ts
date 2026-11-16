import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from './core/services/auth.service';


@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    CommonModule,
],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})

export class AppComponent implements OnInit {
  constructor(public authService: AuthService){
  }

  ngOnInit(): void {
    // A visitor without tokens isn't logged in; asking the API would only end in a 401 and a logout redirect.
    if (!localStorage.getItem('token') && !localStorage.getItem('refreshToken')) {
      this.authService.currentUserSig.set(null);
      return;
    }

    this.authService.getCurrentUser().subscribe();
  }

}

