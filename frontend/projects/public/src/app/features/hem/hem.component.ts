import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { EditionService } from '../../services/edition.service';

@Component({
  selector: 'app-hem',
  standalone: true,
  imports: [RouterLink, MatButtonModule],
  templateUrl: './hem.component.html',
  styleUrl: './hem.component.scss',
})
export class HemComponent {
  readonly editionSvc = inject(EditionService);
}
