import { Pipe, PipeTransform } from '@angular/core';
import { marked } from 'marked';

@Pipe({ name: 'stripMarkdown', standalone: true })
export class StripMarkdownPipe implements PipeTransform {
  transform(value: string | null | undefined, maxLength = 160): string {
    if (!value) return '';
    const html = marked.parse(value, { async: false }) as string;
    const plain = html.replace(/<[^>]+>/g, '').replace(/\s+/g, ' ').trim();
    return plain.length > maxLength ? plain.slice(0, maxLength).trimEnd() + '…' : plain;
  }
}
