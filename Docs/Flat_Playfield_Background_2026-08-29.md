# Flat playfield background

- Tool: built-in image_gen edit.
- Asset: `Assets/_Project/Resources/UI/Workbench/Background_CombatSlate.png` (existing GUID preserved).
- Full-screen background: existing `Background_CommonWood.png` retained.
- Center panel: existing opaque Square sprite, RGB (0.12, 0.10, 0.085), alpha 1. Includes navigation, keys and health bar; gameplay colliders unchanged.

## Final image prompt

Edit this game playfield texture. Preserve portrait 1024x1536 aspect and subtle finely grained blue charcoal slate material. Remove ALL vignetting, corner darkening, edge shadows, central spotlight and large-scale brightness gradients. Entire image edge to edge must have the same average medium-dark slate brightness as the existing central area. Very subtle homogeneous stone grain only, uniform flat diffuse illumination, no borders no objects no text. Fully opaque rectangular image, no transparency. This is a flat albedo texture for a game play surface, NOT a photograph of a lit slab.

## Validation

Image inspected visually. Sampled 200x200 corner regions have mean luminance 59.93–62.29 versus center 62.61 (0–255 scale); sampled alpha is 255 throughout. Editor assembly build succeeded with zero warnings/errors. Live Game view has not been visually verified.
