import type { Kitchen, Recipe } from "../api";
export function RecipeList({
  kitchen,
  onSelect,
}: {
  kitchen: Kitchen;
  onSelect: (recipe: Recipe) => void;
}) {
  return (
    <section aria-label="Household recipes">
      <div className="section-heading">
        <h2>On the kitchen counter</h2>
        <span>{kitchen.recipes.length} recipes to consider</span>
      </div>
      <div className="recipe-grid">
        {kitchen.recipes.map((recipe, index) => (
          <article className="recipe-card" key={recipe.id}>
            <div className={`recipe-art art-${index % 3}`} aria-hidden="true">
              <div className="dish">
                <span />
                <span />
                <span />
              </div>
              <span className="time">{recipe.minutes} min</span>
            </div>
            <div className="recipe-copy">
              <p className="eyebrow">FROM OUR TEST KITCHEN</p>
              <h3>{recipe.title}</h3>
              <p>{recipe.description}</p>
              <div className="card-bottom">
                <span className="vote-status">
                  {kitchen.myVotes.find((v) => v.recipeId === recipe.id)
                    ?.choice ?? "Not voted yet"}
                </span>
                <button
                  className="text-button"
                  onClick={() => onSelect(recipe)}
                >
                  Recipe & vote →
                </button>
              </div>
            </div>
          </article>
        ))}
      </div>
      <p className="seed-note">
        These are original sample recipes for trying out Dinder. Your
        household’s recipe collection comes next.
      </p>
    </section>
  );
}
