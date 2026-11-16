import {ChangeDetectionStrategy, Component, effect, model} from '@angular/core';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {FormControl, FormsModule, ReactiveFormsModule} from '@angular/forms';
import {provideMomentDateAdapter} from '@angular/material-moment-adapter';
import {MatDatepickerModule} from '@angular/material/datepicker';
import {MatFormFieldModule} from '@angular/material/form-field';
import {MatInputModule} from '@angular/material/input';
import * as _moment from 'moment';
import {default as _rollupMoment} from 'moment';
import {MatIcon} from "@angular/material/icon";
const moment = _rollupMoment || _moment;

export const MY_FORMATS = {
  parse: {
    dateInput: 'LL',
  },
  display: {
    dateInput: 'LL',
    monthYearLabel: 'MMM YYYY',
    dateA11yLabel: 'LL',
    monthYearA11yLabel: 'MMMM YYYY',
  },
};


@Component({
  selector: 'app-verbose-datepicker',
  standalone: true,
  providers: [
    provideMomentDateAdapter(MY_FORMATS),
  ],
  imports: [
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    FormsModule,
    ReactiveFormsModule,
    MatIcon,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './verbose-datepicker.component.html',
  styleUrl: './verbose-datepicker.component.scss'
})
export class VerboseDatepickerComponent {


  readonly formControl = new FormControl(moment());
  date = model(new Date);

  constructor() {
    effect(() => {
      const dateValue = this.date();
      if (dateValue) {
        this.formControl.setValue(moment(dateValue), { emitEvent: false });
      }
    });

    this.formControl.valueChanges.pipe(takeUntilDestroyed()).subscribe(momentValue => {
      if (momentValue) {
        this.date.set(momentValue.toDate());
      }
    });
  }
}
