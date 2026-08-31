# Torn parchment cards

## Integration
- SampleScene: 18 authored cards (3 reward, 3 event, 6 shop, 3 alchemy, 3 secret).
- Background: `Assets/_Project/Resources/UI/Workbench/Card_Workbench.png` (existing GUID preserved, sliced rendering).
- Pennant: `Assets/_Project/Resources/UI/Workbench/Card_CategoryPennant.png`.
- Each card owns CategoryPennant / CategorySymbol and a serialized CommonChoiceCardLayout component. Runtime changes category color/sprite only, not their geometry.
- Reward cards switch Ball / Augment category when bound. Other categories are authored on the scene component.
- Reused existing ball and minimap category symbols. No new gameplay icon system.
- Text is authored directly under each card: title 26 pt, auxiliary and description 18 pt; top-right title margin reserves the small pennant. Description overflow retains the existing hover tooltip.
- Reward/Event effectRoot now references the description object itself, preserving visibility control after hierarchy reparenting.
- Reward tier tint remains independent of category pennant color.

## Image generation
Built-in imagegen was used (not CLI). Alpha was preserved by copying PNGs without pixel processing.

Paper production specification: blank cream torn parchment, horizontal 3:1 card, modest worn brown edges, readable clear center, no text, icons, badge, or frame; transparent outside paper. Source: exec-3177e02b-e540-4686-8a78-9349ac88ff36.png from this thread's generated images.

Final pennant prompt:
> Create one small game UI pennant sprite: a flat pure white blank vertical hanging ribbon with a simple V-shaped notch cut into its bottom. Aspect ratio 2:3. Straight sides and top, clean crisp silhouette, NO texture NO stitching NO shading NO shadow NO glow NO background. Actual transparent alpha background outside ribbon including notch. White solid fill inside only. Center with 8% transparent padding. No icons no words. This is a tintable sprite for a casual alchemy parchment card.

The initial stitched pennant was discarded. Final source: exec-e9cf18c6-d3fa-4ad2-92cd-102d4a3fb139.png. Sampled exterior alpha = 0, interior = 254.

## Verification
- dotnet build Assembly-CSharp.csproj --no-restore: 0 warnings, 0 errors.
- Scene: 18 background references and 18 pennants; no duplicate file IDs; 384 RectTransform child links checked against their parent references.
- Play Mode visual verification is still required: long augment descriptions, shop prices, secret costs, event visibility, and scroll clipping. No Unity runtime screenshot was obtained in this pass.
