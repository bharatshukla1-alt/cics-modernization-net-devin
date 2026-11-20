import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// target.frontend.port = 5173, target.frontend.proxyPath = /api, target.backend.port = 5000
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': { target: process.env.API_PROXY_TARGET ?? 'http://localhost:5000', changeOrigin: true },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.js'],
    env: { VITE_AUTH_BYPASS: 'false' },
    coverage: {
      provider: 'v8',
      include: ['src/**/*.{js,jsx}'],
      exclude: ['src/main.jsx', 'src/test/**', 'src/**/*.test.{js,jsx}'],
      thresholds: { lines: 70, functions: 70, branches: 60, statements: 70 },
    },
  },
});
