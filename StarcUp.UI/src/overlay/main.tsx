import React from 'react'
import ReactDOM from 'react-dom/client'
import { OverlayApp } from './OverlayApp'
import './overlay.css'

const root = ReactDOM.createRoot(document.getElementById('root') as HTMLElement)

root.render(
  <React.StrictMode>
    <OverlayApp />
  </React.StrictMode>
)

// 개발 환경에서 디버그 정보
if (process.env.NODE_ENV === 'development') {
  console.log('🎮 StarcUp Overlay 시작됨')
}