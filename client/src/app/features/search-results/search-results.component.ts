import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {FormControl, ReactiveFormsModule} from "@angular/forms";
import {User} from "../../core/models/user.model";
import {UserService} from "../../core/services/user.service";
import {AuthService} from "../../core/services/auth.service";
import {map, of, switchMap, tap} from "rxjs";
import {ActivatedRoute, Router, RouterLink} from "@angular/router";
import {MatIcon} from "@angular/material/icon";

@Component({
  selector: 'app-search-results',
  standalone: true,
  imports: [
    RouterLink,
    MatIcon,
    ReactiveFormsModule
  ],
  templateUrl: './search-results.component.html',
  styleUrl: './search-results.component.scss'
})
export class SearchResultsComponent implements OnInit {
  searchControl = new FormControl('');
  users: User[] = [];
  query: string = '';
  private destroyRef = inject(DestroyRef);

  constructor(private userService: UserService,
              protected authService: AuthService,
              private route: ActivatedRoute,
              private router: Router,) {
  }

  ngOnInit() {
    this.route.queryParamMap.pipe(
      map(params => params.get('q') ?? ''),
      tap(query => {
        this.query = query;
        if (query) this.searchControl.setValue(query);
      }),
      switchMap(query => query ? this.userService.searchUsers(query) : of([])),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(users => this.users = users);
  }

  search(){
    this.router.navigate(['/search'], { queryParams: { q: this.searchControl.value } });
  }
}
