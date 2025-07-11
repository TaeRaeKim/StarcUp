import { IUser, IUserSettings } from '../types'

export interface IAuthService {
  login(username: string, password: string): Promise<{ success: boolean; token?: string; user?: IUser }>
  logout(): Promise<{ success: boolean }>
  getCurrentUser(): Promise<IUser | null>
  updateUserSettings(userId: string, settings: Partial<IUserSettings>): Promise<{ success: boolean }>
}

export interface ISessionManager {
  saveSession(user: IUser, token: string): Promise<void>
  loadSession(): Promise<{ user: IUser; token: string } | null>
  clearSession(): Promise<void>
  isSessionValid(): Promise<boolean>
}