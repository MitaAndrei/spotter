import {Component, inject} from '@angular/core';
import {FormlyFieldConfig, FormlyModule} from "@ngx-formly/core";
import {FormGroup, ReactiveFormsModule} from "@angular/forms";
import {FormlyMaterialModule} from "@ngx-formly/material";
import {MatDatepickerModule} from "@angular/material/datepicker";
import {MatNativeDateModule} from "@angular/material/core";
import {MatInputModule} from "@angular/material/input";
import {Workout} from "../../../core/models/workout.model";
import {WorkoutService} from "../../../core/services/workout.service";
import {MAT_DIALOG_DATA, MatDialogRef, MatDialogTitle} from "@angular/material/dialog";
import {Label} from "../../../core/models/label.model";
import {FormlyMatDatepickerModule} from "@ngx-formly/material/datepicker";
import {DateTimeService} from "../../../core/services/date-time.service";
import {SnackbarService} from "../../../core/services/snackbar.service";

@Component({
  selector: 'app-workout-form-dialog',
  standalone: true,
  imports: [
    FormlyModule,
    FormlyMatDatepickerModule,
    ReactiveFormsModule,
    FormlyMaterialModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatInputModule,
    MatDialogTitle,
  ],
  templateUrl: './workout-form-dialog.component.html',
  styleUrl: './workout-form-dialog.component.scss'
})
export class WorkoutFormDialogComponent {

constructor(private workoutService: WorkoutService,
            private dateTimeService: DateTimeService,
            private snackbarService: SnackbarService,) {
}

  readonly dialogRef = inject(MatDialogRef<WorkoutFormDialogComponent>);
  readonly data = inject(MAT_DIALOG_DATA)

  form = new FormGroup({});

  model : Workout = {...this.data?.workout} ?? {} as Workout;
  fields: FormlyFieldConfig[] = [{
    fieldGroup: [
      {
        key: 'description',
        type: 'textarea',
        props: {
          rows: 5,
          label: 'description',
          placeholder: 'Description',
          required: true,
        },
      },
      {
        key: 'duration',
        type: 'number',
        props: {
          label: 'duration',
          placeholder: 'Duration (minutes)',
          required: true,
          min: 1
        },
      },
      {
        key: 'musclesTargeted',
        type: 'select',
        defaultValue: this.data?.workout?.labels?.map( (label: Label) => label.muscle),
        props: {
          label: 'Muscles Targeted',
          placeholder: 'Muscles Targeted',
          multiple: true,
          options: [
            {
              label: "Chest",
              value: "Chest"
            },
            {
              label: "Back",
              value: "Back"
            },
            {
              label: "Arms",
              value: "Arms"
            },
            {
              label: "Legs",
              value: "Legs"
            },]
        },
      },
      {
        key: 'date',
        type: 'datepicker',
        props: {
          label: 'Select date',
          placeholder: 'Select date',
          required: true,
        },
        hideExpression: () => this.data?.workout?.isTemplate ?? false,
      }
    ],
  }];

  onSubmit(model: Workout) {
    if(!this.form.valid || this.form.pristine) {
      return;
    }

    if(!model.date) model.date = this.dateTimeService.unixEpoch()

    if(this.data?.title === "Edit Workout"){
      this.workoutService.editWorkout(model.id, model).subscribe( () => {
        this.dialogRef.close("edited");
      })
      return;
    }

    this.workoutService.createWorkout(model).subscribe( () => {
      this.dialogRef.close("created");
      this.snackbarService.success("Workout created")
    })
  }

}
