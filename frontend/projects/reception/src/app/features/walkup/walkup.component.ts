import { Component, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { EditionContextService } from '../../services/edition-context.service';

@Component({
  selector: 'app-walkup',
  standalone: true,
  imports: [MatCardModule, MatIconModule],
  templateUrl: './walkup.component.html',
  styleUrl: './walkup.component.scss',
})
export class WalkupComponent {
  readonly editionContext = inject(EditionContextService);
}
