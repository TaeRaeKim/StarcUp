import { useState, useEffect } from "react";

interface TypingAnimationProps {
  tips: string[];
  className?: string;
}

export function TypingAnimation({ tips, className = "" }: TypingAnimationProps) {
  const [currentTipIndex, setCurrentTipIndex] = useState(0);
  const [displayedText, setDisplayedText] = useState("");
  const [isTyping, setIsTyping] = useState(true);
  const [isDeleting, setIsDeleting] = useState(false);

  useEffect(() => {
    const currentTip = tips[currentTipIndex];
    
    if (isTyping && !isDeleting) {
      if (displayedText.length < currentTip.length) {
        const timeout = setTimeout(() => {
          setDisplayedText(currentTip.slice(0, displayedText.length + 1));
        }, 50); // 타이핑 속도
        return () => clearTimeout(timeout);
      } else {
        // 타이핑 완료 후 7초 대기
        const timeout = setTimeout(() => {
          setIsDeleting(true);
          setIsTyping(false);
        }, 7000);
        return () => clearTimeout(timeout);
      }
    } else if (isDeleting) {
      if (displayedText.length > 0) {
        const timeout = setTimeout(() => {
          setDisplayedText(displayedText.slice(0, -1));
        }, 30); // 백스페이스 속도 (더 빠르게)
        return () => clearTimeout(timeout);
      } else {
        // 삭제 완료 후 다음 팁으로
        setIsDeleting(false);
        setIsTyping(true);
        setCurrentTipIndex((prev) => (prev + 1) % tips.length);
      }
    }
  }, [displayedText, isTyping, isDeleting, currentTipIndex, tips]);

  return (
    <div className={`relative ${className} min-h-[50px] flex items-center`}>
      <span className="text-[color:var(--starcraft-green)] text-sm leading-relaxed">
        {displayedText}
        {isTyping && (
          <span className="animate-pulse text-[color:var(--starcraft-green-bright)]">|</span>
        )}
      </span>
    </div>
  );
}