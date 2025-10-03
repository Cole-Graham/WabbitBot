- Recommended minimum (implement now)
  - renderer-build-visual-callbacks.md: High ROI. You already have `VisualBuildResult` and `DiscordMessageBuilderExtensions.WithVisual` in `src/WabbitBot.DiscBot/DSharpPlus/ComponentModels`, and renderers like `MatchRenderer`. Centralizing send/edit via a `buildVisual` callback removes duplication, enforces URL/attachment policy uniformly, and stays within `src/WabbitBot.DiscBot/DSharpPlus/`.
  - retry-throttling-wrappers.md: High operational value. Discord calls are the flaky surface. A small `RetryAsync(Func<Task<Result>>)` wrapper improves resilience for `MatchRenderer` and similar code with minimal surface change and no DI.

- Next best (short follow-up)
  - deterministic-strategy-seams.md: Low-risk, test payoff. Adding optional delegate params (e.g., `Func<string[], string>` in `GameApp.StartNextGameAsync`) enables deterministic tests without touching runtime flows.

- Defer (do when you’re ready to touch domain/state)
  - state-machine-guards-actions.md: Useful for clarity, but touches `MatchCore.State`/`Game` transition pathways. Good medium-term refactor once behavior is fully covered by tests.
  - validation-pipelines.md: Valuable for Core consistency, but introduces a new pattern where some validation already exists. Adopt incrementally per method to avoid churn.

If you want a minimal sequence:
1) Implement renderer callbacks in `WabbitBot.DiscBot/DSharpPlus/Renderers` and switch one or two call sites in `MatchRenderer`.
2) Add `IoPolicy.RetryAsync` and wrap `CreateThreadAsync` and `SendMessageAsync`.
3) Add the `chooseMap` optional delegate to `GameApp.StartNextGameAsync` for deterministic tests.