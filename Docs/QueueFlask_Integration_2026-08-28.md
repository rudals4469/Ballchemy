# Queue flask integration

- Scene: Assets/_Project/Scene/SampleScene.unity, NextBallQueuePanel.
- FlaskFrame is drawn before queue items; FlaskGlassOverlay is drawn after items.
- Both RectTransforms: 92 x 960, centered at (0, -57) relative to queue panel center. Small bottom chamber aligns approximately with QueueBottomPoint at y=-460.
- Existing queue anchors, order, movement and launch timing unchanged.
- Old panel Image and NextSlotBackground Image disabled (not deleted); next label retained.
- FrameSource.png and GlassSource.png retain the generated originals, including baked checkerboards. Do NOT use with default UI material.
- QueueFlask.shader keys neutral background out for frame, and keys only darker glass contour detail into white low-opacity glass. Source UV crop is shared between layers.
- Glass.mat _Opacity = 0.16; adjust material Opacity or overlay Image Color alpha in Inspector.
- Textures must remain unatlased: shader crops full source UVs. Import is uncompressed, no mipmaps, 2048 max.
- Generation used built-in imagegen in previous turns, not regenerated during integration.
- Frame source: exec-0a76537b-d545-4246-9950-a662a661b6c4.png
- Glass source: exec-146bf8e1-1869-4c90-947d-1014ac9b035b.png
- Validation: C# build successful. Scene file ID uniqueness and hierarchy links checked. Source center pixels are neutral and keyed transparent. No matching errors in inspected Unity log tail, but live shader compilation and play-mode visual confirmation are NOT established by that check.
