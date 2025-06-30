# 팀 컬러 변경 시스템 구현 가이드

## 개요
팀 컬러 변경 시스템은 **Diffuse 이미지**와 **Team Color 마스크**를 조합하여 실시간으로 팀 컬러를 변경하는 기능입니다. 이 문서는 `team_color_changer.html`의 핵심 구현 방법을 설명합니다.

## 1. Diffuse와 Team Color를 이용한 이미지 합성

### 1.1 기본 원리
- **Diffuse 이미지**: 유닛의 기본 텍스처 (색상 정보 포함)
- **Team Color 마스크**: 팀 컬러가 적용될 영역을 정의하는 마스크 (밝기 정보)
- **합성 과정**: 마스크의 밝기에 따라 원본 색상과 팀 컬러를 블렌딩

### 1.2 핵심 알고리즘

```javascript
// 픽셀별 처리 알고리즘
for (let i = 0; i < diffuseData.data.length; i += 4) {
    const maskIntensity = maskData.data[i] / 255; // 마스크의 밝기 (0-1)
    
    if (maskIntensity > 0.1) { // 마스크가 있는 부분
        // 원본 색상 추출
        const originalR = diffuseData.data[i];
        const originalG = diffuseData.data[i + 1];
        const originalB = diffuseData.data[i + 2];
        
        // 팀 컬러를 원본의 명도에 따라 조정
        const brightness = (originalR + originalG + originalB) / (3 * 255);
        
        // 블렌딩 공식: 팀컬러 * 명도 * 마스크강도 + 원본 * (1 - 마스크강도)
        resultData.data[i] = teamColor.r * brightness * maskIntensity + originalR * (1 - maskIntensity);
        resultData.data[i + 1] = teamColor.g * brightness * maskIntensity + originalG * (1 - maskIntensity);
        resultData.data[i + 2] = teamColor.b * brightness * maskIntensity + originalB * (1 - maskIntensity);
        resultData.data[i + 3] = diffuseData.data[i + 3]; // 알파 채널 유지
    } else {
        // 마스크가 없는 부분은 원본 그대로 유지
        resultData.data[i] = diffuseData.data[i];
        resultData.data[i + 1] = diffuseData.data[i + 1];
        resultData.data[i + 2] = diffuseData.data[i + 2];
        resultData.data[i + 3] = diffuseData.data[i + 3];
    }
}
```

### 1.3 블렌딩 특징
- **명도 보존**: 원본 이미지의 밝기 정보를 유지하여 자연스러운 색상 변환
- **마스크 강도**: 마스크의 밝기에 따라 팀 컬러 적용 강도 조절
- **알파 채널 보존**: 투명도 정보 유지로 완벽한 합성 효과

## 2. 여백 제거 방법 (동적 캔버스 크기 조절)

### 2.1 여백 제거의 필요성
- 메모리 효율성 향상
- 파일 크기 최적화
- 게임 내 렌더링 성능 개선

### 2.2 완전한 여백 제거 알고리즘

```javascript
// 투명하지 않은 픽셀의 정확한 경계 찾기
applyCropToCanvas(sourceCtx, targetCtx) {
    const sourceCanvas = sourceCtx.canvas;
    const imageData = sourceCtx.getImageData(0, 0, sourceCanvas.width, sourceCanvas.height);
    const data = imageData.data;
    
    let minX = sourceCanvas.width;
    let minY = sourceCanvas.height;
    let maxX = -1;
    let maxY = -1;
    
    // 의미있는 픽셀 임계값 (투명도 10% 이상)
    const alphaThreshold = 25;
    
    // 모든 픽셀을 검사하여 경계 찾기
    for (let y = 0; y < sourceCanvas.height; y++) {
        for (let x = 0; x < sourceCanvas.width; x++) {
            const index = (y * sourceCanvas.width + x) * 4;
            const alpha = data[index + 3];
            
            if (alpha > alphaThreshold) {
                minX = Math.min(minX, x);
                minY = Math.min(minY, y);
                maxX = Math.max(maxX, x);
                maxY = Math.max(maxY, y);
            }
        }
    }
    
    // 유효한 객체가 있는 경우에만 크롭
    if (maxX >= minX && maxY >= minY) {
        const finalWidth = maxX - minX + 1;
        const finalHeight = maxY - minY + 1;
        
        // 정확한 경계로 크롭된 영역 추출
        const croppedImageData = sourceCtx.getImageData(minX, minY, finalWidth, finalHeight);
        
        // 결과 캔버스 크기를 정확한 객체 크기로 변경
        const targetCanvas = targetCtx.canvas;
        targetCanvas.width = finalWidth;
        targetCanvas.height = finalHeight;
        
        // 캔버스 클리어 후 크롭된 이미지 그리기
        targetCtx.clearRect(0, 0, finalWidth, finalHeight);
        targetCtx.putImageData(croppedImageData, 0, 0);
    }
}
```

### 2.3 여백 제거 특징
- **픽셀 단위 정밀도**: 모든 픽셀을 검사하여 정확한 경계 계산
- **투명도 임계값**: 의미있는 픽셀만 고려하여 노이즈 제거
- **동적 크기 조절**: 결과 캔버스가 객체 크기에 정확히 맞춰짐
- **메모리 최적화**: 불필요한 여백 제거로 공간 절약

## 3. 구현 시 고려사항

### 3.1 성능 최적화
- **원본 크기 유지**: 중간 처리 과정에서 원본 해상도 유지
- **임시 캔버스 활용**: 결과 캔버스 크기 변경 전 임시 캔버스에서 처리
- **메모리 관리**: 불필요한 ImageData 객체 생성 최소화

### 3.2 색상 처리
- **RGB 분리**: 각 색상 채널을 개별적으로 처리
- **명도 계산**: (R + G + B) / (3 * 255) 공식으로 명도 계산
- **블렌딩 공식**: 선형 보간법을 사용한 자연스러운 색상 혼합

### 3.3 UI/UX 개선
- **실시간 미리보기**: 색상 변경 시 즉시 결과 확인
- **크기 옵션**: 32x32, 48x48, 64x64 등 게임 내 실제 크기 미리보기
- **토글 기능**: 여백 제거 활성화/비활성화 선택 가능

## 4. 활용 예제

### 4.1 기본 사용법
1. Diffuse 이미지와 Team Color 마스크 업로드
2. 원하는 팀 컬러 선택
3. 실시간으로 결과 확인
4. 동적 캔버스 크기 조절로 여백 제거
5. 최종 결과 이미지 다운로드

### 4.2 확장 가능성
- 다중 팀 컬러 지원
- 애니메이션 프레임 일괄 처리
- 배치 처리 시스템
- 다양한 블렌딩 모드 지원

## 5. 결론

이 팀 컬러 변경 시스템은 게임 개발에서 유닛 스킨 시스템이나 팀 구분 시스템을 구현할 때 매우 유용합니다. 특히 **완전한 여백 제거** 기능을 통해 메모리 효율성을 크게 향상시킬 수 있으며, **실시간 미리보기**를 통해 개발자가 즉시 결과를 확인할 수 있습니다.

핵심은 **Diffuse와 Team Color 마스크의 조합**을 통한 자연스러운 색상 블렌딩과, **동적 캔버스 크기 조절**을 통한 완벽한 여백 제거입니다.