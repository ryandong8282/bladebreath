using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BladeBreath
{
    // Builds the one permitted arena from lightweight, shared-material geometry.
    // It owns presentation only; combat and collision authority stay on the existing fighters.
    public sealed class GreyKilnCourtyard : MonoBehaviour
    {
        private readonly List<Material> _materials = new List<Material>();
        private readonly List<Mesh> _meshes = new List<Mesh>();
        private readonly List<Texture2D> _textures = new List<Texture2D>();

        private Material _floor;
        private Material _fightingGround;
        private Material _outsideEarth;
        private Material _limestone;
        private Material _paleStone;
        private Material _backWallStone;
        private Material _soot;
        private Material _timber;
        private Material _pottery;
        private Material _cloth;
        private Material _bronze;
        private Material _ember;
        private Material _particle;
        private Material _plaster;
        private Material _leafDark;
        private Material _leafDry;
        private Material _grass;
        private Material _flower;
        private Material _water;
        private Material _colossus;

        public void Build(
            Texture2D stoneAlbedo = null,
            Texture2D stoneNormal = null,
            Texture2D wallAlbedo = null,
            Texture2D wallNormal = null,
            Texture2D woodAlbedo = null,
            Texture2D woodNormal = null,
            GameObject landmarkPrefab = null,
            Texture2D colossusAlbedo = null)
        {
            if (transform.childCount > 0)
            {
                return;
            }

            CreatePalette(stoneAlbedo, stoneNormal, wallAlbedo, wallNormal, woodAlbedo, woodNormal, colossusAlbedo);
            ConfigureAtmosphere();
            CreateExterior();
            CreateFloor();
            CreateBoundary();
            CreateHeroBackWall();
            CreateWorldDressing();
            CreateKiln("West kiln", new Vector3(-6.6f, 0f, 4.6f));
            // The generated colossus owns the near-right silhouette. Keep the
            // second kiln deeper in the yard so the two landmarks do not merge.
            CreateKiln("East kiln", landmarkPrefab != null
                ? new Vector3(2.85f, 0f, 7.35f)
                : new Vector3(6.55f, 0f, 4.65f));
            if (landmarkPrefab != null)
            {
                CreateBlenderLandmarks(landmarkPrefab);
            }
            // The Blender landmark kit now places its detailed colossus inside
            // the authored combat frame; do not duplicate it with primitives.
            CreateScaffolding();
            CreateProps();
            CreateHeroFrameDressing();
            CreateLighting();
            CreateDust();
        }

        public void SetGeometryVisible(bool visible)
        {
            foreach (Renderer rendererComponent in GetComponentsInChildren<Renderer>(true))
            {
                rendererComponent.enabled = visible;
            }
        }

        private void CreatePalette(
            Texture2D stoneAlbedo,
            Texture2D stoneNormal,
            Texture2D wallAlbedo,
            Texture2D wallNormal,
            Texture2D woodAlbedo,
            Texture2D woodNormal,
            Texture2D colossusAlbedo)
        {
            // The courtyard plane is thirty metres wide. Repeat the 1K source so
            // the masonry keeps its physical scale instead of becoming a blur.
            _floor = CreateLitMaterial("Ash stone", new Color(0.62f, 0.58f, 0.51f), 0f, 0.075f,
                null, stoneAlbedo, stoneNormal, new Vector2(7.2f, 4.6f));
            _fightingGround = CreateLitMaterial("Trampled ash stone", new Color(0.54f, 0.51f, 0.46f), 0f, 0.045f,
                null, stoneAlbedo, stoneNormal, new Vector2(2.8f, 2.8f));
            _outsideEarth = CreateLitMaterial("Outer ash field", new Color(0.34f, 0.31f, 0.26f), 0f, 0.04f,
                null, stoneAlbedo, stoneNormal, new Vector2(1.8f, 1.8f));
            _limestone = CreateLitMaterial("Kiln limestone", new Color(0.58f, 0.52f, 0.44f), 0f, 0.08f,
                null, wallAlbedo != null ? wallAlbedo : stoneAlbedo,
                wallNormal != null ? wallNormal : stoneNormal, new Vector2(1.8f, 1.8f));
            _paleStone = CreateLitMaterial("Weathered statue", new Color(0.65f, 0.61f, 0.52f), 0f, 0.1f,
                null, wallAlbedo != null ? wallAlbedo : stoneAlbedo,
                wallNormal != null ? wallNormal : stoneNormal, new Vector2(0.85f, 0.85f));
            _backWallStone = CreateLitMaterial("Moonlit kiln wall", new Color(0.72f, 0.67f, 0.58f), 0f, 0.06f,
                new Color(0.018f, 0.019f, 0.024f),
                wallAlbedo != null ? wallAlbedo : stoneAlbedo,
                wallNormal != null ? wallNormal : stoneNormal, new Vector2(1.08f, 1.08f));
            _soot = CreateLitMaterial("Fired soot", new Color(0.024f, 0.019f, 0.015f), 0f, 0.025f);
            _timber = CreateLitMaterial("Charred timber", new Color(0.36f, 0.21f, 0.11f), 0f, 0.045f,
                null, woodAlbedo, woodNormal, new Vector2(1.5f, 2.25f));
            _pottery = CreateLitMaterial("Fired pottery", new Color(0.45f, 0.17f, 0.065f), 0f, 0.08f);
            _cloth = CreateLitMaterial("Office red cloth", new Color(0.13f, 0.012f, 0.008f), 0f, 0.055f);
            _bronze = CreateLitMaterial("Worn register bronze", new Color(0.34f, 0.2f, 0.075f), 0.42f, 0.2f);
            _ember = CreateLitMaterial("Kiln ember", new Color(0.52f, 0.075f, 0.008f), 0f, 0.18f,
                new Color(0.72f, 0.12f, 0.008f));
            _particle = CreateParticleMaterial();
            _plaster = CreateLitMaterial("Faded rammed-earth plaster", new Color(0.47f, 0.24f, 0.14f), 0f, 0.08f);
            _leafDark = CreateLitMaterial("Night cypress foliage", new Color(0.075f, 0.16f, 0.105f), 0f, 0.05f);
            _leafDry = CreateLitMaterial("Ash garden foliage", new Color(0.19f, 0.25f, 0.12f), 0f, 0.04f);
            _grass = CreateLitMaterial("Courtyard grass", new Color(0.16f, 0.25f, 0.11f), 0f, 0.03f);
            _flower = CreateLitMaterial("Small bell flowers", new Color(0.42f, 0.16f, 0.22f), 0f, 0.08f,
                new Color(0.05f, 0.012f, 0.02f));
            _water = CreateLitMaterial("Moonlit garden water", new Color(0.055f, 0.14f, 0.16f), 0.05f, 0.58f);
            _colossus = CreateLitMaterial(
                "Generated weathered colossus",
                new Color(0.9f, 0.86f, 0.78f),
                0f,
                0.08f,
                null,
                colossusAlbedo,
                null,
                Vector2.one);
            SetDoubleSided(_grass);
            SetDoubleSided(_flower);
        }

        private void ConfigureAtmosphere()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.295f, 0.33f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.022f, 0.027f, 0.034f);
            RenderSettings.fogStartDistance = 11f;
            RenderSettings.fogEndDistance = 30f;
        }

        private void CreateExterior()
        {
            CreatePrimitive("Outer ash field", PrimitiveType.Plane, new Vector3(0f, -0.16f, 0.4f),
                Quaternion.identity, new Vector3(5.2f, 1f, 3.85f), _outsideEarth, false, false);
            CreatePrimitive("South approach road", PrimitiveType.Cube, new Vector3(0f, -0.09f, -9.15f),
                Quaternion.Euler(0f, -2f, 0f), new Vector3(7.8f, 0.035f, 10.8f), _soot, false, false);

            for (int i = 0; i < 26; i++)
            {
                float x = -22.1f + i * 1.75f;
                float z = i % 2 == 0 ? 10.65f + (i % 3) * 0.55f : -10.7f - (i % 4) * 0.46f;
                CreatePrimitive("Outer rubble " + i, i % 4 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube,
                    new Vector3(x, 0.02f + (i % 3) * 0.055f, z),
                    Quaternion.Euler(i * 9f, i * 29f, i * 5f),
                    new Vector3(0.34f + (i % 3) * 0.14f, 0.2f + (i % 2) * 0.1f, 0.4f + (i % 4) * 0.1f),
                    i % 3 == 0 ? _paleStone : _limestone, false);
            }

            CreatePrimitive("West outer broken wall", PrimitiveType.Cube, new Vector3(-18.2f, 0.38f, 2.7f),
                Quaternion.Euler(0f, 7f, -3f), new Vector3(5.8f, 0.78f, 0.38f), _limestone);
            CreatePrimitive("East outer broken wall", PrimitiveType.Cube, new Vector3(18.4f, 0.32f, -2.8f),
                Quaternion.Euler(0f, -9f, 2f), new Vector3(5.5f, 0.64f, 0.38f), _limestone);
            CreateExteriorKiln("Far west kiln", new Vector3(-13.8f, 0f, 11.55f));
            CreateExteriorKiln("Far east kiln", new Vector3(13.55f, 0f, 11.8f));
            CreateDistantWorkshopSilhouette(-20.5f, 9.8f, 1f);
            CreateDistantWorkshopSilhouette(20.2f, 8.9f, -1f);
        }

        private void CreateDistantWorkshopSilhouette(float x, float z, float direction)
        {
            CreatePrimitive("Distant workshop wall", PrimitiveType.Cube, new Vector3(x, 1.15f, z),
                Quaternion.Euler(0f, direction * 4f, 0f), new Vector3(5.8f, 2.3f, 0.45f), _soot, false, false);
            CreatePrimitive("Distant workshop roof", PrimitiveType.Cube, new Vector3(x, 2.55f, z),
                Quaternion.Euler(0f, direction * 4f, direction * 4f), new Vector3(6.7f, 0.24f, 1.45f), _timber, false, false);
            for (int i = 0; i < 3; i++)
            {
                CreatePrimitive("Distant chimney", PrimitiveType.Cylinder,
                    new Vector3(x + direction * (1.6f - i * 1.2f), 3.1f + i * 0.25f, z + 0.25f),
                    Quaternion.Euler(0f, 0f, direction * (2f + i)), new Vector3(0.24f, 0.9f + i * 0.18f, 0.24f),
                    _soot, false, false);
            }
        }

        private void CreateExteriorKiln(string name, Vector3 position)
        {
            Transform kiln = new GameObject(name).transform;
            kiln.SetParent(transform, false);
            CreatePrimitive(name + " body", PrimitiveType.Cube, position + new Vector3(0f, 0.72f, 0f),
                Quaternion.identity, new Vector3(1.65f, 1.45f, 1.2f), _soot, false);
            CreatePrimitive(name + " chimney", PrimitiveType.Cylinder, position + new Vector3(0.35f, 1.75f, 0.12f),
                Quaternion.Euler(0f, 0f, -4f), new Vector3(0.3f, 0.9f, 0.3f), _soot, false);
            CreatePrimitive(name + " mouth", PrimitiveType.Cube, position + new Vector3(0f, 0.55f, -0.62f),
                Quaternion.identity, new Vector3(0.55f, 0.5f, 0.05f), _ember, false, false);
        }

        private void CreateFloor()
        {
            CreatePrimitive("Courtyard floor", PrimitiveType.Plane, new Vector3(0f, -0.04f, 0f),
                Quaternion.identity, new Vector3(3.05f, 1f, 1.88f), _floor, true);
            CreatePrimitive("Sooted fighting circle", PrimitiveType.Cylinder, new Vector3(0f, 0.002f, 0f),
                Quaternion.identity, new Vector3(10.35f, 0.014f, 10.35f), _fightingGround);

            // Narrow radial courses read as fitted masonry seams at game distance.
            // Broad blocks here looked like loose planks and exposed the greybox.
            CreateStoneAnnulus("Outer carved ring", 5.12f, 0.38f, _paleStone);
            CreateStoneAnnulus("Inner carved ring", 3.42f, 0.24f, _paleStone);

            CreateZoneFloor("South processional approach", new Vector3(0f, 0.03f, -6.85f), new Vector3(6.2f, 0.045f, 3.55f), _soot);
            CreateZoneFloor("West image yard", new Vector3(-10.3f, 0.025f, 2.7f), new Vector3(7.1f, 0.04f, 9.2f), _limestone);
            CreateZoneFloor("East firing yard", new Vector3(10.45f, 0.026f, 2.55f), new Vector3(7.2f, 0.042f, 9.4f), _soot);
        }

        private void CreateZoneFloor(string name, Vector3 position, Vector3 scale, Material material)
        {
            CreatePrimitive(name, PrimitiveType.Cube, position, Quaternion.identity, scale, material, false, false);
        }

        private void CreateProcessionalPavers()
        {
            for (int i = 0; i < 9; i++)
            {
                float z = -8.25f + i * 0.86f;
                CreatePrimitive("Approach paver " + i, PrimitiveType.Cube,
                    new Vector3((i % 2 == 0 ? -0.06f : 0.08f), 0.075f, z),
                    Quaternion.Euler(0f, (i % 3 - 1) * 2.5f, 0f),
                    new Vector3(2.4f, 0.07f, 0.64f), i % 3 == 0 ? _paleStone : _limestone);
            }
        }

        private void CreatePaverRing(string name, float radius, int count, Vector3 scale, Material material, float y)
        {
            Transform ring = new GameObject(name).transform;
            ring.SetParent(transform, false);
            for (int i = 0; i < count; i++)
            {
                float degrees = 360f * i / count;
                float angle = degrees * Mathf.Deg2Rad;
                float radialJitter = Mathf.Sin(i * 12.9898f) * 0.025f;
                GameObject slab = CreatePrimitive(
                    "Stone " + (i + 1),
                    PrimitiveType.Cube,
                    new Vector3(Mathf.Cos(angle) * (radius + radialJitter), y + (i % 3) * 0.008f,
                        Mathf.Sin(angle) * (radius + radialJitter)),
                    Quaternion.Euler((i % 2) * 0.4f, 90f - degrees + (i % 3 - 1) * 0.8f, 0f),
                    scale + new Vector3((i % 2) * 0.025f, 0f, 0f),
                    material);
                slab.transform.SetParent(ring, true);
            }
        }

        private void CreateStoneAnnulus(string name, float radius, float width, Material material)
        {
            const int segments = 64;
            const float gapRatio = 0.075f;
            var vertices = new Vector3[segments * 4];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            float inner = radius - width * 0.5f;
            float outer = radius + width * 0.5f;
            for (int i = 0; i < segments; i++)
            {
                float start = Mathf.PI * 2f * (i + gapRatio * 0.5f) / segments;
                float end = Mathf.PI * 2f * (i + 1f - gapRatio * 0.5f) / segments;
                int vertex = i * 4;
                vertices[vertex] = new Vector3(Mathf.Cos(start) * inner, 0.031f, Mathf.Sin(start) * inner);
                vertices[vertex + 1] = new Vector3(Mathf.Cos(start) * outer, 0.031f, Mathf.Sin(start) * outer);
                vertices[vertex + 2] = new Vector3(Mathf.Cos(end) * inner, 0.031f, Mathf.Sin(end) * inner);
                vertices[vertex + 3] = new Vector3(Mathf.Cos(end) * outer, 0.031f, Mathf.Sin(end) * outer);
                uv[vertex] = new Vector2(0f, 0f);
                uv[vertex + 1] = new Vector2(0f, 1f);
                uv[vertex + 2] = new Vector2(1f, 0f);
                uv[vertex + 3] = new Vector2(1f, 1f);

                int index = i * 6;
                // Clockwise from above so URP does not back-face cull the ring.
                triangles[index] = vertex;
                triangles[index + 1] = vertex + 2;
                triangles[index + 2] = vertex + 3;
                triangles[index + 3] = vertex;
                triangles[index + 4] = vertex + 3;
                triangles[index + 5] = vertex + 1;
            }

            var mesh = new Mesh { name = name + " mesh" };
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _meshes.Add(mesh);

            GameObject ring = new GameObject(name);
            ring.transform.SetParent(transform, false);
            ring.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = ring.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private void CreateBoundary()
        {
            CreatePrimitive("North boundary", PrimitiveType.Cube, new Vector3(0f, 0.62f, 9.25f),
                Quaternion.identity, new Vector3(30.5f, 1.24f, 0.5f), _limestone, true);
            CreatePrimitive("South boundary", PrimitiveType.Cube, new Vector3(0f, 0.48f, -9.25f),
                Quaternion.identity, new Vector3(30.5f, 0.96f, 0.5f), _limestone, true);
            CreatePrimitive("East boundary", PrimitiveType.Cube, new Vector3(15f, 0.55f, 0f),
                Quaternion.identity, new Vector3(0.5f, 1.1f, 19f), _limestone, true);
            CreatePrimitive("West boundary", PrimitiveType.Cube, new Vector3(-15f, 0.55f, 0f),
                Quaternion.identity, new Vector3(0.5f, 1.1f, 19f), _limestone, true);

            for (int i = -6; i <= 6; i++)
            {
                CreatePrimitive("North wall cap " + i, PrimitiveType.Cube,
                    new Vector3(i * 2.25f, 1.28f + Mathf.Abs(i % 2) * 0.08f, 9.1f),
                    Quaternion.Euler(0f, i * 2.5f, i % 2 == 0 ? 0f : 2.5f),
                    new Vector3(2f, 0.22f, 0.78f), _paleStone);
            }
        }

        private void CreateWorldDressing()
        {
            CreateMemorialGarden();
            CreateKilnPrisonEntrance();
            CreateNorthernCourtSkyline();
        }

        private void CreateHeroBackWall()
        {
            Transform wall = CreateRoot("Grey kiln hero back wall");
            for (int i = 0; i < 9; i++)
            {
                float x = -9.2f + i * 2.3f;
                float height = 3.8f + (i % 4) * 0.22f;
                Parent(CreatePrimitive("Rammed stone wall bay " + i, PrimitiveType.Cube,
                    new Vector3(x, height * 0.5f, 8.55f + (i % 2) * 0.06f),
                    Quaternion.Euler(0f, (i % 3 - 1) * 0.8f, 0f),
                    new Vector3(2.34f, height, 0.48f), i % 3 == 0 ? _paleStone : _backWallStone,
                    false), wall);
            }

            Parent(CreatePrimitive("Central kiln-office recess", PrimitiveType.Cube,
                new Vector3(0f, 1.55f, 8.24f), Quaternion.identity,
                new Vector3(2.65f, 2.75f, 0.12f), _soot, false, false), wall);
            Parent(CreatePrimitive("Central recess lintel", PrimitiveType.Cube,
                new Vector3(0f, 3.04f, 8.05f), Quaternion.Euler(0f, 0f, -1.2f),
                new Vector3(3.35f, 0.32f, 0.48f), _backWallStone, false), wall);
            Parent(CreatePrimitive("Central recess jamb left", PrimitiveType.Cube,
                new Vector3(-1.46f, 1.5f, 8.04f), Quaternion.Euler(0f, 0f, 1.1f),
                new Vector3(0.34f, 2.9f, 0.46f), _limestone, false), wall);
            Parent(CreatePrimitive("Central recess jamb right", PrimitiveType.Cube,
                new Vector3(1.46f, 1.5f, 8.04f), Quaternion.Euler(0f, 0f, -1.1f),
                new Vector3(0.34f, 2.9f, 0.46f), _limestone, false), wall);

            for (int i = 0; i < 6; i++)
            {
                float x = -8.2f + i * 3.25f;
                Parent(CreatePrimitive("Charred wall pier " + i, PrimitiveType.Cylinder,
                    new Vector3(x, 2.05f, 7.98f), Quaternion.Euler(0f, 0f, (i % 2 == 0 ? -1.5f : 1.5f)),
                    new Vector3(0.14f, 2.05f, 0.14f), _timber, false), wall);
            }

            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 7; column++)
                {
                    if (column >= 2 && column <= 4 && row < 3) continue;
                    float x = -5.9f + column * 1.95f + (row % 2) * 0.22f;
                    float y = 0.45f + row * 0.72f;
                    Parent(CreatePrimitive("Exposed wall block " + row + "-" + column,
                        PrimitiveType.Cube, new Vector3(x, y, 7.91f),
                        Quaternion.Euler((column % 2) * 1.2f, (row * 7f + column * 3f) % 7f - 3f, 0f),
                        new Vector3(1.45f, 0.5f, 0.24f),
                        (row + column) % 4 == 0 ? _paleStone : _backWallStone, false), wall);
                }
            }
        }

        private void CreateMemorialGarden()
        {
            Transform garden = CreateRoot("West memorial garden");

            Parent(CreatePrimitive("Garden earth", PrimitiveType.Cube, new Vector3(-10.85f, 0.015f, -6.55f),
                Quaternion.Euler(0f, -2f, 0f), new Vector3(7.15f, 0.045f, 4.6f), _outsideEarth, false, false), garden);
            Parent(CreatePrimitive("Moon pond", PrimitiveType.Cube, new Vector3(-10.65f, 0.055f, -6.62f),
                Quaternion.Euler(0f, -4f, 0f), new Vector3(3.65f, 0.055f, 1.72f), _water, false, false), garden);

            Vector3[] curbPositions =
            {
                new Vector3(-10.65f, 0.13f, -7.57f), new Vector3(-10.65f, 0.13f, -5.66f),
                new Vector3(-12.57f, 0.13f, -6.62f), new Vector3(-8.73f, 0.13f, -6.62f)
            };
            Vector3[] curbScales =
            {
                new Vector3(4.15f, 0.16f, 0.24f), new Vector3(4.15f, 0.16f, 0.24f),
                new Vector3(0.24f, 0.16f, 2.15f), new Vector3(0.24f, 0.16f, 2.15f)
            };
            for (int i = 0; i < curbPositions.Length; i++)
            {
                Parent(CreatePrimitive("Pond curb " + i, PrimitiveType.Cube, curbPositions[i], Quaternion.identity,
                    curbScales[i], i % 2 == 0 ? _paleStone : _limestone, false, false), garden);
            }

            for (int i = 0; i < 6; i++)
            {
                float x = -12.05f + i * 0.56f;
                float z = -6.63f + Mathf.Sin(i * 1.7f) * 0.27f;
                Parent(CreatePrimitive("Pond stepping stone " + i, PrimitiveType.Cylinder,
                    new Vector3(x, 0.13f + (i % 2) * 0.015f, z), Quaternion.identity,
                    new Vector3(0.34f + (i % 2) * 0.05f, 0.055f, 0.28f), _paleStone, false, false), garden);
            }

            CreateGardenTree(garden, "Old cypress", new Vector3(-13.3f, 0f, -5.15f), 1.15f, _leafDark);
            CreateGardenTree(garden, "Wind-bent tree", new Vector3(-8.15f, 0f, -7.62f), 0.9f, _leafDry);
            CreateGardenTree(garden, "Wall pine", new Vector3(-13.55f, 0f, -8.05f), 0.72f, _leafDark);

            var grassPositions = new List<Vector3>();
            var flowerPositions = new List<Vector3>();
            for (int i = 0; i < 32; i++)
            {
                float x = -14f + (i % 8) * 0.84f + Mathf.Sin(i * 2.3f) * 0.13f;
                float z = -8.45f + (i / 8) * 1.12f + Mathf.Cos(i * 1.9f) * 0.16f;
                bool insidePond = x > -12.75f && x < -8.55f && z > -7.8f && z < -5.45f;
                if (!insidePond) grassPositions.Add(new Vector3(x, 0.08f, z));
                if (!insidePond && i % 3 == 0) flowerPositions.Add(new Vector3(x + 0.18f, 0.1f, z - 0.12f));
            }
            CreateCrossedFoliageBatch(garden, "Garden grass batch", grassPositions, 0.28f, 0.38f, _grass);
            CreateCrossedFoliageBatch(garden, "Garden flower batch", flowerPositions, 0.18f, 0.32f, _flower);

            Parent(CreatePrimitive("Garden scholar stone", PrimitiveType.Sphere, new Vector3(-8.25f, 0.68f, -5.35f),
                Quaternion.Euler(8f, 19f, -7f), new Vector3(0.62f, 1.25f, 0.46f), _paleStone, false), garden);
            Parent(CreatePrimitive("Garden stone base", PrimitiveType.Cube, new Vector3(-8.25f, 0.16f, -5.35f),
                Quaternion.Euler(0f, 12f, 0f), new Vector3(1.1f, 0.24f, 0.78f), _limestone, false), garden);
        }

        private void CreateGardenTree(Transform parent, string name, Vector3 position, float scale, Material foliage)
        {
            Parent(CreatePrimitive(name + " trunk", PrimitiveType.Cylinder, position + new Vector3(0f, 1.25f * scale, 0f),
                Quaternion.Euler(0f, 0f, -4f), new Vector3(0.19f * scale, 1.25f * scale, 0.19f * scale),
                _timber, false, false), parent);
            Parent(CreatePrimitive(name + " lower crown", PrimitiveType.Sphere, position + new Vector3(-0.18f, 2.35f, 0.04f) * scale,
                Quaternion.Euler(0f, 16f, -4f), new Vector3(1.05f, 0.74f, 0.92f) * scale, foliage, false, false), parent);
            Parent(CreatePrimitive(name + " upper crown", PrimitiveType.Sphere, position + new Vector3(0.22f, 3.08f, -0.06f) * scale,
                Quaternion.Euler(0f, -11f, 7f), new Vector3(0.76f, 0.86f, 0.68f) * scale, foliage, false, false), parent);
        }

        private void CreateKilnPrisonEntrance()
        {
            Transform prison = CreateRoot("Kiln prison entrance");
            const float x = 8.15f;

            Parent(CreatePrimitive("Prison black doorway", PrimitiveType.Cube, new Vector3(x, 0.92f, -8.82f),
                Quaternion.identity, new Vector3(2.55f, 1.85f, 0.1f), _soot, false, false), prison);
            Parent(CreatePrimitive("Prison left jamb", PrimitiveType.Cube, new Vector3(x - 1.42f, 1.02f, -8.65f),
                Quaternion.Euler(0f, 2f, 0f), new Vector3(0.42f, 2.05f, 0.62f), _limestone, false), prison);
            Parent(CreatePrimitive("Prison right jamb", PrimitiveType.Cube, new Vector3(x + 1.42f, 1.02f, -8.65f),
                Quaternion.Euler(0f, -2f, 0f), new Vector3(0.42f, 2.05f, 0.62f), _limestone, false), prison);
            Parent(CreatePrimitive("Prison lintel", PrimitiveType.Cube, new Vector3(x, 2.03f, -8.64f),
                Quaternion.Euler(0f, 0f, -1.5f), new Vector3(3.3f, 0.42f, 0.72f), _paleStone, false), prison);
            Parent(CreatePrimitive("Prison brow roof", PrimitiveType.Cube, new Vector3(x, 2.42f, -8.68f),
                Quaternion.Euler(0f, 0f, 1.4f), new Vector3(3.85f, 0.22f, 1.05f), _soot, false, false), prison);

            for (int i = 0; i < 6; i++)
            {
                float z = -7.9f + i * 0.34f;
                float y = 0.2f - i * 0.025f;
                Parent(CreatePrimitive("Descending prison step " + i, PrimitiveType.Cube, new Vector3(x, y, z),
                    Quaternion.identity, new Vector3(2.45f - i * 0.05f, 0.17f, 0.44f),
                    i % 2 == 0 ? _paleStone : _limestone, false, false), prison);
            }
            Parent(CreatePrimitive("Prison stair cheek left", PrimitiveType.Cube, new Vector3(x - 1.4f, 0.43f, -7.05f),
                Quaternion.Euler(9f, 0f, 0f), new Vector3(0.28f, 0.58f, 2.45f), _limestone, false, false), prison);
            Parent(CreatePrimitive("Prison stair cheek right", PrimitiveType.Cube, new Vector3(x + 1.4f, 0.43f, -7.05f),
                Quaternion.Euler(9f, 0f, 0f), new Vector3(0.28f, 0.58f, 2.45f), _limestone, false, false), prison);

            for (int i = 0; i < 5; i++)
            {
                Parent(CreatePrimitive("Prison gate bar " + i, PrimitiveType.Cube,
                    new Vector3(x - 0.82f + i * 0.41f, 0.93f, -8.51f), Quaternion.identity,
                    new Vector3(0.09f, 1.65f, 0.09f), _bronze, false, false), prison);
            }
            Parent(CreatePrimitive("Prison gate crossbar", PrimitiveType.Cube, new Vector3(x, 1.04f, -8.49f),
                Quaternion.Euler(0f, 0f, -2f), new Vector3(2.05f, 0.12f, 0.12f), _bronze, false, false), prison);
            Parent(CreatePrimitive("Prison brazier", PrimitiveType.Sphere, new Vector3(x + 1.95f, 1.42f, -8.2f),
                Quaternion.identity, new Vector3(0.28f, 0.18f, 0.28f), _ember, false, false), prison);
        }

        private void CreateNorthernCourtSkyline()
        {
            Transform skyline = CreateRoot("Northern court skyline");
            CreateDistantHall(skyline, "Archive hall", new Vector3(8.7f, 0f, 11.25f), 1f);

            Parent(CreatePrimitive("Distant corridor plinth", PrimitiveType.Cube, new Vector3(-10.45f, 0.28f, 10.8f),
                Quaternion.Euler(0f, 2f, 0f), new Vector3(8.2f, 0.55f, 1.9f), _limestone, false, false), skyline);
            Parent(CreatePrimitive("Distant corridor wall", PrimitiveType.Cube, new Vector3(-10.45f, 1.32f, 11.12f),
                Quaternion.Euler(0f, 2f, 0f), new Vector3(7.65f, 1.7f, 0.72f), _plaster, false, false), skyline);
            for (int i = 0; i < 5; i++)
            {
                float pillarX = -13.45f + i * 1.5f;
                Parent(CreatePrimitive("Distant corridor post " + i, PrimitiveType.Cylinder,
                    new Vector3(pillarX, 1.35f, 10.47f), Quaternion.identity,
                    new Vector3(0.12f, 1.18f, 0.12f), _timber, false, false), skyline);
            }
            Parent(CreatePrimitive("Distant corridor eave", PrimitiveType.Cube, new Vector3(-10.45f, 2.45f, 10.82f),
                Quaternion.Euler(8f, 2f, 0f), new Vector3(8.55f, 0.22f, 1.85f), _soot, false, false), skyline);
            Parent(CreatePrimitive("Distant corridor ridge", PrimitiveType.Cylinder, new Vector3(-10.45f, 2.7f, 11.25f),
                Quaternion.Euler(0f, 0f, 90f), new Vector3(0.1f, 4.2f, 0.1f), _paleStone, false, false), skyline);
        }

        private void CreateDistantHall(Transform parent, string name, Vector3 position, float scale)
        {
            Parent(CreatePrimitive(name + " rammed-earth platform", PrimitiveType.Cube, position + new Vector3(0f, 0.34f, 0f),
                Quaternion.identity, new Vector3(7.1f, 0.68f, 3.55f) * scale, _limestone, false, false), parent);
            Parent(CreatePrimitive(name + " plaster wall", PrimitiveType.Cube, position + new Vector3(0f, 1.55f, 0.42f),
                Quaternion.identity, new Vector3(5.85f, 1.9f, 1.65f) * scale, _plaster, false, false), parent);
            for (int i = 0; i < 4; i++)
            {
                float pillarX = position.x - 2.35f * scale + i * 1.56f * scale;
                Parent(CreatePrimitive(name + " timber post " + i, PrimitiveType.Cylinder,
                    new Vector3(pillarX, position.y + 1.55f * scale, position.z - 0.72f * scale), Quaternion.identity,
                    new Vector3(0.16f, 1.4f, 0.16f) * scale, _timber, false, false), parent);
                Parent(CreatePrimitive(name + " bracket block " + i, PrimitiveType.Cube,
                    new Vector3(pillarX, position.y + 2.78f * scale, position.z - 0.7f * scale), Quaternion.identity,
                    new Vector3(0.68f, 0.18f, 0.48f) * scale, _timber, false, false), parent);
            }
            Parent(CreatePrimitive(name + " front roof slope", PrimitiveType.Cube,
                position + new Vector3(0f, 3.35f, -0.72f) * scale, Quaternion.Euler(13f, 0f, 0f),
                new Vector3(7.35f, 0.22f, 2.05f) * scale, _soot, false, false), parent);
            Parent(CreatePrimitive(name + " rear roof slope", PrimitiveType.Cube,
                position + new Vector3(0f, 3.35f, 0.82f) * scale, Quaternion.Euler(-13f, 0f, 0f),
                new Vector3(7.35f, 0.22f, 2.05f) * scale, _soot, false, false), parent);
            Parent(CreatePrimitive(name + " ridge", PrimitiveType.Cylinder,
                position + new Vector3(0f, 3.72f, 0.05f) * scale, Quaternion.Euler(0f, 0f, 90f),
                new Vector3(0.13f, 3.75f, 0.13f) * scale, _paleStone, false, false), parent);
        }

        private void CreateCrossedFoliageBatch(
            Transform parent,
            string name,
            IReadOnlyList<Vector3> positions,
            float width,
            float height,
            Material material)
        {
            var vertices = new List<Vector3>(positions.Count * 6);
            var triangles = new List<int>(positions.Count * 6);
            var uvs = new List<Vector2>(positions.Count * 6);
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 p = positions[i];
                AddFoliageQuad(vertices, triangles, uvs, p, Vector3.right * width, height);
                AddFoliageQuad(vertices, triangles, uvs, p, Vector3.forward * width, height);
            }

            var mesh = new Mesh { name = name + " mesh" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _meshes.Add(mesh);

            var batch = new GameObject(name);
            batch.transform.SetParent(parent, false);
            batch.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer rendererComponent = batch.AddComponent<MeshRenderer>();
            rendererComponent.sharedMaterial = material;
            rendererComponent.shadowCastingMode = ShadowCastingMode.Off;
            rendererComponent.receiveShadows = true;
        }

        private static void AddFoliageQuad(
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs,
            Vector3 center,
            Vector3 axis,
            float height)
        {
            int start = vertices.Count;
            Vector3 halfAxis = axis * 0.5f;
            vertices.Add(center - halfAxis);
            vertices.Add(center + halfAxis);
            vertices.Add(center + Vector3.up * height);
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(0.5f, 1f));
            triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
        }

        private Transform CreateRoot(string name)
        {
            var root = new GameObject(name).transform;
            root.SetParent(transform, false);
            return root;
        }

        private static GameObject Parent(GameObject child, Transform parent)
        {
            child.transform.SetParent(parent, true);
            return child;
        }

        private void CreateBlenderLandmarks(GameObject landmarkPrefab)
        {
            GameObject landmarkRoot = Instantiate(landmarkPrefab, transform, false);
            landmarkRoot.name = "Blender Grey Kiln Landmarks";
            landmarkRoot.transform.localPosition = new Vector3(0f, 0f, -2.75f);
            // Blender's -Z-forward FBX conversion mirrors the authored north/south
            // composition relative to Unity's gameplay axes. Rotate the whole kit
            // once so the gate is north, the image yard west and the kilns east.
            landmarkRoot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            landmarkRoot.transform.localScale = Vector3.one;

            foreach (Collider colliderComponent in landmarkRoot.GetComponentsInChildren<Collider>(true))
            {
                colliderComponent.enabled = false;
                Destroy(colliderComponent);
            }

            Renderer[] landmarkRenderers = landmarkRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer rendererComponent in landmarkRenderers)
            {
                string lowerName = rendererComponent.name.ToLowerInvariant();
                if (lowerName.Contains("northgate"))
                {
                    // This authored kit also carries a gate study. It reads as a
                    // raised stage in the fixed combat camera, so keep it out of
                    // the Grey Kiln slice and let the kiln wall own the backdrop.
                    rendererComponent.enabled = false;
                    continue;
                }

                Material material = _limestone;
                if (lowerName.Contains("ai_brokencolossus"))
                {
                    // Image-to-3D tools emit meshes in arbitrary authoring scales and
                    // positions. Normalize the landmark against the gameplay arena so
                    // Blender re-exports cannot quietly push it outside the camera.
                    const float targetHeight = 3.85f;
                    const float targetX = 4.05f;
                    const float targetZ = 5.9f;
                    // The FBX conversion leaves the generated statue showing its
                    // narrow side to the fixed camera. Present the authored front.
                    rendererComponent.transform.Rotate(Vector3.up, 90f, Space.World);
                    Bounds bounds = rendererComponent.bounds;
                    if (bounds.size.y > 0.01f)
                    {
                        rendererComponent.transform.localScale *= targetHeight / bounds.size.y;
                        bounds = rendererComponent.bounds;
                    }

                    Vector3 currentBottomCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                    // Bury the generated bell-shaped pedestal in rubble so the
                    // broken face and shoulders, rather than its base, own the frame.
                    Vector3 targetBottomCenter = new Vector3(targetX, -0.9f, targetZ);
                    rendererComponent.transform.position += targetBottomCenter - currentBottomCenter;
                    rendererComponent.gameObject.SetActive(true);
                    rendererComponent.enabled = true;
                    material = _colossus;
                }
                else if (lowerName.Contains("pale")) material = _paleStone;
                else if (lowerName.Contains("timber")) material = _timber;
                else if (lowerName.Contains("roof") || lowerName.Contains("soot")) material = _soot;
                else if (lowerName.Contains("ember")) material = _ember;
                else if (lowerName.Contains("pottery")) material = _pottery;
                else if (lowerName.Contains("pigment")) material = _cloth;
                else if (lowerName.Contains("bronze")) material = _bronze;
                rendererComponent.sharedMaterial = material;
                rendererComponent.shadowCastingMode = ShadowCastingMode.On;
                rendererComponent.receiveShadows = true;
            }
        }

        private void CreateKiln(string name, Vector3 position)
        {
            Transform kiln = new GameObject(name).transform;
            kiln.SetParent(transform, false);
            kiln.localPosition = position;

            CreatePrimitive("Kiln body", PrimitiveType.Sphere, position + new Vector3(0f, 1.18f, 0.28f),
                Quaternion.identity, new Vector3(1.5f, 1.78f, 1.22f), _limestone);
            CreatePrimitive("Kiln crown", PrimitiveType.Cylinder, position + new Vector3(0.38f, 2.54f, 0.36f),
                Quaternion.Euler(0f, 0f, -3f), new Vector3(0.42f, 0.42f, 0.42f), _soot);
            CreatePrimitive("Kiln throat", PrimitiveType.Sphere, position + new Vector3(0f, 0.9f, -0.89f),
                Quaternion.identity, new Vector3(0.72f, 0.88f, 0.12f), _soot);
            CreatePrimitive("Left jamb", PrimitiveType.Cube, position + new Vector3(-0.82f, 0.76f, -0.69f),
                Quaternion.identity, new Vector3(0.38f, 1.52f, 0.48f), _paleStone);
            CreatePrimitive("Right jamb", PrimitiveType.Cube, position + new Vector3(0.82f, 0.76f, -0.69f),
                Quaternion.identity, new Vector3(0.38f, 1.52f, 0.48f), _paleStone);
            for (int i = 0; i < 9; i++)
            {
                float degrees = 18f + i * 18f;
                float radians = degrees * Mathf.Deg2Rad;
                Vector3 brickPosition = position + new Vector3(
                    Mathf.Cos(radians) * 0.93f,
                    0.88f + Mathf.Sin(radians) * 0.93f,
                    -0.96f);
                CreatePrimitive("Kiln arch brick " + i, PrimitiveType.Cube, brickPosition,
                    Quaternion.Euler(0f, 0f, 90f - degrees),
                    new Vector3(0.34f, 0.2f, 0.28f), i % 3 == 0 ? _paleStone : _limestone);
            }
            CreatePrimitive("Chimney", PrimitiveType.Cylinder, position + new Vector3(0.45f, 3.08f, 0.42f),
                Quaternion.Euler(0f, 0f, -4f), new Vector3(0.48f, 0.72f, 0.48f), _soot);

            var lightObject = new GameObject("Kiln glow");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = position + new Vector3(0f, 0.95f, -1.05f);
            Light kilnLight = lightObject.AddComponent<Light>();
            kilnLight.type = LightType.Point;
            kilnLight.color = new Color(1f, 0.58f, 0.28f);
            kilnLight.intensity = 3.8f;
            kilnLight.range = 5.4f;
            kilnLight.shadows = LightShadows.None;
            CreateKilnFlames(name + " flame", position + new Vector3(0f, 0.72f, -1.04f));
            CreateEmbers(name + " flying embers", position + new Vector3(0f, 0.86f, -1.03f));
        }

        private void CreateBrokenColossus()
        {
            Vector3 basePosition = new Vector3(5.15f, 0f, 6.15f);
            Transform statue = new GameObject("Broken Northern Qi colossus").transform;
            statue.SetParent(transform, false);

            CreatePrimitive("Buried torso", PrimitiveType.Cube, basePosition + new Vector3(0f, 0.9f, 0f),
                Quaternion.Euler(-8f, -10f, 4f), new Vector3(1.82f, 1.46f, 0.92f), _backWallStone);
            CreatePrimitive("Broken neck", PrimitiveType.Cylinder, basePosition + new Vector3(-0.06f, 1.76f, -0.04f),
                Quaternion.Euler(4f, 0f, -9f), new Vector3(0.38f, 0.28f, 0.38f), _limestone);
            CreatePrimitive("Colossus head", PrimitiveType.Sphere, basePosition + new Vector3(-0.11f, 2.58f, -0.16f),
                Quaternion.Euler(2f, -7f, -8f), new Vector3(0.9f, 1f, 0.74f), _backWallStone);
            CreatePrimitive("Colossus top knot", PrimitiveType.Sphere, basePosition + new Vector3(-0.17f, 3.43f, -0.06f),
                Quaternion.Euler(3f, -5f, -8f), new Vector3(0.31f, 0.37f, 0.28f), _limestone);
            CreatePrimitive("Brow", PrimitiveType.Cube, basePosition + new Vector3(-0.11f, 2.75f, -0.83f),
                Quaternion.Euler(2f, -7f, -8f), new Vector3(0.91f, 0.12f, 0.09f), _limestone);
            CreatePrimitive("Nose", PrimitiveType.Cube, basePosition + new Vector3(-0.11f, 2.5f, -0.92f),
                Quaternion.Euler(8f, -7f, -8f), new Vector3(0.15f, 0.38f, 0.14f), _limestone);
            CreatePrimitive("Eye shadow left", PrimitiveType.Cube, basePosition + new Vector3(-0.37f, 2.7f, -0.91f),
                Quaternion.Euler(2f, -7f, -8f), new Vector3(0.2f, 0.04f, 0.04f), _soot, false, false);
            CreatePrimitive("Eye shadow right", PrimitiveType.Cube, basePosition + new Vector3(0.14f, 2.7f, -0.91f),
                Quaternion.Euler(2f, -7f, -8f), new Vector3(0.2f, 0.04f, 0.04f), _soot, false, false);
            CreatePrimitive("Broken left arm", PrimitiveType.Cylinder, basePosition + new Vector3(-1.28f, 0.86f, -0.11f),
                Quaternion.Euler(0f, 0f, 58f), new Vector3(0.38f, 0.92f, 0.38f), _paleStone);
            CreatePrimitive("Fallen hand", PrimitiveType.Cube, basePosition + new Vector3(-2.22f, 0.27f, -0.58f),
                Quaternion.Euler(9f, 18f, -6f), new Vector3(1.02f, 0.42f, 0.59f), _paleStone);
        }

        private void CreateScaffolding()
        {
            CreateScaffoldSide(-13.1f, -2.4f, 1f);
            CreateScaffoldSide(13.05f, -2.8f, -1f);
        }

        private void CreateScaffoldSide(float x, float z, float direction)
        {
            for (int i = 0; i < 3; i++)
            {
                float localZ = z + i * 1.25f;
                CreatePrimitive("Scaffold post", PrimitiveType.Cylinder, new Vector3(x, 1.35f, localZ),
                    Quaternion.Euler(0f, 0f, direction * 1.8f), new Vector3(0.095f, 1.35f, 0.095f), _timber);
                CreatePrimitive("Scaffold outer post", PrimitiveType.Cylinder, new Vector3(x + direction * 1.25f, 1.35f, localZ),
                    Quaternion.Euler(0f, 0f, -direction * 2.2f), new Vector3(0.095f, 1.35f, 0.095f), _timber);
            }

            for (int i = 0; i < 2; i++)
            {
                CreatePrimitive("Scaffold platform", PrimitiveType.Cube,
                    new Vector3(x + direction * 0.62f, 1.1f + i * 1.25f, z + 1.25f),
                    Quaternion.Euler(0f, 0f, direction * 1.2f), new Vector3(1.55f, 0.12f, 3.75f), _timber);
                CreatePrimitive("Scaffold brace", PrimitiveType.Cube,
                    new Vector3(x + direction * 0.65f, 1.25f + i * 0.65f, z + 1.25f),
                    Quaternion.Euler(0f, 0f, direction * 28f), new Vector3(0.1f, 2.6f, 0.1f), _timber);
            }
        }

        private void CreateProps()
        {
            CreateBanner(new Vector3(-4.75f, 0f, 5.4f), -6f);
            CreateBanner(new Vector3(4.9f, 0f, 5.38f), 7f);

            Vector3[] pots =
            {
                new Vector3(-13.2f, 0f, -6.7f), new Vector3(-12.55f, 0f, -7.08f),
                new Vector3(12.85f, 0f, -6.65f), new Vector3(13.35f, 0f, -6.05f),
                new Vector3(-13f, 0f, 0.85f), new Vector3(12.95f, 0f, 0.55f)
            };
            for (int i = 0; i < pots.Length; i++)
            {
                CreatePot("Kiln vessel " + (i + 1), pots[i], 0.72f + (i % 3) * 0.12f);
            }

            for (int i = 0; i < 14; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (11.85f + (i % 3) * 0.54f);
                float z = -7.65f + (i % 7) * 2.1f;
                CreatePrimitive("Rubble " + i, i % 4 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube,
                    new Vector3(x, 0.12f + (i % 2) * 0.06f, z),
                    Quaternion.Euler(i * 13f, i * 31f, i * 7f),
                    new Vector3(0.28f + (i % 3) * 0.11f, 0.2f, 0.34f + (i % 2) * 0.13f),
                    i % 3 == 0 ? _paleStone : _limestone);
            }

            CreatePrimitive("Stone command stele", PrimitiveType.Cube, new Vector3(10.95f, 1.05f, -7.35f),
                Quaternion.Euler(0f, -14f, 1.8f), new Vector3(0.85f, 2.1f, 0.28f), _paleStone);
            CreatePrimitive("Stele tally", PrimitiveType.Cube, new Vector3(10.95f, 1.18f, -7.51f),
                Quaternion.Euler(0f, -14f, 1.8f), new Vector3(0.28f, 0.72f, 0.06f), _soot, false, false);
        }

        private void CreateBanner(Vector3 position, float angle)
        {
            CreatePrimitive("Banner pole", PrimitiveType.Cylinder, position + Vector3.up * 1.55f,
                Quaternion.Euler(0f, 0f, angle), new Vector3(0.07f, 1.55f, 0.07f), _timber);
            CreatePrimitive("Torn office banner", PrimitiveType.Cube, position + new Vector3(0.23f, 2.25f, -0.03f),
                Quaternion.Euler(0f, angle, angle * 0.25f), new Vector3(0.72f, 1.15f, 0.045f), _cloth, false, false);
        }

        private void CreatePot(string name, Vector3 position, float height)
        {
            CreatePrimitive(name + " body", PrimitiveType.Sphere, position + Vector3.up * height * 0.42f,
                Quaternion.Euler(0f, height * 19f, 0f), new Vector3(height * 0.55f, height * 0.48f, height * 0.55f), _pottery);
            CreatePrimitive(name + " neck", PrimitiveType.Cylinder, position + Vector3.up * height * 0.78f,
                Quaternion.identity, new Vector3(height * 0.22f, height * 0.2f, height * 0.22f), _pottery);
            CreatePrimitive(name + " rim", PrimitiveType.Cylinder, position + Vector3.up * height,
                Quaternion.identity, new Vector3(height * 0.3f, height * 0.055f, height * 0.3f), _soot);
        }

        private void CreateHeroFrameDressing()
        {
            Transform dressing = CreateRoot("Hero frame kiln workshop dressing");
            CreateWorkshopRack(dressing, "West drying rack", new Vector3(-3.95f, 0f, 6.25f), 1f);
            CreateWorkshopRack(dressing, "East drying rack", new Vector3(6.85f, 0f, 6.25f), -1f);

            CreateBrickPile(dressing, "West fired-brick pile", new Vector3(-5.15f, 0.04f, 3.35f), 4, 3);
            CreateBrickPile(dressing, "East fired-brick pile", new Vector3(4.85f, 0.04f, 3.6f), 3, 4);

            Vector3[] vesselPositions =
            {
                new Vector3(-5.85f, 0f, 2.92f),
                new Vector3(-5.38f, 0f, 2.7f),
                new Vector3(5.72f, 0f, 2.95f),
                new Vector3(5.28f, 0f, 2.7f)
            };
            for (int i = 0; i < vesselPositions.Length; i++)
            {
                CreatePot("Hero-frame kiln vessel " + (i + 1), vesselPositions[i],
                    0.58f + (i % 2) * 0.14f);
            }

        }

        private void CreateWorkshopRack(Transform parent, string name, Vector3 position, float lean)
        {
            Transform rack = CreateRoot(name);
            rack.SetParent(parent, true);
            for (int i = 0; i < 4; i++)
            {
                float x = position.x + (i % 2) * 2.25f;
                float z = position.z + (i / 2) * 0.72f;
                Parent(CreatePrimitive(name + " post " + i, PrimitiveType.Cylinder,
                    new Vector3(x, 1.35f, z), Quaternion.Euler(0f, 0f, lean * (2f + i)),
                    new Vector3(0.075f, 1.35f, 0.075f), _timber, false), rack);
            }
            for (int shelf = 0; shelf < 3; shelf++)
            {
                Parent(CreatePrimitive(name + " shelf " + shelf, PrimitiveType.Cube,
                    position + new Vector3(1.12f, 0.58f + shelf * 0.72f, 0.34f),
                    Quaternion.Euler(0f, 0f, lean * 1.2f),
                    new Vector3(2.55f, 0.09f, 0.92f), _timber, false), rack);
                for (int piece = 0; piece < 4; piece++)
                {
                    Parent(CreatePrimitive(name + " drying tile " + shelf + "-" + piece, PrimitiveType.Cube,
                        position + new Vector3(0.32f + piece * 0.53f, 0.72f + shelf * 0.72f,
                            0.16f + (piece % 2) * 0.2f),
                        Quaternion.Euler(lean * (piece - 1.5f), piece * 7f, 0f),
                        new Vector3(0.38f, 0.12f, 0.32f), piece % 3 == 0 ? _paleStone : _pottery, false), rack);
                }
            }
        }

        private void CreateBrickPile(Transform parent, string name, Vector3 origin, int columns, int rows)
        {
            Transform pile = CreateRoot(name);
            pile.SetParent(parent, true);
            for (int row = 0; row < rows; row++)
            {
                int rowColumns = Mathf.Max(1, columns - row / 2);
                for (int column = 0; column < rowColumns; column++)
                {
                    Vector3 position = origin + new Vector3(
                        column * 0.48f + (row % 2) * 0.19f,
                        row * 0.24f,
                        (column % 2) * 0.08f);
                    Parent(CreatePrimitive(name + " brick " + row + "-" + column, PrimitiveType.Cube,
                        position, Quaternion.Euler((column % 2) * 2f, (column * 11f + row * 7f) % 18f - 9f, 0f),
                        new Vector3(0.42f, 0.2f, 0.28f), row % 3 == 0 ? _paleStone : _limestone, false), pile);
                }
            }
        }

        private void CreateLighting()
        {
            GameObject lightObject = new GameObject("Cold moon key");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(46f, -31f, 0f);
            Light lightComponent = lightObject.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.color = new Color(0.62f, 0.68f, 0.78f);
            lightComponent.intensity = 2.35f;
            lightComponent.shadows = LightShadows.Soft;
            lightComponent.shadowStrength = 0.68f;

            GameObject duelLightObject = new GameObject("Duel cool focus");
            duelLightObject.transform.SetParent(transform, false);
            duelLightObject.transform.localPosition = new Vector3(0f, 4.2f, -2.6f);
            duelLightObject.transform.localRotation = Quaternion.Euler(57f, 0f, 0f);
            Light duelLight = duelLightObject.AddComponent<Light>();
            duelLight.type = LightType.Spot;
            duelLight.color = new Color(0.72f, 0.76f, 0.92f);
            duelLight.intensity = 22f;
            duelLight.range = 11f;
            duelLight.spotAngle = 68f;
            duelLight.innerSpotAngle = 38f;
            duelLight.shadows = LightShadows.None;

            GameObject colossusLightObject = new GameObject("Colossus kiln rim");
            colossusLightObject.transform.SetParent(transform, false);
            colossusLightObject.transform.localPosition = new Vector3(3.25f, 3.8f, 2.7f);
            Light colossusLight = colossusLightObject.AddComponent<Light>();
            colossusLight.type = LightType.Point;
            colossusLight.color = new Color(1f, 0.54f, 0.24f);
            colossusLight.intensity = 5.6f;
            colossusLight.range = 8.2f;
            colossusLight.shadows = LightShadows.None;
        }

        private void CreateDust()
        {
            GameObject dustObject = new GameObject("Courtyard ash motes");
            dustObject.transform.SetParent(transform, false);
            dustObject.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            ParticleSystem particles = dustObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.62f, 0.57f, 0.48f, 0.16f),
                new Color(0.9f, 0.64f, 0.32f, 0.3f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 46;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 5f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(13f, 2.5f, 10f);
            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(0.03f, 0.13f);
            velocity.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.035f, 0.035f);
            ParticleSystemRenderer rendererComponent = particles.GetComponent<ParticleSystemRenderer>();
            rendererComponent.sharedMaterial = _particle;
            rendererComponent.shadowCastingMode = ShadowCastingMode.Off;
            rendererComponent.receiveShadows = false;
        }

        private void CreateEmbers(string name, Vector3 position)
        {
            GameObject emberObject = new GameObject(name);
            emberObject.transform.SetParent(transform, false);
            emberObject.transform.localPosition = position;
            emberObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem particles = emberObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.09f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.18f, 0.02f, 0.95f),
                new Color(1f, 0.72f, 0.1f, 0.8f));
            main.maxParticles = 22;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 9f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.46f;
            ParticleSystemRenderer rendererComponent = particles.GetComponent<ParticleSystemRenderer>();
            rendererComponent.sharedMaterial = _particle;
            rendererComponent.renderMode = ParticleSystemRenderMode.Stretch;
            rendererComponent.lengthScale = 2.4f;
            rendererComponent.velocityScale = 0.09f;
            rendererComponent.shadowCastingMode = ShadowCastingMode.Off;
            rendererComponent.receiveShadows = false;
        }

        private void CreateKilnFlames(string name, Vector3 position)
        {
            GameObject flameObject = new GameObject(name);
            flameObject.transform.SetParent(transform, false);
            flameObject.transform.localPosition = position;
            flameObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem particles = flameObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.62f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.34f, 0.82f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.11f, 0.27f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.82f, 0.2f, 0.92f),
                new Color(1f, 0.13f, 0.015f, 0.72f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 18;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 15f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 9f;
            shape.radius = 0.27f;
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.82f, 0.22f), 0f),
                    new GradientColorKey(new Color(1f, 0.20f, 0.02f), 0.58f),
                    new GradientColorKey(new Color(0.28f, 0.025f, 0.01f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.72f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;
            ParticleSystemRenderer rendererComponent = particles.GetComponent<ParticleSystemRenderer>();
            rendererComponent.sharedMaterial = _particle;
            rendererComponent.renderMode = ParticleSystemRenderMode.Stretch;
            rendererComponent.lengthScale = 1.8f;
            rendererComponent.velocityScale = 0.05f;
            rendererComponent.shadowCastingMode = ShadowCastingMode.Off;
            rendererComponent.receiveShadows = false;
        }

        private GameObject CreatePrimitive(
            string objectName,
            PrimitiveType primitiveType,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Material material,
            bool keepCollider = false,
            bool castShadows = true)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.transform.SetParent(transform, false);
            primitive.transform.localPosition = position;
            primitive.transform.localRotation = rotation;
            primitive.transform.localScale = scale;
            Collider colliderComponent = primitive.GetComponent<Collider>();
            if (!keepCollider && colliderComponent != null)
            {
                colliderComponent.enabled = false;
                Destroy(colliderComponent);
            }

            Renderer rendererComponent = primitive.GetComponent<Renderer>();
            rendererComponent.sharedMaterial = material;
            rendererComponent.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            rendererComponent.receiveShadows = true;
            return primitive;
        }

        private Material CreateLitMaterial(
            string materialName,
            Color color,
            float metallic,
            float smoothness,
            Color? emission = null,
            Texture2D albedo = null,
            Texture2D normal = null,
            Vector2? textureScale = null)
        {
            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Lit")
                : Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader) { name = materialName };
            _materials.Add(material);
            SetColor(material, color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (albedo != null)
            {
                Vector2 scale = textureScale ?? Vector2.one;
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", albedo);
                    material.SetTextureScale("_BaseMap", scale);
                }
                if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", albedo);
                    material.SetTextureScale("_MainTex", scale);
                }
            }
            if (normal != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", 0.62f);
                material.EnableKeyword("_NORMALMAP");
            }
            if (emission.HasValue && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }

            return material;
        }

        private Material CreateParticleMaterial()
        {
            Shader shader = GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                : Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            Material material = new Material(shader) { name = "Ash and ember particles" };
            _materials.Add(material);
            SetColor(material, Color.white);
            Texture2D softDisc = new Texture2D(32, 32, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime soft ember disc",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (int y = 0; y < softDisc.height; y++)
            {
                for (int x = 0; x < softDisc.width; x++)
                {
                    Vector2 point = new Vector2(
                        (x + 0.5f) / softDisc.width * 2f - 1f,
                        (y + 0.5f) / softDisc.height * 2f - 1f);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - point.magnitude), 1.7f);
                    softDisc.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            softDisc.Apply(false, true);
            _textures.Add(softDisc);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", softDisc);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", softDisc);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 1f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.One);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private static void SetColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        private static void SetDoubleSided(Material material)
        {
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in _meshes)
            {
                if (mesh != null) Destroy(mesh);
            }
            _meshes.Clear();
            foreach (Texture2D texture in _textures)
            {
                if (texture != null) Destroy(texture);
            }
            _textures.Clear();
            foreach (Material material in _materials)
            {
                if (material != null) Destroy(material);
            }
            _materials.Clear();
        }
    }
}
