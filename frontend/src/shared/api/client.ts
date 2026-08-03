import type { ApiProblemDetails } from '@shared/contracts';

export class ApiError extends Error {
  public readonly status: number;
  public readonly details: ApiProblemDetails | null;

  constructor(message: string, status: number, details?: ApiProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.details = details ?? null;
  }

  get errorCode(): string | undefined {
    return this.details?.errorCode ?? this.details?.title;
  }
}

export function createApiClient(baseUrl: string) {
  async function request<T>(
    method: string,
    path: string,
    body?: unknown,
    signal?: AbortSignal,
  ): Promise<T> {
    const url = `${baseUrl}${path}`;
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };

    const response = await fetch(url, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined,
      signal,
    });

    if (!response.ok) {
      let details: ApiProblemDetails | undefined;
      try {
        details = (await response.json()) as ApiProblemDetails;
      } catch {
        // response not json
      }
      throw new ApiError(
        details?.detail ?? response.statusText,
        response.status,
        details,
      );
    }

    if (response.status === 204) return undefined as T;

    return response.json() as Promise<T>;
  }

  return {
    get: <T>(path: string, signal?: AbortSignal) => request<T>('GET', path, undefined, signal),
    post: <T>(path: string, body?: unknown, signal?: AbortSignal) =>
      request<T>('POST', path, body, signal),
    put: <T>(path: string, body?: unknown, signal?: AbortSignal) =>
      request<T>('PUT', path, body, signal),
    delete: <T>(path: string, signal?: AbortSignal) => request<T>('DELETE', path, undefined, signal),
  };
}

export type ApiClient = ReturnType<typeof createApiClient>;

let _client: ApiClient | null = null;

export function getApiClient(): ApiClient {
  if (!_client) {
    _client = createApiClient(import.meta.env.VITE_API_BASE_URL ?? '');
  }
  return _client;
}