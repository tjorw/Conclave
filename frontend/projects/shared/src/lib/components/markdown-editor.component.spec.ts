import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideMarkdown } from 'ngx-markdown';

import { MarkdownEditorComponent } from './markdown-editor.component';

describe('MarkdownEditorComponent', () => {
  let fixture: ComponentFixture<MarkdownEditorComponent>;
  let component: MarkdownEditorComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MarkdownEditorComponent],
      providers: [provideMarkdown()],
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
});
