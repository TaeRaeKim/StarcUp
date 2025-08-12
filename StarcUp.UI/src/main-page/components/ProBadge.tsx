import { Crown, Zap } from 'lucide-react';

interface ProBadgeProps {
  variant?: 'default' | 'small' | 'icon';
  className?: string;
}

export function ProBadge({ variant = 'default', className = '' }: ProBadgeProps) {
  if (variant === 'icon') {
    return (
      <div
        className={`inline-flex items-center justify-center p-1 rounded transition-all duration-300 ${className}`}
        style={{
          background: 'linear-gradient(135deg, #ffd700 0%, #ffed4e 50%, #ffa000 100%)',
          boxShadow: '0 0 8px rgba(255, 215, 0, 0.4), inset 0 0 6px rgba(255, 255, 255, 0.3)',
          color: '#8b5a00'
        }}
        title="Pro 전용 기능"
      >
        <Crown className="w-3 h-3" />
      </div>
    );
  }

  if (variant === 'small') {
    return (
      <span
        className={`inline-flex items-center gap-1 px-2 py-0.5 text-xs font-bold rounded-full transition-all duration-300 ${className}`}
        style={{
          background: 'linear-gradient(135deg, #ffd700 0%, #ffed4e 50%, #ffa000 100%)',
          boxShadow: '0 0 6px rgba(255, 215, 0, 0.3), inset 0 0 4px rgba(255, 255, 255, 0.2)',
          color: '#8b5a00',
          textShadow: '0 1px 2px rgba(255, 255, 255, 0.3)'
        }}
      >
        <Crown className="w-3 h-3" />
        Pro
      </span>
    );
  }

  return (
    <span
      className={`inline-flex items-center gap-1.5 px-3 py-1 text-sm font-bold rounded-full transition-all duration-300 hover:scale-105 ${className}`}
      style={{
        background: 'linear-gradient(135deg, #ffd700 0%, #ffed4e 50%, #ffa000 100%)',
        boxShadow: '0 0 12px rgba(255, 215, 0, 0.4), inset 0 0 8px rgba(255, 255, 255, 0.3)',
        color: '#8b5a00',
        textShadow: '0 1px 2px rgba(255, 255, 255, 0.3)'
      }}
    >
      <Crown className="w-4 h-4" />
      Pro
      <Zap className="w-3 h-3" />
    </span>
  );
}