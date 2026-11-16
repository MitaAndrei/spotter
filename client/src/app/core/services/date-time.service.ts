import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class DateTimeService {

  constructor() { }

  // Workouts and logged foods belong to a calendar day, so the API gets the day the user sees,
  // as "yyyy-MM-dd". Going through toISOString would convert it to UTC and could change the day.
  toDateString(date: Date): string {
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${date.getFullYear()}-${month}-${day}`;
  }

  sameDay(d1: Date, d2: Date): boolean {
    return d1.getFullYear() === d2.getFullYear() &&
      d1.getMonth() === d2.getMonth() &&
      d1.getDate() === d2.getDate();
  }

  getCurrentWeekDates(): Date[] {
    const dates: Date[] = [];
    const today = new Date();

    const firstDayOfWeek = new Date(today);
    const dayOffset = today.getDay() === 0 ? -6 : 1 - today.getDay(); // Adjust for Monday start
    firstDayOfWeek.setDate(today.getDate() + dayOffset);
    firstDayOfWeek.setHours(0, 0, 0, 0);

    for (let i = 0; i < 7; i++) {
      const currentDate = new Date(firstDayOfWeek);
      currentDate.setDate(firstDayOfWeek.getDate() + i);
      dates.push(currentDate);
    }

    return dates;
  }

  unixEpoch(){
    return new Date(1970, 0, 1);
  }
}
