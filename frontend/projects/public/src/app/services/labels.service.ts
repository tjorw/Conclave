import { Injectable, computed, inject } from '@angular/core';
import { LocaleService } from './locale.service';
import { EN_LABELS } from '../labels/en';
import { SV_LABELS } from '../labels/sv';
import type { AppLabels } from '../labels/labels.interface';

@Injectable({ providedIn: 'root' })
export class LabelsService {
  private readonly localeService = inject(LocaleService);

  readonly labels = computed<AppLabels>(() =>
    this.localeService.locale() === 'en' ? EN_LABELS : SV_LABELS
  );
}
