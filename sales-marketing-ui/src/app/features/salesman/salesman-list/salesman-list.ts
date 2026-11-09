
import {
  Component,
  inject,
  OnInit,
  OnDestroy,
  ChangeDetectorRef,
  ViewChild
} from '@angular/core';

import {
  CommonModule
} from '@angular/common';

import {
  ReactiveFormsModule,
  FormBuilder,
  Validators,
  AbstractControl,
  ValidationErrors
} from '@angular/forms';

import {
  GridModule,
  GridComponent,
  PageService,
  SortService,
  DataStateChangeEventArgs
} from '@syncfusion/ej2-angular-grids';

import { DropDownListModule } from '@syncfusion/ej2-angular-dropdowns';
import { DialogModule } from '@syncfusion/ej2-angular-popups';
import { DatePickerModule } from '@syncfusion/ej2-angular-calendars';

import { SalesmanApiService } from '../../../core/services/salesman-api.service';

interface Salesman {
  id: number;
  salesmanCode: string;
  salesmanName: string;
  mobileNo: string;
  email: string;
  joiningDate: string;
  status: string;
}

@Component({
  selector: 'app-salesman-list',
  standalone: true,

  imports: [
    CommonModule,
    GridModule,
    DropDownListModule,
    DialogModule,
    DatePickerModule,
    ReactiveFormsModule
  ],

  providers: [
    PageService,
    SortService
  ],

  templateUrl: './salesman-list.html',
  styleUrl: './salesman-list.scss'
})

export class SalesmanList implements OnInit, OnDestroy {

  // ==========================================
  // TOAST NOTIFICATION PROPERTIES
  // ==========================================

  showToast = false;
  toastMessage = '';

  toastType: 'success' | 'error' | 'warning' = 'success';

  private toastTimeout?: ReturnType<typeof setTimeout>;

  // Display toast notification
  showNotification(
    message: string,
    type: 'success' | 'error' | 'warning' = 'success'
  ): void {

    this.toastMessage = message;
    this.toastType = type;
    this.showToast = true;

    if (this.toastTimeout) {
      clearTimeout(this.toastTimeout);
    }

    this.toastTimeout = setTimeout(() => {

      this.showToast = false;
      this.toastTimeout = undefined;

    }, 3000);

  }

  // Close notification manually
  dismissNotification(): void {

    this.showToast = false;

    if (this.toastTimeout) {
      clearTimeout(this.toastTimeout);
      this.toastTimeout = undefined;
    }

  }

  // ==========================================
  // DEPENDENCY INJECTION
  // ==========================================

  private salesmanApi = inject(SalesmanApiService);
  private fb = inject(FormBuilder);
  private cdr = inject(ChangeDetectorRef);

  // ==========================================
  // SYNCFUSION GRID REFERENCE
  // ==========================================

  @ViewChild('salesmanGrid')
  salesmanGrid!: GridComponent;

  // Today's date
  today = new Date();

  // ==========================================
  // SALESMAN DATA
  // ==========================================

  salesmen: Salesman[] = [];

  // Server-side pagination properties
  totalCount = 0;
  currentPage = 1;
  pageSize = 10;

  // Data source for Syncfusion Grid
  gridData: {
    result: Salesman[];
    count: number;
  } = {
    result: [],
    count: 0
  };

  // ==========================================
  // SEARCH AND FILTER PROPERTIES
  // ==========================================

  searchText = '';
  selectedStatus = 'All';

  statusOptions = ['All', 'Active', 'Inactive'];
  formStatusOptions = ['Active', 'Inactive'];

  // Grid pagination settings
  pageSettings = {
    pageSize: 10,
    pageSizes: [10, 20, 50]
  };

  // ==========================================
  // CREATE / EDIT POPUP PROPERTIES
  // ==========================================

  isDialogVisible = false;
  isEditMode = false;
  selectedSalesmanId: number | null = null;

  // ==========================================
  // DELETE POPUP PROPERTIES
  // ==========================================

  isDeleteDialogVisible = false;
  selectedDeleteSalesman: Salesman | null = null;
  isAssignedToActiveTeam = false;

// ==========================================
// STRICT EMAIL VALIDATOR
// ==========================================

private strictEmailValidator(
  control: AbstractControl
): ValidationErrors | null {

  const email = control.value;

  if (!email) {
    return null;
  }

  const emailPattern =
    /^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$/;

  return emailPattern.test(email)
    ? null
    : { invalidEmail: true };
}

  // ==========================================
  // FUTURE JOINING DATE VALIDATOR
  // ==========================================

  private futureDateValidator(
    control: AbstractControl
  ): ValidationErrors | null {

    if (!control.value) {
      return null;
    }

    const selectedDate = new Date(control.value);

    selectedDate.setHours(0, 0, 0, 0);

    const today = new Date();

    today.setHours(0, 0, 0, 0);

    return selectedDate > today
      ? { futureDate: true }
      : null;

  }

  // ==========================================
  // REACTIVE FORM WITH VALIDATIONS
  // ==========================================

  salesmanForm = this.fb.group({

    salesmanCode: [
      '',
      [
        Validators.required,
        Validators.maxLength(20),
        Validators.pattern(/^[A-Z0-9]+$/)
      ]
    ],

    salesmanName: [
      '',
      [
        Validators.required,
        Validators.maxLength(100),
        Validators.pattern(/^[\p{L}][\p{L}\s.'-]*$/u)
      ]
    ],

    mobileNo: [
      '',
      [
        Validators.pattern(/^\d{7,15}$/)
      ]
    ],

    email: [
  '',
  [
    Validators.maxLength(100),
    this.strictEmailValidator.bind(this)
  ]
],

    joiningDate: [
      null as Date | null,
      [
        Validators.required,
        this.futureDateValidator.bind(this)
      ]
    ],

    status: [
      'Active',
      [
        Validators.required,
        Validators.pattern(/^(Active|Inactive)$/)
      ]
    ]

  });

  // ==========================================
  // COMPONENT INITIALIZATION
  // ==========================================

  ngOnInit(): void {

    this.loadSalesmen();

  }

  // ==========================================
  // LOAD SALESMEN FROM API
  // ==========================================

  loadSalesmen(pageNumber: number = 1): void {

    this.currentPage = pageNumber;

    const status =
      this.selectedStatus === 'Active' ? 1 :
      this.selectedStatus === 'Inactive' ? 2 :
      null;

    this.salesmanApi.getAllSalesmen(
      this.searchText,
      status,
      this.currentPage,
      this.pageSize
    ).subscribe({

      next: (response: any) => {

        this.salesmen = response.items.map((s: any) => ({

          id: s.id,
          salesmanCode: s.salesmanCode,
          salesmanName: s.salesmanName,
          mobileNo: s.mobileNo ?? '',
          email: s.email ?? '',
          joiningDate: this.formatApiDate(s.joiningDate),
          status: Number(s.status) === 1 ? 'Active' : 'Inactive'

        }));

        this.totalCount = response.totalCount;

        // Update Syncfusion Grid data
        this.gridData = {
          result: this.salesmen,
          count: this.totalCount
        };

        this.cdr.markForCheck();

      },

      error: (error) => {

        console.error('Error loading salesmen:', error);

        this.showNotification(
          'Unable to load salesmen. Please check the API connection.',
          'error'
        );

      }

    });

  }

  // ==========================================
  // DATE CONVERSION METHODS
  // ==========================================

  // Convert API date into dd-MMM-yyyy format
  formatApiDate(date: string): string {

    if (!date) {
      return '';
    }

    const parsedDate = new Date(date + 'T00:00:00');

    const day = String(parsedDate.getDate()).padStart(2, '0');

    const month = parsedDate.toLocaleDateString('en-US', {
      month: 'short'
    });

    const year = parsedDate.getFullYear();

    return `${day}-${month}-${year}`;

  }

  // Convert Date into yyyy-MM-dd format for API
  formatDateForApi(date: Date): string {

    const year = date.getFullYear();

    const month = String(date.getMonth() + 1).padStart(2, '0');

    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;

  }

  // Convert dd-MMM-yyyy into Date object
  parseDisplayDate(date: string): Date {

    const parts = date.split('-');

    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];

    const day = Number(parts[0]);

    const month = months.indexOf(parts[1]);

    const year = Number(parts[2]);

    return new Date(year, month, day);

  }

  // Format Date as dd-MMM-yyyy
  formatDate(date: Date | null): string {

    if (!date) {
      return '';
    }

    const day = String(date.getDate()).padStart(2, '0');

    const month = date.toLocaleDateString('en-US', {
      month: 'short'
    });

    const year = date.getFullYear();

    return `${day}-${month}-${year}`;

  }

  // ==========================================
  // SEARCH HANDLER
  // ==========================================

  onSearch(event: Event): void {

    this.searchText = (
      event.target as HTMLInputElement
    ).value;

    this.resetGridToFirstPage();

  }

  // ==========================================
  // STATUS FILTER HANDLER
  // ==========================================

  onStatusChange(event: { value: string }): void {

    this.selectedStatus = event.value;

    this.resetGridToFirstPage();

  }

  // ==========================================
  // RESET GRID TO FIRST PAGE
  // ==========================================

  private resetGridToFirstPage(): void {

    if (
      this.salesmanGrid &&
      this.salesmanGrid.pageSettings.currentPage !== 1
    ) {

      // goToPage triggers the Grid pagination event
      this.salesmanGrid.goToPage(1);

    } else {

      // Already on first page
      this.loadSalesmen(1);

    }

  }

  // ==========================================
  // HANDLE GRID PAGINATION
  // ==========================================

  onGridDataStateChange(
    state: DataStateChangeEventArgs
  ): void {

    const take = state.take ?? this.pageSize;

    const skip = state.skip ?? 0;

    if (take <= 0) {
      return;
    }

    const pageNumber = Math.floor(skip / take) + 1;

    // Avoid unnecessary API calls
    if (
      pageNumber === this.currentPage &&
      take === this.pageSize
    ) {
      return;
    }

    this.pageSize = take;

    this.loadSalesmen(pageNumber);

  }

  // ==========================================
  // OPEN NEW SALESMAN POPUP
  // ==========================================

  onNewSalesman(): void {

    this.isEditMode = false;

    this.selectedSalesmanId = null;

    this.salesmanForm.reset({

      salesmanCode: '',
      salesmanName: '',
      mobileNo: '',
      email: '',
      joiningDate: null,
      status: 'Active'

    });

    this.salesmanForm.get('salesmanCode')?.enable();

    this.isDialogVisible = true;

  }

  // ==========================================
  // OPEN EDIT SALESMAN POPUP
  // ==========================================

  onEditSalesman(id: number): void {

    const salesman = this.salesmen.find(
      s => s.id === id
    );

    if (!salesman) {
      return;
    }

    this.isEditMode = true;

    this.selectedSalesmanId = id;

    this.salesmanForm.patchValue({

      salesmanCode: salesman.salesmanCode,
      salesmanName: salesman.salesmanName,
      mobileNo: salesman.mobileNo,
      email: salesman.email,
      joiningDate: this.parseDisplayDate(salesman.joiningDate),
      status: salesman.status

    });

    // Salesman code cannot be changed during editing
    this.salesmanForm.get('salesmanCode')?.disable();

    this.isDialogVisible = true;

  }

  // ==========================================
  // CHECK FUTURE JOINING DATE
  // ==========================================

  isFutureDate(date: Date): boolean {

    const today = new Date();

    today.setHours(0, 0, 0, 0);

    const selectedDate = new Date(date);

    selectedDate.setHours(0, 0, 0, 0);

    return selectedDate > today;

  }

  // ==========================================
  // NORMAL SAVE BUTTON
  // ==========================================

  onSaveSalesman(): void {

    this.saveSalesman(false);

  }

  // ==========================================
  // SAVE & NEW BUTTON
  // ==========================================

  onSaveAndNew(): void {

    this.saveSalesman(true);

  }

  // ==========================================
  // SAVE ADD / EDIT SALESMAN
  // ==========================================

  private saveSalesman(keepDialogOpen: boolean): void {

    // Check form validations
    if (this.salesmanForm.invalid) {

      this.salesmanForm.markAllAsTouched();

      return;
    }

    const formValue = this.salesmanForm.getRawValue();

    if (!formValue.joiningDate) {
      return;
    }

    // Prevent future joining dates
    if (this.isFutureDate(formValue.joiningDate)) {

      this.showNotification(
        'Joining date cannot be in the future.',
        'warning'
      );

      return;

    }

    // ======================================
    // UPDATE EXISTING SALESMAN
    // ======================================

    if (this.isEditMode) {

      const updateData = {

        salesmanName: formValue.salesmanName ?? '',
        mobileNo: formValue.mobileNo ?? '',
        email: formValue.email ?? '',
        joiningDate: this.formatDateForApi(formValue.joiningDate),
        status: formValue.status === 'Active' ? 1 : 2

      };

      this.salesmanApi.updateSalesman(
        this.selectedSalesmanId!,
        updateData
      ).subscribe({

        next: () => {

          this.showNotification(
            'Salesman updated successfully.',
            'success'
          );

          this.onCancel();

          this.loadSalesmen();

        },

        error: (error) => {

          console.error('Error updating salesman:', error);

          this.showNotification(
            error.error?.message ||
            'Unable to update salesman. Please try again.',
            'error'
          );

        }

      });

      return;

    }

    // ======================================
    // CREATE NEW SALESMAN
    // ======================================

    const createData = {

      salesmanCode: formValue.salesmanCode ?? '',
      salesmanName: formValue.salesmanName ?? '',
      mobileNo: formValue.mobileNo ?? '',
      email: formValue.email ?? '',
      joiningDate: this.formatDateForApi(formValue.joiningDate),
      status: formValue.status === 'Active' ? 1 : 2

    };

    this.salesmanApi.createSalesman(createData).subscribe({

      next: () => {

        this.showNotification(
          'Salesman created successfully.',
          'success'
        );

        // Refresh the salesman grid
        this.loadSalesmen();

        if (keepDialogOpen) {

          // Reset form for entering another salesman
          this.salesmanForm.reset({

            salesmanCode: '',
            salesmanName: '',
            mobileNo: '',
            email: '',
            joiningDate: null,
            status: 'Active'

          });

          this.salesmanForm.get('salesmanCode')?.enable();

        } else {

          // Close popup after normal Save
          this.onCancel();

        }

      },

      error: (error) => {

        console.error('Error creating salesman:', error);

        if (error.status === 409) {

          this.showNotification(
            'Salesman code already exists.',
            'error'
          );

        } else {

          this.showNotification(
            error.error?.message ||
            'Unable to create salesman.',
            'error'
          );

        }

      }

    });

  }

  // ==========================================
  // CLOSE ADD / EDIT POPUP
  // ==========================================

  onCancel(): void {

    this.isDialogVisible = false;

    this.salesmanForm.reset({

      salesmanCode: '',
      salesmanName: '',
      mobileNo: '',
      email: '',
      joiningDate: null,
      status: 'Active'

    });

    this.salesmanForm.get('salesmanCode')?.enable();

    this.selectedSalesmanId = null;

  }

  // ==========================================
  // OPEN DELETE CONFIRMATION POPUP
  // ==========================================

  // onDeleteSalesman(id: number): void {

  //   const salesman = this.salesmen.find(
  //     s => s.id === id
  //   );

  //   if (!salesman) {
  //     return;
  //   }

  //   this.selectedDeleteSalesman = salesman;

  //   this.isDeleteDialogVisible = true;

  // }

  onDeleteSalesman(id: number): void {

  const salesman = this.salesmen.find(
    s => s.id === id
  );

  if (!salesman) {
    return;
  }

  this.selectedDeleteSalesman = salesman;

  // Reset the assignment state whenever a new popup opens
  this.isAssignedToActiveTeam = false;

  this.isDeleteDialogVisible = true;
}

  // ==========================================
  // CANCEL DELETE
  // ==========================================

  onCancelDelete(): void {

    this.isDeleteDialogVisible = false;
    this.selectedDeleteSalesman = null;
    this.isAssignedToActiveTeam = false;

  }

  onConfirmDelete(): void {

  if (!this.selectedDeleteSalesman) {
    return;
  }

  const id = this.selectedDeleteSalesman.id;

  // If the salesman is assigned to an active team,
  // submit a removal request instead of deleting.
  if (this.isAssignedToActiveTeam) {
    this.submitRemovalRequest(id);
    return;
  }

  // Otherwise, perform the normal delete operation.
  this.salesmanApi.deleteSalesman(id).subscribe({

    next: (response) => {

      this.showNotification(
        response?.message || 'Salesman deleted successfully.',
        'success'
      );

      this.onCancelDelete();

      this.loadSalesmen();

    },

    error: (error) => {

      console.error('Error deleting salesman:', error);

      if (
        error.status === 409 &&
        error.error?.message?.toLowerCase()
          .includes('assigned to a marketing team')
      ) {

        // Keep the same popup open and change its content.
        this.isAssignedToActiveTeam = true;

      } else {

        this.showNotification(
          error.error?.message ||
          'Unable to delete salesman. Please try again.',
          'error'
        );

      }

    }

  });

}

// Submit removal request to Marketing Team Admin
submitRemovalRequest(id: number): void {

  this.salesmanApi.requestSalesmanRemoval(id).subscribe({

    next: (response) => {

      this.showNotification(
        response?.message ||
        'Removal request submitted successfully.',
        'success'
      );

      this.onCancelDelete();

    },

    error: (error) => {

      console.error('Error submitting removal request:', error);

      this.showNotification(
        error.error?.message ||
        'Unable to submit removal request. Please try again.',
        'error'
      );

    }

  });

}

  // ==========================================
  // COMPONENT DESTRUCTION
  // ==========================================

  ngOnDestroy(): void {

    if (this.toastTimeout) {
      clearTimeout(this.toastTimeout);
      this.toastTimeout = undefined;
    }

  }

  onEmailInput(event: Event): void {

  const input = event.target as HTMLInputElement;

  const lowercaseEmail = input.value.toLowerCase();

  input.value = lowercaseEmail;

  this.salesmanForm.get('email')?.setValue(lowercaseEmail);

}
// ==========================================
// CONVERT SALESMAN CODE TO UPPERCASE
// ==========================================

onSalesmanCodeInput(event: Event): void {

  const input = event.target as HTMLInputElement;

  const uppercaseCode = input.value.toUpperCase();

  input.value = uppercaseCode;

  this.salesmanForm.get('salesmanCode')?.setValue(uppercaseCode);

}

}
