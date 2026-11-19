import { SignIn } from "./components/SignIn";
import { RecipeDetails } from "./components/RecipeDetails";
import { RecipeList } from "./components/RecipeList";
import { StrictMode, useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import {
  api,
  ApiError,
  type Choice,
  type Kitchen,
  type Recipe,
  type Session,
} from "./api";
import "./styles.css";

function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [kitchen, setKitchen] = useState<Kitchen | null>(null);
  const [selected, setSelected] = useState<Recipe | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const householdId = session?.households[0]?.id;

  async function loadSession() {
    const next = await api<Session>("/auth/me");
    setSession(next);
    if (next.households[0])
      setKitchen(
        await api<Kitchen>(`/households/${next.households[0].id}/kitchen`),
      );
  }
  useEffect(() => {
    loadSession()
      .catch((e) => {
        if (!(e instanceof ApiError && e.status === 401)) setError(message(e));
      })
      .finally(() => setLoading(false));
  }, []);
  async function action(work: () => Promise<void>) {
    setBusy(true);
    setError("");
    setNotice("");
    try {
      await work();
    } catch (e) {
      setError(message(e));
      if (e instanceof ApiError && e.status === 401) {
        setSession(null);
        setKitchen(null);
        setSelected(null);
      }
    } finally {
      setBusy(false);
    }
  }
  async function vote(choice: Choice) {
    if (!selected || !kitchen?.activeRound) return;
    await action(async () => {
      await api(
        `/households/${householdId}/rounds/${kitchen.activeRound!.id}/votes/${selected.id}`,
        "PUT",
        { choice },
      );
      setKitchen(await api<Kitchen>(`/households/${householdId}/kitchen`));
      setNotice(`Saved ${choice} for ${selected.title}.`);
    });
  }
  const selectedVote = kitchen?.myVotes.find(
    (v) => v.recipeId === selected?.id,
  )?.choice;
  return (
    <div className="app">
      <header>
        <a className="brand" href="/" aria-label="Dinder home">
          <span className="brand-mark">d</span>dinder
          <span className="brand-dot">.</span>
        </a>
        <span className="header-note">A little less “what’s for dinner?”</span>
        {session && (
          <button
            className="text-button"
            disabled={busy}
            onClick={() =>
              action(async () => {
                await api("/auth/logout", "POST");
                setSession(null);
                setKitchen(null);
                setSelected(null);
              })
            }
          >
            Sign out
          </button>
        )}
      </header>
      <main>
        {error && (
          <div className="error" role="alert">
            {error}
          </div>
        )}
        <div className="announcement" role="status" aria-live="polite">
          {notice}
        </div>
        {loading ? (
          <p className="loading">Opening the kitchen…</p>
        ) : !session ? (
          <SignIn
            busy={busy}
            onSignIn={(email, password) =>
              action(async () => {
                await api("/auth/login", "POST", { email, password });
                await loadSession();
              })
            }
          />
        ) : !kitchen ? (
          <section>
            <h1>No kitchen yet.</h1>
            <p>
              Your account needs a household membership. Contact the person who
              set up Dinder.
            </p>
          </section>
        ) : (
          <>
            <section className="kitchen-heading">
              <div>
                <p className="eyebrow">
                  {kitchen.name.toUpperCase()} / DINNER TOGETHER
                </p>
                <h1>What sounds good?</h1>
                <p className="intro">
                  Take a look. Have a say. We’ll take it from here.
                </p>
              </div>
              <div className="identity">
                <span className="role">{kitchen.role}</span>
                <span>{session.email}</span>
              </div>
            </section>
            <section className="round-banner" aria-label="Planning round">
              <div>
                <span
                  className={`status-dot ${kitchen.activeRound ? "active" : ""}`}
                />
                <strong>
                  {kitchen.activeRound
                    ? "The kitchen is open for votes"
                    : "A new week is waiting"}
                </strong>
                <p>
                  {kitchen.activeRound
                    ? `${kitchen.myVotes.length} of ${kitchen.recipes.length} recipes have your vote. You can change your mind in recipe details.`
                    : kitchen.role === "Chef"
                      ? "Start a planning round to let everyone weigh in."
                      : "Your Chef will start the next planning round. Check back soon."}
                </p>
              </div>
              {!kitchen.activeRound && kitchen.role === "Chef" && (
                <button
                  className="primary"
                  disabled={busy}
                  onClick={() =>
                    action(async () => {
                      await api(`/households/${householdId}/rounds`, "POST");
                      setKitchen(
                        await api<Kitchen>(
                          `/households/${householdId}/kitchen`,
                        ),
                      );
                      setNotice(
                        "Planning round started. Everyone can now vote.",
                      );
                    })
                  }
                >
                  {busy ? "Starting…" : "Start a planning round →"}
                </button>
              )}
              <button
                className="text-button"
                disabled={busy}
                onClick={() =>
                  action(async () => {
                    setKitchen(
                      await api<Kitchen>(`/households/${householdId}/kitchen`),
                    );
                    setNotice("Kitchen refreshed.");
                  })
                }
              >
                Refresh
              </button>
            </section>
            {selected ? (
              <RecipeDetails
                selected={selected}
                selectedVote={selectedVote}
                busy={busy}
                active={!!kitchen.activeRound}
                onBack={() => {
                  setSelected(null);
                  setNotice("");
                }}
                vote={vote}
              />
            ) : (
              <RecipeList kitchen={kitchen} onSelect={setSelected} />
            )}
          </>
        )}
      </main>
      <footer>
        <span>Made for the everyday dinner question.</span>
        <span>Dinder · A seat for everyone</span>
      </footer>
    </div>
  );
}
function message(error: unknown) {
  return error instanceof Error
    ? error.message
    : "Could not reach the kitchen. Please try again.";
}
createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
