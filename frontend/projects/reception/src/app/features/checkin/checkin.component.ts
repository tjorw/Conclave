import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { EditionContextService } from '../../services/edition-context.service';

@Component({
  selector: 'app-checkin',
  standalone: true,
  imports: [MatCardModule, MatIconModule],
  templateUrl: './checkin.component.html',
  styleUrl: './checkin.component.scss',
})
export class CheckinComponent {
  readonly editionContext = inject(EditionContextService);
}
