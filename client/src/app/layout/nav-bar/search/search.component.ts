import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {FormControl, ReactiveFormsModule} from "@angular/forms";
import {User} from "../../../core/models/user.model";
import {Router, RouterLink, RouterLinkActive} from "@angular/router";
import {MatIcon} from "@angular/material/icon";
import {debounceTime, distinctUntilChanged, map, of, switchMap} from "rxjs";
import {UserService} from "../../../core/services/user.service";
import {NgIf, NgFor} from "@angular/common";
import {AuthService} from "../../../core/services/auth.service";

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatIcon,
    NgIf,
    NgFor,
    RouterLinkActive
  ],
  templateUrl: './search.component.html',
  styleUrl: './search.component.scss'
})
export class SearchComponent implements OnInit {
  searchControl = new FormControl('');
  users: User[] = [];
  inputIsFocused = false;
  private destroyRef = inject(DestroyRef);

  constructor(
    private userService: UserService,
    protected authService: AuthService,
    private router: Router,
  ) {}

  ngOnInit() {
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      map(query => query ? query.trim() : ""),
      distinctUntilChanged(),
      // switchMap drops the results of a search that a newer one replaced.
      switchMap(query => query ? this.userService.searchUsers(query) : of([])),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(users => this.users = users);
  }

  onBlur() {
    // Delay to allow click events on results to fire
    setTimeout(() => {
      this.inputIsFocused = false;
    }, 200);
  }

  clearSearch() {
    this.searchControl.setValue('');
    this.users = [];
  }

  selectUser() {
    this.searchControl.setValue('');
    this.users = [];
    this.inputIsFocused = false;
  }

  trackByUsername(index: number, user: User): string {
    return user.username;
  }

  goToSearchPage() {
    this.router.navigate(['/search'], {
      queryParams: { q: this.searchControl.value}
    });
    this.searchControl.setValue("");
    this.inputIsFocused = false;
    (document.activeElement as HTMLElement)?.blur();
  }
}
