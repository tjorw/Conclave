import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { CategoryDto } from 'shared';

export interface ChangeCategoryDialogData {
  currentCategoryId: string;
  categories: CategoryDto[];
}

@Component({
  selector: 'app-change-category-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatSelectModule],
  template: `
    <h2 mat-dialog-title>Byt kategori</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field appearance="outline" class="field">
          <mat-label>Kategori</mat-label>
          <mat-select formControlName="categoryId">
            @for (cat of data.categories; track cat.id) {
              <mat-option [value]="cat.id">{{ cat.name }}</mat-option>
            }
          </mat-select>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="cancel()">Avbryt</button>
      <button mat-flat-button color="primary" (click)="save()"
        [disabled]="form.invalid || form.value.categoryId === data.currentCategoryId">
        Spara
      </button>
    </mat-dialog-actions>
  `,
  styles: [`.field { width: 100%; min-width: 300px; margin-top: 8px; }`],
})
export class ChangeCategoryDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<ChangeCategoryDialogComponent>);
  readonly data = inject<ChangeCategoryDialogData>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.group({
    categoryId: [this.data.currentCategoryId, Validators.required],
  });

  save(): void   { this.dialogRef.close(this.form.getRawValue().categoryId); }
  cancel(): void { this.dialogRef.close(); }
}
