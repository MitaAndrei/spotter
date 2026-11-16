import {environment} from "../../../environments/environment";
import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Workout} from "../models/workout.model";
import {Observable, Subject} from "rxjs";
import {DateTimeService} from "./date-time.service";

@Injectable({
  providedIn: 'root'
})
export class WorkoutService {

  constructor(
    private http: HttpClient,
    private dateTimeService: DateTimeService
  ) { }

  baseApiUrl: string = `${environment.apiUrl}/api/`


  triggerGetWorkouts = new Subject<void>();
  triggerGetTemplates = new Subject<void>();


  createWorkout(workout : Workout) : Observable<Workout>{
    return this.http.post<Workout>(this.baseApiUrl + 'workouts', this.toRequestBody(workout))
  }

  editWorkout(id: string, workout : Workout) : Observable<void>{
    return this.http.put<void>(this.baseApiUrl + 'workouts/' + id, this.toRequestBody(workout))
  }

  getAllForUser() : Observable<Workout[]>{
    return this.http.get<Workout[]>(this.baseApiUrl + 'workouts');
  }

  getTemplates() : Observable<Workout[]>{
    return this.http.get<Workout[]>(this.baseApiUrl + 'workouts/templates');
  }

  getAllInDateRange(start: Date, end: Date) : Observable<Workout[]>{
    const fstart = this.dateTimeService.toDateString(start);
    const fend = this.dateTimeService.toDateString(end);
    return this.http.get<Workout[]>(this.baseApiUrl + 'workouts',{
      params: {from: fstart, to: fend},});
  }

  getForUser(userId: string, start: Date, end: Date) : Observable<Workout[]>{
    const fstart = this.dateTimeService.toDateString(start);
    const fend = this.dateTimeService.toDateString(end);
    return this.http.get<Workout[]>(this.baseApiUrl + `users/${userId}/workouts`,{
      params: {from: fstart, to: fend},});
  }


  private toRequestBody(workout: Workout) {
    return {...workout, date: this.dateTimeService.toDateString(new Date(workout.date))};
  }

  deleteWorkout(id: string): Observable<void> {
    return this.http.delete<void>(this.baseApiUrl + `workouts/${id}`);
  }

}
