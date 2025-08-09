import { RaceType } from '../../types/enums'

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
        iconUrl: '/resources/Icon/Terran/Units/TerranSCV.png',
        diffuseUrl: '/resources/HD/Terran/Units/TerranSCV_diffuse.png',
        teamColorUrl: '/resources/HD/Terran/Units/TerranSCV_teamcolor.png'
      }
    
    case RaceType.Zerg:
      return {
        iconUrl: '/resources/Icon/Zerg/Units/ZergDrone.png',
        diffuseUrl: '/resources/HD/Zerg/Units/ZergDrone_diffuse.png',
        teamColorUrl: '/resources/HD/Zerg/Units/ZergDrone_teamcolor.png'
      }
    
    case RaceType.Protoss:
    default:
      return {
        iconUrl: '/resources/Icon/Protoss/Units/ProtossProbe.png',
        diffuseUrl: '/resources/HD/Protoss/Units/ProtossProbe_diffuse.png',
        teamColorUrl: '/resources/HD/Protoss/Units/ProtossProbe_teamcolor.png'
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