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
  exp: number;
  iss?: string;
  aud?: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface ConfirmEmailRequest {
  email: string;
  token: string;
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
