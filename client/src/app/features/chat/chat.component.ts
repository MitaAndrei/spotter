import {Component, OnDestroy, OnInit, signal} from '@angular/core';
import {User} from "../../core/models/user.model";
import {ActivatedRoute, RouterLink, RouterOutlet} from "@angular/router";
import {AuthService} from "../../core/services/auth.service";
import {FriendsService} from "../../core/services/friends.service";
import {ChatStateService} from "../../core/services/chat-state.service";
import {AsyncPipe, NgClass} from "@angular/common";
import {MatIcon} from "@angular/material/icon";
import {ImageService} from "../../core/services/image.service";

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [
    RouterLink,
    RouterOutlet,
    NgClass,
    MatIcon,
    AsyncPipe
  ],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.scss'
})
export class ChatComponent implements OnInit, OnDestroy {
  currentUser: User = this.route.snapshot.data['user'];
  friends: User[] = [];
  selectedUserSig = signal<User | null>(null);

  constructor(private route: ActivatedRoute,
              private authService: AuthService,
              private friendsService: FriendsService,
              public chatStateService: ChatStateService,
              private imageService: ImageService) {

  }

  ngOnInit() {
    this.getFriends();
    this.selectedUserSig = this.chatStateService.selectedFriend;
  }

  getFriends(){
    this.friendsService.getFriends(this.currentUser.id).subscribe( friends => {
      this.friends = friends;
      this.chatStateService.friends.set(this.friends);
    });
  }

  selectUser(user: User) {
    this.chatStateService.selectedFriend.set(user);
  }

  ngOnDestroy() {
    this.chatStateService.selectedFriend.set(null);
  }
}
