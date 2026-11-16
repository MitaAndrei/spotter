import { Pipe, PipeTransform } from '@angular/core';
import {ChatMessage} from "../../../core/models/chat-message.model";
export interface MessageGroup {
  date: Date;
  messages: ChatMessage[];
}

@Pipe({
  name: 'groupMessagesByDate',
  standalone: true
})
export class GroupMessagesByDatePipe implements PipeTransform {
  transform(messages: ChatMessage[]): MessageGroup[] {
    if (!messages || messages.length === 0) {
      return [];
    }

    const groups = new Map<string, ChatMessage[]>();

    messages.forEach(message => {
      const date = new Date(message.timestamp);
      const dateKey = date.toDateString();

      if (!groups.has(dateKey)) {
        groups.set(dateKey, []);
      }
      groups.get(dateKey)!.push(message);
    });

    return Array.from(groups.entries()).map(([dateStr, msgs]) => ({
      date: new Date(dateStr),
      messages: msgs
    }));
  }
}
