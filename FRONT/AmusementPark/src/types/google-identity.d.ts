interface GoogleCredentialResponse {
  credential: string;
  select_by: string;
}

interface GoogleIdConfiguration {
  client_id: string;
  callback: (response: GoogleCredentialResponse) => void;
  ux_mode?: 'popup' | 'redirect';
  cancel_on_tap_outside?: boolean;
  nonce?: string;
}

interface GoogleRenderedButtonOptions {
  type?: 'standard' | 'icon';
  theme?: 'outline' | 'filled_blue' | 'filled_black';
  size?: 'large' | 'medium' | 'small';
  text?: 'signin_with' | 'signup_with' | 'continue_with' | 'signin';
  shape?: 'rectangular' | 'pill' | 'circle' | 'square';
  logo_alignment?: 'left' | 'center';
  width?: number;
}

interface GoogleAccountsIdApi {
  initialize(configuration: GoogleIdConfiguration): void;
  renderButton(parent: HTMLElement, options: GoogleRenderedButtonOptions): void;
  disableAutoSelect(): void;
}

interface GoogleAccountsApi {
  id: GoogleAccountsIdApi;
}

interface GoogleApi {
  accounts: GoogleAccountsApi;
}

interface Window {
  google?: GoogleApi;
}
