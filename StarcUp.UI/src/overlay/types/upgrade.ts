import { UpgradeItemType } from '../../types/preset/PresetSettings';
import { UpgradeType, TechType } from '../../types/game';

// 업그레이드 아이템 정보
export interface UpgradeItemData {
  item: {
    type: UpgradeItemType;
    value: UpgradeType | TechType;
  };
  level: number;
  remainingFrames: number;
  currentUpgradeLevel: number;
}

// 업그레이드 카테고리
export interface UpgradeCategory {
  id: string;
  name: string;
  items: UpgradeItemData[];
}

// 업그레이드 진행 상태 이벤트
export interface UpgradeProgressData {
  categories: UpgradeCategory[];
}

// 업그레이드 취소 이벤트
export interface UpgradeCancelData {
  item: {
    type: UpgradeItemType;
    value: UpgradeType | TechType;
  };
  lastUpgradeItemData: UpgradeItemData;
  categoryId: string;
  categoryName: string;
}

// 업그레이드 완료 이벤트
export interface UpgradeCompleteData {
  item: {
    type: UpgradeItemType;
    value: UpgradeType | TechType;
  };
  level: number;
  categoryId: string;
  categoryName: string;
  timestamp: string;
}

// 업그레이드 표시 상태
export type UpgradeDisplayStatus = 'inactive' | 'progress' | 'completed';

// 시간 변환 함수
export const framesToTimeString = (frames: number): string => {
  if (frames <= 0) return "0:00";
  
  const seconds = Math.ceil(frames / 24); // 스타크래프트는 24 FPS
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  
  return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
};

// 진행률 계산 함수
export const calculateProgress = (remainingFrames: number, totalFrames: number): number => {
  if (totalFrames <= 0) return 0;
  const progress = Math.round(((totalFrames - remainingFrames) / totalFrames) * 100);
  return Math.max(0, Math.min(100, progress));
};