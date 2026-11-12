import type { ApiErrorBody } from '../types/api';

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/$/, '');

export class ApiError extends Error {
  status: number;
  validationErrors?: Record<string, string[]>;

  constructor(message: string, status: number, validationErrors?: Record<string, string[]>) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.validationErrors = validationErrors;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers: {
      Accept: 'application/json',
      ...(init?.body ? { 'Content-Type': 'application/json' } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    let body: ApiErrorBody = {};
    try {
      body = (await response.json()) as ApiErrorBody;
    } catch {
      // A API pode responder sem corpo (por exemplo, em um erro de proxy).
    }

    const validationMessage = body.errors
      ? Object.values(body.errors).flat().join(' ')
      : undefined;
    const message = validationMessage || body.detail || body.message || body.title ||
      `A solicitação falhou (HTTP ${response.status}).`;

    throw new ApiError(message, response.status, body.errors);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export const http = {
  get<T>(path: string): Promise<T> {
    return request<T>(path);
  },
  post<TBody, TResult>(path: string, body: TBody): Promise<TResult> {
    return request<TResult>(path, {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },
};
