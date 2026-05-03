import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideMarkdown } from 'ngx-markdown';
import { of, throwError } from 'rxjs';

import { MarkdownEditorComponent } from './markdown-editor.component';
import { UploadService } from '../services/upload.service';

describe('MarkdownEditorComponent', () => {
  let fixture: ComponentFixture<MarkdownEditorComponent>;
  let component: MarkdownEditorComponent;
  let uploadImage: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    uploadImage = vi.fn();

    await TestBed.configureTestingModule({
      imports: [MarkdownEditorComponent],
      providers: [
        provideMarkdown(),
        { provide: UploadService, useValue: { uploadImage } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(MarkdownEditorComponent);
    component = fixture.componentInstance;
  });

  it('renders the configured max length and current character count', async () => {
    fixture.componentRef.setInput('maxLength', 10_000);
    component.writeValue('**Rubrik**');
    fixture.detectChanges();
    await fixture.whenStable();

    const textarea = fixture.nativeElement.querySelector('textarea') as HTMLTextAreaElement;
    const hint = fixture.nativeElement.querySelector('mat-hint') as HTMLElement;

    expect(textarea.maxLength).toBe(10_000);
    expect(hint.textContent?.replace(/\s+/g, ' ').trim()).toBe('10 / 10000');
  });

  it('emits changes from textarea input', () => {
    const onChange = vi.fn();
    component.registerOnChange(onChange);
    fixture.detectChanges();

    const textarea = fixture.nativeElement.querySelector('textarea') as HTMLTextAreaElement;
    textarea.value = '- punkt';
    textarea.dispatchEvent(new Event('input'));

    expect(onChange).toHaveBeenCalledWith('- punkt');
  });

  it('uploads an image and inserts markdown at the cursor', () => {
    uploadImage.mockReturnValue(of('/uploads/tenant/image.jpg'));
    const onChange = vi.fn();
    component.registerOnChange(onChange);
    component.writeValue('Hej  världen');
    fixture.detectChanges();

    const textarea = fixture.nativeElement.querySelector('textarea') as HTMLTextAreaElement;
    textarea.selectionStart = 4;
    textarea.selectionEnd = 4;

    const input = fixture.nativeElement.querySelector('input[type="file"]') as HTMLInputElement;
    const file = new File(['image'], 'poster.png', { type: 'image/png' });
    Object.defineProperty(input, 'files', { configurable: true, value: [file] });
    input.dispatchEvent(new Event('change'));

    expect(uploadImage).toHaveBeenCalledWith(file);
    expect(onChange).toHaveBeenLastCalledWith('Hej ![bild](/uploads/tenant/image.jpg) världen');
  });

  it('leaves the textarea unchanged when image upload fails', () => {
    uploadImage.mockReturnValue(throwError(() => new Error('fail')));
    const onChange = vi.fn();
    component.registerOnChange(onChange);
    component.writeValue('Oförändrad');
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector('input[type="file"]') as HTMLInputElement;
    const file = new File(['image'], 'poster.png', { type: 'image/png' });
    Object.defineProperty(input, 'files', { configurable: true, value: [file] });
    input.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(onChange).not.toHaveBeenCalled();
    expect(component.value()).toBe('Oförändrad');
    expect(fixture.nativeElement.textContent).toContain('Kunde inte ladda upp bilden.');
  });
});
