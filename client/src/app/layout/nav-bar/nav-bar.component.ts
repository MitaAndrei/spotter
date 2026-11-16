import {CommonModule} from '@angular/common';
import {Component} from '@angular/core';
import {RouterModule} from '@angular/router';
import {MatIcon} from "@angular/material/icon";
import {AuthService} from '../../core/services/auth.service';
import {SearchComponent} from "./search/search.component";

@Component({
  selector: 'app-nav-bar',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIcon, SearchComponent],
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.scss'
})
export class NavBarComponent {
  constructor(public authService: AuthService) {
  }

  signOut() {
    this.authService.logOut();
  }
}
