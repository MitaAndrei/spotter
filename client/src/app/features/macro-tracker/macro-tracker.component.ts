import {Component, effect, model, OnInit} from '@angular/core';
import {Subscription} from "rxjs";
import { CommonModule } from '@angular/common';
import {FoodService} from "../../core/services/food.service";
import {LoggedFood} from "../../core/models/logged-food.model";
import {MatIcon} from "@angular/material/icon";
import {MatDialog} from "@angular/material/dialog";
import {LogFoodDialogComponent} from "../workouts/log-food-dialog/log-food-dialog.component";
import {ActivatedRoute, Router} from "@angular/router";
import {FormsModule} from "@angular/forms";
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import {MatCard} from "@angular/material/card";
import {
  VerboseDatepickerComponent
} from "../../shared/form-fields/verbose-datepicker/verbose-datepicker.component";

@Component({
  selector: 'app-macro-tracker',
  standalone: true,
  imports: [CommonModule, MatIcon, FormsModule, MatInputModule, MatDatepickerModule, MatNativeDateModule, MatFormFieldModule, MatCard, VerboseDatepickerComponent],
  templateUrl: './macro-tracker.component.html',
  styleUrl: './macro-tracker.component.scss'
})
export class MacroTrackerComponent implements OnInit{
  date = model(new Date());
  foodsForCurrentDate: LoggedFood[] = [];
  beingEdited: LoggedFood | null = null;
  oldValue: number | null = null;
  private loadSubscription?: Subscription;

  constructor(private foodService: FoodService, private dialog: MatDialog,
              private router: Router, private route: ActivatedRoute) {

    effect( ()=> {
      this.date(); // re-run whenever the date changes
      this.getLoggedFoods()
    })
  }

  ngOnInit(): void {
    this.date.update(old => old ? new Date(old) : new Date())
    this.getLoggedFoods()
  }

  changeDay(offset: number) {
    this.date.update(old => new Date(old.setDate(old.getDate() + offset)));
  }

  getTotalCalories() : number {
    return this.foodService.getTotalCalories(this.foodsForCurrentDate)
  }

  getTotalFats() : number {
    return this.foodService.getTotalFats(this.foodsForCurrentDate)
  }

  getTotalProtein() : number {
    return this.foodService.getTotalProtein(this.foodsForCurrentDate)
  }

  getTotalCarbs() : number {
    return this.foodService.getTotalCarbs(this.foodsForCurrentDate)
  }

  getLoggedFoods(){
    // A slower response for the previous day must not replace the one for the current day.
    this.loadSubscription?.unsubscribe();
    this.loadSubscription = this.foodService.getAllForDate(this.date()).subscribe(
      foods => this.foodsForCurrentDate = foods
    )
  }

  openLogFoodDialog(){
    const dialogRef = this.dialog.open(LogFoodDialogComponent, {autoFocus: false, data: {date: this.date()}});
    dialogRef.afterClosed().subscribe( result => {
      if( result == 'logged')
        this.getLoggedFoods();
    } )
  }

  deleteLoggedFood(id: string) {
    this.foodService.deleteLoggedFood(id).subscribe(
      () => this.getLoggedFoods(),
    )
  }

  edit(loggedFood: LoggedFood) {
    this.beingEdited = loggedFood;
    this.oldValue = loggedFood.grams;
  }

  cancel(){
    this.beingEdited!.grams = this.oldValue!;
    this.beingEdited = null;
  }

  save(loggedFood: LoggedFood) {
    const shouldReturn : boolean = this.beingEdited?.grams == this.oldValue
    this.beingEdited = null;
    this.oldValue = null;
    if(shouldReturn) return;
    this.foodService.editLoggedFood(loggedFood).subscribe(
    )
  }
}
