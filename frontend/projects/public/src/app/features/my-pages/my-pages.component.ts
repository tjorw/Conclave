import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-my-pages',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="page-container" style="padding: 48px 16px; max-width: 640px;">
      <h1 style="font-size: 1.75rem; font-weight: 500; color: var(--brand-primary); margin: 0 0 32px;">
        Mina sidor
      </h1>
      <div style="display: flex; flex-direction: column; gap: 12px;">
        <a routerLink="/my-pages/events"
           style="display: block; background: var(--brand-surface); border-radius: 10px;
                  box-shadow: 0 2px 8px rgba(0,0,0,.06); padding: 18px 22px;
                  text-decoration: none; color: var(--brand-text); font-weight: 500; font-size: 1rem;">
          Mina arrangemang
        </a>
        <a routerLink="/my-pages/profile"
           style="display: block; background: var(--brand-surface); border-radius: 10px;
                  box-shadow: 0 2px 8px rgba(0,0,0,.06); padding: 18px 22px;
                  text-decoration: none; color: var(--brand-text); font-weight: 500; font-size: 1rem;">
          Min profil
        </a>
      </div>
    </div>
  `,
})
export class MyPagesComponent {}
