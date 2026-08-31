# Alchemy HP bar — 2026-08-28

- Generated with built-in imagegen; original PNG alpha preserved.
- Assets: Assets/_Project/Resources/UI/HealthBar/HP_Background.png, HP_Liquid.png, HP_Frame.png.
- Importer sprite rectangles trim transparent margins without rewriting source pixels; frame uses sliced rendering, liquid uses horizontal Filled, left origin.
- PlayerHealthPanel: centered lower HUD, 700 x 44 reference units, y=12, unit scale. Matches the existing approximately 700-unit center playfield; not a full-window bar.
- GoldPanel moved under Navigation at (175,495.2), left of retreat. No currency logic changed.
- HealthText retained but inactive (no numbers requested in the design).
- Background and liquid share the same inner rectangle. Fixed foreground frame stays above liquid and damage flash.
- C# build: zero errors/warnings. Scene transform parent-child links validated. Play Mode screenshot not obtained.

## Prompts

### Frame

Use case: precise-object-edit. Use reference health bar only as design reference. Create a production 2D game UI sprite layer, flat casual alchemy style, no text no numbers no shadows no texture. Canvas 1536x512, identical layout: entire thin long bar bounding box x=64 to1472 y=192 to320. True transparent background outside the requested layer. Do not draw checkerboard. ONLY foreground frame: thin dark brown top and bottom edges and small flat ochre brass end caps at x64..100 and x1436..1472. Very subtle thin translucent glass highlight just inside the upper rim. Interior is completely transparent, NO red liquid and NO dark interior fill. Preserve simple straight rectangular horizontal tube silhouette with small rounded caps.

### Liquid

Use case: precise-object-edit. Use reference health bar only as design reference. Create a production 2D game UI sprite layer, flat casual alchemy style, no text no numbers no shadows no texture. Canvas 1536x512, identical layout: entire thin long bar bounding box x=64 to1472 y=192 to320. True transparent background outside the requested layer. Do not draw checkerboard. ONLY the full red liquid fill rectangle x100..1436 y200..312. Vivid solid red, fully filled across its width, flat even color, straight edges. No caps no outline no glass highlight no background, outside this red rectangle fully transparent.

### Background

Use case: precise-object-edit. Use reference health bar only as design reference. Create a production 2D game UI sprite layer, flat casual alchemy style, no text no numbers no shadows no texture. Canvas 1536x512, identical layout: entire thin long bar bounding box x=64 to1472 y=192 to320. True transparent background outside the requested layer. Do not draw checkerboard. ONLY the empty tube interior rectangle x100..1436 y200..312, solid dark warm charcoal gray. Flat uniform color, no caps no outline no liquid no shine. Transparent everywhere outside the rectangle.
