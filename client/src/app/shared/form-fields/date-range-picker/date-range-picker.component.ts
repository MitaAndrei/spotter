import {ChangeDetectionStrategy, Component, Injectable, inject, model} from '@angular/core';
import {
  DateAdapter,
  NativeDateAdapter
} from '@angular/material/core';
import {
  DateRange,
  MAT_DATE_RANGE_SELECTION_STRATEGY,
  MatDateRangeSelectionStrategy,
  MatDatepickerInputEvent,
  MatDatepickerModule,
} from '@angular/material/datepicker';
import {MatFormFieldModule} from '@angular/material/form-field';
import {DatePipe, NgIf} from "@angular/common";
import {
  FormsModule,
  ReactiveFormsModule
} from "@angular/forms";
import {CdkConnectedOverlay} from "@angular/cdk/overlay";

@Injectable()
export class MondayFirstDateAdapter extends NativeDateAdapter {
  override getFirstDayOfWeek(): number {
    return 1; // 0 = Sunday, 1 = Monday
  }
}


@Injectable()
export class WeekSelectionStrategy<D> implements MatDateRangeSelectionStrategy<D> {
  private _dateAdapter = inject<DateAdapter<D>>(DateAdapter<D>);

  selectionFinished(date: D | null): DateRange<D> {
    return this._createWeek(date);
  }

  createPreview(activeDate: D | null): DateRange<D> {
    return this._createWeek(activeDate);
  }

  private _createWeek(date: D | null): DateRange<D> {
    if (!date) {
      return new DateRange<D>(null, null);
    }

    const dayOfWeek = this._dateAdapter.getDayOfWeek(date); // Sunday = 0, Monday = 1, ...
    const mondayDiff = (dayOfWeek + 6) % 7; // days to subtract to reach Monday
    const sundayDiff = 7 - mondayDiff - 1;  // days to add to reach Sunday

    const start = this._dateAdapter.addCalendarDays(date, -mondayDiff);
    const end = this._dateAdapter.addCalendarDays(date, sundayDiff);

    return new DateRange<D>(start, end);
  }
}

@Component({
  selector: 'app-date-range-picker',
  standalone: true,
  providers: [
    {
      provide: MAT_DATE_RANGE_SELECTION_STRATEGY,
      useClass: WeekSelectionStrategy,
    },
    { provide: DateAdapter, useClass: MondayFirstDateAdapter },
  ],
  imports: [MatFormFieldModule, MatDatepickerModule, DatePipe, NgIf, FormsModule, ReactiveFormsModule, CdkConnectedOverlay],
  templateUrl: './date-range-picker.component.html',
  styleUrl: './date-range-picker.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DateRangePickerComponent{

  selectedWeek = model(new DateRange<Date>(new Date(), new Date()));

  onChange(event: MatDatepickerInputEvent<Date>){
    if (!event.value) return;
    const startDay = new Date(event.value);
    const endDay = new Date(startDay);
    endDay.setDate(endDay.getDate() + 6);
    this.selectedWeek.set(new DateRange(startDay, endDay))
  }

  getDatesString(): string {
    const first = this.selectedWeek().start!;
    const last = this.selectedWeek().end!;

    const firstMonth = first.toLocaleString("en-US", {month: "long"});
    const lastMonth = last.toLocaleString("en-US", {month: "long"});
    const firstYear = first.getFullYear();
    const lastYear = last.getFullYear();

    if (firstMonth === lastMonth && firstYear === lastYear) {
      return `${firstMonth} ${first.getDate()}-${last.getDate()}, ${firstYear}`;
    } else if (firstMonth !== lastMonth && firstYear === lastYear) {
      return `${firstMonth} ${first.getDate()} - ${lastMonth} ${last.getDate()}, ${firstYear}`;
    } else {
      return `${firstMonth} ${first.getDate()}, ${firstYear} - ${lastMonth} ${last.getDate()}, ${lastYear}`;
    }
  }

}


