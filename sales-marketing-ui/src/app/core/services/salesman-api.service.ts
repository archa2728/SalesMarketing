
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams  } from '@angular/common/http';
import { Observable } from 'rxjs';

import { API_CONFIG } from '../config/api.config';

@Injectable({
  providedIn: 'root'
})
export class SalesmanApiService {

  private http = inject(HttpClient);

  private apiUrl = `${API_CONFIG.baseUrl}/Salesman`;

  // Get all salesmen
  getAllSalesmen(
  search: string = '',
  status: number | null = null,
  pageNumber: number = 1,
  pageSize: number = 10
): Observable<any> {
  let params = new HttpParams()
    .set('pageNumber', pageNumber)
    .set('pageSize', pageSize);

  if (search.trim()) {
    params = params.set('search', search.trim());
  }

  if (status !== null) {
    params = params.set('status', status);
  }

  return this.http.get<any>(this.apiUrl, { params });
}
  // getAllSalesmen(): Observable<any> {
  //   return this.http.get<any>(this.apiUrl);
  // }

  // Get salesman by ID
  getSalesmanById(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  // Create salesman
  createSalesman(data: any): Observable<any> {
    return this.http.post<any>(this.apiUrl, data);
  }

  // Update salesman
  updateSalesman(id: number, data: any): Observable<any> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, data);
  }

  // Delete salesman
  deleteSalesman(id: number): Observable<any> {
    return this.http.delete<any>(`${this.apiUrl}/${id}`);
  }

 // Submit a salesman removal request
requestSalesmanRemoval(id: number): Observable<any> {
  return this.http.post<any>(
    `${this.apiUrl}/${id}/removal-request`,
    {}
  );
}

  // Get active salesmen for dropdowns
 getSalesmanLookup(): Observable<any> {
  return this.http.get<any>(`${this.apiUrl}/active`);
}
}
