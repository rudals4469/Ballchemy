# 단색 문 아이콘

최종 선택: 사용자가 이후 밝은 나무색 + 갈색 테두리 버전을 선택하여 단색 버전은 미사용. Map_Start_Readable.png는 Minimap_Door_Refinement_2026-08-28.md의 투명 배경 소스로 복원했다.

Built-in imagegen. 기존 문 PNG만 교체, Unity 메타/GUID와 다른 아이콘은 유지.

Asset: Assets/_Project/Resources/UI/MapIcons/Map_Start_Readable.png
Source: C:\Users\rudal\.codex\generated_images\01a0060d-990a-7e43-a411-67915bdfea36\exec-1ec5a2c2-8b89-43b9-91fa-5a0ec79e9063.png

## Prompt

Use case: precise-object-edit. Convert attached door game icon into a strictly MONOCHROME single-ink pictogram. Preserve arched outer silhouette, centered front-facing closed door, proportions and threshold. All visible ink MUST be identical flat solid dark brown #542D18. NO tan, gold, white, black or any second visible color. NO shading, gradients, texture or lighting. Fill door face brown too. Show the two simple vertical plank divisions as narrow TRANSPARENT negative-space slots, and right-hand round doorknob as a small TRANSPARENT circular cutout. Slots stop before outer edge so door silhouette stays connected. The outside background and all cutouts must be genuine alpha transparency, not a printed checkerboard, not white. Crisp smooth silhouette, minimal chunky icon readable at 32px. One icon only, unchanged canvas framing.

## Alpha correction

Background-extraction edit. Keep this exact monochrome brown door symbol silhouette and framing. Remove ALL gray/white checkerboard pixels outside the door AND inside the two thin vertical slots AND inside the round doorknob hole. These must become actual alpha=0 transparent pixels. Output genuine transparent PNG cutout, no backdrop. The door ink is one uniform flat brown #542D18, eliminate all shading/texture and gradients. No new colors, no new geometry. This is a single-color icon, not an illustration.
