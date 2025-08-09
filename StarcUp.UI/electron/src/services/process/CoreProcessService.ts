import { spawn, ChildProcess } from 'child_process'
import { app } from 'electron'
import * as path from 'path'
import * as fs from 'fs'
import { ICoreProcessService } from './interfaces'

export class CoreProcessService implements ICoreProcessService {
  private coreProcess: ChildProcess | null = null
  private isDevelopment = false

  /**
   * StarcUp.Core 프로세스 시작
   */
  async startCoreProcess(isDevelopment: boolean = false): Promise<void> {
    this.isDevelopment = isDevelopment
    
    console.log(`🚀 StarcUp.Core 시작 (${isDevelopment ? '개발' : '프로덕션'} 모드)`)

    try {
      // 프로덕션 모드에서만 프로세스 시작
      if (!this.isDevelopment) {
        await this.startCoreProcessInternal()
      }
      
      console.log('✅ StarcUp.Core 시작 완료')
    } catch (error) {
      console.error('❌ StarcUp.Core 시작 실패:', error)
      throw error
    }
  }

  /**
   * StarcUp.Core 프로세스 중지
   */
  async stopCoreProcess(): Promise<void> {
    console.log('🔌 StarcUp.Core 중지 중...')
    
    // 프로세스 종료 (프로덕션 모드에서만)
    if (!this.isDevelopment && this.coreProcess) {
      this.coreProcess.kill('SIGTERM')
      this.coreProcess = null
      console.log('✅ StarcUp.Core 프로세스 종료 완료')
    }
  }

  /**
   * 연결 상태 확인
   */
  get isRunning(): boolean {
    return this.coreProcess !== null && !this.coreProcess.killed
  }

  // ========== Private Methods ==========

  /**
   * Core 프로세스 시작 (프로덕션 모드만)
   */
  private async startCoreProcessInternal(): Promise<void> {
    // 패키지된 앱에서는 process.resourcesPath 사용
    const resourcesPath = process.resourcesPath || app.getAppPath()
    const coreDir = path.join(resourcesPath, 'core')
    const coreWorkingDir = coreDir

    // StarcUp.Core.exe 파일을 먼저 찾기 (우선순위)
    const coreExePath = path.join(coreDir, 'StarcUp.Core.exe')
    const coreDllPath = path.join(coreDir, 'StarcUp.Core.dll')

    console.log(`📂 리소스 경로: ${resourcesPath}`)
    console.log(`📂 코어 디렉토리: ${coreDir}`)
    console.log(`📂 EXE 경로 확인: ${coreExePath}`)
    console.log(`📂 DLL 경로 확인: ${coreDllPath}`)

    if (fs.existsSync(coreExePath)) {
      // EXE 파일이 있으면 직접 실행
      console.log(`📂 StarcUp.Core.exe 직접 실행: ${coreExePath}`)
      
      this.coreProcess = spawn(coreExePath, [], {
        detached: false,
        windowsHide: true,
        cwd: coreWorkingDir
      })
    } else if (fs.existsSync(coreDllPath)) {
      // DLL 파일만 있으면 dotnet 경로를 찾아서 실행
      console.log(`📂 StarcUp.Core.dll 발견, dotnet으로 실행: ${coreDllPath}`)
      
      const dotnetPath = this.findDotnetExecutable()
      if (!dotnetPath) {
        throw new Error('dotnet 실행 파일을 찾을 수 없습니다. .NET Runtime이 설치되어 있는지 확인하세요.')
      }
      
      console.log(`📂 dotnet 경로: ${dotnetPath}`)
      
      this.coreProcess = spawn(dotnetPath, [coreDllPath], {
        detached: false,
        windowsHide: true,
        cwd: coreWorkingDir
      })
    } else {
      throw new Error(`StarcUp.Core 실행 파일을 찾을 수 없습니다.\nEXE: ${coreExePath}\nDLL: ${coreDllPath}`)
    }

    // 프로세스 이벤트 핸들러
    this.coreProcess.on('exit', (code, signal) => {
      console.log(`🔌 StarcUp.Core 프로세스 종료 (코드: ${code}, 신호: ${signal})`)
      this.coreProcess = null
    })

    this.coreProcess.on('error', (error) => {
      console.error('❌ StarcUp.Core 프로세스 오류:', error)
      this.coreProcess = null
    })

    // 프로세스 시작 대기
    await new Promise(resolve => setTimeout(resolve, 1000))
    
    if (!this.coreProcess || this.coreProcess.killed) {
      throw new Error('StarcUp.Core 프로세스 시작 실패')
    }
  }

  /**
   * dotnet 실행 파일 경로 찾기
   */
  private findDotnetExecutable(): string | null {
    const possiblePaths = [
      'C:\\Program Files\\dotnet\\dotnet.exe',
      'C:\\Program Files (x86)\\dotnet\\dotnet.exe',
      path.join(process.env.ProgramFiles || 'C:\\Program Files', 'dotnet', 'dotnet.exe'),
      path.join(process.env['ProgramFiles(x86)'] || 'C:\\Program Files (x86)', 'dotnet', 'dotnet.exe'),
      // PATH에서도 시도
      'dotnet.exe',
      'dotnet'
    ]

    for (const dotnetPath of possiblePaths) {
      try {
        if (fs.existsSync(dotnetPath)) {
          console.log(`✅ dotnet 발견: ${dotnetPath}`)
          return dotnetPath
        }
      } catch (error) {
        // 경로 확인 중 오류 발생, 다음 경로 시도
        continue
      }
    }

    console.error('❌ dotnet 실행 파일을 찾을 수 없습니다.')
    return null
  }
}