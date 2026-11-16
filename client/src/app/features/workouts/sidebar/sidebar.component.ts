import { Component, OnDestroy, OnInit } from '@angular/core';
import { Workout } from "../../../core/models/workout.model";
import {CdkDrag, CdkDragDrop, CdkDropList} from "@angular/cdk/drag-drop";
import {NgClass, NgForOf, NgIf} from "@angular/common";
import { MatIcon } from "@angular/material/icon";
import { WorkoutService } from "../../../core/services/workout.service";
import { DateTimeService } from "../../../core/services/date-time.service";
import { Label } from "../../../core/models/label.model";
import { Subject, takeUntil } from "rxjs";
import { MatDialog } from "@angular/material/dialog";
import { WorkoutFormDialogComponent } from "../workout-form-dialog/workout-form-dialog.component";
import { MatMenu, MatMenuItem, MatMenuTrigger } from "@angular/material/menu";
import {ActivityComponent} from "../activity/activity.component";

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    CdkDropList,
    NgForOf,
    NgIf,
    MatIcon,
    MatMenuTrigger,
    MatMenu,
    MatMenuItem,
    NgClass,
    CdkDrag,
    ActivityComponent
  ],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent implements OnInit, OnDestroy {
  templates: Workout[] = [];
  private destroy$ = new Subject<void>();

  constructor(
    public workoutService: WorkoutService,
    public dateTimeService: DateTimeService,
    private dialog: MatDialog
  ) {}

  ngOnInit() {
    this.getTemplates();
    this.workoutService.triggerGetTemplates
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.getTemplates());
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  drop(event: CdkDragDrop<Workout>){
    if(event.previousContainer == event.container) return;

    const w: Workout = {...event.item.data,
      isTemplate: true,
      date: this.dateTimeService.unixEpoch(),
      musclesTargeted: event.item.data.labels.map( (label: Label) => label.muscle)
    };
    this.workoutService.createWorkout(w).subscribe( () => this.getTemplates())
  }

  getTemplates() {
    this.workoutService.getTemplates().subscribe(templates => {
      this.templates = templates;
    });
  }

  openNewTemplateDialog() {
    const dialogRef = this.dialog.open(WorkoutFormDialogComponent, {
      data: { title: "Create Template", workout: {isTemplate: true} as Workout }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.workoutService.triggerGetTemplates.next();
      }
    });
  }

}
