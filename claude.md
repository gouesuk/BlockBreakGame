
# 벽돌깨기 프로젝트

## Agent 역할 정의

### 설계 Agent
- 담당: 전체 클래스 구조 설계, 파일 목록 결정
- 산출물: 설계 문서 (design.md)

### 렌더링 Agent  
- 담당: GDI+ 기반 화면 그리기
- 파일: Renderer.vb
- 규칙: OnPaint 또는 PictureBox 방식 사용

### 게임로직 Agent
- 담당: 공 이동, 충돌 감지, 점수/목숨 처리
- 파일: GameLogic.vb
- 규칙: UI 코드 포함 금지, 순수 로직만

### UI Agent
- 담당: Form 레이아웃, 버튼, 키보드 이벤트
- 파일: Form1.vb
- 규칙: GameLogic을 호출만 하고 직접 계산 금지