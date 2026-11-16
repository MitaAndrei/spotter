import {Component, input} from '@angular/core';
import {Workout} from "../../../core/models/workout.model";
import {CommonModule} from "@angular/common";
import {MatIcon} from "@angular/material/icon";
import {MatDialog} from "@angular/material/dialog";
import {ConfirmationDialogComponent} from "../../../shared/components/confirmation-dialog/confirmation-dialog.component";
import {CdkDrag, CdkDragPlaceholder, CdkDropList} from "@angular/cdk/drag-drop";
import {WorkoutFormDialogComponent} from "../workout-form-dialog/workout-form-dialog.component";
import {WorkoutService} from "../../../core/services/workout.service";

@Component({
  selector: 'app-activity',
  standalone: true,
  imports: [CommonModule, MatIcon, CdkDrag, CdkDropList, CdkDragPlaceholder],
  templateUrl: './activity.component.html',
  styleUrl: './activity.component.scss'
})
export class ActivityComponent {

  workout = input.required<Workout>();
  editable = input(true);

  isDragging: boolean = false;

  constructor(public dialog: MatDialog, public workoutService: WorkoutService) {
  }


  openDeleteDialog(){
    const title = this.workout().isTemplate ? "Delete template?" : "Delete workout?"
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {data: { workout: this.workout(), title:title}})

    dialogRef.afterClosed().subscribe(result => {
      if(result == "cancel") return;
      this.update()
    })
  }

  openEditDialog(): void {
    const dialogRef = this.dialog.open(WorkoutFormDialogComponent,
      {data: { workout: this.workout(), title: "Edit Workout" }}
    )

    dialogRef.afterClosed().subscribe(result => {
      if(result){
      this.update()
      }
    })
  }

  update() {
    if(this.workout().isTemplate){
      this.workoutService.triggerGetTemplates.next();
    }
    else{
      this.workoutService.triggerGetWorkouts.next();
    }
  }

  onDragStarted(): void {
    this.isDragging = true;
  }

  onDragEnded(): void {
    this.isDragging = false;
  }
}
