# Labels

This repo uses a small, consistent label set to keep triage simple and filtering useful.

## Labeling rules (quick)

- Every issue/PR should have **exactly one `Kind`** label.
- Add **0-2 `scope:`** labels (keep it focused).
- Add **`platform:`** only when the issue is OS-specific or OS-reproducible.
- Add **`priority:`** only when you're intentionally ordering work.
- Use **`status:`** labels only when state is not obvious from the conversation or GitHub state.

---

## Kind (pick exactly one)

| Label         | Description                                                                                                                                                                                              |
| ------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `bug`         | Something is broken, crashes, regresses, or behaves unexpectedly. The behavior is observable and differs from documented or intended behavior.                                                           |
| `feature`     | A new user-facing capability that does not exist yet. Adds something new to the public surface: a new type, method, system, or subsystem.                                                                |
| `enhancement` | An improvement to an existing feature. Covers quality-of-life improvements, better defaults, expanded platform/format support, or additional overloads for existing APIs.                                |
| `refactor`    | Internal restructuring with no intended behavior change. If it *might* change behavior, consider `bug` or `enhancement` instead.                                                                         |
| `docs`        | Documentation only work: XML doc comments, README updates, guides, or website docs. If code is also changed, prefer the relevant `Kind` label and add `docs` only if documentation is the primary focus. |
| `question`    | Support, clarification, or discussion threads. These often close without code changes once answered.                                                                                                     |
| `security`    | A security concern or vulnerability. If sensitive, avoid posting details publicly; use private reporting where possible.                                                                                 |

---

## Compatibility

| Label             | Description                                                                                                                                                                                                                                                                                                    |
| ----------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `breaking-change` | The change requires user code changes or breaks binary/behavioral compatibility. This includes, but is not limited to, public API signature changes or removals, behavioral changes that will break existing projects at runtime, and content pipeline changes requiring reexport, rebuild, or workflow changes. |

---

## Scope (0-2 labels)

> Scopes describe **where** the work lands. Keep this focused; two labels max.

| Label                     | Description                                                                                                                                                                                           |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `scope: api`              | The public API surface: types, method signatures, properties, interfaces, or serialization contracts. Use when the primary concern is what consumers see and call.                                    |
| `scope: font-processing`  | TTF/OTF parsing, glyph data extraction, quadratic Bezier curve decomposition, band data computation, and `.forme` file format reading and writing (the `Forme` core library).                         |
| `scope: rendering`        | `FormeRenderer` draw calls, glyph batching, texture management, vertex layout, draw ordering, and render correctness (the `Forme.MonoGame` library).                                                  |
| `scope: shaders`          | HLSL/GLSL shader source, compiled `.mgfxo` artifacts, effect parameter binding, shader model compatibility, and correctness of the Slug algorithm GPU implementation.                                 |
| `scope: content-pipeline` | MGCB workflows; the `.forme` file importer and processor; content build errors; pipeline output formats; and the `Forme.MonoGame.Content.Pipeline` library.                                           |
| `scope: math`             | Quadratic Bezier curve math, glyph coordinate transforms, band interval arithmetic, dilation geometry, and numeric correctness or precision issues.                                                   |
| `scope: performance`      | Allocation investigations, hot-path profiling, batching efficiency, benchmark additions, and profiling-driven improvements across any subsystem.                                                      |
| `scope: tests`            | Test additions, test fixes, test harness improvements, and validation coverage.                                                                                                                       |
| `scope: build`            | CI pipelines, NuGet packaging, release automation, build scripts, multi-targeting (DesktopGL / WindowsDX), and solution/project structure.                                                           |

---

## Priority (optional)

Only set priority when you're intentionally ordering work.

| Label              | Description                                                  |
| ------------------ | ------------------------------------------------------------ |
| `priority: high`   | Important; should be addressed next sprint/cycle.            |
| `priority: medium` | Default. Apply only if you want the priority to be explicit. |
| `priority: low`    | Nice to have; backlog item with no near-term urgency.        |

---

## Status (optional)

Use status labels to reflect workflow state *beyond* open/closed.

| Label                  | Description                                                                                                      |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `status: in-progress`  | Someone is actively working on it. Ideally link a branch, PR, or comment.                                        |
| `status: blocked`      | Cannot proceed until something external changes or a decision is made. Add a short note explaining the blocker.  |
| `status: needs-info`   | Waiting on the reporter or a maintainer to provide logs, a repro, a sample project, or other clarifying details. |
| `status: needs-review` | PR is ready for review (prefer on PRs, not issues).                                                              |

---

## Platform (optional)

Use when the issue is OS-specific or the repro is OS-dependent.

| Label               | Description |
| ------------------- | ----------- |
| `platform: windows` | Windows     |
| `platform: macos`   | macOS       |
| `platform: linux`   | Linux       |

---

## Community / Triage helpers

| Label              | Description                                                                                                                                                                                                                                                    |
| ------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `good first issue` | Beginner-friendly, well-scoped, and ready for an outside contributor. Should have clear reproduction steps or a precise acceptance criteria, pointers to the relevant files and subsystems, and requires minimal domain knowledge of the Slug algorithm or Forme internals. |
| `help wanted`      | Maintainers are open to outside contribution. Ideally add pointers on where to start and what an acceptable solution looks like.                                                                                                                               |
| `duplicate`        | The issue already exists. Close with a link to the original.                                                                                                                                                                                                   |
| `invalid`          | Not actionable, cannot be reproduced, or not a bug in Forme. Explain why when applying.                                                                                                                                                                       |
| `wontfix`          | Intentionally not addressing. Explain why and suggest alternatives where possible.                                                                                                                                                                             |

---

## Examples

| Scenario                                        | Labels                                                          |
| ----------------------------------------------- | --------------------------------------------------------------- |
| TTF parsing crash on Linux                      | `bug`, `scope: font-processing`, `platform: linux`              |
| Add support for color glyphs (COLR/CPAL)        | `feature`, `scope: font-processing`, `scope: rendering`         |
| Reduce allocations in glyph batch flushing      | `enhancement`, `scope: performance`, `scope: rendering`         |
| Band texture lookup returns wrong glyph         | `bug`, `scope: rendering`                                       |
| `.forme` file fails to load after format change | `bug`, `scope: content-pipeline`                                |
| Shader compile error on DX11                    | `bug`, `scope: shaders`, `platform: windows`                    |
| Upgrade CI/release pipeline                     | `enhancement`, `scope: build`                                   |
| Add XML docs to public font types               | `docs`, `scope: api`                                            |
| Dilation math incorrect at small font sizes     | `bug`, `scope: math`, `scope: rendering`                        |
