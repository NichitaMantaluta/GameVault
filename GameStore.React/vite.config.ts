import { dirname, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..')

export default defineConfig(({ mode }) => {
  const env = {
    ...loadEnv(mode, repoRoot, ''),
    ...loadEnv(mode, process.cwd(), ''),
  }
  const proxyTarget = env.VITE_API_PROXY_TARGET || 'http://localhost:5261'

  return {
    envDir: repoRoot,
    plugins: [react()],
    server: {
      proxy: {
        '/api': {
          target: proxyTarget,
          changeOrigin: true,
          secure: false,
          configure(proxy) {
            proxy.on('proxyReq', (proxyReq, req) => {
              const authorization = req.headers.authorization
              if (authorization) {
                proxyReq.setHeader('Authorization', authorization)
              }
            })
          },
        },
      },
    },
  }
})
