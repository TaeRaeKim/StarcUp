import React from 'react'
import { Eye, EyeOff, Palette, Construction, X, Users } from 'lucide-react'

interface OverlaySettings {
  showWorkerStatus: boolean
  showBuildOrder: boolean
  showUnitCount: boolean
  showUpgradeProgress: boolean
  showPopulationWarning: boolean
  opacity: number
  unitIconStyle: 'default' | 'white' | 'yellow' | 'hd'
  upgradeIconStyle: 'default' | 'white' | 'yellow'
  teamColor: string
}

interface OverlaySettingsPanelProps {
  isOpen: boolean
  onClose: () => void
  settings: OverlaySettings
  onSettingsChange: (settings: OverlaySettings) => void
}

// 간단한 토글 스위치 컴포넌트
function Switch({ checked, onCheckedChange, disabled = false }: { 
  checked: boolean
  onCheckedChange: (checked: boolean) => void
  disabled?: boolean
}) {
  return (
    <button
      onClick={() => !disabled && onCheckedChange(!checked)}
      disabled={disabled}
      style={{
        width: '44px',
        height: '24px',
        borderRadius: '12px',
        border: 'none',
        cursor: disabled ? 'not-allowed' : 'pointer',
        backgroundColor: disabled 
          ? 'rgba(128, 128, 128, 0.3)' 
          : checked 
            ? '#0099ff' 
            : 'rgba(255, 255, 255, 0.2)',
        position: 'relative',
        transition: 'all 0.2s ease',
        opacity: disabled ? 0.5 : 1
      }}
    >
      <div
        style={{
          width: '18px',
          height: '18px',
          borderRadius: '50%',
          backgroundColor: '#ffffff',
          position: 'absolute',
          top: '3px',
          left: checked ? '23px' : '3px',
          transition: 'all 0.2s ease',
          boxShadow: '0 2px 4px rgba(0, 0, 0, 0.2)'
        }}
      />
    </button>
  )
}

// 라디오 그룹 컴포넌트
function RadioGroup({ value, onValueChange, children }: {
  value: string
  onValueChange: (value: string) => void
  children: React.ReactNode
}) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
      {children}
    </div>
  )
}

// 라디오 버튼 아이템 컴포넌트
function RadioGroupItem({ value, id, checked, onChange }: {
  value: string
  id: string
  checked: boolean
  onChange: (value: string) => void
}) {
  return (
    <input
      type="radio"
      id={id}
      value={value}
      checked={checked}
      onChange={(e) => onChange(e.target.value)}
      style={{
        width: '16px',
        height: '16px',
        accentColor: '#0099ff'
      }}
    />
  )
}

// 라벨 컴포넌트
function Label({ htmlFor, children, style }: {
  htmlFor: string
  children: React.ReactNode
  style?: React.CSSProperties
}) {
  return (
    <label
      htmlFor={htmlFor}
      style={{
        fontSize: '12px',
        color: '#ffffff',
        cursor: 'pointer',
        ...style
      }}
    >
      {children}
    </label>
  )
}

// 팀 컬러 데이터
const TEAM_COLORS = [
  { hex: '#F40404', name: 'Red' },
  { hex: '#0C48CC', name: 'Blue' },
  { hex: '#2CB494', name: 'Teal' },
  { hex: '#88409C', name: 'Purple' },
  { hex: '#F88C14', name: 'Orange' },
  { hex: '#703014', name: 'Brown' },
  { hex: '#CCE0D0', name: 'White' },
  { hex: '#FCFC38', name: 'Yellow' }
]

// 슬라이더 컴포넌트
function Slider({ value, onValueChange, min = 0, max = 100, step = 1 }: {
  value: number[]
  onValueChange: (value: number[]) => void
  min?: number
  max?: number
  step?: number
}) {
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onValueChange([parseInt(e.target.value)])
  }

  return (
    <div style={{ position: 'relative', width: '100%' }}>
      <input
        type="range"
        min={min}
        max={max}
        step={step}
        value={value[0]}
        onChange={handleChange}
        style={{
          width: '100%',
          height: '6px',
          borderRadius: '3px',
          background: `linear-gradient(to right, #0099ff 0%, #0099ff ${((value[0] - min) / (max - min)) * 100}%, rgba(255, 255, 255, 0.2) ${((value[0] - min) / (max - min)) * 100}%, rgba(255, 255, 255, 0.2) 100%)`,
          outline: 'none',
          cursor: 'pointer',
          appearance: 'none',
          WebkitAppearance: 'none'
        }}
      />
      <style>
        {`
          input[type="range"]::-webkit-slider-thumb {
            appearance: none;
            width: 18px;
            height: 18px;
            border-radius: 50%;
            background: #ffffff;
            cursor: pointer;
            border: 2px solid #0099ff;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
          }
          input[type="range"]::-moz-range-thumb {
            width: 18px;
            height: 18px;
            border-radius: 50%;
            background: #ffffff;
            cursor: pointer;
            border: 2px solid #0099ff;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
          }
        `}
      </style>
    </div>
  )
}

export function OverlaySettingsPanel({ isOpen, onClose, settings, onSettingsChange }: OverlaySettingsPanelProps) {
  if (!isOpen) return null

  const handleSettingChange = async (key: keyof OverlaySettings, value: boolean | number | string) => {
    onSettingsChange({
      ...settings,
      [key]: value
    })

    // 기능 On/Off 설정이 변경될 때 presetAPI를 통해 Main 페이지와 동기화
    if (key.startsWith('show') && typeof value === 'boolean') {
      try {
        const featureIndex = getFeatureIndexFromKey(key)
        if (featureIndex !== -1 && window.presetAPI?.toggleFeature) {
          console.log(`🔄 [Overlay] ${key} 변경 → Main 프리셋과 동기화:`, featureIndex, value)
          await window.presetAPI.toggleFeature(featureIndex, value)
        }
      } catch (error) {
        console.error('❌ [Overlay] 프리셋 동기화 실패:', error)
      }
    }
  }

  // 설정 키를 기능 인덱스로 변환하는 함수
  const getFeatureIndexFromKey = (key: keyof OverlaySettings): number => {
    switch (key) {
      case 'showWorkerStatus': return 0        // 일꾼 기능
      case 'showPopulationWarning': return 1   // 인구수 기능
      case 'showUnitCount': return 2           // 유닛 기능
      case 'showUpgradeProgress': return 3     // 업그레이드 기능
      case 'showBuildOrder': return 4          // 빌드오더 기능
      default: return -1
    }
  }

  return (
    <div 
      style={{
        position: 'absolute',
        top: 0,
        left: 0,
        width: '100%',
        height: '100%',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundColor: 'rgba(0, 0, 0, 0.5)',
        backdropFilter: 'blur(4px)',
        zIndex: 10000,
        pointerEvents: 'auto'
      }}
      onClick={(e) => {
        if (e.target === e.currentTarget) onClose()
      }}
    >
      <div 
        style={{
          position: 'relative',
          borderRadius: '8px',
          boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)',
          maxWidth: '400px',
          width: '90%',
          maxHeight: '80vh',
          overflowY: 'auto',
          backgroundColor: 'rgba(0, 0, 0, 0.95)',
          border: '1px solid rgba(255, 255, 255, 0.1)',
          color: '#ffffff'
        }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* 헤더 */}
        <div 
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '20px 24px',
            borderBottom: '1px solid rgba(255, 255, 255, 0.1)'
          }}
        >
          <h2 style={{
            fontSize: '18px',
            fontWeight: '600',
            color: '#ffffff',
            margin: 0
          }}>
            오버레이 설정
          </h2>
          <button
            onClick={onClose}
            style={{
              width: '32px',
              height: '32px',
              borderRadius: '4px',
              border: 'none',
              backgroundColor: 'transparent',
              color: 'rgba(255, 255, 255, 0.7)',
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontSize: '18px',
              transition: 'all 0.2s ease'
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.backgroundColor = 'rgba(255, 255, 255, 0.1)'
              e.currentTarget.style.color = '#ffffff'
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.backgroundColor = 'transparent'
              e.currentTarget.style.color = 'rgba(255, 255, 255, 0.7)'
            }}
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        {/* 설정 내용 */}
        <div style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '24px' }}>
          {/* 표시 항목 */}
          <div>
            <h3 style={{
              fontSize: '14px',
              fontWeight: '600',
              color: '#ffffff',
              marginBottom: '16px',
              margin: 0
            }}>
              표시 항목
            </h3>
            
            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <span style={{ fontSize: '14px', color: '#ffffff' }}>
                    일꾼 상태
                  </span>
                  {settings.showWorkerStatus ? 
                    <Eye className="w-4 h-4" style={{ color: '#00ff88' }} /> : 
                    <EyeOff className="w-4 h-4" style={{ color: 'rgba(255, 255, 255, 0.4)' }} />
                  }
                </div>
                <Switch
                  checked={settings.showWorkerStatus}
                  onCheckedChange={async (checked) => await handleSettingChange('showWorkerStatus', checked)}
                />
              </div>

              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <span style={{ fontSize: '14px', color: '#ffffff' }}>
                    인구 경고
                  </span>
                  {settings.showPopulationWarning ? 
                    <Users className="w-4 h-4" style={{ color: '#00ff88' }} /> : 
                    <Users className="w-4 h-4" style={{ color: 'rgba(255, 255, 255, 0.4)' }} />
                  }
                </div>
                <Switch
                  checked={settings.showPopulationWarning}
                  onCheckedChange={async (checked) => await handleSettingChange('showPopulationWarning', checked)}
                />
              </div>

              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <span style={{ fontSize: '14px', color: 'rgba(255, 255, 255, 0.5)' }}>
                    빌드 오더
                  </span>
                  <Construction className="w-4 h-4" style={{ color: 'rgba(255, 255, 255, 0.3)' }} />
                </div>
                <Switch
                  checked={settings.showBuildOrder}
                  onCheckedChange={async (checked) => await handleSettingChange('showBuildOrder', checked)}
                  disabled={true}
                />
              </div>

              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <span style={{ fontSize: '14px', color: 'rgba(255, 255, 255, 0.5)' }}>
                    유닛 수
                  </span>
                  <Construction className="w-4 h-4" style={{ color: 'rgba(255, 255, 255, 0.3)' }} />
                </div>
                <Switch
                  checked={settings.showUnitCount}
                  onCheckedChange={async (checked) => await handleSettingChange('showUnitCount', checked)}
                  disabled={true}
                />
              </div>

              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <span style={{ fontSize: '14px', color: '#ffffff' }}>
                    업그레이드 진행도
                  </span>
                  {settings.showUpgradeProgress ? 
                    <Eye className="w-4 h-4" style={{ color: '#00ff88' }} /> : 
                    <EyeOff className="w-4 h-4" style={{ color: 'rgba(255, 255, 255, 0.4)' }} />
                  }
                </div>
                <Switch
                  checked={settings.showUpgradeProgress}
                  onCheckedChange={async (checked) => await handleSettingChange('showUpgradeProgress', checked)}
                />
              </div>
            </div>
          </div>

          {/* 아이콘 설정 */}
          <div>
            <h3 style={{
              fontSize: '14px',
              fontWeight: '600',
              color: '#ffffff',
              marginBottom: '16px',
              margin: '0 0 16px 0'
            }}>
              아이콘 설정
            </h3>
            
            <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
              {/* 유닛 아이콘 설정 */}
              <div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '12px' }}>
                  <Palette className="w-4 h-4" style={{ color: '#0099ff' }} />
                  <span style={{
                    fontSize: '13px',
                    fontWeight: '500',
                    color: '#ffffff'
                  }}>
                    유닛 아이콘
                  </span>
                </div>
                <RadioGroup
                  value={settings.unitIconStyle}
                  onValueChange={(value) => handleSettingChange('unitIconStyle', value)}
                >
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <RadioGroupItem 
                      value="default" 
                      id="unit-default" 
                      checked={settings.unitIconStyle === 'default'}
                      onChange={(value) => handleSettingChange('unitIconStyle', value)}
                    />
                    <Label htmlFor="unit-default">Default</Label>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <RadioGroupItem 
                      value="white" 
                      id="unit-white" 
                      checked={settings.unitIconStyle === 'white'}
                      onChange={(value) => handleSettingChange('unitIconStyle', value)}
                    />
                    <Label htmlFor="unit-white">White</Label>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <RadioGroupItem 
                      value="yellow" 
                      id="unit-yellow" 
                      checked={settings.unitIconStyle === 'yellow'}
                      onChange={(value) => handleSettingChange('unitIconStyle', value)}
                    />
                    <Label htmlFor="unit-yellow">Warm Yellow</Label>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <RadioGroupItem 
                      value="hd" 
                      id="unit-hd" 
                      checked={settings.unitIconStyle === 'hd'}
                      onChange={(value) => handleSettingChange('unitIconStyle', value)}
                    />
                    <Label htmlFor="unit-hd">HD (Team Color)</Label>
                  </div>
                </RadioGroup>
              </div>

              {/* HD 모드 팀 컬러 선택 */}
              {settings.unitIconStyle === 'hd' && (
                <div>
                  <span style={{
                    fontSize: '13px',
                    fontWeight: '500',
                    color: '#ffffff',
                    display: 'block',
                    marginBottom: '12px'
                  }}>
                    팀 컬러 선택
                  </span>
                  <div style={{ 
                    display: 'grid', 
                    gridTemplateColumns: 'repeat(4, 1fr)', 
                    gap: '8px' 
                  }}>
                    {TEAM_COLORS.map((color) => (
                      <button
                        key={color.hex}
                        onClick={() => handleSettingChange('teamColor', color.hex)}
                        style={{
                          width: '32px',
                          height: '32px',
                          borderRadius: '4px',
                          border: `2px solid ${settings.teamColor === color.hex ? '#0099ff' : 'rgba(255, 255, 255, 0.2)'}`,
                          backgroundColor: color.hex,
                          cursor: 'pointer',
                          transition: 'all 0.2s ease',
                          boxShadow: settings.teamColor === color.hex ? '0 0 8px rgba(0, 153, 255, 0.4)' : 'none',
                          transform: settings.teamColor === color.hex ? 'scale(1.1)' : 'scale(1)'
                        }}
                        title={color.name}
                        onMouseEnter={(e) => {
                          if (settings.teamColor !== color.hex) {
                            e.currentTarget.style.transform = 'scale(1.05)'
                          }
                        }}
                        onMouseLeave={(e) => {
                          e.currentTarget.style.transform = settings.teamColor === color.hex ? 'scale(1.1)' : 'scale(1)'
                        }}
                      />
                    ))}
                  </div>
                  <p style={{
                    fontSize: '11px',
                    color: 'rgba(255, 255, 255, 0.5)',
                    lineHeight: '1.4',
                    marginTop: '8px',
                    margin: '8px 0 0 0'
                  }}>
                    선택한 색상: {TEAM_COLORS.find(c => c.hex === settings.teamColor)?.name || 'Unknown'}
                  </p>
                </div>
              )}

              {/* 업그레이드 아이콘 설정 */}
              <div>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '12px' }}>
                  <Palette className="w-4 h-4" style={{ color: '#ff8800' }} />
                  <span style={{
                    fontSize: '13px',
                    fontWeight: '500',
                    color: '#ffffff'
                  }}>
                    업그레이드 아이콘
                  </span>
                </div>
                <RadioGroup
                  value={settings.upgradeIconStyle}
                  onValueChange={(value) => handleSettingChange('upgradeIconStyle', value)}
                >
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <RadioGroupItem 
                      value="default" 
                      id="upgrade-default" 
                      checked={settings.upgradeIconStyle === 'default'}
                      onChange={(value) => handleSettingChange('upgradeIconStyle', value)}
                    />
                    <Label htmlFor="upgrade-default">Default</Label>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <RadioGroupItem 
                      value="white" 
                      id="upgrade-white" 
                      checked={settings.upgradeIconStyle === 'white'}
                      onChange={(value) => handleSettingChange('upgradeIconStyle', value)}
                    />
                    <Label htmlFor="upgrade-white">White</Label>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <RadioGroupItem 
                      value="yellow" 
                      id="upgrade-yellow" 
                      checked={settings.upgradeIconStyle === 'yellow'}
                      onChange={(value) => handleSettingChange('upgradeIconStyle', value)}
                    />
                    <Label htmlFor="upgrade-yellow">Warm Yellow</Label>
                  </div>
                </RadioGroup>
              </div>
            </div>
          </div>

          {/* 투명도 설정 */}
          <div>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '12px' }}>
              <h3 style={{
                fontSize: '14px',
                fontWeight: '600',
                color: '#ffffff',
                margin: 0
              }}>
                오버레이 투명도
              </h3>
              <span style={{
                fontSize: '12px',
                color: 'rgba(255, 255, 255, 0.7)'
              }}>
                {settings.opacity}%
              </span>
            </div>
            <Slider
              value={[settings.opacity]}
              onValueChange={(value) => handleSettingChange('opacity', value[0])}
              max={100}
              min={10}
              step={5}
            />
            <p style={{
              fontSize: '10px',
              color: 'rgba(255, 255, 255, 0.5)',
              lineHeight: '1.4',
              marginTop: '4px',
              margin: '4px 0 0 0'
            }}>
              오버레이 컴포넌트들의 투명도를 조절합니다.
            </p>
          </div>

          {/* 도움말 */}
          <div style={{
            padding: '12px',
            borderRadius: '6px',
            backgroundColor: 'rgba(255, 255, 255, 0.05)',
            border: '1px solid rgba(255, 255, 255, 0.1)'
          }}>
            <p style={{
              fontSize: '12px',
              color: 'rgba(255, 255, 255, 0.7)',
              lineHeight: '1.4',
              margin: 0
            }}>
              <strong style={{ color: '#ffffff' }}>편집 모드:</strong> Ctrl + Tab 키를 눌러 오버레이 위치를 드래그로 조정할 수 있습니다.
              <br /><br />
              <strong style={{ color: '#ffffff' }}>🚧 개발 중:</strong> 빌드 오더, 유닛 수 기능은 현재 개발 중입니다.
              <br /><br />
              <strong style={{ color: '#ffffff' }}>아이콘 효과:</strong>
              <br />• <strong>White:</strong> 선명한 흰색 효과 (그레이스케일 + 밝기 증가)
              <br />• <strong>Warm Yellow:</strong> 따뜻한 황금빛 효과
              <br />• <strong>HD (Team Color):</strong> 실시간 팀 컬러 합성 (8가지 색상)
            </p>
          </div>
        </div>

        {/* 푸터 */}
        <div style={{
          display: 'flex',
          justifyContent: 'flex-end',
          gap: '8px',
          padding: '20px 24px',
          borderTop: '1px solid rgba(255, 255, 255, 0.1)'
        }}>
          <button
            onClick={onClose}
            style={{
              padding: '8px 16px',
              borderRadius: '6px',
              border: '1px solid rgba(255, 255, 255, 0.2)',
              backgroundColor: 'transparent',
              color: '#ffffff',
              cursor: 'pointer',
              fontSize: '14px',
              transition: 'all 0.2s ease'
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.backgroundColor = 'rgba(255, 255, 255, 0.1)'
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.backgroundColor = 'transparent'
            }}
          >
            닫기
          </button>
        </div>
      </div>
    </div>
  )
}

export type { OverlaySettings }