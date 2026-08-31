# 오른쪽 작업대 배경 교체

- 기존 RectTransform, 텍스트 내용/크기/정렬, 아이콘, 별, 버튼 기능 유지.
- 패널: Panel_Workbench. 지도: Map_Workbench. 카드: Card_Workbench.
- 밝은 종이 위 텍스트만 갈색으로 대비 조정. 증강/보상 등급은 옅은 파랑/초록/노랑 tint 유지.
- 카드 오른쪽 위 속성 탭은 이번 작업에서 추가하지 않음.
- ProductScrollView/MapViewport 등 기존 채색 배경은 투명하게 변경. Mask/ScrollRect는 유지.
- 원본 생성 PNG를 그대로 복사했으며 픽셀 재가공 없음. 투명도 유지.
- 9-slice: 패널/지도 border 170, multiplier 6; 카드 border 90, multiplier 4. SetNativeSize 사용 안 함.
- 런타임 보라색/등급 스프라이트 덮어쓰기는 Card_Workbench 스프라이트에 한해 우회. 다른 카드 스타일은 기존 동작 유지.

## 이미지 생성

내장 imagegen 사용. 위치: Assets/_Project/Resources/UI/Workbench/.

생성 프롬프트 사양:

1. Panel_Workbench: ONE production Unity 9-sliced empty UI background. Casual alchemist workbench, square dark warm walnut tray, thin rounded beveled brown border, tiny leather fasteners in corners, uniform dark brown matte leather center. Orthographic flat 2D. No text/title/icons/cards/symbols. Minimal outside padding, transparent outside, opaque center, corner ornament only.
2. Card_Workbench: ONE empty Unity card background, landscape 2.5:1, clean light ivory paper, thin double warm brown border, small rounded corners, uniform blank center. No text/icons/tabs/stars. Transparent outside, opaque inside, subtle edge shading, no cast shadow. Suitable for 9-slice.
3. Map_Workbench: ONE empty square minimap background, cream paper mounted on narrow walnut frame, tiny brass corner pins. Blank center, no map marks/routes/icons/title/text, no central crease. Flat orthographic casual 2D, transparent outside, opaque inside, 9-slice-ready corners.

## 검증

Assembly-CSharp.csproj 빌드: 오류 0, 경고 0. Unity 플레이 화면은 별도 확인 필요.
