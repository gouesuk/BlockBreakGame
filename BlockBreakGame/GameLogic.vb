' =============================================================
' GameLogic.vb - 게임로직 Agent
' 담당: 공 이동, 충돌 감지, 점수/목숨 처리
' 규칙: UI 코드 포함 금지, 순수 로직만
' =============================================================

Imports System.Drawing

''' <summary>게임 진행 상태 열거형</summary>
Public Enum GameState
    Waiting     ' 시작 대기 (공이 패들 위에 있음, Space 입력 대기)
    Playing     ' 게임 진행 중
    GameOver    ' 게임 종료 (목숨 소진)
    Clear       ' 스테이지 클리어 (벽돌 전부 제거)
End Enum

''' <summary>
''' 개별 벽돌 데이터 클래스
''' 위치, 크기, 색상, 생존 여부를 보관
''' </summary>
Public Class Brick
    Public X As Single
    Public Y As Single
    Public Width As Single
    Public Height As Single
    Public IsAlive As Boolean
    Public BrickColor As Color

    ''' <summary>벽돌 영역을 RectangleF로 반환</summary>
    Public Function GetRect() As RectangleF
        Return New RectangleF(X, Y, Width, Height)
    End Function
End Class

''' <summary>
''' 게임 로직 클래스 - 모든 게임 상태와 물리 계산을 담당
''' UI/렌더링 관련 코드는 포함하지 않음
''' </summary>
Public Class GameLogic

    ' ── 게임 영역 상수 ──────────────────────────────────────────
    Public Const GAME_WIDTH As Integer = 780
    Public Const GAME_HEIGHT As Integer = 540

    ' ── 패들 상수 ───────────────────────────────────────────────
    Public Const PADDLE_WIDTH As Integer = 100
    Public Const PADDLE_HEIGHT As Integer = 12
    Public Const PADDLE_SPEED As Integer = 14       ' 키보드 이동 속도 (픽셀/프레임)

    ' ── 공 상수 ─────────────────────────────────────────────────
    Public Const BALL_SIZE As Integer = 12
    Public Const BALL_SPEED As Single = 5.5F

    ' ── 벽돌 상수 ───────────────────────────────────────────────
    Public Const BRICK_ROWS As Integer = 5
    Public Const BRICK_COLS As Integer = 10
    Public Const BRICK_WIDTH As Integer = 68
    Public Const BRICK_HEIGHT As Integer = 22
    Public Const BRICK_MARGIN As Integer = 6
    Public Const BRICK_OFFSET_X As Integer = 23    ' 좌우 여백: (780 - 10*68 - 9*6) / 2 = 23
    Public Const BRICK_OFFSET_Y As Integer = 60    ' 상단 HUD 여백

    ' ── 게임 규칙 상수 ──────────────────────────────────────────
    Public Const SCORE_PER_BRICK As Integer = 10
    Public Const INITIAL_LIVES As Integer = 3

    ' ── 공 상태 ─────────────────────────────────────────────────
    Public BallX As Single          ' 공 좌상단 X 좌표
    Public BallY As Single          ' 공 좌상단 Y 좌표
    Public BallVX As Single         ' 공 X 방향 속도 (픽셀/프레임)
    Public BallVY As Single         ' 공 Y 방향 속도 (픽셀/프레임)

    ' ── 패들 상태 ───────────────────────────────────────────────
    Public PaddleX As Single        ' 패들 좌상단 X 좌표
    Public PaddleY As Single        ' 패들 Y 좌표 (고정)

    ' ── 벽돌 배열 ───────────────────────────────────────────────
    Public Bricks(BRICK_ROWS - 1, BRICK_COLS - 1) As Brick

    ' ── 게임 상태 ───────────────────────────────────────────────
    Public Score As Integer
    Public Lives As Integer
    Public State As GameState
    Public BricksRemaining As Integer

    ' 난수 생성기 (공 발사 각도 랜덤화)
    Private _rng As New Random()

    ''' <summary>생성자: 게임 초기화</summary>
    Public Sub New()
        InitGame()
    End Sub

    ' ═══════════════════════════════════════════════════════════
    ' 초기화
    ' ═══════════════════════════════════════════════════════════

    ''' <summary>게임 전체 초기화 (시작 및 재시작 공통)</summary>
    Public Sub InitGame()
        ' 패들 중앙 배치
        PaddleX = GAME_WIDTH / 2 - PADDLE_WIDTH / 2
        PaddleY = GAME_HEIGHT - 45

        ' 공 패들 위에 배치
        ResetBall()

        ' 벽돌 생성
        InitBricks()

        ' 점수·목숨 초기화
        Score = 0
        Lives = INITIAL_LIVES
        BricksRemaining = BRICK_ROWS * BRICK_COLS
        State = GameState.Waiting
    End Sub

    ''' <summary>공을 패들 중앙 위에 리셋 (목숨 감소 후 재배치)</summary>
    Private Sub ResetBall()
        BallX = PaddleX + PADDLE_WIDTH / 2.0F - BALL_SIZE / 2.0F
        BallY = PaddleY - BALL_SIZE - 3
        BallVX = 0
        BallVY = 0
    End Sub

    ''' <summary>벽돌 배열 초기화 (행별 색상 설정)</summary>
    Private Sub InitBricks()
        ' 행 순서: 빨강 → 주황 → 노랑 → 초록 → 하늘
        Dim rowColors() As Color = {
            Color.FromArgb(220, 60, 60),
            Color.FromArgb(220, 150, 50),
            Color.FromArgb(200, 200, 50),
            Color.FromArgb(60, 180, 60),
            Color.FromArgb(50, 180, 220)
        }

        For r = 0 To BRICK_ROWS - 1
            For c = 0 To BRICK_COLS - 1
                Bricks(r, c) = New Brick() With {
                    .X = BRICK_OFFSET_X + c * (BRICK_WIDTH + BRICK_MARGIN),
                    .Y = BRICK_OFFSET_Y + r * (BRICK_HEIGHT + BRICK_MARGIN),
                    .Width = BRICK_WIDTH,
                    .Height = BRICK_HEIGHT,
                    .IsAlive = True,
                    .BrickColor = rowColors(r)
                }
            Next
        Next
    End Sub

    ' ═══════════════════════════════════════════════════════════
    ' 입력 처리
    ' ═══════════════════════════════════════════════════════════

    ''' <summary>공 발사 (Space 키 → UI Agent가 호출)</summary>
    Public Sub StartBall()
        If State <> GameState.Waiting Then Return

        ' 60°~120° 사이 위쪽 방향으로 랜덤 발사
        Dim angle As Double = _rng.NextDouble() * (Math.PI / 3.0) + (Math.PI / 3.0)
        BallVX = CSng(Math.Cos(Math.PI - angle) * BALL_SPEED)
        BallVY = -CSng(Math.Sin(angle) * BALL_SPEED)

        State = GameState.Playing
    End Sub

    ''' <summary>패들 왼쪽 이동 (Left 키)</summary>
    Public Sub MovePaddleLeft()
        PaddleX -= PADDLE_SPEED
        If PaddleX < 0 Then PaddleX = 0
        ' Waiting 상태면 공도 함께 이동
        If State = GameState.Waiting Then ResetBall()
    End Sub

    ''' <summary>패들 오른쪽 이동 (Right 키)</summary>
    Public Sub MovePaddleRight()
        PaddleX += PADDLE_SPEED
        If PaddleX + PADDLE_WIDTH > GAME_WIDTH Then PaddleX = GAME_WIDTH - PADDLE_WIDTH
        ' Waiting 상태면 공도 함께 이동
        If State = GameState.Waiting Then ResetBall()
    End Sub

    ''' <summary>마우스 X 좌표로 패들 이동</summary>
    Public Sub SetPaddleByMouse(mouseX As Integer)
        PaddleX = mouseX - PADDLE_WIDTH / 2.0F
        If PaddleX < 0 Then PaddleX = 0
        If PaddleX + PADDLE_WIDTH > GAME_WIDTH Then PaddleX = GAME_WIDTH - PADDLE_WIDTH
        ' Waiting 상태면 공도 함께 이동
        If State = GameState.Waiting Then ResetBall()
    End Sub

    ' ═══════════════════════════════════════════════════════════
    ' 게임 루프 업데이트
    ' ═══════════════════════════════════════════════════════════

    ''' <summary>
    ''' 게임 상태 업데이트 (매 타이머 틱마다 호출)
    ''' Playing 상태일 때만 실행
    ''' </summary>
    Public Sub Update()
        If State <> GameState.Playing Then Return

        ' 1. 공 이동
        BallX += BallVX
        BallY += BallVY

        ' 2. 좌우 벽 충돌
        If BallX <= 0 Then
            BallX = 0
            BallVX = Math.Abs(BallVX)
        ElseIf BallX + BALL_SIZE >= GAME_WIDTH Then
            BallX = GAME_WIDTH - BALL_SIZE
            BallVX = -Math.Abs(BallVX)
        End If

        ' 3. 상단 벽 충돌
        If BallY <= 0 Then
            BallY = 0
            BallVY = Math.Abs(BallVY)
        End If

        ' 4. 하단 이탈 → 목숨 감소
        If BallY + BALL_SIZE >= GAME_HEIGHT Then
            LoseLife()
            Return
        End If

        ' 5. 패들 충돌 체크
        CheckPaddleCollision()

        ' 6. 벽돌 충돌 체크
        CheckBrickCollision()
    End Sub

    ' ═══════════════════════════════════════════════════════════
    ' 충돌 처리 (Private)
    ' ═══════════════════════════════════════════════════════════

    ''' <summary>패들과의 충돌 처리 - 히트 위치에 따른 각도 반사</summary>
    Private Sub CheckPaddleCollision()
        Dim ballRect As New RectangleF(BallX, BallY, BALL_SIZE, BALL_SIZE)
        Dim paddleRect As New RectangleF(PaddleX, PaddleY, PADDLE_WIDTH, PADDLE_HEIGHT)

        ' 공이 아래 방향으로 움직이고 패들과 겹칠 때만 처리
        If Not (ballRect.IntersectsWith(paddleRect) AndAlso BallVY > 0) Then Return

        ' 히트 위치 비율 계산 (0.0 = 왼쪽 끝, 1.0 = 오른쪽 끝)
        Dim hitRatio As Single = (BallX + BALL_SIZE / 2.0F - PaddleX) / PADDLE_WIDTH
        hitRatio = Math.Max(0.05F, Math.Min(0.95F, hitRatio))

        ' -60°~+60° 사이의 반사 각도 계산
        Dim bounceAngle As Double = (hitRatio - 0.5) * 2.0 * (Math.PI / 3.0)
        Dim speed As Single = CSng(Math.Sqrt(BallVX * BallVX + BallVY * BallVY))

        BallVX = CSng(Math.Sin(bounceAngle) * speed)
        BallVY = -CSng(Math.Cos(bounceAngle) * speed)

        ' 공이 패들 안에 박히지 않도록 위치 보정
        BallY = PaddleY - BALL_SIZE
    End Sub

    ''' <summary>벽돌과의 충돌 처리 - 겹침 방향으로 반사, 프레임당 1개</summary>
    Private Sub CheckBrickCollision()
        Dim ballRect As New RectangleF(BallX, BallY, BALL_SIZE, BALL_SIZE)

        For r = 0 To BRICK_ROWS - 1
            For c = 0 To BRICK_COLS - 1
                Dim brick = Bricks(r, c)
                If Not brick.IsAlive Then Continue For

                Dim brickRect = brick.GetRect()
                If Not ballRect.IntersectsWith(brickRect) Then Continue For

                ' 벽돌 파괴 및 점수 추가
                brick.IsAlive = False
                Score += SCORE_PER_BRICK
                BricksRemaining -= 1

                ' 충돌 방향 판단: 4방향 겹침 중 최솟값 방향으로 반사
                Dim overlapLeft = (BallX + BALL_SIZE) - brickRect.Left
                Dim overlapRight = brickRect.Right - BallX
                Dim overlapTop = (BallY + BALL_SIZE) - brickRect.Top
                Dim overlapBottom = brickRect.Bottom - BallY

                Dim minH = Math.Min(overlapLeft, overlapRight)
                Dim minV = Math.Min(overlapTop, overlapBottom)

                If minH < minV Then
                    BallVX = -BallVX   ' 좌우 반사
                Else
                    BallVY = -BallVY   ' 상하 반사
                End If

                ' 스테이지 클리어 확인
                If BricksRemaining = 0 Then
                    State = GameState.Clear
                End If

                Return  ' 프레임당 하나의 벽돌만 처리 (터널링 방지)
            Next
        Next
    End Sub

    ''' <summary>목숨 감소 및 상태 전환</summary>
    Private Sub LoseLife()
        Lives -= 1
        If Lives <= 0 Then
            State = GameState.GameOver
        Else
            ' 공을 패들 위로 복귀 후 대기 상태
            ResetBall()
            State = GameState.Waiting
        End If
    End Sub

End Class
