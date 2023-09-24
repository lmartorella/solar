import { Component } from '@angular/core';
import { config } from '@lucky-home/solar-lib';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html'
})
export class AppComponent {
  constructor() {
    config.baseUrl = "http://localhost:8081";
  }
}
