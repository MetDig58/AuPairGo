import { Injectable } from '@angular/core';
import { Preferences } from '@capacitor/preferences';

@Injectable({ providedIn: 'root' })
export class TokenStorage {
  async readToken(): Promise<string | null> {
    return (await Preferences.get({ key: 'auth.token' })).value;
  }

  async readUser(): Promise<any | null> {
    const raw = (await Preferences.get({ key: 'auth.user' })).value;
    return raw ? JSON.parse(raw) : null;
  }

  async write(token: string, user: any): Promise<void> {
    await Preferences.set({ key: 'auth.token', value: token });
    await Preferences.set({ key: 'auth.user', value: JSON.stringify(user) });
  }

  async clear(): Promise<void> {
    await Preferences.remove({ key: 'auth.token' });
    await Preferences.remove({ key: 'auth.user' });
  }
}
