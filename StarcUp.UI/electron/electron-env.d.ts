/// <reference types="vite-plugin-electron/electron-env" />

declare namespace NodeJS {
  interface ProcessEnv {
    /** /dist/ or /public/ */
    VITE_PUBLIC: string
  }
}

// Core API response type
interface ICoreResponse {
  success: boolean
  data?: any
  error?: string
}

// Window 인터페이스는 src/vite-env.d.ts에서 통합 관리됨
