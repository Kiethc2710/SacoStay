export const APP_CONSTANTS = {
  // API
  API_BASE_URL: 'http://localhost:5219/api',
  
  // Storage Keys
  TOKEN_KEY: 'saco_stay_token',
  USER_KEY: 'saco_stay_user',
  
  // Roles
  ROLES: {
    TENANT: 'tenant',
    LANDLORD: 'landlord'
  },
  
  // Status
  STATUS: {
    ACTIVE: 'active',
    INACTIVE: 'inactive',
    PENDING: 'pending'
  },
  
  // Pagination
  DEFAULT_PAGE_SIZE: 10,
  MAX_PAGE_SIZE: 100,
  
  // Validation
  PASSWORD_MIN_LENGTH: 6,
  PHONE_MIN_LENGTH: 10,
  PHONE_MAX_LENGTH: 11,
  
  // Colors
  COLORS: {
    SACO_ORANGE: '#FF9F43',
    SACO_ORANGE_DARK: '#FF8C2A',
    SACO_BLUE: '#1A1A2E',
    SACO_GRAY: '#6B7280'
  }
} as const;
