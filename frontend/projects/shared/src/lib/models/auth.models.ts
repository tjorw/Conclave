export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
}

export interface JwtClaims {
  person_id: string;
  is_admin?: string;
  is_system_admin?: string;
  is_reception?: string;
  tenant_id?: string;
  user_type?: string;
  exp: number;
  iss?: string;
  aud?: string;
}

export function getLoginReasonMessage(reason: string | null): string | null {
  switch (reason) {
    case 'session-expired':
      return 'Sessionen har gått ut. Logga in igen för att fortsätta.';
    default:
      return null;
  }
}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface ConfirmEmailRequest {
  email: string;
  token: string;
  tenantId?: string | null;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface MyProfileResponse {
  name: string;
  email: string;
  phone: string | null;
}

export interface UpdateProfileRequest {
  name: string;
  email: string;
  phone: string | null;
}
