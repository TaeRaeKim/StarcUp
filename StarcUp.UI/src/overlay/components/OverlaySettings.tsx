import React from 'react'

interface OverlaySettings {
  showWorkerStatus: boolean
  showBuildOrder: boolean
  showUnitCount: boolean
  showUpgradeProgress: boolean
  showPopulationWarning: boolean
  opacity: number
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

  const handleSettingChange = (key: keyof OverlaySettings, value: boolean | number) => {
    onSettingsChange({
      ...settings,
      [key]: value
    })
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
            ✕
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
                  <span style={{ fontSize: '12px', color: '#00ff00' }}>
                    {settings.showWorkerStatus ? '👁️' : '🙈'}
                  </span>
                </div>
                <Switch
                  checked={settings.showWorkerStatus}
                  onCheckedChange={(checked) => handleSettingChange('showWorkerStatus', checked)}
                />
              </div>

              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <span style={{ fontSize: '14px', color: 'rgba(255, 255, 255, 0.5)' }}>
                    빌드 오더
                  </span>
                  <span style={{ fontSize: '12px', color: 'rgba(255, 255, 255, 0.5)' }}>
                    🚧
                  </span>
                </div>
                <Switch
                  checked={settings.showBuildOrder}
                  onCheckedChange={(checked) => handleSettingChange('showBuildOrder', checked)}
                  disabled={true}
                />
              </div>

              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <span style={{ fontSize: '14px', color: 'rgba(255, 255, 255, 0.5)' }}>
                    유닛 수
                  </span>
                  <span style={{ fontSize: '12px', color: 'rgba(255, 255, 255, 0.5)' }}>
                    🚧
                  </span>
                </div>
                <Switch
                  checked={settings.showUnitCount}
                  onCheckedChange={(checked) => handleSettingChange('showUnitCount', checked)}
                  disabled={true}
                />
              </div>

              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <span style={{ fontSize: '14px', color: 'rgba(255, 255, 255, 0.5)' }}>
                    업그레이드 진행도
                  </span>
                  <span style={{ fontSize: '12px', color: 'rgba(255, 255, 255, 0.5)' }}>
                    🚧
                  </span>
                </div>
                <Switch
                  checked={settings.showUpgradeProgress}
                  onCheckedChange={(checked) => handleSettingChange('showUpgradeProgress', checked)}
                  disabled={true}
                />
              </div>

              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <span style={{ fontSize: '14px', color: 'rgba(255, 255, 255, 0.5)' }}>
                    인구 경고
                  </span>
                  <span style={{ fontSize: '12px', color: 'rgba(255, 255, 255, 0.5)' }}>
                    🚧
                  </span>
                </div>
                <Switch
                  checked={settings.showPopulationWarning}
                  onCheckedChange={(checked) => handleSettingChange('showPopulationWarning', checked)}
                  disabled={true}
                />
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
              <strong style={{ color: '#ffffff' }}>편집 모드:</strong> Shift + Tab 키를 눌러 오버레이 위치를 드래그로 조정할 수 있습니다.
              <br /><br />
              <strong style={{ color: '#ffffff' }}>🚧 개발 중:</strong> 빌드 오더, 유닛 수, 업그레이드 진행도, 인구 경고 기능은 현재 개발 중입니다.
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