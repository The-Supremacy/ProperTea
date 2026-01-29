/**
 * User preferences stored in backend and synced across devices.
 */
export interface UserPreferences {
  theme: 'light' | 'dark';
  language: string; // BCP-47 language tag (e.g., 'en', 'uk')
}

/**
 * Request body for updating user preferences.
 */
export interface UpdateUserPreferencesRequest {
  theme: 'light' | 'dark';
  language: string;
}

/**
 * Response from GET /api/user/preferences endpoint.
 */
export interface GetUserPreferencesResponse {
  theme: 'light' | 'dark';
  language: string;
}
