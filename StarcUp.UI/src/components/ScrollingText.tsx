import { useState, useEffect } from "react";

interface ScrollingTextProps {
  tips: string[];
  className?: string;
}

export function ScrollingText({ tips, className = "" }: ScrollingTextProps) {
  const [currentTipIndex, setCurrentTipIndex] = useState(0);
  
  useEffect(() => {
    // 각 팁이 완전히 스크롤된 후 다음 팁으로 변경
    const interval = setInterval(() => {
      setCurrentTipIndex((prev) => (prev + 1) % tips.length);
    }, 20000); // 12초마다 팁 변경 (스크롤 시간 고려)

    return () => clearInterval(interval);
  }, [tips.length]);

  const currentTip = tips[currentTipIndex];

  return (
    <div className={`relative ${className} overflow-hidden rounded-none`}>
      
      {/* 뉴스 스타일 티커 */}
      <div 
        className="flex items-center h-8 relative"
        style={{ 
          backgroundColor: 'var(--starcraft-bg-secondary)',
          borderTop: '1px solid var(--starcraft-border)',
          borderBottom: '1px solid var(--starcraft-border)'
        }}
      >
        {/* 스크롤링 텍스트 영역 */}
        <div className="flex-1 relative overflow-hidden h-full">
          <div 
            key={currentTipIndex} // 키 변경으로 애니메이션 재시작
            className="absolute whitespace-nowrap text-sm py-1 px-4 scrolling-text-news"
            style={{ 
              color: 'var(--starcraft-green)'
            }}
          >
            {currentTip}
          </div>
          
          {/* 우측 페이드 효과 */}
          <div 
            className="absolute right-0 top-0 h-full w-8 pointer-events-none z-10"
            style={{
              background: `linear-gradient(to left, var(--starcraft-bg-secondary) 0%, transparent 100%)`
            }}
          ></div>
        </div>
      </div>
    </div>
  );
}