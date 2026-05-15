export const AUTH_API_ENDPOINTS = {
  login: 'auth/login',
  refreshToken: 'auth/refresh-token',
  logout: 'auth/logout',
  register: 'users',
  confirmEmail: 'users/confirm-email',
  resendConfirmation: 'users/resend-confirmation',
  forgotPassword: 'users/forgot-password',
  resetPassword: 'users/reset-password',
  externalLogin: (provider: string) => `auth/external/${provider}`,
  getCurrentUserById: (id: string) => `users/${id}`
} as const;
