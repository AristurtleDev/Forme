# Contributing to Forme

Welcome to Forme! We're excited that you want to contribute.

Forme is a GPU accelerated text rendering library for MonoGame built on the Slug algorithm. Please read this document fully before contributing.

---

## Branching & Workflow

Forme follows a **GitFlow-style workflow**:

- `develop`: active development branch
- feature and hotfix branches: created from `develop`

**All pull requests must target the `develop` branch unless explicitly instructed otherwise.**

Keep changes focused and scoped to a single concern.

---

## Quick Contribution Rules

These rules prevent 90% of contribution issues:

- **NEVER** submit code you did not personally write.
- **NEVER** submit decompiled or reverse engineered code.
- **DO NOT** submit style only changes.
- **DO NOT** reorder type members.
- **DO NOT** introduce breaking APIs without discussion.
- **DO** follow the existing style of the file you are modifying.
- **DO** include unit tests when adding or modifying functionality.
- **DO** open an issue before implementing large features.
- **DO** keep PRs focused and reasonably sized.

---

## Intellectual Property & Decompilation

We strictly prohibit submitting:

- Decompiled XNA assemblies
- Decompiled MonoGame code
- Decompiled third-party libraries
- Any code you do not have explicit rights to contribute

It does not matter:
- How much you modify it
- Whether the original project is discontinued
- Whether you believe it is "safe"

If you did not write the code or receive explicit permission, do not submit it.

Violations will result in immediate removal of the contribution and may result in loss of contributor privileges.

All accepted contributions are distributed under the MIT License.

---

## Getting Started Locally

Clone the repository and restore, build, and test the source libraries:

```bash
git clone https://github.com/aristurtledev/forme.git
cd forme
dotnet build src/Forme/Forme.csproj
dotnet build src/Forme.MonoGame/Forme.MonoGame.csproj
dotnet build src/Forme.MonoGame.Content.Pipeline/Forme.MonoGame.Content.Pipeline.csproj
dotnet test tests/Forme.Tests/Forme.Tests.csproj
dotnet test tests/Forme.Content.Pipeline.Tests/Forme.Content.Pipeline.Tests.csproj
```

### Prerequisites
- .NET SDK 9.0 or later
- The current stable release of MonoGame (3.8.4.1)

If you are working on content pipeline extensions, test with real content and document any new processor parameters.

If you are modifying the shader, recompile the `.mgfxo` files using `src/Forme.MonoGame/compile-shaders.sh` and commit the updated binaries alongside your changes. DirectX 11 shader compilation requires Windows.

---

## Ways to Contribute

### Bug Reports

Before opening a bug report:

1. Search existing issues.
2. Provide clear reproduction steps.
3. Include expected vs actual behavior.
4. Provide minimal reproduction code (text preferred over screenshots).

Well structured bug reports significantly improve resolution speed.

---

### Feature Requests

Before implementing a feature:

1. Open a feature request.
2. Explain the problem being solved.
3. Wait for discussion and approval.

---

### Documentation & Examples

High quality documentation is just as valuable as code.

You can contribute by:

- Improving XML documentation
- Adding usage examples
- Writing tutorials
- Clarifying migration steps

---

## Code Quality & Standards

Consistency and maintainability matter.

### Architecture

- Follow existing patterns within the codebase.
- Prefer composition over inheritance when appropriate.
- Keep dependencies minimal and justified.
- Avoid unnecessary breaking changes.
- Provide deprecation paths when required.

Refer to [CODING_GUIDELINES.md](CODING_GUIDELINES.md) for detailed conventions.

---

### Performance

Game development demands performance awareness:

- Minimize allocations in hot paths.
- Use pooling where appropriate.
- Prefer `Span<T>` and `ReadOnlySpan<T>` when beneficial.
- Consider cache-friendly layouts.
- Profile performance sensitive changes.

Document performance implications in your PR description when relevant.

---

### Testing Requirements

All new functionality requires test coverage.

- Cover happy paths and edge cases.
- Validate error conditions.
- Update affected tests.
- Provide manual test instructions if automation is insufficient.

CI must pass before review.

---

## Pull Request Process

Before submitting:

- Fork the repository.
- Create a branch from `develop`.
- Use descriptive commit messages.
- Keep commits focused.
- Ensure tests pass locally.

When submitting:

- Complete the PR template.
- Link related issues (`closes #123`).
- Keep scope limited to one concern.
- Ensure GitHub Actions CI passes.

Incomplete PRs will not be reviewed.

---

## Code Review Expectations

We aim to acknowledge PRs within 3-5 business days.

Review focuses on:

- Code quality & maintainability
- Performance implications
- Test coverage
- Documentation completeness
- API stability
- Architectural consistency

Address reviewer feedback in new commits (avoid force-pushing during review).

Reviews focus on the code, not the contributor.

---

## Version Compatibility

We:

- Target the current MonoGame stable baseline version (3.8.4.1)
- Follow [semantic versioning](https://semver.org/)
- Use major versions for breaking changes
- Avoid depending on unreleased MonoGame APIs
- Minimize external dependencies

If your contribution requires a version bump, open an issue first.

---

## Platform Considerations

Forme supports:

- Windows (DesktopGL and WindowsDX)
- macOS (DesktopGL)
- Linux (DesktopGL)

The DirectX 11 shader and the WindowsDX demo require Windows. All other source libraries and tests build and run cross-platform.

---

## Getting Help

- GitHub Issues: Bug reports and feature requests

---

## Recognition

Contributors are recognized in release notes and README acknowledgements. Significant contributors may be invited to join the maintainer team.

---

## Licensing

Forme is licensed under the [MIT License](LICENSE). Third-party libraries used by Forme are under their own licenses. Please refer to [THIRD_PARTY_NOTICES](THIRD_PARTY_NOTICES) for details.

We accept contributions in "good faith" that they are not bound to a conflicting license.

**By submitting a PR you agree to distribute your work under the Forme license and copyright.**

---

Thank you for helping make Forme better.
