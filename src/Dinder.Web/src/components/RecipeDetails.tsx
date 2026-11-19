import { useEffect, useRef } from "react";
import type { Choice, Recipe } from "../api";
export function RecipeDetails({
  selected,
  selectedVote,
  busy,
  active,
  onBack,
  vote,
}: {
  selected: Recipe;
  selectedVote?: Choice;
  busy: boolean;
  active: boolean;
  onBack: () => void;
  vote: (choice: Choice) => Promise<void>;
}) {
  const heading = useRef<HTMLHeadingElement>(null);
  useEffect(() => {
    heading.current?.focus();
  }, [selected.id]);
  return (
    <section className="recipe-detail" aria-label="Recipe details">
      <button className="text-button" disabled={busy} onClick={onBack}>
        ← All recipes
      </button>
      <div className="detail-heading">
        <p className="eyebrow">
          {selected.minutes} MINUTES · DEVELOPMENT RECIPE
        </p>
        <h2 ref={heading} tabIndex={-1}>
          {selected.title}
        </h2>
        <p>{selected.description}</p>
      </div>
      <div className="recipe-columns">
        <div>
          <h3>What you’ll need</h3>
          <ul>
            {selected.ingredients.map((i) => (
              <li key={i}>{i}</li>
            ))}
          </ul>
        </div>
        <div>
          <h3>At the stove</h3>
          <ol>
            {selected.instructions.map((i) => (
              <li key={i}>{i}</li>
            ))}
          </ol>
        </div>
      </div>
      <div className="voting">
        <div>
          <h3>Would you have this for dinner?</h3>
          <p>
            {selectedVote
              ? `Your current choice: ${selectedVote}. Choose again to update it.`
              : "Yes adds support. No lowers its score. Skip stays neutral."}
          </p>
        </div>
        <div className="vote-buttons">
          {(["Yes", "No", "Skip"] as Choice[]).map((choice) => (
            <button
              key={choice}
              className={choice === "Yes" ? "primary" : "secondary"}
              aria-pressed={selectedVote === choice}
              disabled={busy || !active}
              onClick={() => vote(choice)}
            >
              {choice === "Yes"
                ? "Yes, please"
                : choice === "No"
                  ? "Not for me"
                  : "Skip for now"}
            </button>
          ))}
        </div>
        {!active && <p>Voting opens when your Chef starts a round.</p>}
      </div>
    </section>
  );
}
