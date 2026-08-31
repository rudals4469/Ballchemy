# Retreat and room keys

Integrated into SampleScene using the existing UI objects; positions and pickup behavior are unchanged. Both owned and flying key Images use the new sprites. All five Images preserve aspect ratio.

Assets: `Assets/_Project/Resources/UI/Navigation/Retreat_Alchemy.png`, `Key_Event.png`, `Key_Secret.png` (with Sprite import metadata).

Created with built-in imagegen. Source files remain in the generated_images thread folder.

- Retreat: exec-04135646-1dfe-44a7-a55a-b58106d94c67.png; approved brass U-turn arrow with turquoise accent and alchemical triangle.
- Event: exec-d75fc3b5-ebfc-40f2-b519-f087df9dd125.png. Edit prompt: Replace ONLY the triangle-and-bar emblem within the round turquoise handle with a large cream QUESTION MARK ? with dark brown outline. Actual transparent alpha background, no checkerboard. Preserve outer shape, ornaments, shading, position, size, color, texture, teeth and orientation; do not redesign or flatten.
- Secret: exec-c6d1d99b-ffca-4fba-8340-3a1d2307e2ef.png. Edit prompt: Rotate ONLY the small cream KEY symbol inside the turquoise handle so its round head is upper left and its stem points diagonally LOWER RIGHT, parallel to the long shaft of the outer large key. Keep symbol centered at same size. Remove checkerboard to actual transparent alpha. Preserve all other design details.

Corner alpha checked: 0 for all three final PNGs. The earlier secret-key preview contained opaque checkerboard and is not used. Build passed with zero warnings/errors; Unity Play Mode visual verification remains manual.
