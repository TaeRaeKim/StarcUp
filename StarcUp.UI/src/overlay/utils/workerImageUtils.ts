import { RaceType } from '../../types/enums'

// 정적 애셋 import - Vite의 정적 자산 import 방식
// Terran
import scvIconUrl from '/resources/Icon/Terran/Units/TerranSCV.png'
import scvDiffuseUrl from '/resources/HD/Terran/Units/TerranSCV_diffuse.png'
import scvTeamColorUrl from '/resources/HD/Terran/Units/TerranSCV_teamcolor.png'

// Zerg
import droneIconUrl from '/resources/Icon/Zerg/Units/ZergDrone.png'
import droneDiffuseUrl from '/resources/HD/Zerg/Units/ZergDrone_diffuse.png'
import droneTeamColorUrl from '/resources/HD/Zerg/Units/ZergDrone_teamcolor.png'

// Protoss
import probeIconUrl from '/resources/Icon/Protoss/Units/ProtossProbe.png'
import probeDiffuseUrl from '/resources/HD/Protoss/Units/ProtossProbe_diffuse.png'
import probeTeamColorUrl from '/resources/HD/Protoss/Units/ProtossProbe_teamcolor.png'

// 종족별 일꾼 이미지 경로
export interface WorkerImages {
  iconUrl: string
  diffuseUrl?: string
  teamColorUrl?: string
}

// 종족별 일꾼 이미지 매핑
export const getWorkerImagesByRace = (race: RaceType): WorkerImages => {
  switch (race) {
    case RaceType.Terran:
      return {
        iconUrl: scvIconUrl,
        diffuseUrl: scvDiffuseUrl,
        teamColorUrl: scvTeamColorUrl
      }
    
    case RaceType.Zerg:
      return {
        iconUrl: droneIconUrl,
        diffuseUrl: droneDiffuseUrl,
        teamColorUrl: droneTeamColorUrl
      }
    
    case RaceType.Protoss:
    default:
      return {
        iconUrl: probeIconUrl,
        diffuseUrl: probeDiffuseUrl,
        teamColorUrl: probeTeamColorUrl
      }
  }
}

// 일꾼 이름 가져오기
export const getWorkerNameByRace = (race: RaceType): string => {
  switch (race) {
    case RaceType.Terran:
      return 'SCV'
    case RaceType.Zerg:
      return 'Drone'
    case RaceType.Protoss:
    default:
      return 'Probe'
  }
}