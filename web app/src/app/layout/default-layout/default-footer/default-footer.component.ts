import { Component } from '@angular/core';
import { FooterComponent } from '@coreui/angular';
import { TranslateService, TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-default-footer',
    templateUrl: './default-footer.component.html',
    styleUrls: ['./default-footer.component.scss'],
    imports: [TranslateModule],
    standalone: true,
})
export class DefaultFooterComponent extends FooterComponent {
  constructor(private translate: TranslateService) {
    super();
    this.translate.setDefaultLang('vi');
    const savedLang = localStorage.getItem('language') || 'vi';
    this.translate.use(savedLang);
  }
}
