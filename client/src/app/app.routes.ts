import {Routes} from '@angular/router';
import {MainLayoutComponent} from "./layout/main-layout/main-layout.component";
import {BlankLayoutComponent} from "./layout/blank-layout/blank-layout.component";
import {userResolver} from "./core/resolvers/user.resolver";
import {authGuard} from "./core/guards/auth.guard";

// Pages are lazy-loaded, so each one is downloaded only when it's first opened.
export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      { path: '', loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent) },
      { path: 'workouts/:username', canActivate: [authGuard],
        loadComponent: () => import('./features/workouts/workouts.component').then(m => m.WorkoutsComponent) },
      { path: 'macro', canActivate: [authGuard],
        loadComponent: () => import('./features/macro-tracker/macro-tracker.component').then(m => m.MacroTrackerComponent) },
      { path: 'auth-form',
        loadComponent: () => import('./features/auth/auth-form/auth-form.component').then(m => m.AuthFormComponent) },
      { path: 'profile/:username', canActivate: [authGuard], resolve: {user: userResolver},
        loadComponent: () => import('./features/user-profile/user-profile.component').then(m => m.UserProfileComponent) },
      { path: 'search', canActivate: [authGuard],
        loadComponent: () => import('./features/search-results/search-results.component').then(m => m.SearchResultsComponent) },
      { path: 'chat', canActivate: [authGuard], resolve: {user: userResolver},
        loadComponent: () => import('./features/chat/chat.component').then(m => m.ChatComponent),
        children: [{ path: ':username',
          loadComponent: () => import('./features/chat/chat-detail/chat-detail.component').then(m => m.ChatDetailComponent) }]},
    ]
  },
  {
    path: '',
    component: BlankLayoutComponent,
    children: [
      { path: '**', loadComponent: () => import('./features/not-found/not-found.component').then(m => m.NotFoundComponent) }
    ]
  }
];
