import { Injectable } from '@angular/core';
import { Resolve, ActivatedRouteSnapshot } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class RouteTitleResolver implements Resolve<string> {
  constructor(private translate: TranslateService) {}

  resolve(route: ActivatedRouteSnapshot): Observable<string> {
    const titleKey = route.data['title'];
    if (!titleKey) {
      return of('');
    }
    return this.translate.get(titleKey).pipe(map(translatedTitle => translatedTitle));
  }
}
