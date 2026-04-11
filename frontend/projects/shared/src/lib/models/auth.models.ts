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
