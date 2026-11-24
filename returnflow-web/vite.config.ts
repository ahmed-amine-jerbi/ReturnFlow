import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// In development the Vite server forwards /api to the .NET API, so the browser never needs CORS.
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    host: true,
    port: 5173,
    proxy: {
      '/api': { target: process.env.API_URL ?? 'http://localhost:5080', changeOrigin: true },
    },
  },
  preview: {
    host: true,
    allowedHosts: ['.trycloudflare.com'],
  },
})
