import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  forwardRef,
  inject,
  Input,
  signal,
  ViewChild,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MarkdownComponent } from 'ngx-markdown';
import { UploadService } from '../services/upload.service';

@Component({
  selector: 'lib-markdown-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatTooltipModule, MarkdownComponent],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => MarkdownEditorComponent), multi: true }],
  template: `
    <div class="md-wrap">
      <div class="md-toolbar">
        <button type="button" mat-icon-button (click)="wrapSel('**','**','fetstil')" matTooltip="Fetstil (Ctrl+B)">
          <mat-icon>format_bold</mat-icon>
        </button>
        <button type="button" mat-icon-button (click)="wrapSel('*','*','kursiv')" matTooltip="Kursiv (Ctrl+I)">
          <mat-icon>format_italic</mat-icon>
        </button>
        <button type="button" mat-icon-button (click)="linePrefix('## ')" matTooltip="Rubrik">
          <mat-icon>title</mat-icon>
        </button>
        <button type="button" mat-icon-button (click)="linePrefix('- ')" matTooltip="Punktlista">
          <mat-icon>format_list_bulleted</mat-icon>
        </button>
        <button type="button" mat-icon-button (click)="wrapSel('[','](https://)', 'länktext')" matTooltip="Länk">
          <mat-icon>link</mat-icon>
        </button>
        <button type="button" mat-icon-button
                (click)="openImagePicker()"
                [disabled]="disabled() || uploadingImage()"
                matTooltip="Ladda upp bild">
          <mat-icon>image</mat-icon>
        </button>
        <input #imageInput class="md-file-input" type="file"
               accept="image/jpeg,image/png,image/gif,image/webp"
               (change)="onImageSelected($event)" />
        <span class="md-spacer"></span>
        <button type="button" mat-icon-button
                (click)="helpOpen.update(v => !v)"
                [matTooltip]="helpOpen() ? 'Dölj hjälp' : 'Markdown-hjälp'">
          <mat-icon>{{ helpOpen() ? 'help' : 'help_outline' }}</mat-icon>
        </button>
      </div>

      @if (helpOpen()) {
        <div class="md-help">
          <span><code>**fetstil**</code> → <strong>fetstil</strong></span>
          <span><code>*kursiv*</code> → <em>kursiv</em></span>
          <span><code>## Rubrik</code> → stor rubrik</span>
          <span><code>### Underrubrik</code></span>
          <span><code>- punkt</code> → punktlista</span>
          <span><code>1. punkt</code> → numrerad lista</span>
          <span><code>[text](https://url)</code> → länk</span>
          <span><code>![bild](url)</code> → bild</span>
        </div>
      }

      @if (imageError()) {
        <p class="md-error">{{ imageError() }}</p>
      }

      <div class="md-row">
        <mat-form-field appearance="outline" class="md-input">
          <mat-label>{{ label }}</mat-label>
          <textarea #ta matInput [rows]="rows" [disabled]="disabled()"
                    [attr.maxlength]="maxLength"
                    [value]="value()"
                    (input)="onInput($event)"
                    (blur)="onTouchedFn()"
                    (keydown)="onKeydown($event)"></textarea>
          @if (maxLength !== null) {
            <mat-hint align="end">{{ value().length }} / {{ maxLength }}</mat-hint>
          }
        </mat-form-field>
        <div class="md-preview">
          <p class="md-preview-label">Förhandsvisning</p>
          <markdown [data]="value()" />
        </div>
      </div>
    </div>
  `,
  styles: [`
    .md-wrap {
      display: flex;
      flex-direction: column;
    }

    .md-toolbar {
      display: flex;
      align-items: center;
      background: #f5f5f5;
      border: 1px solid rgba(0,0,0,.12);
      border-radius: 4px 4px 0 0;
      padding: 2px 6px;
    }

    .md-spacer { flex: 1; }

    .md-file-input {
      display: none;
    }

    .md-error {
      margin: 8px 0;
      color: #b00020;
      font-size: .82rem;
    }

    .md-help {
      display: flex;
      flex-wrap: wrap;
      gap: 6px 20px;
      background: #fffde7;
      border: 1px solid rgba(249,168,37,.5);
      border-top: none;
      padding: 10px 16px;
      font-size: .8rem;
      line-height: 1.8;

      code {
        background: rgba(0,0,0,.07);
        padding: 1px 4px;
        border-radius: 3px;
        font-size: .78rem;
      }
    }

    .md-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
      align-items: start;
    }

    .md-input { width: 100%; }

    .md-preview {
      border: 1px solid rgba(0,0,0,.12);
      border-radius: 4px;
      padding: 10px 14px;
      min-height: 150px;
      font-size: .875rem;
      line-height: 1.6;
    }

    .md-preview-label {
      margin: 0 0 8px;
      font-size: .7rem;
      opacity: .6;
      text-transform: uppercase;
      letter-spacing: .05em;
    }

    @media (max-width: 520px) {
      .md-row { grid-template-columns: 1fr; }
    }
  `],
})
export class MarkdownEditorComponent implements ControlValueAccessor {
  private readonly uploadService = inject(UploadService);

  @Input() label = 'Text';
  @Input() rows  = 8;
  @Input() maxLength: number | null = null;

  @ViewChild('ta') private ta!: ElementRef<HTMLTextAreaElement>;
  @ViewChild('imageInput') private imageInput!: ElementRef<HTMLInputElement>;

  readonly value    = signal('');
  readonly disabled = signal(false);
  readonly helpOpen = signal(false);
  readonly uploadingImage = signal(false);
  readonly imageError = signal<string | null>(null);

  onChangeFn:  (v: string) => void = () => {};
  onTouchedFn: () => void = () => {};

  writeValue(v: string | null): void            { this.value.set(v ?? ''); }
  registerOnChange(fn: (v: string) => void): void { this.onChangeFn = fn; }
  registerOnTouched(fn: () => void): void       { this.onTouchedFn = fn; }
  setDisabledState(d: boolean): void            { this.disabled.set(d); }

  onInput(e: Event): void {
    const v = (e.target as HTMLTextAreaElement).value;
    this.value.set(v);
    this.onChangeFn(v);
  }

  onKeydown(e: KeyboardEvent): void {
    if (e.ctrlKey || e.metaKey) {
      if (e.key === 'b') { e.preventDefault(); this.wrapSel('**', '**', 'fetstil'); }
      if (e.key === 'i') { e.preventDefault(); this.wrapSel('*', '*', 'kursiv'); }
    }
  }

  wrapSel(before: string, after: string, placeholder: string): void {
    const el = this.ta?.nativeElement;
    if (!el) return;
    const s = el.selectionStart;
    const e = el.selectionEnd;
    const sel = el.value.substring(s, e) || placeholder;
    const next = el.value.substring(0, s) + before + sel + after + el.value.substring(e);
    this.commit(el, next, s + before.length, s + before.length + sel.length);
  }

  linePrefix(prefix: string): void {
    const el = this.ta?.nativeElement;
    if (!el) return;
    const pos = el.selectionStart;
    const lineStart = el.value.lastIndexOf('\n', pos - 1) + 1;
    const next = el.value.substring(0, lineStart) + prefix + el.value.substring(lineStart);
    this.commit(el, next, pos + prefix.length, pos + prefix.length);
  }

  openImagePicker(): void {
    if (this.disabled() || this.uploadingImage()) return;
    this.imageError.set(null);
    this.imageInput?.nativeElement.click();
  }

  onImageSelected(e: Event): void {
    const input = e.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';

    if (!file || this.uploadingImage()) return;

    this.uploadingImage.set(true);
    this.imageError.set(null);
    this.uploadService.uploadImage(file).subscribe({
      next: url => {
        this.uploadingImage.set(false);
        this.insertImage(url);
      },
      error: () => {
        this.uploadingImage.set(false);
        this.imageError.set('Kunde inte ladda upp bilden.');
      },
    });
  }

  private insertImage(url: string): void {
    const el = this.ta?.nativeElement;
    if (!el) return;

    const s = el.selectionStart;
    const e = el.selectionEnd;
    const altText = el.value.substring(s, e).trim() || 'bild';
    const markdown = `![${altText}](${url})`;
    const next = el.value.substring(0, s) + markdown + el.value.substring(e);
    this.commit(el, next, s + markdown.length, s + markdown.length);
  }

  private commit(el: HTMLTextAreaElement, value: string, selStart: number, selEnd: number): void {
    el.value = value;
    el.selectionStart = selStart;
    el.selectionEnd = selEnd;
    el.focus();
    this.value.set(value);
    this.onChangeFn(value);
  }
}
