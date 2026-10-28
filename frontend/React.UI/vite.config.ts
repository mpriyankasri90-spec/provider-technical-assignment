import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// The UI calls the API with relative URLs (/api/...). In development, Vite forwards them to
// the ASP.NET Core API, so the browser sees one origin and no CORS setup is needed.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, '.', '')
  const apiTarget = env.API_PROXY_TARGET || 'http://localhost:5264'

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        '/api': { target: apiTarget, changeOrigin: true },
      },
    },
  }
})
