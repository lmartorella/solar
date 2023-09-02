import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminComponent } from './components/admin';
import { GardenComponent } from './components/garden';
import { SolarComponent } from './components/solar';

export const InitialPage = "/solar";

const routes: Routes = [
  { path: 'solar', component: SolarComponent },
  { path: 'garden', component: GardenComponent },
  { path: 'admin', component: AdminComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
