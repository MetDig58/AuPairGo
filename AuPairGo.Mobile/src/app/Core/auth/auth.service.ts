import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';

import { TokenStorage } from './token-storage.service';

export interface AuthResponse {
  token: string;
  user: any;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private storage = inject(TokenStorage);
  private router = inject(Router);

  private readonly base = `${environment.apiBaseUrl}/api/Auth`;

  private readonly _token = signal<string | null>(null);
  private readonly _user = signal<any | null>(null);

  readonly isAuthenticated = computed(() => this._token() !== null);

  async restoreSession(): Promise<void> {
    const [token, user] = await Promise.all([this.storage.readToken(), this.storage.readUser()]);

    if (token) {
      this._token.set(token);
      this._user.set(user);
    }
  }

  async login(email: string, password: string): Promise<void> {
    const res = await firstValueFrom(this.http.post<AuthResponse>(`${this.base}/login`, { email, password }));
    await this.setSession(res);
  }

  async logout(): Promise<void> {
    this._token.set(null);
    this._user.set(null);
    await this.storage.clear();
    await this.router.navigateByUrl('/login', { replaceUrl: true });
  }

  private async setSession(res: AuthResponse): Promise<void> {
    this._token.set(res.token);
    this._user.set(res.user);
    await this.storage.write(res.token, res.user);
  }
}
