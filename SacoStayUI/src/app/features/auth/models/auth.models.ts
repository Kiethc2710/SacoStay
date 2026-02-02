export interface LoginRequest {
  emailPhoneorUsername: string;
  password: string;
}

export interface LoginResponse {
  token: string;
}

export interface UserProfile {
  id: string;
  userName: string;
  email: string;
  phoneNumber: string;
  roles: string[];
}
