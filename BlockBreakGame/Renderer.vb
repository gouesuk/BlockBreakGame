' =============================================================
' Renderer.vb - 렌더링 Agent
' 담당: GDI+ 기반 화면 그리기
' 방식: PictureBox Paint 이벤트에서 DrawGame() 호출
' =============================================================

Imports System.Drawing
Imports System.Drawing.Drawing2D

''' <summary>
''' 게임 화면 렌더러
''' GameLogic의 상태를 읽어 GDI+로 화면에 출력
''' </summary>
Public Class Renderer

    ''' <summary>게임 전체 화면 그리기 (메인 진입점)</summary>
    Public Sub DrawGame(g As Graphics, logic As GameLogic)
        ' 렌더링 품질 설정
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit
        g.InterpolationMode = InterpolationMode.HighQualityBicubic

        ' 배경 색 (어두운 남색)
        g.Clear(Color.FromArgb(15, 15, 30))

        ' 요소별 그리기
        DrawBricks(g, logic)
        DrawPaddle(g, logic)
        DrawBall(g, logic)
        DrawHUD(g, logic)
        DrawOverlay(g, logic)
    End Sub

    ' ─────────────────────────────────────────────────────────
    ' 벽돌 그리기
    ' ─────────────────────────────────────────────────────────

    ''' <summary>전체 벽돌 배열 렌더링</summary>
    Private Sub DrawBricks(g As Graphics, logic As GameLogic)
        For r = 0 To GameLogic.BRICK_ROWS - 1
            For c = 0 To GameLogic.BRICK_COLS - 1
                Dim brick = logic.Bricks(r, c)
                If Not brick.IsAlive Then Continue For
                DrawSingleBrick(g, brick)
            Next
        Next
    End Sub

    ''' <summary>벽돌 1개 그리기 (그라데이션 + 하이라이트 + 테두리)</summary>
    Private Sub DrawSingleBrick(g As Graphics, brick As Brick)
        Dim rect = brick.GetRect()
        Dim brightColor = BrightenColor(brick.BrickColor, 55)

        ' 세로 그라데이션 채우기
        Using grad As New LinearGradientBrush(
                rect, brightColor, brick.BrickColor, LinearGradientMode.Vertical)
            g.FillRectangle(grad, rect)
        End Using

        ' 상단 하이라이트 선 (밝게)
        Using highlightPen As New Pen(Color.FromArgb(110, 255, 255, 255), 1.0F)
            g.DrawLine(highlightPen,
                       rect.Left + 2, rect.Top + 2,
                       rect.Right - 3, rect.Top + 2)
        End Using

        ' 테두리 (반투명 검정)
        Using borderPen As New Pen(Color.FromArgb(160, 0, 0, 0), 1.0F)
            g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height)
        End Using
    End Sub

    ' ─────────────────────────────────────────────────────────
    ' 패들 그리기
    ' ─────────────────────────────────────────────────────────

    ''' <summary>패들 렌더링 (둥근 모서리 + 그라데이션)</summary>
    Private Sub DrawPaddle(g As Graphics, logic As GameLogic)
        Dim rect As New RectangleF(
            logic.PaddleX, logic.PaddleY,
            GameLogic.PADDLE_WIDTH, GameLogic.PADDLE_HEIGHT)

        Using grad As New LinearGradientBrush(
                rect,
                Color.FromArgb(130, 210, 255),
                Color.FromArgb(30, 100, 200),
                LinearGradientMode.Vertical)

            Using path As GraphicsPath = CreateRoundedRect(rect, 5)
                g.FillPath(grad, path)
                Using borderPen As New Pen(Color.FromArgb(180, 180, 255), 1.5F)
                    g.DrawPath(borderPen, path)
                End Using
            End Using
        End Using
    End Sub

    ' ─────────────────────────────────────────────────────────
    ' 공 그리기
    ' ─────────────────────────────────────────────────────────

    ''' <summary>공 렌더링 (그림자 + 본체 + 하이라이트)</summary>
    Private Sub DrawBall(g As Graphics, logic As GameLogic)
        Dim x = logic.BallX
        Dim y = logic.BallY
        Dim s = GameLogic.BALL_SIZE

        ' 그림자
        Using shadowBrush As New SolidBrush(Color.FromArgb(70, 0, 0, 0))
            g.FillEllipse(shadowBrush, x + 2, y + 3, s, s)
        End Using

        ' 본체 (대각 그라데이션으로 입체감)
        Dim ballRect As New RectangleF(x, y, s, s)
        Using grad As New LinearGradientBrush(
                ballRect, Color.White, Color.Silver, 135.0F)
            g.FillEllipse(grad, ballRect)
        End Using

        ' 상단 하이라이트 (작은 흰 타원)
        Using highlightBrush As New SolidBrush(Color.FromArgb(210, 255, 255, 255))
            g.FillEllipse(highlightBrush, x + 2, y + 1, s * 0.38F, s * 0.32F)
        End Using
    End Sub

    ' ─────────────────────────────────────────────────────────
    ' HUD (점수, 목숨) 그리기
    ' ─────────────────────────────────────────────────────────

    ''' <summary>상단 HUD 렌더링 (점수 + 목숨)</summary>
    Private Sub DrawHUD(g As Graphics, logic As GameLogic)
        ' HUD 배경 바
        Using hudBrush As New SolidBrush(Color.FromArgb(55, 255, 255, 255))
            g.FillRectangle(hudBrush, 0, 0, GameLogic.GAME_WIDTH, 42)
        End Using
        Using hudLine As New Pen(Color.FromArgb(80, 150, 150, 255), 1)
            g.DrawLine(hudLine, 0, 42, GameLogic.GAME_WIDTH, 42)
        End Using

        Using font As New Font("Arial", 12, FontStyle.Bold)
            ' 점수 (왼쪽)
            Using brush As New SolidBrush(Color.White)
                g.DrawString($"SCORE: {logic.Score}", font, brush, 10, 11)
            End Using

            ' 목숨 (오른쪽) - 빨간 원으로 표현
            Dim livesLabel = "LIVES: "
            Dim labelW = g.MeasureString(livesLabel, font).Width
            Dim startX As Single = GameLogic.GAME_WIDTH - 160

            Using brush As New SolidBrush(Color.White)
                g.DrawString(livesLabel, font, brush, startX, 11)
            End Using

            ' 목숨 아이콘 (빨간 원)
            For i As Integer = 1 To logic.Lives
                Dim iconX = startX + labelW + (i - 1) * 22
                Using iconBrush As New SolidBrush(Color.Crimson)
                    g.FillEllipse(iconBrush, iconX, 14, 14, 14)
                End Using
                Using iconBorder As New Pen(Color.White, 1.0F)
                    g.DrawEllipse(iconBorder, iconX, 14, 14, 14)
                End Using
            Next
        End Using
    End Sub

    ' ─────────────────────────────────────────────────────────
    ' 오버레이 메시지 (대기 / 게임오버 / 클리어)
    ' ─────────────────────────────────────────────────────────

    ''' <summary>게임 상태에 따른 오버레이 메시지 렌더링</summary>
    Private Sub DrawOverlay(g As Graphics, logic As GameLogic)
        Select Case logic.State

            Case GameState.Waiting
                ' 하단 안내 박스
                DrawInfoBox(g, "SPACE: 발사     ←→ / 마우스: 패들 이동", Color.Yellow, 14)

            Case GameState.GameOver
                ' 반투명 어두운 오버레이
                Using overlayBrush As New SolidBrush(Color.FromArgb(160, 0, 0, 0))
                    g.FillRectangle(overlayBrush, 0, 0, GameLogic.GAME_WIDTH, GameLogic.GAME_HEIGHT)
                End Using
                DrawCenteredText(g, "GAME OVER", Color.OrangeRed, 42, -40)
                DrawCenteredText(g, $"최종 점수: {logic.Score}", Color.White, 20, 20)
                DrawCenteredText(g, "'재시작' 버튼을 누르세요", Color.LightGray, 13, 65)

            Case GameState.Clear
                ' 반투명 황금 오버레이
                Using overlayBrush As New SolidBrush(Color.FromArgb(130, 200, 160, 0))
                    g.FillRectangle(overlayBrush, 0, 0, GameLogic.GAME_WIDTH, GameLogic.GAME_HEIGHT)
                End Using
                DrawCenteredText(g, "STAGE CLEAR!", Color.Gold, 42, -40)
                DrawCenteredText(g, $"점수: {logic.Score}", Color.White, 20, 20)
                DrawCenteredText(g, "'재시작' 버튼을 누르세요", Color.White, 13, 65)

        End Select
    End Sub

    ' ─────────────────────────────────────────────────────────
    ' 헬퍼 메서드
    ' ─────────────────────────────────────────────────────────

    ''' <summary>게임 영역 중앙에 텍스트 그리기</summary>
    Private Sub DrawCenteredText(g As Graphics, text As String, color As Color,
                                  fontSize As Single, Optional offsetY As Single = 0)
        Using font As New Font("Arial", fontSize, FontStyle.Bold)
            Dim sz = g.MeasureString(text, font)
            Dim x = (GameLogic.GAME_WIDTH - sz.Width) / 2
            Dim y = (GameLogic.GAME_HEIGHT - sz.Height) / 2 + offsetY

            ' 그림자
            Using shadowBrush As New SolidBrush(Color.FromArgb(130, 0, 0, 0))
                g.DrawString(text, font, shadowBrush, x + 2, y + 2)
            End Using

            ' 본문
            Using brush As New SolidBrush(color)
                g.DrawString(text, font, brush, x, y)
            End Using
        End Using
    End Sub

    ''' <summary>하단에 안내 정보 박스 그리기 (Waiting 상태용)</summary>
    Private Sub DrawInfoBox(g As Graphics, text As String, color As Color, fontSize As Single)
        Using font As New Font("Arial", fontSize, FontStyle.Bold)
            Dim textSz = g.MeasureString(text, font)
            Dim boxW = textSz.Width + 24
            Dim boxH = textSz.Height + 12
            Dim boxX = (GameLogic.GAME_WIDTH - boxW) / 2
            Dim boxY = GameLogic.GAME_HEIGHT - boxH - 18

            ' 박스 배경
            Using boxBrush As New SolidBrush(Color.FromArgb(160, 0, 0, 0))
                g.FillRectangle(boxBrush, boxX, boxY, boxW, boxH)
            End Using
            Using borderPen As New Pen(color, 1.5F)
                g.DrawRectangle(borderPen, boxX, boxY, boxW, boxH)
            End Using

            ' 텍스트
            Using brush As New SolidBrush(color)
                g.DrawString(text, font, brush, boxX + 12, boxY + 6)
            End Using
        End Using
    End Sub

    ''' <summary>색상을 지정된 양만큼 밝게 만들기</summary>
    Private Function BrightenColor(c As Color, amount As Integer) As Color
        Return Color.FromArgb(
            Math.Min(255, CInt(c.R) + amount),
            Math.Min(255, CInt(c.G) + amount),
            Math.Min(255, CInt(c.B) + amount))
    End Function

    ''' <summary>둥근 모서리 GraphicsPath 생성</summary>
    Private Function CreateRoundedRect(rect As RectangleF, radius As Single) As GraphicsPath
        Dim path As New GraphicsPath()
        Dim d = radius * 2
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90)
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90)
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90)
        path.CloseFigure()
        Return path
    End Function

End Class
