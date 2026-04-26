import {
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

declare class BarcodeDetector {
  constructor(options: { formats: string[] });
  detect(source: HTMLVideoElement): Promise<{ rawValue: string }[]>;
}

@Component({
  selector: 'app-qr-scanner',
  standalone: true,
  imports: [MatButtonModule, MatIconModule],
  templateUrl: './qr-scanner.component.html',
  styleUrl: './qr-scanner.component.scss',
})
export class QrScannerComponent implements OnInit, OnDestroy {
  readonly scanned = output<string>();
  readonly closed = output<void>();

  private readonly videoRef = viewChild.required<ElementRef<HTMLVideoElement>>('video');

  readonly error = signal<string | null>(null);
  readonly active = signal(false);

  private stream: MediaStream | null = null;
  private detector: BarcodeDetector | null = null;
  private animationFrame = 0;

  readonly supported = 'BarcodeDetector' in window;

  ngOnInit(): void {
    if (this.supported) {
      this.startCamera();
    }
  }

  ngOnDestroy(): void {
    this.stopCamera();
  }

  private async startCamera(): Promise<void> {
    try {
      this.stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'environment' },
      });
      const video = this.videoRef().nativeElement;
      video.srcObject = this.stream;
      await video.play();
      this.active.set(true);
      this.detector = new BarcodeDetector({ formats: ['qr_code'] });
      this.scan();
    } catch {
      this.error.set('Kameråtkomst nekad. Ange biljettnumret manuellt.');
    }
  }

  private scan(): void {
    const video = this.videoRef().nativeElement;
    if (!this.detector || !video.readyState) {
      this.animationFrame = requestAnimationFrame(() => this.scan());
      return;
    }
    this.detector.detect(video).then(results => {
      for (const r of results) {
        const val = r.rawValue.trim();
        if (this.isGuid(val)) {
          this.stopCamera();
          this.scanned.emit(val);
          return;
        }
      }
      this.animationFrame = requestAnimationFrame(() => this.scan());
    }).catch(() => {
      this.animationFrame = requestAnimationFrame(() => this.scan());
    });
  }

  private stopCamera(): void {
    cancelAnimationFrame(this.animationFrame);
    this.stream?.getTracks().forEach(t => t.stop());
    this.stream = null;
    this.active.set(false);
  }

  private isGuid(val: string): boolean {
    return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(val);
  }

  close(): void {
    this.stopCamera();
    this.closed.emit();
  }
}
