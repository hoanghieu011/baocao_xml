import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, NavigationEnd, RouterModule } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { filter, map } from 'rxjs/operators';

@Component({
  selector: 'app-breadcrumb-wrapper',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslateModule],
  template: `
    <nav aria-label="breadcrumb">
      <ul class="breadcrumb">
        <li *ngFor="let breadcrumb of breadcrumbs; let last = last" class="breadcrumb-item"
            [class.active]="last">
          <a *ngIf="!last" [routerLink]="breadcrumb.url">{{ breadcrumb.label }}</a>
          <span *ngIf="last">{{ breadcrumb.label }}</span>
        </li>
      </ul>
    </nav>
  `,
  styles: [`
    .breadcrumb {
      display: flex;
      list-style: none;
      padding: 0;
      margin: 0;
      background: none;
    }
    .breadcrumb-item {
      margin-right: 5px;
    }
    .breadcrumb-item::after {
      content: '';
      margin-left: 0px;
    }
    .breadcrumb-item:last-child::after {
      content: '';
    }
    .breadcrumb-item.active {
      font-weight: bold;
    }
  `]
})
export class BreadcrumbWrapperComponent {
  breadcrumbs: { label: string; url?: string }[] = [];

  constructor(private router: Router, private activatedRoute: ActivatedRoute, private translate: TranslateService) {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      map(() => this.buildBreadcrumbs(this.activatedRoute.root))
    ).subscribe(breadcrumbs => {
      this.breadcrumbs = breadcrumbs;
    });

    this.translate.onLangChange.subscribe(() => {
      this.breadcrumbs = this.buildBreadcrumbs(this.activatedRoute.root);
    });
  }

  private buildBreadcrumbs(route: ActivatedRoute, url: string = '', breadcrumbs: any[] = []): any[] {
    if (route.routeConfig && route.routeConfig.data) {
      let titleKey = route.routeConfig.data['title'];
      this.translate.get(titleKey).subscribe(translatedTitle => {
        breadcrumbs.push({ label: translatedTitle, url: url || undefined });
      });
    }

    if (route.firstChild) {
      return this.buildBreadcrumbs(route.firstChild, url, breadcrumbs);
    }

    return breadcrumbs;
  }
}
