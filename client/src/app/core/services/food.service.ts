import {environment} from "../../../environments/environment";
import { Injectable } from '@angular/core';
import {Food} from "../models/food.model";
import {Observable, Subject} from "rxjs";
import {HttpClient, HttpParams} from "@angular/common/http";
import {FoodGramsPair} from "../models/food-grams-pair.model";
import {LoggedFood} from "../models/logged-food.model";
import {DateTimeService} from "./date-time.service";

@Injectable({
  providedIn: 'root'
})
export class FoodService {

  constructor(private http: HttpClient, private dateTimeService: DateTimeService) {
  }

  baseApiUrl: string = `${environment.apiUrl}/api/`


  triggerGetFoods = new Subject<void>();

  createFood(food: Food) : Observable<Food>{
    return this.http.post<Food>(this.baseApiUrl + 'foods', food)
  }

  getAllFood(): Observable<Food[]>{
    return this.http.get<Food[]>(this.baseApiUrl + 'foods');
  }

  logFoods(date: Date, foods: FoodGramsPair[]): Observable<LoggedFood[]>{
    const params = new HttpParams().set('date', this.dateTimeService.toDateString(date));
    return this.http.post<LoggedFood[]>(this.baseApiUrl + 'logged-foods', foods, {params: params});
  }

  deleteLoggedFood(id: string): Observable<void>{
    return this.http.delete<void>(this.baseApiUrl + 'logged-foods/' + id);
  }

  editLoggedFood(loggedFood: LoggedFood): Observable<void> {
    return this.http.patch<void>(this.baseApiUrl + 'logged-foods/' + loggedFood.id, {grams: loggedFood.grams});
  }

  getAllInDateRange(start: Date, end: Date) : Observable<LoggedFood[]>{
    const fstart = this.dateTimeService.toDateString(start);
    const fend = this.dateTimeService.toDateString(end);
    return this.http.get<LoggedFood[]>(this.baseApiUrl + 'logged-foods',{
      params: {from: fstart, to: fend},});
  }

  getInDateRangeFor(userId: string, start: Date, end: Date) : Observable<LoggedFood[]>{
    const fstart = this.dateTimeService.toDateString(start);
    const fend = this.dateTimeService.toDateString(end);
    return this.http.get<LoggedFood[]>(this.baseApiUrl + `users/${userId}/logged-foods`,{
      params: {from: fstart, to: fend},});
  }

  getAllForDate(date: Date) : Observable<LoggedFood[]>{
    const fdate = this.dateTimeService.toDateString(new Date(date));
    return this.http.get<LoggedFood[]>(this.baseApiUrl + 'logged-foods',{
      params: {date: fdate},});
  }

  getTotalCalories(foods: LoggedFood[]) : number {
    return foods.reduce((total, loggedfood) => total + loggedfood.food.calories / 100 * loggedfood.grams, 0);
  }

  getTotalFats(foods: LoggedFood[]) : number {
    return foods.reduce((total, loggedfood) => total + loggedfood.food.fats / 100 * loggedfood.grams, 0);
  }

  getTotalProtein(foods: LoggedFood[]) : number {
    return foods.reduce((total, loggedfood) => total + loggedfood.food.protein / 100 * loggedfood.grams, 0);
  }

  getTotalCarbs(foods: LoggedFood[]) : number {
    return foods.reduce((total, loggedfood) => total + loggedfood.food.carbs / 100 * loggedfood.grams, 0);
  }

}
