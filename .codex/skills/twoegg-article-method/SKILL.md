---
name: twoegg-article-method
description: Use when drafting, revising, outlining, or reviewing TwoEgg / TechStackShow articles in this repo. Follow the repo article method: start from problem space, then abstract model, then concrete implementation; choose the right article type; avoid API-first writing.
---

# TwoEgg Article Method

Use this skill when the task is to draft, revise, outline, review, or extend articles for this repository.

## What to read first

Before writing, read these repo documents:

- [Article Method](../../../docs/article-writing-method.md)
- [Article Outline Template](../../../docs/article-outline-template.md)
- [Series Planning Method](../../../docs/series-planning-method.md)
- [Article Production Workflow](../../../docs/article-production-workflow.md)

Treat `docs/article-writing-method.md` as the single source of truth for writing rules.

## Workflow

1. Identify the article type first:
   - principle
   - mapping
   - case
   - index
   - if the task is series-level planning, use `docs/series-planning-method.md`
2. Build the outline from `docs/article-outline-template.md`.
3. For most technical articles, use the default progression:
   - problem space
   - abstract model
   - concrete implementation
   - engineering boundaries / tradeoffs
4. Lead with the real project problem, not with engine APIs.
5. When the topic is not inherently engine-specific, write the engine-agnostic model first, then land it in Unity / Unreal / self-built terms.
6. Keep the article focused on boundary clarity:
   - what this system is
   - what it is not
   - what it should not swallow
7. End with one short takeaway sentence that compresses the article's main claim.
8. When the task is a full article push, follow `docs/article-production-workflow.md` rather than jumping straight from idea to final draft.

## Hard rules

- Do not start by listing APIs, package names, or Inspector settings unless the article is explicitly a narrow tool/configuration article.
- Do not skip the abstract model layer.
- Do not stop at abstract concepts; land the model back into concrete engine or project implementation.
- Do not duplicate the full repo method inside this skill. If the method changes, update `docs/article-writing-method.md` first.

## When to switch article type

- Use the principle structure for most architecture or system articles.
- Use the mapping structure when comparing Unity / Unreal / GAS / self-built implementations.
- Use the case structure for diagnosis, incident review, and root-cause articles.
- Use the index structure for series map and reading-order articles.
