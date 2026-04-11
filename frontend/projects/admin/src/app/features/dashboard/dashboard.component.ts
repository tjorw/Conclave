import { Component, inject, OnInit, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ConventionDto, ConventionService, EditionSummaryDto } from 'shared';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    MatCardModule,
    MatChipsModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private readonly conventionService = inject(ConventionService);

  readonly convention = signal<ConventionDto | null>(null);
  readonly editions = signal<EditionSummaryDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.conventionService.getConvention().subscribe({
      next: c => this.convention.set(c),
      error: () => this.error.set('Kunde inte hämta konventionsdata.'),
    });

    this.conventionService.listEditions().subscribe({
      next: e => {
        this.editions.set(e);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Kunde inte hämta upplagedata.');
        this.loading.set(false);
      },
    });
  }

  statusLabel(status: string): string {
    return status === 'Published' ? 'Publicerad' : 'Utkast';
  }

  statusColor(status: string): string {
    return status === 'Published' ? 'primary' : 'default';
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('sv-SE');
  }
}
