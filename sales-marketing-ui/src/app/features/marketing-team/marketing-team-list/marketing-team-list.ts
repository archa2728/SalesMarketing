
import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  ChangeDetectorRef
} from '@angular/core';

import { CommonModule } from '@angular/common';

import {
  FormsModule,
  ReactiveFormsModule,
  FormBuilder,
  Validators,
  FormArray
} from '@angular/forms';

import {
  Router,
  RouterLink,
  ActivatedRoute
} from '@angular/router';

import {
  GridModule,
  PageService,
  SortService
} from '@syncfusion/ej2-angular-grids';

import { DropDownListModule } from '@syncfusion/ej2-angular-dropdowns';
import { DialogModule } from '@syncfusion/ej2-angular-popups';

import { MarketingTeamApiService } from '../../../core/services/marketing-team-api.service';
import { SalesmanApiService } from '../../../core/services/salesman-api.service';


interface MarketingTeam {
  id: number;
  teamCode: string;
  teamName: string;
  teamLeaderId: number;
  teamLeaderName: string | null;
  description: string | null;
  status: number | string;
  createdDate: string;
  modifiedDate: string | null;
  totalTargetAmount: number;
  members: any[];
}

interface SalesmanLookup {
  id: number;
  salesmanCode: string;
  salesmanName: string;
}

interface SalesmanRemovalRequest {
  id: number;
  salesmanId: number;
  salesmanCode: string;
  salesmanName: string;
  marketingTeamId: number;
  teamCode: string;
  teamName: string;
  requestedBy: number;
  requestedByUsername: string;
  requestedDate: string;
  status: number;
  reviewedBy: number | null;
  reviewedByUsername: string | null;
  reviewedDate: string | null;
  rejectionReason: string | null;
}

@Component({
  selector: 'app-marketing-team-list',
  standalone: true,

  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    GridModule,
    DropDownListModule,
    DialogModule,
    RouterLink
  ],

  providers: [
    PageService,
    SortService
  ],

  templateUrl: './marketing-team-list.html',
  styleUrl: './marketing-team-list.scss'
})

export class MarketingTeamList implements OnInit, OnDestroy {

  // Dependency injection
  private marketingTeamApi = inject(MarketingTeamApiService);
  private salesmanApi = inject(SalesmanApiService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);
  private fb = inject(FormBuilder);


  // Used in HTML number comparisons
  Number = Number;


  // Marketing team data
  teams: MarketingTeam[] = [];
  filteredTeams: MarketingTeam[] = [];

  // Salesman dropdown data
  salesmen: SalesmanLookup[] = [];

  // =========================================
// SALESMAN REMOVAL REQUESTS
// =========================================

removalRequests: SalesmanRemovalRequest[] = [];

isLoadingRemovalRequests = false;

processingRequestId: number | null = null;

// Reject request dialog
isRejectDialogVisible = false;

selectedRemovalRequest: SalesmanRemovalRequest | null = null;

rejectionReason = '';

isRejecting = false;

  // Search and filter
  searchText = '';
  selectedStatus = 'All';

  statusOptions = ['All', 'Active', 'Inactive'];


  // Grid pagination
  pageSettings = {
    pageSize: 10,
    pageSizes: [10, 20, 50]
  };


  // Loading states
  isLoading = false;
  isSaving = false;
  isLoadingSalesmen = false;


  // Create/Edit dialog
  isDialogVisible = false;
  isEditMode = false;
  selectedTeamId: number | null = null;


  // Add member fields
  selectedSalesmanId: number | null = null;
  selectedMemberRole = 2;
  selectedTargetAmount = 0;


  // Toast notification properties
  showToast = false;
  toastMessage = '';

  toastType: 'success' | 'error' | 'warning' = 'success';

  private toastTimer: ReturnType<typeof setTimeout> | undefined;


  // Delete confirmation dialog
  isDeleteDialogVisible = false;
  isDeleting = false;
  teamToDelete: MarketingTeam | null = null;


  // Reactive form
  teamForm = this.fb.group({

    teamCode: [
      '',
      [
        Validators.required,
        Validators.maxLength(20),
        Validators.pattern(/^[a-zA-Z0-9]+$/)
      ]
    ],

    teamName: [
      '',
      [
        Validators.required,
        Validators.maxLength(100)
      ]
    ],

    teamLeaderId: [
      null as number | null,
      [
        Validators.required,
        Validators.min(1)
      ]
    ],

    description: [
      '',
      Validators.maxLength(500)
    ],

    status: [
      1,
      Validators.required
    ],

    members: this.fb.array([])

  });


  // Get members FormArray
  get members(): FormArray {
    return this.teamForm.get('members') as FormArray;
  }


  // Component initialization
  ngOnInit(): void {

    this.loadTeams();
    this.loadSalesmen();
      this.loadPendingRemovalRequests();

    this.route.queryParams.subscribe(params => {

      const editId = Number(params['editId']);

      if (editId) {
        this.editTeam(editId);
      }

    });

  }


  // Clear toast timer when component is destroyed
  ngOnDestroy(): void {

    if (this.toastTimer) {
      clearTimeout(this.toastTimer);
    }

  }


  // =========================================
  // NORMALIZE STATUS
  // =========================================

  normalizeStatus(status: any): number {

    const value = String(status ?? '').trim().toLowerCase();

    if (value === '1' || value === 'active') {
      return 1;
    }

    if (value === '2' || value === 'inactive') {
      return 2;
    }

    return 0;

  }


  // =========================================
  // TOAST NOTIFICATIONS
  // =========================================

  showNotification(
    message: string,
    type: 'success' | 'error' | 'warning' = 'success'
  ): void {

    if (this.toastTimer) {
      clearTimeout(this.toastTimer);
    }

    this.toastMessage = message;
    this.toastType = type;
    this.showToast = true;

    this.cdr.markForCheck();

    this.toastTimer = setTimeout(() => {

      this.dismissNotification();

    }, 3500);

  }


  dismissNotification(): void {

    this.showToast = false;

    if (this.toastTimer) {
      clearTimeout(this.toastTimer);
      this.toastTimer = undefined;
    }

    this.cdr.markForCheck();

  }


  // =========================================
  // LOAD MARKETING TEAMS
  // =========================================

  loadTeams(): void {

    this.isLoading = true;

    this.marketingTeamApi.getAllTeams().subscribe({

      next: (response: MarketingTeam[]) => {

        this.teams = (response ?? []).map(team => ({

          ...team,

          teamCode: String(team.teamCode ?? ''),
          teamName: String(team.teamName ?? ''),

          status: this.normalizeStatus(team.status),

          members: team.members ?? []

        }));

        this.applyFilters();

        this.isLoading = false;

        this.cdr.markForCheck();

      },

      error: (error: any) => {

        console.error('Error loading marketing teams:', error);

        this.isLoading = false;

        this.cdr.markForCheck();

        this.showNotification(
          'Unable to load marketing teams.',
          'error'
        );

      }

    });

  }


  // =========================================
  // LOAD ACTIVE SALESMEN
  // =========================================

  loadSalesmen(): void {

    this.isLoadingSalesmen = true;

    this.salesmanApi.getSalesmanLookup().subscribe({

      next: (response: SalesmanLookup[]) => {

        this.salesmen = response ?? [];

        this.isLoadingSalesmen = false;

        this.cdr.markForCheck();

      },

      error: (error: any) => {

        console.error('Error loading salesman lookup:', error);

        this.isLoadingSalesmen = false;

        this.cdr.markForCheck();

        this.showNotification(
          'Unable to load active salesmen.',
          'error'
        );

      }

    });

  }

  // =========================================
// LOAD PENDING SALESMAN REMOVAL REQUESTS
// =========================================

loadPendingRemovalRequests(): void {

  this.isLoadingRemovalRequests = true;

  this.marketingTeamApi.getPendingRemovalRequests().subscribe({

    next: (response: SalesmanRemovalRequest[]) => {

      this.removalRequests = response ?? [];

      this.isLoadingRemovalRequests = false;

      this.cdr.markForCheck();

    },

    error: (error: any) => {

      console.error('Error loading removal requests:', error);

      this.isLoadingRemovalRequests = false;

      this.showNotification(
        'Unable to load pending salesman removal requests.',
        'error'
      );

      this.cdr.markForCheck();

    }

  });

}

// =========================================
// APPROVE SALESMAN REMOVAL REQUEST
// =========================================

approveRemovalRequest(request: SalesmanRemovalRequest): void {

  if (this.processingRequestId !== null) {
    return;
  }

  const confirmed = window.confirm(
    `Are you sure you want to approve the removal request for ${request.salesmanName}?`
  );

  if (!confirmed) {
    return;
  }

  this.processingRequestId = request.id;

  this.marketingTeamApi.approveRemovalRequest(request.id).subscribe({

    next: (response: any) => {

      this.processingRequestId = null;

      this.showNotification(
        response?.message ||
        'Salesman removal request approved successfully.',
        'success'
      );

      this.loadPendingRemovalRequests();

      this.loadTeams();

      this.cdr.markForCheck();

    },

    error: (error: any) => {

      console.error('Error approving removal request:', error);

      this.processingRequestId = null;

      const message =
        error?.error?.message ||
        error?.error?.title ||
        'Unable to approve the removal request.';

      this.showNotification(message, 'error');

      this.cdr.markForCheck();

    }

  });

}

// =========================================
// OPEN REJECT REQUEST DIALOG
// =========================================

openRejectDialog(request: SalesmanRemovalRequest): void {

  this.selectedRemovalRequest = request;

  this.rejectionReason = '';

  this.isRejectDialogVisible = true;

  this.cdr.markForCheck();

}


// =========================================
// CANCEL REJECT REQUEST
// =========================================

cancelRejectDialog(): void {

  if (this.isRejecting) {
    return;
  }

  this.isRejectDialogVisible = false;

  this.selectedRemovalRequest = null;

  this.rejectionReason = '';

  this.cdr.markForCheck();

}


// =========================================
// SUBMIT REJECT REQUEST
// =========================================

submitRejectRequest(): void {

  if (!this.selectedRemovalRequest || this.isRejecting) {
    return;
  }

  const reason = this.rejectionReason.trim();

  if (!reason) {

    this.showNotification(
      'Please enter a rejection reason.',
      'warning'
    );

    return;
  }

  if (reason.length > 500) {

    this.showNotification(
      'Rejection reason cannot exceed 500 characters.',
      'warning'
    );

    return;
  }

  const requestId = this.selectedRemovalRequest.id;

  this.isRejecting = true;

  this.marketingTeamApi.rejectRemovalRequest(
    requestId,
    reason
  ).subscribe({

    next: (response: any) => {

      this.isRejecting = false;

      this.isRejectDialogVisible = false;

      this.selectedRemovalRequest = null;

      this.rejectionReason = '';

      this.showNotification(
        response?.message ||
        'Salesman removal request rejected successfully.',
        'success'
      );

      this.loadPendingRemovalRequests();

      this.cdr.markForCheck();

    },

    error: (error: any) => {

      console.error('Error rejecting removal request:', error);

      this.isRejecting = false;

      const message =
        error?.error?.message ||
        error?.error?.title ||
        'Unable to reject the removal request.';

      this.showNotification(message, 'error');

      this.cdr.markForCheck();

    }

  });

}

  // =========================================
  // SEARCH AND FILTER
  // =========================================

  applyFilters(): void {

    const search = String(this.searchText ?? '')
      .trim()
      .toLowerCase();

    const statusFilter = String(this.selectedStatus ?? 'All')
      .trim()
      .toLowerCase();

    this.filteredTeams = this.teams.filter(team => {

      const teamCode = String(team.teamCode ?? '').toLowerCase();

      const teamName = String(team.teamName ?? '').toLowerCase();

      // Search by team code or team name
      const matchesSearch =
        teamCode.includes(search) ||
        teamName.includes(search);


      // Convert team status into a consistent numeric value
      const status = this.normalizeStatus(team.status);

      // Check selected status
      const matchesStatus =
        statusFilter === 'all' ||
        (statusFilter === 'active' && status === 1) ||
        (statusFilter === 'inactive' && status === 2);

      return matchesSearch && matchesStatus;

    });

    this.cdr.markForCheck();

  }


  // Search input change
  onSearchChange(): void {

    this.applyFilters();

  }


  // Status dropdown change
  onStatusChange(event?: any): void {

    // Read the selected value from Syncfusion dropdown event
    if (event && event.value !== undefined) {

      this.selectedStatus = String(event.value);

    }

    this.applyFilters();

  }


  // Reset search and status filters
  clearFilters(): void {

    this.searchText = '';
    this.selectedStatus = 'All';

    this.applyFilters();

  }


  // =========================================
  // VIEW TEAM
  // =========================================

  viewTeam(id: number): void {

    this.router.navigate(['/marketing-team', id]);

  }


  // =========================================
  // STATUS TEXT
  // =========================================

  getStatusText(status: number | string): string {

    return this.normalizeStatus(status) === 1
      ? 'Active'
      : 'Inactive';

  }


  // =========================================
  // OPEN CREATE DIALOG
  // =========================================

  openCreateDialog(): void {

    if (this.isLoadingSalesmen) {

      this.showNotification(
        'Please wait while active salesmen are loading.',
        'warning'
      );

      return;
    }

    if (this.salesmen.length === 0) {

      this.showNotification(
        'No active salesmen are available.',
        'warning'
      );

      return;
    }

    this.isEditMode = false;

    this.selectedTeamId = null;

    this.members.clear();

    this.teamForm.reset({

      teamCode: '',
      teamName: '',
      teamLeaderId: null,
      description: '',
      status: 1

    });

    this.teamForm.get('teamCode')?.enable();

    this.selectedSalesmanId = null;
    this.selectedMemberRole = 2;
    this.selectedTargetAmount = 0;

    this.isDialogVisible = true;

    this.cdr.markForCheck();

  }


  // =========================================
  // OPEN EDIT DIALOG
  // =========================================

  editTeam(id: number): void {

    this.isEditMode = true;

    this.selectedTeamId = id;

    this.members.clear();

    this.marketingTeamApi.getTeamById(id).subscribe({

      next: (team: MarketingTeam) => {

        this.teamForm.patchValue({

          teamCode: team.teamCode,
          teamName: team.teamName,
          teamLeaderId: Number(team.teamLeaderId),
          description: team.description ?? '',
          status: this.normalizeStatus(team.status)

        });

        this.teamForm.get('teamCode')?.disable();


        // Load existing members
        (team.members ?? []).forEach(member => {

          this.members.push(

            this.fb.group({

              salesmanId: [
                Number(member.salesmanId),
                Validators.required
              ],

              salesmanCode: [
                member.salesmanCode
              ],

              salesmanName: [
                member.salesmanName
              ],

              role: [
                Number(member.role),
                [
                  Validators.required,
                  Validators.min(1),
                  Validators.max(4)
                ]
              ],

              targetAmount: [
                Number(member.targetAmount),
                [
                  Validators.required,
                  Validators.min(0)
                ]
              ],

              status: [
                Number(member.status),
                Validators.required
              ]

            })

          );

        });


        this.isDialogVisible = true;


        // Remove editId from URL
        this.router.navigate([], {

          relativeTo: this.route,
          queryParams: {},
          replaceUrl: true

        });

        this.cdr.markForCheck();

      },

      error: (error: any) => {

        console.error('Error loading team details:', error);

        this.showNotification(
          'Unable to load marketing team details.',
          'error'
        );

      }

    });

  }


  // =========================================
  // ADD MEMBER
  // =========================================

  addMember(): void {

    if (!this.selectedSalesmanId) {

      this.showNotification(
        'Please select a salesman.',
        'warning'
      );

      return;
    }


    const salesman = this.salesmen.find(

      s => Number(s.id) === Number(this.selectedSalesmanId)

    );


    if (!salesman) {

      this.showNotification(
        'Selected salesman was not found.',
        'error'
      );

      return;
    }


    // Prevent duplicate members
    const alreadyAdded = this.members.controls.some(

      member =>
        Number(member.get('salesmanId')?.value) ===
        Number(salesman.id)

    );


    if (alreadyAdded) {

      this.showNotification(
        'This salesman has already been added.',
        'warning'
      );

      return;
    }


    const isTeamLeader =
      Number(this.teamForm.get('teamLeaderId')?.value) ===
      Number(salesman.id);


    const role = isTeamLeader ? 1 : this.selectedMemberRole;


    if (!isTeamLeader && role === 1) {

      this.showNotification(
        'Only the selected Team Leader can have the Team Leader role.',
        'warning'
      );

      return;
    }


    this.members.push(

      this.fb.group({

        salesmanId: [
          salesman.id,
          Validators.required
        ],

        salesmanCode: [
          salesman.salesmanCode
        ],

        salesmanName: [
          salesman.salesmanName
        ],

        role: [
          role,
          [
            Validators.required,
            Validators.min(1),
            Validators.max(4)
          ]
        ],

        targetAmount: [
          Number(this.selectedTargetAmount),
          [
            Validators.required,
            Validators.min(0)
          ]
        ],

        status: [
          1,
          Validators.required
        ]

      })

    );


    this.selectedSalesmanId = null;
    this.selectedTargetAmount = 0;

    this.cdr.markForCheck();

  }


  // =========================================
  // TEAM LEADER SELECTION
  // =========================================

  onTeamLeaderSelected(): void {

    if (this.isEditMode) {
      return;
    }

    const teamLeaderId = Number(
      this.teamForm.get('teamLeaderId')?.value
    );

    if (!teamLeaderId) {
      return;
    }

    const existingMember = this.members.controls.find(

      member =>
        Number(member.get('salesmanId')?.value) === teamLeaderId

    );


    if (existingMember) {

      existingMember.patchValue({
        role: 1
      });

      return;
    }


    const selectedLeader = this.salesmen.find(

      salesman =>
        Number(salesman.id) === teamLeaderId

    );


    if (!selectedLeader) {
      return;
    }


    // Automatically add selected Team Leader
    this.members.push(

      this.fb.group({

        salesmanId: [
          selectedLeader.id
        ],

        salesmanCode: [
          selectedLeader.salesmanCode
        ],

        salesmanName: [
          selectedLeader.salesmanName
        ],

        role: [
          1
        ],

        targetAmount: [
          0
        ],

        status: [
          1
        ]

      })

    );

    this.cdr.markForCheck();

  }


  // =========================================
  // REMOVE MEMBER
  // =========================================

  removeMember(index: number): void {

    const member = this.members.at(index);

    const salesmanId = Number(
      member.get('salesmanId')?.value
    );

    const teamLeaderId = Number(
      this.teamForm.get('teamLeaderId')?.value
    );


    if (salesmanId === teamLeaderId) {

      this.showNotification(
        'The Team Leader cannot be removed from the team.',
        'warning'
      );

      return;
    }


    this.members.removeAt(index);

    this.cdr.markForCheck();

  }


  // =========================================
  // SAVE CREATE OR EDIT
  // =========================================

  onSaveTeam(): void {

    if (this.teamForm.invalid) {

      this.teamForm.markAllAsTouched();

      this.showNotification(
        'Please fill in all required fields correctly.',
        'warning'
      );

      return;
    }


    const formValue = this.teamForm.getRawValue();

    const teamLeaderId = Number(formValue.teamLeaderId);


    if (!formValue.members || formValue.members.length === 0) {

      this.showNotification(
        'Please add at least one salesman to the team.',
        'warning'
      );

      return;
    }


    const leaderMember = formValue.members.find(

      (member: any) =>
        Number(member.salesmanId) === teamLeaderId

    ) as any;


    if (!leaderMember) {

      this.showNotification(
        'Please add the selected Team Leader to the members list.',
        'warning'
      );

      return;
    }


    const otherLeader = formValue.members.find(

      (member: any) =>

        Number(member.role) === 1 &&
        Number(member.salesmanId) !== teamLeaderId

    );


    if (otherLeader) {

      this.showNotification(
        'Only the selected Team Leader can have the Team Leader role.',
        'warning'
      );

      return;
    }


    // Ensure selected Team Leader has Role = 1
    leaderMember.role = 1;


    // Prepare member payload
    const membersPayload = formValue.members.map(

      (member: any) => ({

        salesmanId: Number(member.salesmanId),
        role: Number(member.role),
        targetAmount: Number(member.targetAmount),
        status: Number(member.status)

      })

    );


    // Prepare team payload
    const payload = {

      teamCode: formValue.teamCode ?? '',
      teamName: formValue.teamName ?? '',
      teamLeaderId,
      description: formValue.description ?? '',
      status: Number(formValue.status),
      members: membersPayload

    };


    this.isSaving = true;


    // UPDATE EXISTING TEAM
    if (this.isEditMode && this.selectedTeamId) {

      this.marketingTeamApi.updateTeam(

        this.selectedTeamId,

        {

          teamName: payload.teamName,
          teamLeaderId: payload.teamLeaderId,
          description: payload.description,
          status: payload.status,
          members: payload.members

        }

      ).subscribe({

        next: () => {

          this.isSaving = false;

          this.onCancelEdit();

          this.loadTeams();

          this.showNotification(
            'Marketing team updated successfully.',
            'success'
          );

          this.cdr.markForCheck();

        },

        error: (error: any) => {

          this.handleSaveError(error);

        }

      });

    } else {

      // CREATE NEW TEAM
      this.marketingTeamApi.createTeam(payload).subscribe({

        next: () => {

          this.isSaving = false;

          this.onCancelEdit();

          this.loadTeams();

          this.showNotification(
            'Marketing team created successfully.',
            'success'
          );

          this.cdr.markForCheck();

        },

        error: (error: any) => {

          this.handleSaveError(error);

        }

      });

    }

  }


  // =========================================
  // SAVE ERROR HANDLER
  // =========================================

  private handleSaveError(error: any): void {

    console.error('Error saving marketing team:', error);

    this.isSaving = false;

    this.cdr.markForCheck();

    const message =
      error?.error?.message ||
      error?.error?.title ||
      'Unable to save marketing team. Please try again.';

    this.showNotification(message, 'error');

  }


  // =========================================
  // CANCEL CREATE / EDIT
  // =========================================

  onCancelEdit(): void {

    this.isDialogVisible = false;

    this.selectedTeamId = null;

    this.isEditMode = false;

    this.isSaving = false;

    this.members.clear();


    this.teamForm.reset({

      teamCode: '',
      teamName: '',
      teamLeaderId: null,
      description: '',
      status: 1

    });


    this.teamForm.get('teamCode')?.enable();

    this.selectedSalesmanId = null;

    this.selectedMemberRole = 2;

    this.selectedTargetAmount = 0;

    this.cdr.markForCheck();

  }


  // =========================================
  // ROLE TEXT
  // =========================================

  getRoleText(role: number): string {

    switch (Number(role)) {

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


  // =========================================
  // DELETE CONFIRMATION
  // =========================================

  openDeleteConfirmation(team: MarketingTeam): void {

    this.teamToDelete = team;

    this.isDeleteDialogVisible = true;

    this.cdr.markForCheck();

  }


  cancelDelete(): void {

    if (this.isDeleting) {
      return;
    }

    this.isDeleteDialogVisible = false;

    this.teamToDelete = null;

    this.cdr.markForCheck();

  }


  confirmDeleteTeam(): void {

    if (!this.teamToDelete || this.isDeleting) {
      return;
    }

    const teamId = this.teamToDelete.id;

    this.isDeleting = true;

    this.marketingTeamApi.deleteTeam(teamId).subscribe({

      next: () => {

        this.isDeleting = false;

        this.isDeleteDialogVisible = false;

        this.teamToDelete = null;

        this.loadTeams();

        this.showNotification(
          'Marketing team deleted successfully.',
          'success'
        );

        this.cdr.markForCheck();

      },

      error: (error: any) => {

        console.error('Error deleting marketing team:', error);

        this.isDeleting = false;

        this.isDeleteDialogVisible = false;

        this.teamToDelete = null;

        const message =
          error?.error?.message ||
          error?.error?.title ||
          'Unable to delete marketing team.';

        this.showNotification(message, 'error');

        this.cdr.markForCheck();

      }

    });

  }
}
