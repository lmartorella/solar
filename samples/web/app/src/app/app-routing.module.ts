import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AdminComponent } from './components/admin';
import { GardenComponent } from './components/garden';
import { SolarComponent } from './components/solar';
import { LoginComponent } from './components/login';

export const InitialPage = "/solar";

const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'solar', component: SolarComponent },
  { path: 'garden', component: GardenComponent },
  { path: 'admin', component: AdminComponent },
  { path: '**', component: SolarComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { useHash: true })],
  exports: [RouterModule]
})
export class AppRoutingModule { }
