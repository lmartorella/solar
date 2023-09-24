import { NgModule } from '@angular/core';

import { HttpClientModule } from '@angular/common/http';
import { SolarComponent } from './components/solar';
import { XhrService } from './services/xhr';
import { CommonModule } from '@angular/common';

@NgModule({
  declarations: [
    SolarComponent
  ],
  providers: [
    XhrService
  ],
  exports: [
    SolarComponent
  ],
  imports: [
    CommonModule,
    HttpClientModule,
  ]
})
export class SolarModule { }
