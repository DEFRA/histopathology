---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
description: 'LLD Agent generation instructions used to generate civica specific LLD documents from respective HLSA and HLD source files. This file is used by the documentation-lld agent to fill a LLD .docx template with content from the source files.'
applyTo: '**'
---
 
# LLD Agent Instructions
 
## 1. Who Are We . For Which Project Are We Generating This LLD? For example, for BSE Project:
- **Project:** [BSE]
- **Team:** [LAP Civica Migration Team]
- **Cloud:** [Azure]
 
---
 
## 2. What to Read
 
Input files are located in the `docs/` folder of the workspace:
 
| File | Format | Purpose |
|------|--------|---------|
| `HLSA.pptx` | `.pptx` | Architecture overview — extract using PowerShell ZipFile + `<a:t>` regex |
| `HLD.docx` or `HLD.pptx` | `.docx` or `.pptx` | High-level design — check the actual file extension at runtime before parsing |
| `LLD-Template.docx` | `.docx` | Template to populate — extract using PowerShell ZipFile + `<w:t>` regex |
 
**Format detection rule:** always inspect the actual file extension of each input before extracting. If the file is `.docx`, use `<w:t[^>]*>([^<]+)</w:t>`. If `.pptx`, use `<a:t>([^<]*)</a:t>`. Do not assume format from the file name alone — the HLD file may be either format.
 
---
 
## 3. How to Fill the Document
- Use only information from the HLSA and HLD source files
- Keep the template structure exactly as-is — don't add or remove sections
- Write in clear, technical, third-person English
- Cite the source slide number or HLD section for every claim (e.g. "per HLD Slide 7" or "per HLD Section 2.5")
- If information is missing, write: `[NEEDS INPUT: <what is missing>]`
- Never guess or invent facts
 
---
 
## 4. Sections That Always Need Human Review
- Security Design
- Risks and Open Questions
 
Mark these with: `[MANUAL REVIEW REQUIRED]`
 
---
 
## 5. Our Naming Style
- Services: `{domain}-{capability}-svc` (e.g. `payments-auth-svc`)
- APIs: `/api/v1/{resource}` (e.g. `/api/v1/transactions`)
- Databases: `{domain}-{entity}-db` (e.g. `payments-transaction-db`)
- Environments: `dev` → `test` → `staging` → `prod`
 
---
 
## 6. Diagrams
- Use **Mermaid** format for all diagrams
- If a diagram can't be generated, write a plain text description instead
 
---
 
## 7. Output — DOCX File (MANDATORY)
 
- The final output must be saved as `docs/LLD-BSE-Populated.docx`.
- The blank template (`LLD-Template.docx`) must **not** be overwritten — always save as a new file.
- All `<placeholder>` tokens in the template must be replaced with generated content.
- All empty table cells must be populated. For tables where the technology is not applicable to this project (for example, API Gateway or Service Bus where the architecture does not use them), populate the first data cell with `N/A — [reason and source reference]`. Do not leave any cell blank.
- All Figure caption placeholders must have a Mermaid diagram or plain-text description below them. Figure captions are paragraphs whose text starts with `Figure N:` — they do not have angle-bracket placeholder tokens. Find them by scanning the template paragraph list for text beginning with `Figure`, not by searching for `< >` tokens.
- A **Document Generation Summary** section must be appended as the final section of the DOCX.
- A `.md` file is **not** an acceptable final output.
 
---
 
## 8. When You're Done
The final DOCX must contain an appended **"Document Generation Summary"** section with:
- Which sections were auto-filled
- Which sections need human input (`[MANUAL REVIEW REQUIRED]`)
- Any assumptions made
- Generation date (ISO 8601)
 
---
 
## 9. Known Word Automation Constraints
 
When using Word automation to produce the DOCX, keep these four rules in mind:
 
- The replacement text field in Word's Find and Replace has a 255-character limit. For any replacement longer than 200 characters, locate the token by searching the document and overwrite the matched text directly rather than using the replacement field.
- When setting a heading or paragraph style on a range, assign it as a property. Calling it as a function does not work in this environment and will throw an error.
- Scripts that contain special characters such as em-dashes, smart quotes, or ampersands must be saved to a separate script file with UTF-8 encoding before they are run. Executing them inline will cause parse errors.
- Token strings that contain angle brackets must not be used as variable keys. Pass them directly to a helper function instead.

---

## 10. Inserting Text into Body Sections — Skip TOC and List Entries

When using Word's Find to locate a section heading or figure caption before inserting content, the search will match **Table of Contents entries and List of Figures/Tables entries first**, because those appear earlier in the document. Inserting at that position places content inside the TOC rather than under the correct body section.

**Rule:** After every Find match, read the paragraph style of the matched range before inserting. If the style name is any navigation or list style — including `TOC 1` through `TOC 9`, `Table of Contents`, `Table of Figures`, `Table of Tables`, or any style containing `List of` — skip that match and search forward for the next occurrence. Only insert content when the matched paragraph has a body style such as `Heading 1`, `Heading 2`, `Heading 3`, `Caption`, `Normal`, or a project-specific body style. Repeat the search in a loop until a body-style match is found or the document is exhausted.

**Important — style name mismatch:** The DOCX XML stores List of Tables and List of Figures paragraphs under the internal style ID `TableofFigures` (no spaces). Word's COM interface returns the display name `Table of Figures` (with spaces). Any skip check based only on the unspaced form will fail to match and will incorrectly treat those list entries as valid body paragraphs. The skip check must match both the spaced and unspaced forms, for example by checking for the substring `Table of` as well as `Tableof`.

---

## 11. Inserting into Sections with No Body Paragraph — Consecutive Heading Problem

Some sections in the template have a heading paragraph that is **directly followed by another heading paragraph**, with no blank body paragraph between them. Confirmed examples from this template: `Anti-Virus` (Heading 2) is immediately followed by `Vulnerability scanning` (Heading 2), which is immediately followed by `SOC Integration` (Heading 2), with no body paragraph appearing until after all three headings.

When the insert function finds the target heading and takes the next paragraph, the result is the next heading, not a body paragraph. Inserting content at the start of that next heading prepends it into the heading text, so the content appears visually under the wrong section number. The error then cascades: the contaminated heading is found on the next search and its content is prepended into the heading after it.

**Rule:** After finding the target heading and before inserting, check whether the next paragraph is itself a heading. If it is, the template has no body paragraph for that section, so create a new paragraph after the current heading, explicitly reset its style to `Normal` (because a paragraph inserted after a heading inherits the heading style), and then insert the content into that new paragraph. Never insert content directly into a paragraph whose style contains `Heading`.

---

## 13. Document Generation Summary — Companion Markdown File Only, Not in DOCX

The Document Generation Summary (auto-filled sections, manual review flags, needs-input list, assumptions, confidence ratings) must be written only to the companion `docs/LLD-BSE-Summary.md` file. Do not append a Document Generation Summary section to the DOCX. The DOCX must end at the last substantive content section that was present in the template — no summary appendix is added to the Word document.

---

## References

- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — Common coding standards](https://defra.github.io/software-development-standards/standards/common_coding_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Security guidance](https://digital.defra.gov.uk/ai-toolkit/guidance/security)
- [Defra AI Toolkit — Keeping data safe](https://digital.defra.gov.uk/ai-toolkit/guidance/keeping-data-safe)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)