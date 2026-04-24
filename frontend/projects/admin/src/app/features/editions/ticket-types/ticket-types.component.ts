import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ConventionService, EditionDto, RegistrationService, TicketTypeAdminDto, toContextErrorMessage } from 'shared';
import { ERROR } from '../../../labels/errors.labels';
import { EDITION_DETAIL } from '../../../labels/pages.labels';
import { TOOLTIP } from '../../../labels/ui.labels';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog.component';
import { nextSort, sortBy, sortIcon, SortState } from '../../../shared/sort-utils';

type SortKey = 'name' | 'category' | 'validDays' | 'allowedCategories' | 'price';

@Component({
  selector: 'app-ticket-types',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
  ],
  templateUrl: './ticket-types.component.html',
  styleUrl: './ticket-types.component.scss',
})
export class TicketTypesComponent implements OnInit {
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly svc    = inject(ConventionService);
  private readonly regSvc = inject(RegistrationService);
  private readonly dialog = inject(MatDialog);

  readonly edition     = signal<EditionDto | null>(null);
  readonly ticketTypes = signal<TicketTypeAdminDto[]>([]);
  readonly loading     = signal(true);
  readonly error       = signal<string | null>(null);
  readonly saving      = signal(false);
  readonly sort        = signal<SortState<SortKey>>({ key: 'name', direction: 'asc' });

  readonly PAGE    = EDITION_DETAIL;
  readonly TOOLTIP = TOOLTIP;

  readonly editionDayOptions = computed(() => {
    const e = this.edition();
    if (!e) return [] as { value: string; label: string }[];
    const start = new Date(`${e.start.substring(0, 10)}T00:00:00`);
    const end   = new Date(`${e.end.substring(0, 10)}T00:00:00`);
    if (Number.isNaN(start.getTime()) || Number.isNaN(end.getTime()) || start > end) {
      return [] as { value: string; label: string }[];
    }
    const options: { value: string; label: string }[] = [];
    for (const cur = new Date(start); cur <= end; cur.setDate(cur.getDate() + 1)) {
      const value = `${cur.getFullYear()}-${String(cur.getMonth() + 1).padStart(2, '0')}-${String(cur.getDate()).padStart(2, '0')}`;
      const label = new Intl.DateTimeFormat('sv-SE', { weekday: 'long', day: 'numeric', month: 'long' }).format(cur);
      options.push({ value, label });
    }
    return options;
  });

  ngOnInit(): void {
    this.route.paramMap.pipe(map(p => p.get('id')!)).subscribe(id => this.loadData(id));
  }

  private loadData(editionId: string): void {
    this.loading.set(true);
    this.svc.getEdition(editionId).subscribe({
      next: e => { this.edition.set(e); this.loading.set(false); },
      error: () => { this.error.set(ERROR.fetchEdition); this.loading.set(false); },
    });
    this.regSvc.listTicketTypes(editionId).subscribe({ next: tt => this.ticketTypes.set(tt) });
  }

  private reload(): void {
    const id = this.edition()!.id;
    this.svc.getEdition(id).subscribe({ next: e => this.edition.set(e) });
    this.regSvc.listTicketTypes(id).subscribe({ next: tt => this.ticketTypes.set(tt) });
  }

  categoryLabel(cat: string): string {
    const map: Record<string, string> = { Visitor: 'Besökare', Organiser: 'Arrangör', Staff: 'Funktionär' };
    return map[cat] ?? cat;
  }

  validDaysLabel(validDays: string[] | null): string {
    if (!validDays || validDays.length === 0) return 'Alla dagar';
    const labelMap = new Map(this.editionDayOptions().map(o => [o.value, o.label]));
    return validDays.map(d => labelMap.get(d) ?? d).join(', ');
  }

  allowedCategoriesLabel(allowedCategories: string[] | null): string {
    if (!allowedCategories || allowedCategories.length === 0) return 'Alla kategorier';
    const catMap = new Map((this.edition()?.categories ?? []).map(c => [c.id, c.name]));
    return allowedCategories.map(id => catMap.get(id) ?? id).join(', ');
  }

  formatPrice(priceInOre: number): string {
    return (priceInOre / 100).toLocaleString('sv-SE', { style: 'currency', currency: 'SEK', maximumFractionDigits: 0 });
  }

  sortedTicketTypes(): TicketTypeAdminDto[] {
    return sortBy(this.ticketTypes(), this.sort(), {
      name:             tt => tt.name,
      category:         tt => this.categoryLabel(tt.category),
      validDays:        tt => this.validDaysLabel(tt.validDays),
      allowedCategories:tt => this.allowedCategoriesLabel(tt.allowedCategories),
      price:            tt => tt.price,
    });
  }

  setSort(key: SortKey): void { this.sort.set(nextSort(this.sort(), key)); }
  sortIcon(key: SortKey): string { return sortIcon(this.sort(), key); }

  openDetail(ticketTypeId: string): void {
    void this.router.navigate([ticketTypeId], { relativeTo: this.route });
  }

  openNew(): void {
    void this.router.navigate(['new'], { relativeTo: this.route });
  }

  delete(tt: TicketTypeAdminDto): void {
    this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: { title: this.PAGE.deleteTicketTypeTitle, message: this.PAGE.deleteTicketTypeMessage(tt.name) },
        width: '400px',
      })
      .afterClosed()
      .pipe(map(r => r === true))
      .subscribe(confirmed => {
        if (!confirmed) return;
        this.saving.set(true);
        this.regSvc.deleteTicketType(this.edition()!.id, tt.id).subscribe({
          next: () => { this.reload(); this.saving.set(false); },
          error: (err) => { this.error.set(toContextErrorMessage(err, ERROR.deleteTicketType)); this.saving.set(false); },
        });
      });
  }
}
