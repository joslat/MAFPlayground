# MAFPlayground

Experiments and code samples for the Microsoft Agent Framework
They contain Agent samples, agentic workflows and automation that I use in conference talks, trainings, workshops, and books.  
This repository is **source-available** under a custom license that **allows private/self-learning and internal use**, but **forbids Presentational Use** without written permission.

> ⚠️ **Use at your own risk.** Samples are experimental and may not work or may create loops that incur cloud/API costs.

- Repo: https://github.com/joslat/MAFPlayground  
- Author: **Jose Luis Latorre** (joslat@gmail.com)

---

## What’s inside

- Small, focused samples and experiments (C#, Python, and related tooling)
- Patterns for agent frameworks and automation workflows
- Snippets I may later port into other repositories (see “Porting to MIT repos”)

---

## Getting started

1. Clone:
   ```bash
   git clone https://github.com/joslat/MAFPlayground.git
   ```
2. Open the sample folder you’re interested in and follow its local `README.md` if present.
3. Use environment variables or `.env` for API keys. Start with a **test account / low quotas**.

> **Cost safety tip:** Add quotas and alerts to any API keys you use while running samples.

---

## License & Attribution

**MAFPlayground © 2025 Jose Luis Latorre**  
Licensed under **MAFPlayground Public License — No Presentational Use 1.0 (Swiss)**  
SPDX: `LicenseRef-MAFPlayground-NPU-1.0-CH`

- **Private use allowed**: self-learning, experimentation, and internal (non-public) use are permitted.
- **No Presentational Use**: using this code in talks, trainings, workshops, courses,
  classroom instruction, tutorials, or books is **not permitted** without written permission.
- **Use at your own risk**: samples may not work or may create loops incurring cloud/API costs.
- **Attribution required** in redistributions.

Permission requests: **joslat@gmail.com**

**How to attribute (for allowed uses):**  
“Based on MAFPlayground by Jose Luis Latorre — https://github.com/joslat/MAFPlayground”

> Note: Some specific files are **dual-licensed for use in MIT repositories**.  
> See SPDX headers in those files or `DUAL-LICENSED-FILES.md` if present.

---

## Dual-licensed files (MIT compatibility)

Some files are explicitly marked with this SPDX header to allow contribution to MIT-licensed repositories:

```txt
// SPDX-License-Identifier: LicenseRef-MAFPlayground-NPU-1.0-CH OR MIT
// Copyright (c) 2025 Jose Luis Latorre
```

Only files with that header (or listed in `DUAL-LICENSED-FILES.md`) are dual-licensed. Everything else remains under `LicenseRef-MAFPlayground-NPU-1.0-CH`.

---

## Porting to MIT repos (e.g., microsoft/agent-framework)

When contributing a sample from this repo to an MIT repository:

1. Ensure the source file here is dual-licensed (`… OR MIT` SPDX).
2. In the destination repo, follow its header policy (e.g., might require **only**):
   ```txt
   // Copyright (c) Microsoft. All rights reserved.
   ```
3. In your PR description, include provenance:
   - Original author: **Jose Luis Latorre**
   - Source link to the file/commit in this repo
   - Statement that the contribution is under the destination’s **MIT** license with no extra terms.

---

## Contributing

This is primarily a personal playground. PRs may be accepted case-by-case.

- By contributing, you confirm you wrote the code or have rights to contribute it.
- Use DCO sign-off in commits:
  ```
  Signed-off-by: Your Name <your.email@example.com>
  ```

> If you contribute a file that should later be portable to MIT repos, mention it in the PR and add the `… OR MIT` SPDX header.

---

## Security & costs

- Rotate API keys frequently; use least-privilege keys.
- Limit spend with provider-side budgets and rate limits.
- Run samples in sandbox environments whenever possible.

---

## Contact

- Email: **joslat@gmail.com**
- LinkedIn: https://www.linkedin.com/in/joslat/
