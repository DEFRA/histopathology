# Troubleshooting (Auth Configuration) — Template

| Symptom | Likely cause | What to check | Safe action |
|---|---|---|---|
| Redirect loop | Redirect URL mismatch or misrouted return URL | redirect URL config + app routing | correct redirect URL config |
| Access denied after login | Missing required claim or role mapping mismatch | canonical claims contract + mapping matrix | update mapping and re-test |
| Token rejected | Issuer/audience mismatch | authority/tenant + client id config | correct config and re-test |
| Logout doesn’t clear session | Session cleanup not wired | logout handler + session store | ensure session cleared, retry |

> Keep entries non-sensitive; do not paste tokens or claim dumps.