import {Injectable, signal} from '@angular/core';
import {User} from "../models/user.model";

@Injectable({
  providedIn: 'root'
})
export class ChatStateService {

  friends = signal<User[]>([]);
  selectedFriend = signal<User | null>(null);

  constructor() { }
}
