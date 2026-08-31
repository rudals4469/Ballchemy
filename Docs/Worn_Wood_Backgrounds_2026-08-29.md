# Worn wood backgrounds

Built-in image_gen was used. Existing slate texture and gameplay geometry were retained.

## Full-screen asset

`Assets/_Project/Resources/UI/Workbench/Background_CommonWood.png`

Prompt: Edit this full screen game background texture. Preserve horizontal wooden planks and warm medium brown wood. Remove ALL vignette and side/corner darkening. Left and right edges, top bottom and corners must have the SAME average brightness as the center. Flat uniform diffuse illumination, no central glow, no cast shadows, no gradient. Lighten overall to comfortable medium warm brown, not near black. Continuous fully opaque edge to edge wood texture, landscape 16:9. No added objects or text.

## Center panel asset

`Assets/_Project/Resources/UI/Workbench/Panel_CenterWornWood.png`

Prompt: Game UI asset: one tall portrait wooden backing board, top-down orthographic perfectly front facing, aspect 2:3, 1024x1536. Warm medium brown wood, lighter than dark chocolate, subtle smooth fine grain with low contrast. Entire interior fully solid opaque wood with absolutely no holes, windows or cutouts: game board and HUD will overlay this image. Silhouette approximately a rectangle but gently worn chamfered corners and slightly uneven softly chipped outer edges, not torn paper, not jagged. Thin warm lighter carved beveled perimeter, restrained handcrafted fantasy alchemy workbench style. Entire board nearly fills canvas with only 2 percent transparent margin. TRUE transparent alpha background outside board only. Interior 100 percent opaque. No checkerboard pattern drawn, no ground, no text, no symbols, no objects, no screws. No vignette or interior gradient, uniform diffuse lighting. A tiny subtle shadow immediately OUTSIDE silhouette only.

The board exterior samples have alpha 0. Its generated interior sample has alpha 253, so OpaqueBoardInterior.shader promotes alpha >= 0.9 to 1 while preserving the antialiased outer silhouette. This shader is adapted from the installed URP Sprite-Unlit-Default shader, with sprite behavior retained. The scene and live editor sync reference the new board and material. The live Game view and shader rendering have not been visually verified.
