# returnflow-web

Web and mobile interface for ReturnFlow. One responsive React app, installable on a phone as a PWA.

- **Customer:** file a return and claim, follow its progress, ask for a human review
- **Agent:** review queue with the AI's category, risk meter and reasons, approve or reject
- **Manager and admin:** KPI dashboard with D1, D7 and lifetime views, a recap, AI vs human charts and backlog
- **Languages:** English, French and German, with the selected language remembered in the browser

## Run

```bash
npm install
npm run dev        # http://localhost:5173, forwards /api to http://localhost:5080
npm run build
```

Set `API_URL` to point the dev proxy at another API. In Docker, nginx serves the build and forwards `/api` to the API container, so the app and API share one origin and need no CORS.

For a temporary Cloudflare Tunnel, use `npm run build` followed by `npm run preview`. The preview server allows `*.trycloudflare.com` hostnames for the tunnel; other unrecognized hosts remain blocked.

## Structure

`src/lib` API client, auth, formatting and shared claim labels. `src/components` UI kit, risk meter, AI insight card, claim timeline. `src/pages` one file per screen.

## License

MIT
