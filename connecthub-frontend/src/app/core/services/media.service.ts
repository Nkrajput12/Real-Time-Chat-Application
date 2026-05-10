import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MediaFile } from '../models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class MediaService {
  private apiUrl = `${environment.apiUrl}/api/media`;

  constructor(private http: HttpClient) {}

  uploadFile(file: File, roomId?: number): Observable<MediaFile> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    if (roomId) formData.append('roomId', roomId.toString());
    return this.http.post<MediaFile>(`${this.apiUrl}/upload`, formData);
  }

  getFilesByRoom(roomId: number): Observable<MediaFile[]> {
    return this.http.get<MediaFile[]>(`${this.apiUrl}/room/${roomId}`);
  }

  getSasUrl(fileId: string): Observable<{ sasUrl: string }> {
    return this.http.get<{ sasUrl: string }>(`${this.apiUrl}/${fileId}/sas-url`);
  }

  deleteFile(fileId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${fileId}`);
  }
}
