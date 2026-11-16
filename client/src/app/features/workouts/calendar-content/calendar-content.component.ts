import {Component, input} from '@angular/core';
import {Workout} from "../../../core/models/workout.model";
import { CommonModule } from '@angular/common';
import {ActivityComponent} from "../activity/activity.component";
import {MatDialog} from "@angular/material/dialog";
import {WorkoutService} from "../../../core/services/workout.service";
import {CdkDrag, CdkDragDrop, CdkDropList} from "@angular/cdk/drag-drop";
import {Label} from "../../../core/models/label.model";
import {FoodCardComponent} from "../food-card/food-card.component";
import {LoggedFood} from "../../../core/models/logged-food.model";
import {DateTimeService} from "../../../core/services/date-time.service";
import {MatIcon} from "@angular/material/icon";

@Component({
  selector: 'app-calendar-content',
  standalone: true,
  imports: [CommonModule, ActivityComponent, CdkDrag, CdkDropList, FoodCardComponent, MatIcon],
  templateUrl: './calendar-content.component.html',
  styleUrl: './calendar-content.component.scss'
})
export class CalendarContentComponent {
  workouts = input.required<Workout[]>();
  foods = input.required<LoggedFood[]>();
  date = input.required<Date>();
  // False when looking at someone else's workouts: nothing can be added, dropped or edited.
  editable = input(true);

  constructor(public dialog: MatDialog,
              public workoutService: WorkoutService,
              public dateTimeService: DateTimeService,) {
  }

  drop(event: CdkDragDrop<Workout[]>){
    if(event.container == event.previousContainer ||
      (event.previousContainer.id != "templateList"  && !event.event.shiftKey) ) return;

    const w: Workout = {...event.item.data,
      isTemplate: false,
      date: this.date(),
      musclesTargeted: event.item.data.labels.map( (label: Label) => label.muscle)
    };

    this.workoutService.createWorkout(w).subscribe( () => this.workoutService.triggerGetWorkouts.next());

  }

  dayIsToday(): boolean{
    return this.dateTimeService.sameDay(new Date(), this.date())
  }

  getDayName(date: Date){
    const days = [
      "Sunday",
      "Monday",
      "Tuesday",
      "Wednesday",
      "Thursday",
      "Friday",
      "Saturday",
    ];

    return days[date.getDay()];
  }
}
