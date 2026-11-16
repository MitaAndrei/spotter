import {environment} from "../../../environments/environment";
import { Injectable } from '@angular/core';
import {HttpClient} from "@angular/common/http";
import {Observable} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class ImageService {

  constructor(private http: HttpClient) { }

  apiUrl: string = `${environment.apiUrl}/api/images`

  uploadImage(file: File): Observable<{ fileKey: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ fileKey: string }>(this.apiUrl, formData);
  }

  getImageUrl(fileKey: string): Observable<{ url: string }> {
    return this.http.get<{ url: string }>(`${this.apiUrl}/${fileKey}/url`);
  }

  deleteImage(fileKey: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${fileKey}`);
  }
}
