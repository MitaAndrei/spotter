import {environment} from "../../../environments/environment";
import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {User} from "../models/user.model";

@Injectable({
  providedIn: 'root'
})
export class UserService {

  constructor(private http: HttpClient) { }

  baseApiUrl: string = `${environment.apiUrl}/api/`

  searchUsers(query: string) {
    return this.http.get<User[]>(this.baseApiUrl + "users", {params: {search: query}});
  }

  getUserByUsername(username: string) {
    return this.http.get<User>(this.baseApiUrl + "users/" + encodeURIComponent(username));
  }
}
