import { Routes } from '@angular/router';
import { TargetListComponent } from './target-list/target-list.component';
import { TargetDetailComponent } from './target-detail/target-detail.component';

export const routes: Routes = [
  { path: '', component: TargetListComponent },
  { path: 'targets/:id', component: TargetDetailComponent },
];
