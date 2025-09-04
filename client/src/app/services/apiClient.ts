import { msalInstance } from "./msalConfig";

const API_BASE_URL = import.meta.env.VITE_SERVER_API_URL;

const getAccessToken = async (): Promise<string> => {
  const accounts = msalInstance.getAllAccounts();
  if (accounts.length === 0) {
    throw new Error("No accounts available");
  }

  const request = {
    scopes: [import.meta.env.VITE_ENTRA_SCOPE],
    account: accounts[0],
  };

  try {
    const response = await msalInstance.acquireTokenSilent(request);
    return response.accessToken;
  } catch (error) {
    console.warn("Silent token acquisition failed. Trying interactive...");
    const response = await msalInstance.acquireTokenPopup(request);
    return response.accessToken;
  }
};

export const apiFetch = async (
  path: string,
  init: RequestInit = {},
  signal?: AbortSignal
) => {
  const token = await getAccessToken();
  const url = `${API_BASE_URL}${path}`;

  const headers = {
    "Content-Type": "application/json",
    Authorization: `Bearer ${token}`,
    ...(init.headers ?? {}),
  };

  const response = await fetch(url, {
    ...init,
    headers,
    signal,
  });

  if (response.status === 401) {
    throw new Error("Unauthorized");
  }

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || response.statusText);
  }

  const contentType = response.headers.get("Content-Type");
  const hasBody =
    response.status !== 204 && contentType?.includes("application/json");

  return hasBody ? response.json() : undefined;
};
