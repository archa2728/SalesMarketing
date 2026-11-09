import { Routes } from '@angular/router';

import { MainLayout } from './layout/main-layout/main-layout';
import { Login } from './features/auth/login/login';
import { SalesmanList } from './features/salesman/salesman-list/salesman-list';
import { MarketingTeamList } from './features/marketing-team/marketing-team-list/marketing-team-list';
import { MarketingTeamView } from './features/marketing-team/marketing-team-view/marketing-team-view';

import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    component: Login
  },
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      {
        path: 'salesman',
        component: SalesmanList,
        canActivate: [roleGuard],
        data: { role: 1 }
      },
      {
        path: 'marketing-team',
        component: MarketingTeamList,
        canActivate: [roleGuard],
        data: { role: 2 }
      },
      {
        path: 'marketing-team/:id',
        component: MarketingTeamView,
        canActivate: [roleGuard],
        data: { role: 2 }
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];