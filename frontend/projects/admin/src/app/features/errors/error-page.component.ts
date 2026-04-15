import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ERROR_PAGE } from '../../labels/pages.labels';

type ErrorCode = '401' | '403' | '404';

const ICONS: Record<ErrorCode, string> = {
  '401': 'lock',
  '403': 'block',
  '404': 'search_off',
};

const LINKS: Record<ErrorCode, string> = {
  '401': '/login',
  '403': '/',
  '404': '/',
};

@Component({
  selector: 'app-error-page',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './error-page.component.html',
  styleUrl:    './error-page.component.scss',
})
export class ErrorPageComponent {
  private readonly route = inject(ActivatedRoute);

  private readonly code = (this.route.snapshot.data['errorCode'] ?? '404') as ErrorCode;

  readonly page = {
    ...ERROR_PAGE[this.code],
    icon: ICONS[this.code],
    link: LINKS[this.code],
  };
}
