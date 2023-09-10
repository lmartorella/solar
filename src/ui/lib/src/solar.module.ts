import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';

import { HttpClientModule } from '@angular/common/http';
import { SolarComponent } from './components/solar';

@NgModule({
  declarations: [
    SolarComponent
  ],
  exports: [
    SolarComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    FormsModule
  ]
})
export class SolarModule { }
