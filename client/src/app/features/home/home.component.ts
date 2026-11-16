import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatIcon } from '@angular/material/icon';
import {WorkoutService} from "../../core/services/workout.service";
import {FoodService} from "../../core/services/food.service";
import {DateTimeService} from "../../core/services/date-time.service";
import {MatButton} from "@angular/material/button";

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatIcon,
    MatButton
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent implements OnInit {
  imgSource: string = "../../../assets";
  currentDate: Date = new Date();

  weeklyWorkouts: number = 0;
  todayCalories: number = 0;

  dailyQuote: string = "The only bad workout is the one that didn't happen.";

  quotes: string[] = [
    "The only bad workout is the one that didn't happen.",
    "Push yourself, because no one else is going to do it for you.",
    "Success starts with self-discipline.",
    "Your body can stand almost anything. It's your mind you have to convince.",
    "The pain you feel today will be the strength you feel tomorrow.",
    "Don't wish for it, work for it.",
    "Strive for progress, not perfection."
  ];

  constructor(
    public authService: AuthService,
    private workoutService: WorkoutService,
    private foodService: FoodService,
    private dateTimeService: DateTimeService
  ) {}

  ngOnInit() {
    this.dailyQuote = this.quotes[Math.floor(Math.random() * this.quotes.length)];

    this.getWorkouts();
    this.getCalories()
  }

  getWorkouts() {
    const currentWeek = this.dateTimeService.getCurrentWeekDates()
    this.workoutService.getAllInDateRange(currentWeek[0], currentWeek[6]).subscribe(
        workouts => {
          this.weeklyWorkouts = workouts.length;
        }
    )
  }

  getCalories() {
    this.foodService.getAllForDate(this.currentDate).subscribe(
      foods => this.todayCalories = this.foodService.getTotalCalories(foods),
    )
  }
}
