import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-my-program',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="page-container" style="padding: 48px 16px; max-width: 640px;">
      <a routerLink="/my-pages" style="display: inline-block; margin-bottom: 24px;
         color: var(--brand-primary); text-decoration: none; font-size: 0.9rem;">
        ← Mina sidor
      </a>
      <h1 style="font-size: 1.75rem; font-weight: 500; color: var(--brand-primary); margin: 0 0 16px;">
        Mitt program
      </h1>
      <p style="color: var(--brand-text-secondary);">Sessionsregistreringar är inte tillgängliga ännu.</p>
    </div>
  `,
})
export class MyProgramComponent {}
