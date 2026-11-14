export interface CreatedParkDataEditorTokenResponse {
  token: {
    id: string;
    expiresAtUtc: string;
  };
  plainTextToken: string;
}
