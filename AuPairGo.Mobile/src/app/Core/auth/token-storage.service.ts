import { Injectable } from '@angular/core';
import { Preferences } from '@capacitor/preferences';

@Injectable({ providedIn: 'root' })
export class TokenStorage {
  async readToken(): Promise<string | null> {
    return (await Preferences.get({ key: 'auth.token' })).value;
  }

  async readUser(): Promise<any | null> {
    const raw = (await Preferences.get({ key: 'auth.user' })).value;
    if (!raw || raw === 'undefined') {
      return null;
    }
    try {
      return JSON.parse(raw);
    } catch (e) {
      console.error('Failed to parse user from storage', e);
      return null;
    }
  }

  async write(token: string, user: any): Promise<void> {
    await Preferences.set({ key: 'auth.token', value: token });
    if (user !== undefined && user !== null) {
      await Preferences.set({ key: 'auth.user', value: JSON.stringify(user) });
    } else {
      await Preferences.remove({ key: 'auth.user' });
    }
  }

  async clear(): Promise<void> {
    await Preferences.remove({ key: 'auth.token' });
    await Preferences.remove({ key: 'auth.user' });
  }
}
