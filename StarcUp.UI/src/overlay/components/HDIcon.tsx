import React, { useRef, useEffect, useState } from 'react'

interface HDIconProps {
  diffuseSrc: string
  teamColorSrc: string
  teamColor: string
  width: number
  height: number
  alt?: string
  style?: React.CSSProperties
}

// 팀 컬러 hex 값을 RGB로 변환
const hexToRgb = (hex: string): [number, number, number] => {
  const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex)
  return result 
    ? [parseInt(result[1], 16), parseInt(result[2], 16), parseInt(result[3], 16)]
    : [0, 153, 255] // 기본값 파란색
}

export const HDIcon: React.FC<HDIconProps> = ({ 
  diffuseSrc, 
  teamColorSrc, 
  teamColor, 
  width, 
  height, 
  alt = '', 
  style = {} 
}) => {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return

    const ctx = canvas.getContext('2d')
    if (!ctx) return

    setIsLoading(true)

    // 이미지 로드 및 합성
    const loadAndComposite = async () => {
      try {
        // 두 이미지를 병렬로 로드
        const [diffuseImg, teamColorImg] = await Promise.all([
          loadImage(diffuseSrc),
          loadImage(teamColorSrc)
        ])

        // Canvas 크기 설정
        canvas.width = width
        canvas.height = height

        // 1. diffuse 이미지 그리기
        ctx.drawImage(diffuseImg, 0, 0, width, height)

        // 2. teamcolor 이미지를 팀 컬러로 착색해서 합성
        const tempCanvas = document.createElement('canvas')
        const tempCtx = tempCanvas.getContext('2d')
        if (!tempCtx) return

        tempCanvas.width = width
        tempCanvas.height = height

        // teamcolor 이미지를 임시 캔버스에 그리기
        tempCtx.drawImage(teamColorImg, 0, 0, width, height)

        // 팀 컬러 적용
        const imageData = tempCtx.getImageData(0, 0, width, height)
        const data = imageData.data
        const [r, g, b] = hexToRgb(teamColor)

        // 각 픽셀에 팀 컬러 적용 (흰색 영역을 팀 컬러로 변환)
        for (let i = 0; i < data.length; i += 4) {
          const alpha = data[i + 3]
          if (alpha > 0) {
            // 흰색 강도를 기반으로 팀 컬러 적용
            const intensity = (data[i] + data[i + 1] + data[i + 2]) / (3 * 255)
            data[i] = r * intensity     // Red
            data[i + 1] = g * intensity // Green  
            data[i + 2] = b * intensity // Blue
            // Alpha는 그대로 유지
          }
        }

        tempCtx.putImageData(imageData, 0, 0)

        // 3. 착색된 teamcolor 이미지를 메인 캔버스에 multiply 블렌딩으로 합성
        ctx.globalCompositeOperation = 'multiply'
        ctx.drawImage(tempCanvas, 0, 0)

        // 4. 블렌딩 모드 리셋
        ctx.globalCompositeOperation = 'source-over'

        setIsLoading(false)
      } catch (error) {
        console.error('HD 아이콘 합성 실패:', error)
        setIsLoading(false)
      }
    }

    loadAndComposite()
  }, [diffuseSrc, teamColorSrc, teamColor, width, height])

  return (
    <canvas
      ref={canvasRef}
      style={{
        imageRendering: 'pixelated',
        display: isLoading ? 'none' : 'block',
        ...style
      }}
      alt={alt}
    />
  )
}

// 이미지 로드 헬퍼 함수
const loadImage = (src: string): Promise<HTMLImageElement> => {
  return new Promise((resolve, reject) => {
    const img = new Image()
    img.onload = () => resolve(img)
    img.onerror = reject
    img.src = src
  })
}