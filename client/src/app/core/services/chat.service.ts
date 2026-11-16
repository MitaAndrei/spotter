import {environment} from "../../../environments/environment";
import {effect, Injectable, signal} from '@angular/core';
import {HubConnection, HubConnectionBuilder, HubConnectionState} from "@microsoft/signalr";
import {ChatMessage} from "../models/chat-message.model";
import {AuthService} from "./auth.service";
import {Observable, tap} from "rxjs";
import {HttpClient, HttpParams} from "@angular/common/http";


interface ChatHistoryResponse {
  messages: ChatMessage[];
  hasMore: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  constructor(public authService: AuthService, private http: HttpClient) {
    // Drop the hub connection on logout so the next user doesn't reuse the previous user's identity.
    effect(() => {
      if (this.authService.currentUserSig() === null) {
        this.stopConnection();
        this.resetMessages();
      }
    }, {allowSignalWrites: true});
  }

  messagesSignal = signal<ChatMessage[]>([]);
  hasMoreSignal = signal<boolean>(true);
  loadingSignal = signal<boolean>(false);
  baseApiUrl: string = `${environment.apiUrl}/api/`


  private hubConnection?: HubConnection;
  // The user whose conversation is open; incoming messages from anyone else are not shown in it.
  private activeChatUserId: string | null = null;

  startConnection() {
    if (this.hubConnection && this.hubConnection.state !== HubConnectionState.Disconnected) {
      return;
    }

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/chatHub`, {
        // Read the token on every (re)connect: access tokens are short-lived and get refreshed.
        accessTokenFactory: () => localStorage.getItem('token') ?? ''
      })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start()
      .catch(err => console.error("Chat connection failed", err));

    this.listenForMessages();
  }

  stopConnection() {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = undefined;
    }
  }

  private listenForMessages() {
    this.hubConnection!.on("ReceiveMessage", (message: ChatMessage) => {
      if (message.senderId !== this.activeChatUserId && message.receiverId !== this.activeChatUserId) {
        return;
      }
      this.messagesSignal.update(messages => [
        ...messages,
        message
      ]);
    });
  }

  sendMessage(receiverId: string, message: string) {
    if(!message.trim()) return;
    this.hubConnection?.invoke("SendMessage", receiverId, message)
      .catch(err => console.error("Sending message failed", err));
  }

  loadInitialMessages(otherUser: string, limit: number = 10): Observable<ChatHistoryResponse> {
    this.activeChatUserId = otherUser;
    this.loadingSignal.set(true);

    const params = new HttpParams()
      .set('limit', limit.toString());

    return this.http.get<ChatHistoryResponse>(this.baseApiUrl + `chats/${otherUser}/messages`, { params })
      .pipe(
        tap(response => {
          this.messagesSignal.set(response.messages);
          this.hasMoreSignal.set(response.hasMore);
          this.loadingSignal.set(false);
        })
      );
  }

  loadMoreMessages(otherUser: string, limit: number = 10): Observable<ChatHistoryResponse> {
    const currentMessages = this.messagesSignal();

    if (currentMessages.length === 0 || this.loadingSignal() || !this.hasMoreSignal()) {
      return new Observable(observer => observer.complete());
    }

    this.loadingSignal.set(true);

    const oldestMessage = currentMessages[0];
    const params = new HttpParams()
      .set('limit', limit.toString())
      .set('before', new Date(oldestMessage.timestamp).toISOString());

    return this.http.get<ChatHistoryResponse>(this.baseApiUrl + `chats/${otherUser}/messages`, { params })
      .pipe(
        tap(response => {
          const updatedMessages = [...response.messages, ...currentMessages];
          this.messagesSignal.set(updatedMessages);
          this.hasMoreSignal.set(response.hasMore);
          this.loadingSignal.set(false);
        })
      );
  }

  resetMessages() {
    this.activeChatUserId = null;
    this.messagesSignal.set([]);
    this.hasMoreSignal.set(true);
    this.loadingSignal.set(false);
  }

}
