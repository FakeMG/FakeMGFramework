# Outlines

Requires Unity 6000.3 / URP 17.3 with Render Graph enabled.

## Pixel silhouette

Add `RenderingLayerSilhouetteOutlineRenderFeature`, assign its material and layer filters, and use `RenderingLayerOutlineController` for selection.

Selected meshes merge into one silhouette. Alpha cutouts and vertex deformation are unsupported; transparent occluders must write depth. Upgrade the shader and renderer feature together.

## Baked hull

1. Select a readable Mesh asset and run **Assets > FakeMG > Outline > Create Mesh With Smooth Outline Normals**.
2. Create an outline-only child beneath the original MeshFilter's object, with an identity local transform.
3. Add a MeshFilter using the baked mesh and a MeshRenderer using only the hull outline material. Assign the outline material to every submesh slot.
4. Enable **Use Baked Smooth Normals (UV4)** on that material. Disable shadow casting, shadow receiving, and static batching on the child renderer. Save the child with the model's prefab.

Keep the original MeshFilter and renderer unchanged. Baked normals require unused UV4 and an unbatched static mesh; use the silhouette renderer for skinned meshes.
