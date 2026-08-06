# Token/Session Edge Cases (Checklist)

## Token edge cases
- Wrong issuer → request denied/challenged
- Wrong audience/client id → request denied/challenged
- Expired token → re-auth triggered
- Token missing required fields → request denied safely

## Session edge cases
- Session expired mid-navigation → re-auth triggered, user lands back safely
- Logout while multiple tabs open → next request triggers re-auth
- Mixed auth state (token valid but session missing markers) → reconcile deterministically (choose a single rule and document it)

## Output artifact
Record the behavior decisions in `AUTH_VALIDATION.md`:
- Which edge cases were tested
- Expected behavior for each case