import {
  Component,
  effect,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild
} from '@angular/core';
import {ActivatedRoute, Router, RouterLink} from "@angular/router";
import {DatePipe, NgClass, NgForOf} from "@angular/common";
import {ChatService} from "../../../core/services/chat.service";
import {MatIcon} from "@angular/material/icon";
import {FormsModule} from "@angular/forms";
import {User} from "../../../core/models/user.model";
import {map} from "rxjs";
import {ChatStateService} from "../../../core/services/chat-state.service";
import {UserService} from "../../../core/services/user.service";
import {AuthService} from "../../../core/services/auth.service";
import {toSignal} from "@angular/core/rxjs-interop";
import {GroupMessagesByDatePipe} from "./group-messages-by-date.pipe";

@Component({
  selector: 'app-chat-detail',
  standalone: true,
  imports: [
    NgForOf,
    MatIcon,
    FormsModule,
    NgClass,
    DatePipe,
    RouterLink,
    GroupMessagesByDatePipe
  ],
  templateUrl: './chat-detail.component.html',
  styleUrl: './chat-detail.component.scss'
})
export class ChatDetailComponent implements OnInit, OnDestroy {
  @ViewChild('scrollContainer') private scrollContainer!: ElementRef<HTMLDivElement>;

  message: string = '';
  friend: User = {} as User;
  currentUser: User = this.authService.currentUserSig()!;

  chatMessages = this.chatService.messagesSignal;
  hasMore = this.chatService.hasMoreSignal;
  loading = this.chatService.loadingSignal;

  username = toSignal(this.route.paramMap.pipe(map(params => params.get('username')!)));

  private previousScrollHeight = 0;

  constructor(private route: ActivatedRoute,
              private chatService: ChatService,
              private router: Router,
              private chatStateService: ChatStateService,
              private userService: UserService,
              private authService: AuthService,) {

  }

  ngOnInit() {
    this.chatService.startConnection();
  }

  ngOnDestroy() {
    this.chatService.resetMessages();
  }

  send(){
    this.chatService.sendMessage(this.friend.id, this.message)
    this.message = '';

  }

  onScroll(event: Event) {
    const element = event.target as HTMLElement;

    // Check if user scrolled to the top (within 50px of top)
    if (element.scrollTop < 50 && this.hasMore() && !this.loading()) {
      // Save current scroll height before loading new messages
      this.previousScrollHeight = element.scrollHeight;

      // Load more messages
      this.loadMoreMessages();
    }
  }

  private loadMoreMessages() {
    this.chatService.loadMoreMessages(this.friend.id).subscribe(() => {

      setTimeout(() => {
        if (this.scrollContainer) {
          const element = this.scrollContainer.nativeElement;
          const newScrollHeight = element.scrollHeight;
          element.scrollTop = newScrollHeight - this.previousScrollHeight;
        }
      }, 0);
    });
  }


  readonly _syncFriendEffect = effect(() => {
    const cachedFriends = this.chatStateService.friends();
    const user = cachedFriends.find(f => f.username === this.username());

    if (!user) return;

    this.friend = user;
    this.chatStateService.selectedFriend.set(user);

    this.chatService.resetMessages();
    this.chatService.loadInitialMessages(user.id).subscribe(() => {
      this.scrollToBottom();
    });
  },
    {
    allowSignalWrites: true
  });


  private scrollToBottom() {
    setTimeout(() => {
      if (this.scrollContainer) {
        this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
      }
    }, 0);
  }

  private isScrolledToBottom(): boolean {
    const container = this.scrollContainer.nativeElement;
    const threshold = 100; // px distance from bottom where auto-scroll still happens
    const position = container.scrollTop + container.clientHeight;
    const height = container.scrollHeight;
    return position > height - threshold;
  }

  readonly _autoScrollEffect = effect(() => {
    const messages = this.chatService.messagesSignal();

    if (!this.scrollContainer || messages.length === 0) return;

    if(this.isScrolledToBottom()){
      this.scrollToBottom()
    }
  });
}
