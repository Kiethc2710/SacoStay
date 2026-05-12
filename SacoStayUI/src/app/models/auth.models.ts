export interface LoginRequest {
  emailPhoneorUsername: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  user?: UserProfile;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  role: 'tenant' | 'landlord';
}

export interface RegisterResponse {
  message: string;
}

export interface UserProfile {
  id: string;
  userName: string;
  email: string;
  phoneNumber: string;
  roles: string[];
}
