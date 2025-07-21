import { useCallback, useRef } from 'react'

export type EffectType = 'ProductionCompleted' | 'WorkerDied'

interface EffectConfig {
  duration: number  // 애니메이션 지속 시간 (ms)
  className: string // CSS 클래스명
}

const EFFECT_CONFIGS: Record<EffectType, EffectConfig> = {
  ProductionCompleted: {
    duration: 800,
    className: 'spawn-effect'
  },
  WorkerDied: {
    duration: 600,
    className: 'death-effect'
  }
}

export function useEffectSystem() {
  const timeoutRef = useRef<NodeJS.Timeout | null>(null)
  const currentElementRef = useRef<HTMLElement | null>(null)

  const triggerEffect = useCallback((element: HTMLElement | null, effectType: EffectType) => {
    if (!element) {
      console.warn('⚠️ 효과를 적용할 요소를 찾을 수 없습니다')
      return
    }

    const config = EFFECT_CONFIGS[effectType]
    if (!config) {
      console.warn(`⚠️ 알 수 없는 효과 타입: ${effectType}`)
      return
    }

    // 기존 타이머 정리
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current)
    }

    // 기존 효과 클래스들 모두 제거
    const allEffectClasses = Object.values(EFFECT_CONFIGS).map(c => c.className)
    element.classList.remove(...allEffectClasses)

    currentElementRef.current = element

    // CSS 리플로우를 위한 짧은 지연 후 새 효과 적용
    setTimeout(() => {
      if (currentElementRef.current) {
        currentElementRef.current.classList.add(config.className)
        
        console.log(`✨ 효과 적용: ${effectType} (${config.duration}ms)`)
        
        // 효과 완료 후 클래스 제거
        timeoutRef.current = setTimeout(() => {
          if (currentElementRef.current) {
            currentElementRef.current.classList.remove(config.className)
            console.log(`✅ 효과 완료: ${effectType}`)
          }
        }, config.duration)
      }
    }, 10)
  }, [])

  const clearEffect = useCallback(() => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current)
      timeoutRef.current = null
    }
    
    if (currentElementRef.current) {
      const allEffectClasses = Object.values(EFFECT_CONFIGS).map(c => c.className)
      currentElementRef.current.classList.remove(...allEffectClasses)
    }
  }, [])

  return {
    triggerEffect,
    clearEffect
  }
}