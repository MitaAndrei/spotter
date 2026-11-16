# Spotter client

The Angular 17 front end of Spotter. See the [main README](../README.md) for the full setup, including the API it talks to.

```bash
npm ci       # install the exact versions from package-lock.json
npm start    # dev server on http://localhost:4200
npm run build  # production build into dist/
npm run lint   # ESLint (angular-eslint), also run in CI
```

The API address comes from `src/environments/environment.ts` (production build) and `environment.development.ts` (`npm start`).

## Structure

```
src/app/
  core/       services, models, the auth guard, interceptor and resolver
  layout/     main and blank layouts, nav bar
  shared/     reusable components and form fields
  features/   one folder per page (workouts, macro-tracker, chat, ...)
```
