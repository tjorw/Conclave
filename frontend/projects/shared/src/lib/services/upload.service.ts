import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs';

import { ENVIRONMENT } from '../environment/environment.token';

export interface UploadResponse {
  url: string;
}

@Injectable({ providedIn: 'root' })
export class UploadService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);

  uploadImage(file: File) {
    const form = new FormData();
    form.append('file', file);

    return this.http
      .post<UploadResponse>(`${this.env.apiBaseUrl}/api/uploads`, form)
      .pipe(map(response => response.url));
  }
}
