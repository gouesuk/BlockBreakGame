' =============================================================
' Form1.vb - UI Agent
' 담당: Form 레이아웃, 버튼, 키보드/마우스 이벤트
' 규칙: GameLogic을 호출만 하고 직접 계산 금지
' =============================================================

Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' 메인 폼 - UI 이벤트 처리 및 게임 모듈 연결
''' GameLogic(순수 로직)과 Renderer(화면 출력)를 조율
''' </summary>
Public Class Form1

    ' ── 게임 모듈 ───────────────────────────────────────────────
    Private _logic As GameLogic       ' 게임 로직 모듈
    Private _renderer As Renderer     ' 화면 렌더링 모듈

    ' ── UI 컨트롤 (코드로 생성) ─────────────────────────────────
    Private picGame As PictureBox     ' 게임 캔버스
    Private gameTimer As Timer        ' 게임 루프 타이머 (~60 FPS)
    Private btnStart As Button        ' 시작 버튼
    Private btnRestart As Button      ' 재시작 버튼
    Private lblInfo As Label          ' 조작 안내 레이블

    ' ── 키 입력 상태 ────────────────────────────────────────────
    Private _keyLeft As Boolean = False   ' Left 키 누름 여부
    Private _keyRight As Boolean = False  ' Right 키 누름 여부

    ' ── 효과음 동시 재생 방지 플래그 ───────────────────────────────
    ' 0 = 유휴, 1 = 재생 중 (Threading.Interlocked로 원자적 접근)
    Private _beepBusy As Integer = 0

    ' ═══════════════════════════════════════════════════════════
    ' 초기화
    ' ═══════════════════════════════════════════════════════════

    ''' <summary>생성자: 모듈 및 UI 컨트롤 초기화</summary>
    Sub New()
        ' 디자이너 기본 초기화 (필수 호출)
        InitializeComponent()

        ' 게임 모듈을 먼저 생성 (Paint 이벤트보다 앞서야 함)
        _logic = New GameLogic()
        _renderer = New Renderer()

        ' UI 컨트롤 생성 및 배치
        InitializeGameControls()
    End Sub

    ''' <summary>게임 UI 컨트롤 생성 및 배치</summary>
    Private Sub InitializeGameControls()

        ' ── 폼 속성 설정 ────────────────────────────────────────
        Me.Text = "벽돌깨기 게임"
        Me.ClientSize = New Size(
            GameLogic.GAME_WIDTH + 20,
            GameLogic.GAME_HEIGHT + 78)
        Me.BackColor = Color.FromArgb(25, 25, 45)
        Me.KeyPreview = True                             ' 폼에서 키 이벤트 수신
        Me.FormBorderStyle = FormBorderStyle.FixedSingle ' 크기 고정
        Me.MaximizeBox = False
        Me.StartPosition = FormStartPosition.CenterScreen

        ' ── 게임 캔버스 (PictureBox) ────────────────────────────
        picGame = New PictureBox()
        picGame.Location = New Point(10, 10)
        picGame.Size = New Size(GameLogic.GAME_WIDTH, GameLogic.GAME_HEIGHT)
        picGame.BackColor = Color.Black
        picGame.BorderStyle = BorderStyle.FixedSingle
        AddHandler picGame.Paint, AddressOf picGame_Paint
        AddHandler picGame.MouseMove, AddressOf picGame_MouseMove
        Me.Controls.Add(picGame)

        ' ── 버튼 공통 Y 위치 ────────────────────────────────────
        Dim btnY = GameLogic.GAME_HEIGHT + 22

        ' ── 시작 버튼 ───────────────────────────────────────────
        btnStart = New Button()
        btnStart.Text = "시작"
        btnStart.Size = New Size(100, 38)
        btnStart.Location = New Point(10, btnY)
        btnStart.Font = New Font("굴림", 11, FontStyle.Bold)
        btnStart.BackColor = Color.FromArgb(55, 115, 195)
        btnStart.ForeColor = Color.White
        btnStart.FlatStyle = FlatStyle.Flat
        btnStart.FlatAppearance.BorderColor = Color.CornflowerBlue
        btnStart.FlatAppearance.BorderSize = 1
        AddHandler btnStart.Click, AddressOf btnStart_Click
        Me.Controls.Add(btnStart)

        ' ── 재시작 버튼 ─────────────────────────────────────────
        btnRestart = New Button()
        btnRestart.Text = "재시작"
        btnRestart.Size = New Size(100, 38)
        btnRestart.Location = New Point(118, btnY)
        btnRestart.Font = New Font("굴림", 11, FontStyle.Bold)
        btnRestart.BackColor = Color.FromArgb(175, 70, 55)
        btnRestart.ForeColor = Color.White
        btnRestart.FlatStyle = FlatStyle.Flat
        btnRestart.FlatAppearance.BorderColor = Color.Salmon
        btnRestart.FlatAppearance.BorderSize = 1
        AddHandler btnRestart.Click, AddressOf btnRestart_Click
        Me.Controls.Add(btnRestart)

        ' ── 조작 안내 레이블 ────────────────────────────────────
        lblInfo = New Label()
        lblInfo.Text = "← →  또는 마우스: 패들 이동     Space: 공 발사"
        lblInfo.ForeColor = Color.FromArgb(180, 180, 210)
        lblInfo.Font = New Font("굴림", 9)
        lblInfo.AutoSize = True
        lblInfo.Location = New Point(228, btnY + 10)
        Me.Controls.Add(lblInfo)

        ' ── 게임 루프 타이머 ─────────────────────────────────────
        gameTimer = New Timer()
        gameTimer.Interval = 16    ' 약 60 FPS
        AddHandler gameTimer.Tick, AddressOf gameTimer_Tick
    End Sub

    ' ═══════════════════════════════════════════════════════════
    ' 게임 루프
    ' ═══════════════════════════════════════════════════════════

    ''' <summary>
    ''' 타이머 틱 - 매 프레임 실행
    ''' 패들 이동 → 게임 상태 업데이트 → 효과음 처리 → 화면 갱신
    ''' </summary>
    Private Sub gameTimer_Tick(sender As Object, e As EventArgs)
        ' 키보드 패들 이동 (키가 눌린 동안 계속 이동)
        If _keyLeft Then _logic.MovePaddleLeft()
        If _keyRight Then _logic.MovePaddleRight()

        ' 게임 로직 업데이트
        _logic.Update()

        ' 효과음 처리: GameLogic의 플래그를 읽어 백그라운드에서 재생
        ' 우선순위: GameOver > Clear > LifeLost > BrickHit > PaddleHit > WallHit
        ' (같은 프레임에 복수 이벤트 발생 시 가장 중요한 것만 재생)
        If _logic.SoundGameOver Then
            ' 게임 오버: 낮고 무거운 3음 하강
            PlayBeepAsync(400, 150, 300, 150, 200, 250)
        ElseIf _logic.SoundClear Then
            ' 스테이지 클리어: 밝고 상승하는 팡파레
            PlayBeepAsync(600, 100, 800, 100, 1000, 200)
        ElseIf _logic.SoundLifeLost Then
            ' 목숨 잃음: 3음 하강 (더 극적으로) 523Hz→392Hz→261Hz
            PlayBeepAsync(523, 120, 392, 120, 261, 250)
        ElseIf _logic.SoundBrickHit Then
            ' 벽돌 파괴: 높고 선명한 타격음 (80ms로 연장해 가청성 확보)
            PlayBeepAsync(880, 80)
        ElseIf _logic.SoundPaddleHit Then
            ' 패들 충돌: 중간 톤 타격음 (70ms)
            PlayBeepAsync(523, 70)
        ElseIf _logic.SoundWallHit Then
            ' 벽 충돌: 낮고 짧은 효과음 (60ms)
            PlayBeepAsync(330, 60)
        End If

        ' 화면 갱신 요청 (→ picGame_Paint 호출)
        picGame.Invalidate()

        ' 게임 종료 시 타이머 정지
        If _logic.State = GameState.GameOver OrElse _logic.State = GameState.Clear Then
            gameTimer.Stop()
        End If
    End Sub

    ''' <summary>
    ''' 효과음을 백그라운드 스레드에서 재생하는 헬퍼 메서드
    ''' Interlocked로 동시 재생을 방지하여 Console.Beep 장치 충돌 해결
    ''' ParamArray로 (주파수1, 지속시간1, 주파수2, 지속시간2, ...) 형태로 전달
    ''' </summary>
    Private Sub PlayBeepAsync(ParamArray freqDurationPairs() As Integer)
        If freqDurationPairs Is Nothing OrElse freqDurationPairs.Length Mod 2 <> 0 Then Return

        ' 이미 재생 중이면 건너뜀 (장치 독점 충돌 방지)
        ' CompareExchange: _beepBusy가 0이면 1로 바꾸고 0 반환, 아니면 현재값 반환
        If Threading.Interlocked.CompareExchange(_beepBusy, 1, 0) <> 0 Then Return

        Dim pairs() As Integer = CType(freqDurationPairs.Clone(), Integer())

        Threading.ThreadPool.QueueUserWorkItem(
            Sub(state)
                Try
                    Dim i As Integer = 0
                    Do While i < pairs.Length - 1
                        Dim freq As Integer = pairs(i)
                        Dim dur As Integer = pairs(i + 1)
                        If freq >= 37 AndAlso freq <= 32767 AndAlso dur > 0 Then
                            Console.Beep(freq, dur)
                        End If
                        i += 2
                    Loop
                Catch ex As Exception
                    ' 효과음 실패는 게임 진행에 영향 없으므로 조용히 무시
                Finally
                    ' 재생 완료(또는 실패) 후 반드시 플래그 해제
                    Threading.Interlocked.Exchange(_beepBusy, 0)
                End Try
            End Sub)
    End Sub

    ' ═══════════════════════════════════════════════════════════
    ' 렌더링
    ' ═══════════════════════════════════════════════════════════

    ''' <summary>PictureBox Paint 이벤트 - Renderer에 위임</summary>
    Private Sub picGame_Paint(sender As Object, e As PaintEventArgs)
        If _renderer Is Nothing OrElse _logic Is Nothing Then Return
        _renderer.DrawGame(e.Graphics, _logic)
    End Sub

    ' ═══════════════════════════════════════════════════════════
    ' 입력 이벤트
    ' ═══════════════════════════════════════════════════════════

    ''' <summary>
    ''' 화살표·스페이스 키 선행 캡처 (ProcessCmdKey)
    ''' Windows Forms는 화살표 키를 네비게이션 키로 먼저 처리하기 때문에
    ''' KeyDown 이벤트에 도달하기 전에 이 메서드에서 가로챈다.
    ''' True 반환 = 해당 키를 이 메서드에서 소비(더 이상 전파 안 함)
    ''' </summary>
    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        Select Case keyData
            Case Keys.Left
                _keyLeft = True
                Return True
            Case Keys.Right
                _keyRight = True
                Return True
            Case Keys.Space
                If _logic IsNot Nothing AndAlso _logic.State = GameState.Waiting Then
                    _logic.StartBall()
                End If
                Return True
        End Select
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    ''' <summary>키 뗌 처리 - 방향키 이동 정지 (KeyUp은 ProcessCmdKey를 거치지 않으므로 그대로 사용)</summary>
    Private Sub Form1_KeyUp(sender As Object, e As KeyEventArgs) Handles Me.KeyUp
        Select Case e.KeyCode
            Case Keys.Left
                _keyLeft = False
            Case Keys.Right
                _keyRight = False
        End Select
    End Sub

    ''' <summary>마우스 이동으로 패들 조작 (picGame 내 X 좌표 사용)</summary>
    Private Sub picGame_MouseMove(sender As Object, e As MouseEventArgs)
        _logic.SetPaddleByMouse(e.X)
    End Sub

    ' ═══════════════════════════════════════════════════════════
    ' 버튼 이벤트
    ' ═══════════════════════════════════════════════════════════

    ''' <summary>시작 버튼 - 게임 초기화 후 타이머 시작</summary>
    Private Sub btnStart_Click(sender As Object, e As EventArgs)
        gameTimer.Stop()    ' 혹시 실행 중이면 정지 후 재시작 (안전)
        _logic.InitGame()
        gameTimer.Start()
        Me.Focus()    ' 폼이 키보드 이벤트를 받도록
    End Sub

    ''' <summary>재시작 버튼 - 게임 초기화 후 타이머 재시작</summary>
    Private Sub btnRestart_Click(sender As Object, e As EventArgs)
        gameTimer.Stop()    ' 혹시 실행 중이면 정지 후 재시작 (안전)
        _logic.InitGame()
        gameTimer.Start()
        Me.Focus()
    End Sub

End Class
