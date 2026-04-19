import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

type ErrorCode = '401' | '403' | '404';

const PAGE_COPY: Record<ErrorCode, { title: string; description: string; icon: string; action: string; link: string }> = {
  '401': {
    title: 'Ej inloggad',
    description: 'Du måste logga in för att komma åt den här sidan.',
    icon: 'lock',
    action: 'Gå till login',
    link: '/login',
  },
  '403': {
    title: 'Saknar behörighet',
    description: 'Det här kontot är inte systemadministratör.',
    icon: 'block',
    action: 'Logga in med annat konto',
    link: '/login',
  },
  '404': {
    title: 'Sidan hittades inte',
    description: 'Kontrollera URL:en eller gå tillbaka till startsidan.',
    icon: 'search_off',
    action: 'Till översikten',
    link: '/dashboard',
  },
};

@Component({
  selector: 'app-error-page',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './error-page.component.html',
  styleUrl: './error-page.component.scss',
})
export class ErrorPageComponent {
  private readonly route = inject(ActivatedRoute);

  private readonly code = (this.route.snapshot.data['errorCode'] ?? '404') as ErrorCode;
  readonly page = PAGE_COPY[this.code];
}
