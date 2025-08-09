import React, { useRef, useEffect, useState } from 'react'

interface HDIconProps {
  diffuseSrc: string
  teamColorSrc: string
  teamColor: string
  width: number
  height: number
  alt?: string
  style?: React.CSSProperties
  scale?: number // 스케일 조정 (기본값: 1.0)
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

        // Canvas 크기를 원본 이미지 크기로 설정
        canvas.width = diffuseImg.width
        canvas.height = diffuseImg.height

        // 1. diffuse 이미지 그리기 (원본 크기)
        ctx.drawImage(diffuseImg, 0, 0)

        // 2. teamcolor 이미지를 팀 컬러로 착색해서 합성
        const tempCanvas = document.createElement('canvas')
        const tempCtx = tempCanvas.getContext('2d')
        if (!tempCtx) return

        tempCanvas.width = teamColorImg.width
        tempCanvas.height = teamColorImg.height

        // teamcolor 이미지를 임시 캔버스에 그리기 (원본 크기)
        tempCtx.drawImage(teamColorImg, 0, 0)

        // 팀 컬러 적용
        const imageData = tempCtx.getImageData(0, 0, tempCanvas.width, tempCanvas.height)
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

        // 5. 여백 제거 적용 (프로토타입과 동일한 방식)
        const croppedCanvas = applyCropToCanvas(ctx)
        
        // 6. 최종 크기 조정 - 크롭된 이미지를 원하는 크기로 조정
        const finalCanvas = document.createElement('canvas')
        const finalCtx = finalCanvas.getContext('2d')
        if (!finalCtx) return

        finalCanvas.width = width
        finalCanvas.height = height
        finalCtx.imageSmoothingEnabled = true
        finalCtx.imageSmoothingQuality = 'high'
        finalCtx.drawImage(croppedCanvas, 0, 0, width, height)

        // 메인 캔버스에 최종 결과 그리기
        canvas.width = width
        canvas.height = height
        ctx.clearRect(0, 0, width, height)
        ctx.drawImage(finalCanvas, 0, 0)

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
      title={alt}
    />
  )
}

// 완전한 여백 제거 알고리즘 (프로토타입에서 가져옴)
const applyCropToCanvas = (sourceCtx: CanvasRenderingContext2D): HTMLCanvasElement => {
  const sourceCanvas = sourceCtx.canvas
  const imageData = sourceCtx.getImageData(0, 0, sourceCanvas.width, sourceCanvas.height)
  const data = imageData.data
  
  let minX = sourceCanvas.width
  let minY = sourceCanvas.height
  let maxX = -1
  let maxY = -1
  
  // 의미있는 픽셀 임계값 (투명도 10% 이상)
  const alphaThreshold = 25
  
  // 모든 픽셀을 검사하여 경계 찾기
  for (let y = 0; y < sourceCanvas.height; y++) {
    for (let x = 0; x < sourceCanvas.width; x++) {
      const index = (y * sourceCanvas.width + x) * 4
      const alpha = data[index + 3]
      
      if (alpha > alphaThreshold) {
        minX = Math.min(minX, x)
        minY = Math.min(minY, y)
        maxX = Math.max(maxX, x)
        maxY = Math.max(maxY, y)
      }
    }
  }
  
  // 결과 캔버스 생성
  const resultCanvas = document.createElement('canvas')
  const resultCtx = resultCanvas.getContext('2d')
  
  if (!resultCtx) {
    return sourceCanvas // fallback
  }
  
  // 유효한 객체가 있는 경우에만 크롭
  if (maxX >= minX && maxY >= minY) {
    const finalWidth = maxX - minX + 1
    const finalHeight = maxY - minY + 1
    
    // 정확한 경계로 크롭된 영역 추출
    const croppedImageData = sourceCtx.getImageData(minX, minY, finalWidth, finalHeight)
    
    // 결과 캔버스 크기를 정확한 객체 크기로 변경
    resultCanvas.width = finalWidth
    resultCanvas.height = finalHeight
    
    // 캔버스 클리어 후 크롭된 이미지 그리기
    resultCtx.clearRect(0, 0, finalWidth, finalHeight)
    resultCtx.putImageData(croppedImageData, 0, 0)
    
    return resultCanvas
  }
  
  return sourceCanvas // fallback
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