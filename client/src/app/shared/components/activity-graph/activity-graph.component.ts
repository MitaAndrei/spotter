import {Component, input, OnChanges, SimpleChanges} from '@angular/core';
import {MatTooltip} from "@angular/material/tooltip";
import {DatePipe, NgForOf} from "@angular/common";
import {Workout} from "../../../core/models/workout.model";
import {DateTimeService} from "../../../core/services/date-time.service";

@Component({
  selector: 'app-activity-graph',
  standalone: true,
  imports: [
    MatTooltip,
    NgForOf,
    DatePipe
  ],
  templateUrl: './activity-graph.component.html',
  styleUrl: './activity-graph.component.scss'
})
export class ActivityGraphComponent implements OnChanges {
  contributions = input.required<Workout[]>();
  levels: string[] = ['#ebedf0', '#c6e48b', '#7bc96f', '#239a3b', '#196127'];
  startDate= input.required<Date>();

  weeks: { date: Date; value: number }[][] = [];


  constructor(private dateTimeService: DateTimeService) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if(changes['contributions']){
      this.generateWeeks();
    }
  }

  private generateWeeks(): void {
    this.weeks = [];
    const baseDate = new Date(this.startDate());
    const daysInYear = 365;
    const daysInWeek = 7;
    let week: { date: Date; value: number }[] = [];

    for (let i = 0; i < daysInYear; i++) {
      const currentDate = new Date(baseDate);
      currentDate.setDate(baseDate.getDate() + i);

      week.push({
        date: currentDate,
        value: this.contributions().filter(w => this.dateTimeService.sameDay(new Date(w.date), currentDate)).length,
      });

      if (week.length === daysInWeek || i === daysInYear - 1) {
        this.weeks.push(week);
        week = [];
      }
    }
  }

  getColor(value: number): string {
    const level = Math.min(this.levels.length - 1, value);
    return this.levels[level];
  }
}
