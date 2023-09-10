import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { InitialPage } from './app-routing.module';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  constructor(router: Router) {
    router.navigate([InitialPage]);
  }
}
