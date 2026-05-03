import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ConventionService, EditionDto, ProgramTagDefinitionDto } from 'shared';
import { ERROR } from '../../../labels/errors.labels';
import { createSortController, sortBy } from '../../../shared/sort-utils';

type SortKey = 'name';

@Component({
  selector: 'app-program-tag-definitions',
  standalone: true,
  imports: [MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './program-tag-definitions.component.html',
  styleUrl: './program-tag-definitions.component.scss',
})
export class ProgramTagDefinitionsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly svc = inject(ConventionService);

  readonly edition = signal<EditionDto | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly saving = signal(false);
  readonly sort = createSortController<SortKey>({ key: 'name', direction: 'asc' });

  ngOnInit(): void {
    this.route.paramMap.pipe(map((p) => p.get('id')!)).subscribe((id) => this.loadData(id));
  }

  private loadData(editionId: string): void {
    this.loading.set(true);
    this.svc.getEdition(editionId).subscribe({
      next: (e) => {
        this.edition.set(e);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(ERROR.fetchEdition);
        this.loading.set(false);
      },
    });
  }

  sortedTags(tags: ProgramTagDefinitionDto[]): ProgramTagDefinitionDto[] {
    return sortBy(tags, this.sort.state(), {
      name: (t) => t.name,
    });
  }

  openDetail(name: string): void {
    void this.router.navigate([encodeURIComponent(name)], { relativeTo: this.route });
  }

  openNew(): void {
    void this.router.navigate(['new'], { relativeTo: this.route });
  }
}
