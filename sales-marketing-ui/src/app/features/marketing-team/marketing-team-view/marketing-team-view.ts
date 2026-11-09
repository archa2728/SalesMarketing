
import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MarketingTeamApiService } from '../../../core/services/marketing-team-api.service';

interface MarketingTeamMember {
  id: number;
  salesmanId: number;
  salesmanCode: string;
  salesmanName: string;
  role: number;
  targetAmount: number;
  status: number;
}

interface MarketingTeam {
  id: number;
  teamCode: string;
  teamName: string;
  teamLeaderId: number;
  teamLeaderName: string | null;
  description: string | null;
  status: number;
  createdDate: string;
  modifiedDate: string | null;
  totalTargetAmount: number;
  members: MarketingTeamMember[];
}

@Component({
  selector: 'app-marketing-team-view',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './marketing-team-view.html',
  styleUrl: './marketing-team-view.scss'
})
export class MarketingTeamView implements OnInit {

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private marketingTeamApi = inject(MarketingTeamApiService);
  private cdr = inject(ChangeDetectorRef);

  team: MarketingTeam | null = null;

  isLoading = false;
  errorMessage = '';

  private teamId = 0;

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    if (!Number.isInteger(id) || id <= 0) {
      this.errorMessage = 'Invalid marketing team ID.';
      return;
    }

    this.teamId = id;
    this.loadTeam(id);
  }

  // Load Marketing Team Details
  loadTeam(id: number): void {

    this.isLoading = true;
    this.errorMessage = '';

    this.marketingTeamApi.getTeamById(id).subscribe({

      next: (response: MarketingTeam) => {

        if (!response) {
          this.team = null;
          this.errorMessage = 'Marketing team not found.';
          this.isLoading = false;
          this.cdr.markForCheck();
          return;
        }

        this.team = {
          ...response,
          members: response.members ?? []
        };

        this.isLoading = false;
        this.cdr.markForCheck();
      },

      error: (error: any) => {

        console.error('Error loading marketing team:', error);

        this.team = null;

        this.errorMessage =
          error.status === 404
            ? 'Marketing team not found.'
            : 'Unable to load marketing team details. Please try again.';

        this.isLoading = false;
        this.cdr.markForCheck();
      }

    });
  }

  // Retry Loading
  retryLoading(): void {

    if (this.teamId > 0) {
      this.loadTeam(this.teamId);
    }
  }

  // Get Role Name
  getRoleText(role: number): string {

    switch (role) {

      case 1:
        return 'Team Leader';

      case 2:
        return 'Senior Sales Executive';

      case 3:
        return 'Sales Executive';

      case 4:
        return 'Trainee';

      default:
        return 'Unknown';
    }
  }

  // Get Status Name
  getStatusText(status: number): string {

    return status === 1 ? 'Active' : 'Inactive';
  }

  // Calculate Total Target Amount
  get totalTargetAmount(): number {

    return this.team?.members.reduce(
      (total, member) => total + Number(member.targetAmount || 0),
      0
    ) ?? 0;
  }

  // Calculate Active Members
  get activeMemberCount(): number {

    return this.team?.members.filter(
      member => Number(member.status) === 1
    ).length ?? 0;
  }

  // Navigate Back to Marketing Team List
  goBack(): void {

    this.router.navigate(['/marketing-team']);
  }

  // Navigate to List and Open Edit Dialog
  editTeam(): void {

    if (this.team) {

      this.router.navigate(['/marketing-team'], {
        queryParams: { editId: this.team.id }
      });

    }
  }

}
