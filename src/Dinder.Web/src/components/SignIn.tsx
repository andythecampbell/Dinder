export function SignIn({
  busy,
  onSignIn,
}: {
  busy: boolean;
  onSignIn: (email: string, password: string) => Promise<void>;
}) {
  return (
    <section className="login-layout">
      <div className="welcome">
        <p className="eyebrow">GOOD DINNERS START TOGETHER</p>
        <h1>
          Everyone gets
          <br />a say at the table.
        </h1>
        <p className="intro">
          A few simple choices today.
          <br />
          Something to look forward to for dinner.
        </p>
        <div className="table-illustration" aria-hidden="true">
          <div className="plate">
            <span className="pea p1" />
            <span className="pea p2" />
            <span className="pea p3" />
            <span className="leaf l1" />
            <span className="leaf l2" />
            <span className="tomato" />
          </div>
          <span className="table-caption">
            Your next favorite dinner starts here.
          </span>
        </div>
      </div>
      <form
        className="login-card"
        onSubmit={(event) => {
          event.preventDefault();
          const data = new FormData(event.currentTarget);
          void onSignIn(
            String(data.get("email")),
            String(data.get("password")),
          );
        }}
      >
        <p className="eyebrow">WELCOME HOME</p>
        <h2>Come on in.</h2>
        <p>Sign in to your household’s kitchen.</p>
        <label htmlFor="email">Email address</label>
        <input
          id="email"
          name="email"
          type="email"
          autoComplete="username"
          required
          maxLength={254}
        />
        <label htmlFor="password">Password</label>
        <input
          id="password"
          name="password"
          type="password"
          autoComplete="current-password"
          required
          maxLength={256}
        />
        <button className="primary" disabled={busy}>
          {busy ? "Signing in…" : "Enter the kitchen →"}
        </button>
        <p className="fine-print">
          Your account is provided by the person who set up your household.
        </p>
      </form>
    </section>
  );
}
