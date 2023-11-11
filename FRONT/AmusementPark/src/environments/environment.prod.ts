import { apiConfig } from "./api.config";

export const environment = {
  production: true,
  api: {
    ...apiConfig,
    baseUrl: 'https://your-production-api-url.com' // URL de production
  }
};
