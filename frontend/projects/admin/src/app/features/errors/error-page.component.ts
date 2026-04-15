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
  '403': '/login',
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
  private readonly reason = this.route.snapshot.queryParamMap.get('reason');

  readonly page = {
    ...ERROR_PAGE[this.code],
    description:
      this.code === '403' && this.reason === 'role'
        ? 'Du är inloggad men saknar adminrättigheter. Du har loggats ut. Logga in med ett admin-konto för att fortsätta.'
        : ERROR_PAGE[this.code].description,
    icon: ICONS[this.code],
    link: LINKS[this.code],
  };
}
