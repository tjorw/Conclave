import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ENVIRONMENT } from '../environment/environment.token';
import { MailTemplateDto, MailTemplateSummaryDto, UpdateMailTemplateRequest } from '../models/content.models';
import { ConventionContextService } from './convention-context.service';

@Injectable({ providedIn: 'root' })
export class MailTemplateService {
  private readonly http = inject(HttpClient);
  private readonly env = inject(ENVIRONMENT);
  private readonly ctx = inject(ConventionContextService);

  listTemplates() {
    const id = this.ctx.requireConventionId();
    return this.http.get<MailTemplateSummaryDto[]>(`${this.env.apiBaseUrl}/api/conventions/${id}/mail-templates`);
  }

  getTemplate(type: string) {
    const id = this.ctx.requireConventionId();
    return this.http.get<MailTemplateDto>(`${this.env.apiBaseUrl}/api/conventions/${id}/mail-templates/${type}`);
  }

  updateTemplate(type: string, request: UpdateMailTemplateRequest) {
    const id = this.ctx.requireConventionId();
    return this.http.put<void>(`${this.env.apiBaseUrl}/api/conventions/${id}/mail-templates/${type}`, request);
  }

  resetTemplate(type: string) {
    const id = this.ctx.requireConventionId();
    return this.http.post<void>(`${this.env.apiBaseUrl}/api/conventions/${id}/mail-templates/${type}/reset`, {});
  }
}
