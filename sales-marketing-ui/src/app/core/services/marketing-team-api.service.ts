
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../config/api.config';

@Injectable({
  providedIn: 'root'
})
export class MarketingTeamApiService {

  private http = inject(HttpClient);

  private apiUrl = `${API_CONFIG.baseUrl}/MarketingTeam`;

  // Get all marketing teams
  getAllTeams(): Observable<any> {
    return this.http.get<any>(this.apiUrl);
  }

  // Get marketing team by ID
  getTeamById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  // Create marketing team
  createTeam(data: any): Observable<any> {
    return this.http.post<any>(this.apiUrl, data);
  }

  // Update marketing team
  updateTeam(id: number, data: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, data);
  }

  // Update marketing team status
  updateTeamStatus(id: number, status: number): Observable<any> {
    return this.http.patch<any>(
      `${this.apiUrl}/${id}/status`,
      { status }
    );
  }

  // Delete marketing team
  deleteTeam(id: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }

  // Add a member to an existing team
  addTeamMember(teamId: number, data: any): Observable<any> {
    return this.http.post<any>(
      `${this.apiUrl}/${teamId}/members`,
      data
    );
  }
    // Get all pending salesman removal requests
  getPendingRemovalRequests(): Observable<any> {
    return this.http.get<any>(
      `${this.apiUrl}/removal-requests`
    );
  }

  // Approve a salesman removal request
  approveRemovalRequest(id: number): Observable<any> {
    return this.http.put<any>(
      `${this.apiUrl}/removal-requests/${id}/approve`,
      {}
    );
  }

  // Reject a salesman removal request
  rejectRemovalRequest(
    id: number,
    rejectionReason: string
  ): Observable<any> {
    return this.http.put<any>(
      `${this.apiUrl}/removal-requests/${id}/reject`,
      { rejectionReason }
    );
  }
}
