# 벽돌깨기 게임 설계 문서

## 1. 프로젝트 개요

| 항목 | 내용 |
|------|------|
| 언어 | Visual Basic .NET (.NET Framework 4.8) |
| IDE | Visual Studio 2022 |
| UI 방식 | Windows Forms (단일 Form) |
| 렌더링 | GDI+ (PictureBox Paint 이벤트) |

---

## 2. 파일 구성

```
BlockBreakGame/
├── Form1.vb            ← UI Agent: 폼, 버튼, 입력 이벤트, 모듈 연결
├── Form1.Designer.vb   ← VS 자동 생성 (폼 기본 속성)
├── GameLogic.vb        ← 게임로직 Agent: 공·패들·벽돌 로직, 순수 계산
├── Renderer.vb         ← 렌더링 Agent: GDI+ 화면 출력
└── BlockBreakGame.vbproj
```

---

## 3. 클래스 / 타입 구조

### 3-1. Enum GameState (GameLogic.vb)
```
GameState
  ├── Waiting   : 공이 패들 위에 있음 (Space 대기)
  ├── Playing   : 게임 진행 중
  ├── GameOver  : 목숨 소진
  └── Clear     : 벽돌 전부 제거
```

### 3-2. Class Brick (GameLogic.vb)
| 필드 | 타입 | 설명 |
|------|------|------|
| X, Y | Single | 좌상단 좌표 |
| Width, Height | Single | 크기 |
| IsAlive | Boolean | 생존 여부 |
| BrickColor | Color | 행별 색상 |
| GetRect() | Function | RectangleF 반환 |

### 3-3. Class GameLogic (GameLogic.vb)
**상수**
| 상수 | 값 | 설명 |
|------|----|------|
| GAME_WIDTH | 780 | 게임 영역 너비 |
| GAME_HEIGHT | 540 | 게임 영역 높이 |
| PADDLE_WIDTH | 100 | 패들 너비 |
| PADDLE_HEIGHT | 12 | 패들 높이 |
| BALL_SIZE | 12 | 공 지름 |
| BALL_SPEED | 5.5 | 공 초기 속도 |
| BRICK_ROWS | 5 | 벽돌 행 수 |
| BRICK_COLS | 10 | 벽돌 열 수 |
| BRICK_WIDTH | 68 | 벽돌 너비 |
| BRICK_HEIGHT | 22 | 벽돌 높이 |
| BRICK_MARGIN | 6 | 벽돌 간격 |
| BRICK_OFFSET_X | 23 | 벽돌 시작 X (좌우 대칭) |
| BRICK_OFFSET_Y | 60 | 벽돌 시작 Y |
| SCORE_PER_BRICK | 10 | 벽돌 1개당 점수 |
| INITIAL_LIVES | 3 | 초기 목숨 |

**공개 필드 (상태)**
- BallX, BallY, BallVX, BallVY — 공 위치·속도
- PaddleX, PaddleY — 패들 위치
- Bricks(4,9) — 벽돌 2D 배열
- Score, Lives, State, BricksRemaining

**공개 메서드**
| 메서드 | 호출자 | 설명 |
|--------|--------|------|
| InitGame() | Form1 | 전체 초기화 (재시작 포함) |
| StartBall() | Form1 | 공 발사 (Space 키) |
| MovePaddleLeft/Right() | Form1 | 키보드 패들 이동 |
| SetPaddleByMouse(x) | Form1 | 마우스 패들 이동 |
| Update() | Form1 (타이머) | 매 프레임 로직 갱신 |

### 3-4. Class Renderer (Renderer.vb)
| 메서드 | 설명 |
|--------|------|
| DrawGame(g, logic) | 전체 화면 그리기 (진입점) |
| DrawBricks(g, logic) | 벽돌 렌더링 (그라데이션) |
| DrawPaddle(g, logic) | 패들 렌더링 (둥근 모서리) |
| DrawBall(g, logic) | 공 렌더링 (하이라이트) |
| DrawHUD(g, logic) | 점수·목숨 표시 |
| DrawOverlay(g, logic) | 대기/게임오버/클리어 메시지 |

### 3-5. Class Form1 (Form1.vb)
**컨트롤**
- picGame (PictureBox) — 게임 캔버스 780×540
- gameTimer (Timer) — 16ms 간격 (~60 FPS)
- btnStart, btnRestart (Button)
- lblInfo (Label) — 조작 안내

**이벤트 핸들러**
| 이벤트 | 처리 |
|--------|------|
| gameTimer.Tick | Update() 호출 → Invalidate() |
| Form.KeyDown/Up | 좌우 키 상태 추적, Space → StartBall() |
| picGame.MouseMove | SetPaddleByMouse() 호출 |
| picGame.Paint | Renderer.DrawGame() 호출 |
| btnStart.Click | InitGame() + 타이머 시작 |
| btnRestart.Click | InitGame() + 타이머 재시작 |

---

## 4. 화면 레이아웃

```
┌──────────────────────────────────────────────────────────┐ Form (800×620)
│ ┌────────────────────────────────────────────────────┐   │
│ │ [SCORE: 0]              [LIVES: ● ● ●]             │   │
│ │                                                    │   │
│ │  [■][■][■][■][■][■][■][■][■][■]  ← 빨강           │   │
│ │  [■][■][■][■][■][■][■][■][■][■]  ← 주황           │   │ picGame
│ │  [■][■][■][■][■][■][■][■][■][■]  ← 노랑           │   │ 780×540
│ │  [■][■][■][■][■][■][■][■][■][■]  ← 초록           │   │
│ │  [■][■][■][■][■][■][■][■][■][■]  ← 하늘           │   │
│ │                                                    │   │
│ │              ○  ← 공                               │   │
│ │            [━━━━━]  ← 패들                         │   │
│ └────────────────────────────────────────────────────┘   │
│  [시작]  [재시작]   ← → 또는 마우스: 패들이동  Space: 발사 │
└──────────────────────────────────────────────────────────┘
```

---

## 5. 게임 로직 흐름

```
초기화 (InitGame)
  └→ Waiting 상태 진입 (공이 패들 위에 위치)

Space 키 입력
  └→ StartBall(): 임의 각도(60~120°)로 공 발사, Playing 상태

매 프레임 (gameTimer.Tick → Update)
  ├→ 공 이동 (BallX += BallVX, BallY += BallVY)
  ├→ 벽 충돌: 좌우/상단 반사, 하단 이탈 시 LoseLife()
  ├→ 패들 충돌: 히트 위치에 따른 각도 반사
  └→ 벽돌 충돌: 겹침 방향 판단 후 반사, 점수 추가

LoseLife()
  ├→ Lives > 0: Waiting 상태로 복귀
  └→ Lives == 0: GameOver 상태

BricksRemaining == 0 → Clear 상태
```

---

## 6. 충돌 처리 방식

### 패들 반사
- 히트 비율(0~1)을 -60°~+60° 각도로 변환
- `BallVX = sin(angle) * speed`, `BallVY = -cos(angle) * speed`
- 공 속도 크기는 일정하게 유지

### 벽돌 반사
- 4방향 겹침(overlap) 중 최소값 방향으로 반사
- 프레임당 최대 1개 벽돌만 처리 (터널링 방지)

---

## 7. 벽돌 배치 계산

- 전체 너비: 10 × 68 + 9 × 6 = 734px
- 좌우 여백: (780 - 734) / 2 = 23px
- 행별 Y 시작: 60 + row × (22 + 6)
- 행별 색상: 빨강→주황→노랑→초록→하늘
