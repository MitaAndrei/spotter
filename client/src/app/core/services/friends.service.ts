import {environment} from "../../../environments/environment";
import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";
import {FriendshipStatus} from "../models/friendship-status.model";
import {User} from "../models/user.model";

@Injectable({
  providedIn: 'root'
})
export class FriendsService {

  constructor(private http: HttpClient) { }

  baseApiUrl: string = `${environment.apiUrl}/api/`

  sendFriendRequest(receiverId: string) : Observable<void>{
    return this.http.post<void>(this.baseApiUrl + 'friends/requests', {receiverId})
  }

  acceptFriendRequest(userId: string) : Observable<void>{
    return this.http.post<void>(this.baseApiUrl + `friends/requests/${userId}/accept`, null)
  }

  rejectFriendRequest(userId: string) : Observable<void>{
    return this.http.delete<void>(this.baseApiUrl + `friends/requests/${userId}`)
  }

  unfriend(userId: string) : Observable<void>{
    return this.http.delete<void>(this.baseApiUrl + `friends/${userId}`)
  }

  getFriendshipStatus(userId: string) : Observable<FriendshipStatus>{
    return this.http.get<FriendshipStatus>(this.baseApiUrl + `friends/${userId}/status`);
  }

  getFriends(userId: string): Observable<User[]>{
    return this.http.get<User[]>(this.baseApiUrl + `users/${userId}/friends`)
  }

}
