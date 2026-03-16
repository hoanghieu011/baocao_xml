/// <reference types="@angular/localize" />

import { provideHttpClient } from '@angular/common/http';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';

import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';
import { AppRoutingModule } from './app/app.routes';

bootstrapApplication(AppComponent, appConfig)
  .catch(err => console.error(err));

// bootstrapApplication(AppComponent, {
//   providers: [
//     provideRouter(AppRoutingModule.routes),
//     provideHttpClient(),
//   ]
// })
// .catch(err => console.error(err));