export type Choice = "Yes" | "No" | "Skip";
export type Role = "Chef" | "Member";
export interface Session {
  email: string;
  households: { id: string; name: string; role: Role }[];
}
export interface Recipe {
  id: string;
  title: string;
  description: string;
  minutes: number;
  ingredients: string[];
  instructions: string[];
  isDevelopmentSeed: boolean;
}
export interface Kitchen {
  name: string;
  role: Role;
  activeRound: { id: string; startedAt: string; targetYesCount: number } | null;
  recipes: Recipe[];
  myVotes: { recipeId: string; choice: Choice }[];
}
export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
  }
}

export async function api<T>(
  path: string,
  method = "GET",
  body?: unknown,
): Promise<T> {
  const headers: Record<string, string> = {};
  if (method !== "GET") {
    // Fetch after identity changes; antiforgery tokens are bound to the signed-in user.
    const csrf = await fetch("/api/auth/csrf", {
      credentials: "same-origin",
      cache: "no-store",
    });
    if (!csrf.ok)
      throw new ApiError(
        csrf.status,
        "Could not verify your session. Refresh and try again.",
      );
    headers["X-CSRF-TOKEN"] = ((await csrf.json()) as { token: string }).token;
    if (body !== undefined) headers["Content-Type"] = "application/json";
  }
  const response = await fetch(`/api${path}`, {
    method,
    headers,
    credentials: "same-origin",
    cache: "no-store",
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!response.ok) {
    const problem = (await response.json().catch(() => null)) as {
      title?: string;
    } | null;
    throw new ApiError(
      response.status,
      problem?.title ??
        (response.status === 401
          ? "Please sign in again."
          : response.status === 429
            ? "Too many attempts. Please wait a minute."
            : "Something went wrong. Please try again."),
    );
  }
  return response.status === 204
    ? (undefined as T)
    : ((await response.json()) as T);
}
