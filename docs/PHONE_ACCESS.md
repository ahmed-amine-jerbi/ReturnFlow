# Open ReturnFlow on your phone

The whole app is served from one port (8080) on your PC, so a phone only needs one address.

## 1. Same Wi-Fi (simplest, private)

1. Start the stack on your PC: `docker compose up --build`
2. Find your PC's local IP address:
   - Windows: `ipconfig`, look for **IPv4 Address** on your Wi-Fi adapter
   - macOS: `ipconfig getifaddr en0`
   - Linux: `hostname -I`
3. On your phone (same Wi-Fi), open `http://<that-ip>:8080`, for example `http://192.168.1.23:8080`.

If the page does not load:

- **Windows Firewall.** Allow port 8080 on private networks (PowerShell as administrator):
  `New-NetFirewallRule -DisplayName "ReturnFlow 8080" -Direction Inbound -Protocol TCP -LocalPort 8080 -Action Allow -Profile Private`
- Make sure Windows calls your Wi-Fi a **Private** network, not Public.
- Some routers have "client isolation" or a guest network that blocks devices from talking to each other. Use the normal network.
- Your PC's IP can change. In your router, reserve an address for the PC so the link stays the same.
- Test on the PC first: `http://localhost:8080` must work.

Over plain `http` on a local network the app works, but your phone browser will not offer to install it as an app and camera access is blocked. For those, use HTTPS (next section).

## 2. From anywhere, with HTTPS

Pick one:

- **Tailscale (private).** Install Tailscale on the PC and on the phone, sign in to the same account, then open `http://<pc-tailscale-ip>:8080` on the phone. Nobody else can reach it.
- **Cloudflare quick tunnel (public link, good for showing a recruiter).** Install `cloudflared`, then run
  `cloudflared tunnel --url http://localhost:8080`
  It prints a temporary `https://...trycloudflare.com` address. Stop the command and the link stops working. Check Cloudflare's current docs, since the free quick-tunnel terms can change.

## 3. Before you share a public link

- Demo mode shows one-tap logins and fake data. That is fine for a short demo, but the demo password is shown on the login page. Anyone with the link can sign in and create accounts.
- Copy `.env.example` to `.env` and set your own `JWT_KEY` and `DEMO_PASSWORD`.
- Set `DEMO_MODE=false` for anything longer-lived. You then get no fake data and no demo buttons, and users are only created if you set `DEMO_PASSWORD`.
- Stop the tunnel when you are done.

## 4. Keeping it running like a server

- Containers use `restart: unless-stopped`, and Docker Desktop can start with your PC.
- In your power settings, stop the PC from sleeping while you want it reachable.
- For a permanent link, deploy the same three containers to a free-tier host later. The compose file is the blueprint.

## Resetting the demo data

The database lives in a Docker volume. To start fresh with new demo data: `docker compose down -v` and then `docker compose up --build`.
