import { ModuleWithProviders, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';

import { HttpClientModule } from '@angular/common/http';
import { SolarComponent } from './components/solar';
import { XhrService } from './services/xhr';

export interface ISolarModuleConfig {
  baseUrl?: string;
}

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
    BrowserModule,
    HttpClientModule,
    FormsModule
  ]
})
export class SolarModule {
  public static forRoot(configuration?: ISolarModuleConfig): ModuleWithProviders<SolarModule> {
    return {
      ngModule: SolarModule,
      providers: [XhrService, { provide: 'config', useValue: configuration }]
    };
  }
}
