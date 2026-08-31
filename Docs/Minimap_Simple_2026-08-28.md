# 단순 미니맵

일반 전투방 아이콘 제거. 기존 랜덤맵 배치/이동/공개 규칙과 흰색 MAP 제목 유지.
개별 방 배경만 Map_Tile_Simple.png로 연결. 연결 바닥 구현 없음.
테두리 없이 현재 방은 기존 금색으로 구분. CurrentRoomOutline Image 비활성화.
복잡한 시작/상점/보스 이미지만 단순 문/동전/해골로 교체. 플라스크/물음표/별/열쇠 유지.
기존 원본 이미지 보관. 최종 파일은 Assets/_Project/Resources/UI/MapIcons/Map_*_Simple.png.

## 투명도 확인

미사용 첫 타일 중앙 RGBA=154,141,120,146: 중앙까지 반투명해 뒤 배경이 비침.
새 타일 중앙 RGBA=248,235,207,255. 모서리 alpha=255. 임포트 alphaUsage=0으로 투명도 비활성화.
타일은 Image.Simple, 256px 무압축 임포트. 크기/배치 변경 없음.

## 생성 프롬프트 — 내장 imagegen 사용

Start: ONE extremely minimalist minimap entrance symbol. Dark brown SOLID flat silhouette: a single thick upside-down U arch, with flat bottom ends, empty transparent middle. Just ONE continuous arch shape, no bricks, no door leaf, no handle, no step, no perspective. Rounded arch top and straight vertical sides. No texture, gradient, border, badge or shadow. Genuinely transparent background, centered square composition, shape fills 85% of canvas. Must be clear at 24px.

Shop: ONE extremely minimalist minimap shop symbol. A solid dark brown circular COIN with just one short vertical rectangular transparent slot cut out at its center. ONE filled circle and ONE slot, no currency sign, no letters, no double rim, no texture, no shading, no gradient, no shadow. Perfect flat 2D pictogram, genuinely transparent background. Centered, 85% square canvas, clear at 24px.

Boss: ONE extremely minimalist minimap boss symbol. A single dark brown flat solid SKULL silhouette, round top, small rectangular jaw, with TWO large round transparent eye holes only. No crown, NO teeth, NO nose hole, no bones, no shading, no outline, no texture, no badge. Friendly casual 2D game pictogram. Genuine transparent background. Bold simple shape occupies 85% square canvas, clearly readable at 24px.

Tile: ONE minimal game UI sprite image. Entire square canvas is ONE completely uniform opaque pale cream color #F5E8CD from edge to edge. A FLAT COLOR SWATCH. Perfect sharp square. NO border, NO rounding, NO margins, NO transparency anywhere, NO vignette, NO shading, NO shadows, NO texture, NO gradient, NO highlights, NO gray center, NO symbols, NO text. Absolutely homogeneous opaque cream rectangular fill only. This will be used as a tiny borderless minimap room tile with runtime color tint.

