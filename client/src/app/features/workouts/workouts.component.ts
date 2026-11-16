import {Component, effect, ElementRef, OnDestroy, OnInit, ViewChild} from '@angular/core';
import {WorkoutService} from "../../core/services/workout.service";
import {Workout} from "../../core/models/workout.model";
import {CommonModule} from "@angular/common";
import {DateTimeService} from "../../core/services/date-time.service";
import {MatButtonModule, MatFabButton} from "@angular/material/button";
import {MatIcon, MatIconModule} from "@angular/material/icon";
import {WorkoutFormDialogComponent} from "./workout-form-dialog/workout-form-dialog.component";
import {CalendarContentComponent} from "./calendar-content/calendar-content.component";
import {MatDialog} from "@angular/material/dialog";
import {DateRangePickerComponent} from "../../shared/form-fields/date-range-picker/date-range-picker.component";
import {DateRange} from "@angular/material/datepicker";
import {SidebarComponent} from "./sidebar/sidebar.component";
import {CdkDropList, CdkDropListGroup} from "@angular/cdk/drag-drop";
import {filter, map, Subject, takeUntil} from "rxjs";
import {CdkMenu, CdkMenuItem, CdkMenuTrigger} from "@angular/cdk/menu";
import {MatMenu, MatMenuItem, MatMenuTrigger} from "@angular/material/menu";
import {LogFoodDialogComponent} from "./log-food-dialog/log-food-dialog.component";
import {FoodService} from "../../core/services/food.service";
import {LoggedFood} from "../../core/models/logged-food.model";
import {FoodCardComponent} from "./food-card/food-card.component";
import { User } from '../../core/models/user.model';
import {toSignal} from "@angular/core/rxjs-interop";
import {ActivatedRoute} from "@angular/router";
import {AuthService} from "../../core/services/auth.service";
import {UserService} from "../../core/services/user.service";
import {FormsModule} from "@angular/forms";

@Component({
  selector: 'app-workouts',
  standalone: true,
  imports: [
    MatIconModule,
    MatButtonModule, DateRangePickerComponent,
    CalendarContentComponent, CommonModule,
    MatFabButton, MatIcon, SidebarComponent,
    CdkDropListGroup, CdkMenuTrigger, CdkMenu,
    CdkMenuItem, MatMenuTrigger, MatMenu, MatMenuItem,
    FoodCardComponent, CdkDropList, FormsModule
  ],
  templateUrl: './workouts.component.html',
  styleUrl: './workouts.component.scss'
})

export class WorkoutsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  @ViewChild('templatesScroll') templatesScroll!: ElementRef<HTMLDivElement>;

  workouts: Workout[] = [];
  loggedFoods: LoggedFood[] = []
  days: { date: Date, workouts: Workout[], loggedFoods: LoggedFood[] }[] = [];
  currentWeek: DateRange<Date> = new DateRange<Date>(new Date(), new Date());
  User: User = {} as User;
  isCurrentUser: boolean = false;
  // Only friends can see someone's workouts; the API answers 403 otherwise.
  activityHidden = false;

  username = toSignal(
    this.route.paramMap.pipe(
      map(params => params.get('username')),
      filter((username): username is string => !!username)
    ),
    {initialValue: ''}
  );

  constructor(private workoutService: WorkoutService,
              private dateTimeService: DateTimeService,
              private foodService: FoodService,
              private dialog: MatDialog,
              private route: ActivatedRoute,
              protected authService: AuthService,
              private userService: UserService,) {

    effect(() => {
      const username = this.username();
      const currentUser = this.authService.currentUserSig();

      if (!username || currentUser === undefined) return;
      this.activityHidden = false;

      if (currentUser && username === currentUser.username) {
        this.User = currentUser;
        this.isCurrentUser = true;
        this.getWorkouts()
        this.getLoggedFoods()

      } else {
        this.userService.getUserByUsername(username).subscribe(user => {
          this.User = user;
          this.isCurrentUser = false;
          this.getWorkouts()
          this.getLoggedFoods()
        });
      }
    });

  }

  ngOnInit(): void {
    const currentWeekDates = this.dateTimeService.getCurrentWeekDates();
    this.changeDates(new DateRange(currentWeekDates[0], currentWeekDates[6]));

    this.workoutService.triggerGetWorkouts
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.getWorkouts());

    this.foodService.triggerGetFoods
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.getLoggedFoods())
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  getWorkouts() {
    // Wait until we know whose page this is; the effect in the constructor fetches again then.
    if (!this.User.id) return;
    if (this.isCurrentUser) {
      this.workoutService.getAllInDateRange(this.days[0].date, this.days[6].date).subscribe(
        workouts => this.setWorkouts(workouts)
      )
    } else {
      this.workoutService.getForUser(this.User.id, this.days[0].date, this.days[6].date).subscribe({
        next: workouts => {
          this.activityHidden = false;
          this.setWorkouts(workouts);
        },
        error: () => this.activityHidden = true
      })
    }
  }

  setWorkouts(workouts: Workout[]) {
    this.workouts = workouts;
    this.days.forEach(day => {
      day.workouts = workouts
        .filter(w => this.dateTimeService.sameDay(new Date(w.date), day.date))
    })
  }

  getLoggedFoods() {
    if (!this.User.id) return;
    if (this.isCurrentUser) {
      this.foodService.getAllInDateRange(this.days[0].date, this.days[6].date).subscribe(
        lf => this.setLoggedFoods(lf)
      )
    } else {
      this.foodService.getInDateRangeFor(this.User.id, this.days[0].date, this.days[6].date).subscribe({
        next: lf => this.setLoggedFoods(lf),
        error: () => this.activityHidden = true
      })
    }
  }

  setLoggedFoods(lf: LoggedFood[]) {
    this.loggedFoods = lf;

    this.days.forEach(day => {
      day.loggedFoods = lf
        .filter(f => this.dateTimeService.sameDay(new Date(f.date), day.date))
    })
  }

  changeDates(range: DateRange<Date> | null) {
    if (range == null) return;

    if (range.start == null) return;

    this.days = []
    this.currentWeek = new DateRange(range.start, range.end);

    const first = new Date(range.start!);
    first.setHours(0, 0, 0, 0);

    for (let i = 0; i < 7; i++) {
      const currentDate = new Date(first);
      currentDate.setDate(first.getDate() + i);
      this.days.push({date: currentDate, workouts: [], loggedFoods: []});
    }
    this.getWorkouts();
    this.getLoggedFoods();
  }

  previousWeek() {
    const first = new Date(this.days[0].date);
    const last = new Date(this.days[6].date);

    first.setDate(first.getDate() - 7);
    last.setDate(last.getDate() - 7);
    this.changeDates(new DateRange(first, last));
  }

  nextWeek() {
    const first = new Date(this.days[0].date);
    const last = new Date(this.days[6].date);

    first.setDate(first.getDate() + 7);
    last.setDate(last.getDate() + 7);
    this.changeDates(new DateRange(first, last));
  }

  openAddDialog() {
    const dialogRef = this.dialog.open(WorkoutFormDialogComponent)

    dialogRef.afterClosed().subscribe(() => {
      this.getWorkouts();
    })
  }

  openFoodDialog() {
    const dialogRef = this.dialog.open(LogFoodDialogComponent, {autoFocus: false})

    dialogRef.afterClosed().subscribe(() => {
      this.getWorkouts();
    })
  }

  onSelectedWeekChange($event: DateRange<Date>) {
    this.changeDates($event);
  }
}
