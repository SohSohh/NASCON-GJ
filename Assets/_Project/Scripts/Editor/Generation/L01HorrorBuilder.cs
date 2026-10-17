using NasconGJ.Horror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace NasconGJ.EditorTools.Generation
{
    /// <summary>
    /// Adds L01's horror layer on top of the team's hand-built level, all under one "--- HORROR ---" root so it can be
    /// rebuilt without touching anything else: the shadow murals, the hall's dread bed, the look-away scare between
    /// the pillars and the shadow that darts across the exit arch.
    /// </summary>
    public static class L01HorrorBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Levels/L01_TombEntrance.unity";
        private const string RootName = "--- HORROR ---";
        private static string Designed(string f) => GenUtil.Root + "/Audio/Designed/" + f;

        // texture, a point inside the room, the direction to the wall, size (m), height of the mural's centre
        private static readonly (string tex, Vector3 from, Vector3 dir, Vector2 size, float y)[] Murals =
        {
            ("Mural_02_Following", new Vector3(0f, 0f, 13.6f), Vector3.left,    new Vector2(3.0f, 1.12f), 1.55f),
            ("Mural_04_Feeding",   new Vector3(0f, 0f, 24.8f), Vector3.left,    new Vector2(3.0f, 1.12f), 1.55f),
            ("Mural_03_Holding",   new Vector3(-6f, 0f, 35.4f), Vector3.left,   new Vector2(4.8f, 1.8f), 2.9f),
            ("Mural_02_Following", new Vector3(6f, 0f, 40.3f), Vector3.right,   new Vector2(3.6f, 1.35f), 2.5f),
            ("Mural_05_Swallowing",new Vector3(-4f, 0f, 52f),  Vector3.forward, new Vector2(5.2f, 2.6f), 3.4f),
        };

        [MenuItem("NASCON/L01/Build Horror Layer")]
        public static void Build()
        {
            var scene = SceneManager_Open();
            var old = GameObject.Find(RootName);
            if (old != null) Object.DestroyImmediate(old);
            var root = new GameObject(RootName).transform;

            BuildMurals(root);
            var shadowMat = ShadowMaterial();

            // ---- the hall's dread bed, faded in while you're in the hall
            var bed = new GameObject("HallDread");
            bed.transform.SetParent(root, false);
            bed.transform.position = new Vector3(0f, 3f, 44f);
            var bedBox = bed.AddComponent<BoxCollider>();
            bedBox.isTrigger = true;
            bedBox.size = new Vector3(20f, 6f, 28f);
            var bedSrc = bed.AddComponent<AudioSource>();
            bedSrc.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Designed("AMB_HallDread.ogg"));
            bedSrc.loop = true; bedSrc.playOnAwake = false; bedSrc.spatialBlend = 0f; bedSrc.volume = 0f;
            var zone = bed.AddComponent<AmbienceZone>();
            GenUtil.Set(zone, "source", bedSrc);
            GenUtil.Set(zone, "volume", 0.55f);

            // ---- look-away scare: a figure in the dark left aisle, then right behind you
            var scare = new GameObject("Scare_LookAway");
            scare.transform.SetParent(root, false);
            scare.transform.position = new Vector3(0f, 1.5f, 44.5f);
            var scareBox = scare.AddComponent<BoxCollider>();
            scareBox.isTrigger = true;
            scareBox.size = new Vector3(9f, 3f, 2.5f);
            var stand = new GameObject("StandPoint").transform;
            stand.SetParent(scare.transform, false);
            stand.position = Floor(new Vector3(-7.8f, 0f, 47f));
            var lookFigure = Figure("LookAwayFigure", scare.transform, shadowMat);
            var lookSfx = Sfx(scare.transform);
            var look = scare.AddComponent<LookAwayScare>();
            GenUtil.Set(look, "figure", lookFigure);
            GenUtil.Set(look, "standPoint", stand);
            GenUtil.Set(look, "sfx", lookSfx);
            GenUtil.Set(look, "whisperClip", AssetDatabase.LoadAssetAtPath<AudioClip>(Designed("SFX_Whisper.ogg")));
            GenUtil.Set(look, "screechClip", AssetDatabase.LoadAssetAtPath<AudioClip>(Designed("SFX_Screech.ogg")));
            GenUtil.Set(look, "dreadClip", AssetDatabase.LoadAssetAtPath<AudioClip>(Designed("SFX_DreadHit.ogg")));

            BuildGallery(root, shadowMat);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("L01 horror layer built.");
        }

        // ------------------------------------------------------------------ the gallery (past the hall)
        // A P.T.-style candle corridor beyond the hall's exit room: framed murals, a side room where shadow people
        // stand in a candle circle (the side-glance scare), and a narrow lit arch at the end that a shadow darts across.
        private const float GX = 8f;           // corridor centre line
        private const float GZ0 = 62f, GZ1 = 78f;

        private static void BuildGallery(Transform root, Material shadowMat)
        {
            var g = new GameObject("Gallery").transform;
            g.SetParent(root, false);

            // open the exit room's back wall into the corridor (the team's wall is switched off, not deleted)
            foreach (Transform k in GameObject.Find("Kit").transform)
                if (k.name == "PF_Wall_4x4" && Vector3.Distance(k.position, new Vector3(GX, 0f, GZ0)) < 0.1f) k.gameObject.SetActive(false);
            Kit("PF_Wall_Doorway_4x4", g, new Vector3(GX, 0f, GZ0), 0f);

            for (float z = GZ0 + 2f; z < GZ1; z += 4f)
            {
                Kit("PF_Floor_4x4", g, new Vector3(GX, 0f, z), 0f);
                Kit("PF_Ceiling_4x4", g, new Vector3(GX, 3f, z), 0f);
                Kit(Mathf.Approximately(z, 68f) ? "PF_Wall_Doorway_4x4" : "PF_Wall_4x4", g, new Vector3(GX - 1.3f, 0f, z), 90f);   // left (side-room door at z 68)
                Kit("PF_Wall_4x4", g, new Vector3(GX + 1.3f, 0f, z), 90f);                                                           // right
            }
            // the narrow arch at the end, and the bright room beyond it (the level exit moves here)
            Kit("PF_Wall_Doorway_4x4", g, new Vector3(GX, 0f, GZ1), 0f);
            Kit("PF_Floor_4x4", g, new Vector3(GX, 0f, GZ1 + 2f), 0f);
            Kit("PF_Ceiling_4x4", g, new Vector3(GX, 4f, GZ1 + 2f), 0f);
            Kit("PF_Wall_4x4", g, new Vector3(GX - 2f, 0f, GZ1 + 2f), 90f);
            Kit("PF_Wall_4x4", g, new Vector3(GX + 2f, 0f, GZ1 + 2f), 90f);
            Kit("PF_Wall_4x4", g, new Vector3(GX, 0f, GZ1 + 4f), 0f);
            var exit = GameObject.Find("LevelExit");
            if (exit != null) exit.transform.position = new Vector3(GX, 1.5f, GZ1 + 2.6f);
            var glow = GameObject.Find("ExitGlow");
            if (glow != null)
            {
                glow.transform.position = new Vector3(GX, 2.4f, GZ1 + 3.2f);
                var l = glow.GetComponent<Light>();
                if (l != null) { l.range = Mathf.Max(l.range, 9f); l.intensity = Mathf.Max(l.intensity, 4f); }
            }

            // side room: x 2.5..6.4, z 66..70
            const float RX = 4.5f, RZ = 68f;
            Kit("PF_Floor_4x4", g, new Vector3(RX, 0f, RZ), 0f);
            Kit("PF_Ceiling_4x4", g, new Vector3(RX, 3f, RZ), 0f);
            Kit("PF_Wall_4x4", g, new Vector3(RX - 2.3f, 0f, RZ), 90f);
            Kit("PF_Wall_4x4", g, new Vector3(RX, 0f, RZ - 2.3f), 0f);
            Kit("PF_Wall_4x4", g, new Vector3(RX, 0f, RZ + 2.3f), 0f);

            // candles along both walls of the corridor, every 2 m; a light on every other pair
            var candleMat = Lit("M_CandleWax", new Color(0.85f, 0.78f, 0.62f));
            var flameMat = Unlit("M_CandleFlame", new Color(1f, 0.78f, 0.45f));
            int n = 0;
            var corridorCandles = new System.Collections.Generic.List<CandleFlicker>();
            for (float z = GZ0 + 1.2f; z < GZ1 - 0.5f; z += 2f, n++)
                foreach (float wallSide in new[] { -1f, 1f })
                {
                    var c = Candle(g, new Vector3(GX + wallSide * 0.8f, 0f, z + (wallSide > 0 ? 1f : 0f)), candleMat, flameMat, n % 2 == 0, 2.2f);
                    if (c != null) corridorCandles.Add(c);
                }

            // framed murals, like paintings in a hallway
            Painting(g, "Mural_03_Holding", new Vector3(GX + 1f, 1.7f, 64.6f), Vector3.left, new Vector2(2.2f, 0.82f));
            Painting(g, "Mural_04_Feeding", new Vector3(GX - 1f, 1.7f, 73f), Vector3.right, new Vector2(2.2f, 0.82f));
            Painting(g, "Mural_05_Swallowing", new Vector3(GX + 1f, 1.75f, 75.6f), Vector3.left, new Vector2(1.7f, 0.85f));

            // the candle circle and the three standing in it
            var roomCentre = new GameObject("RoomCentre").transform;
            roomCentre.SetParent(g, false);
            roomCentre.position = new Vector3(RX - 0.2f, 0f, RZ);
            var circleCandles = new System.Collections.Generic.List<CandleFlicker>();
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                var c = Candle(g, roomCentre.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.05f, candleMat, flameMat, i % 3 == 0, 2.6f);
                if (c != null) circleCandles.Add(c);
            }
            var figures = new ShadowFigureDriver[3];
            for (int i = 0; i < 3; i++)
            {
                float a = (i / 3f + 0.08f) * Mathf.PI * 2f;
                figures[i] = Figure($"CircleFigure_{i}", g, shadowMat);
                var ft = figures[i].transform;
                ft.position = roomCentre.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.6f;
                ft.rotation = Quaternion.LookRotation(roomCentre.position - ft.position);
            }

            var scare = new GameObject("Scare_SideRoom");
            scare.transform.SetParent(g, false);
            scare.transform.position = new Vector3(GX, 1.5f, 65f);
            var box = scare.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2f, 3f, 1.5f);
            var door = new GameObject("Doorway").transform;
            door.SetParent(scare.transform, false);
            door.SetPositionAndRotation(new Vector3(GX - 0.75f, 0f, RZ + 0.3f), Quaternion.LookRotation(Vector3.forward));
            var side = scare.AddComponent<SideRoomScare>();
            GenUtil.SetArray(side, "circle", figures);
            GenUtil.Set(side, "doorwayFigure", Figure("DoorwayFigure", g, shadowMat));
            GenUtil.SetArray(side, "candles", circleCandles.ToArray());
            GenUtil.SetArray(side, "corridorCandles", corridorCandles.ToArray());
            GenUtil.Set(side, "doorway", door);
            GenUtil.Set(side, "roomCentre", roomCentre);
            GenUtil.Set(side, "sfx", Sfx(scare.transform));
            GenUtil.Set(side, "snuffClip", AssetDatabase.LoadAssetAtPath<AudioClip>(Designed("SFX_DreadHit.ogg")));
            GenUtil.Set(side, "whisperClip", AssetDatabase.LoadAssetAtPath<AudioClip>(Designed("SFX_Whisper.ogg")));
            GenUtil.Set(side, "screechClip", AssetDatabase.LoadAssetAtPath<AudioClip>(Designed("SFX_Screech.ogg")));

            // a shadow darts across the lit arch at the end
            var pass = new GameObject("Scare_ArchPassBy");
            pass.transform.SetParent(g, false);
            pass.transform.position = new Vector3(GX, 1.5f, 72.5f);
            var passBox = pass.AddComponent<BoxCollider>();
            passBox.isTrigger = true;
            passBox.size = new Vector3(2f, 3f, 1.5f);
            var a0 = new GameObject("From").transform; a0.SetParent(pass.transform, false); a0.position = new Vector3(GX - 1.8f, 0f, GZ1 + 1.6f);
            var b0 = new GameObject("To").transform; b0.SetParent(pass.transform, false); b0.position = new Vector3(GX + 1.8f, 0f, GZ1 + 1.6f);
            var passBy = pass.AddComponent<ShadowPassBy>();
            GenUtil.Set(passBy, "figure", Figure("PassByFigure", pass.transform, shadowMat));
            GenUtil.Set(passBy, "from", a0);
            GenUtil.Set(passBy, "to", b0);
            GenUtil.Set(passBy, "duration", 0.9f);
            GenUtil.Set(passBy, "sfx", Sfx(pass.transform));
            GenUtil.Set(passBy, "passClip", AssetDatabase.LoadAssetAtPath<AudioClip>(Designed("SFX_PassBy.ogg")));
            GenUtil.Set(passBy, "whisperClip", AssetDatabase.LoadAssetAtPath<AudioClip>(Designed("SFX_Whisper.ogg")));

            // the dread bed carries on down the corridor, a little closer
            var bed = new GameObject("GalleryDread");
            bed.transform.SetParent(g, false);
            bed.transform.position = new Vector3(5.5f, 1.5f, 71f);
            var bedBox = bed.AddComponent<BoxCollider>();
            bedBox.isTrigger = true;
            bedBox.size = new Vector3(9f, 3f, 20f);
            var bedSrc = bed.AddComponent<AudioSource>();
            bedSrc.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Designed("AMB_HallDread.ogg"));
            bedSrc.loop = true; bedSrc.playOnAwake = false; bedSrc.spatialBlend = 0f;
            var zone = bed.AddComponent<AmbienceZone>();
            GenUtil.Set(zone, "source", bedSrc);
            GenUtil.Set(zone, "volume", 0.7f);
        }

        private static GameObject Kit(string name, Transform parent, Vector3 pos, float yaw)
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>($"{GenUtil.Root}/Prefabs/Environment/Kit/{name}.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            go.isStatic = true;
            return go;
        }

        private static CandleFlicker Candle(Transform parent, Vector3 floor, Material wax, Material flame, bool withLight, float range)
        {
            var root = new GameObject("Candle").transform;
            root.SetParent(parent, false);
            root.position = floor;
            float h = Random.Range(0.12f, 0.3f);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root, false);
            body.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
            body.transform.localScale = new Vector3(0.07f, h * 0.5f, 0.07f);
            body.GetComponent<Renderer>().sharedMaterial = wax;
            var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(tip.GetComponent<Collider>());
            tip.transform.SetParent(root, false);
            tip.transform.localPosition = new Vector3(0f, h + 0.035f, 0f);
            tip.transform.localScale = new Vector3(0.028f, 0.06f, 0.028f);
            var tipR = tip.GetComponent<Renderer>();
            tipR.sharedMaterial = flame;
            tipR.shadowCastingMode = ShadowCastingMode.Off;
            var lgo = new GameObject("Flame");
            lgo.transform.SetParent(root, false);
            lgo.transform.localPosition = new Vector3(0f, h + 0.1f, 0f);
            if (!withLight)
            {
                var plain = lgo.AddComponent<CandleFlicker>();
                GenUtil.SetArray(plain, "flames", new Renderer[] { tipR });
                return plain;
            }
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.72f, 0.42f);
            l.range = range;
            l.intensity = 1.3f;
            l.shadows = LightShadows.None;
            var f = lgo.AddComponent<CandleFlicker>();
            GenUtil.SetArray(f, "flames", new Renderer[] { tipR });
            return f;
        }

        private static void Painting(Transform parent, string tex, Vector3 wallPoint, Vector3 facing, Vector2 size)
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>($"{GenUtil.Root}/Art/Textures/Murals/{tex}.png");
            if (t == null) return;
            var frameMat = Lit("M_PaintingFrame", new Color(0.09f, 0.06f, 0.045f));
            var boardMat = Lit("M_PaintingBoard", new Color(0.42f, 0.34f, 0.24f));
            var holder = new GameObject("Painting_" + tex).transform;
            holder.SetParent(parent, false);
            holder.SetPositionAndRotation(wallPoint + facing * 0.03f, Quaternion.LookRotation(-facing, Vector3.up));
            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(frame.GetComponent<Collider>());
            frame.transform.SetParent(holder, false);
            frame.transform.localScale = new Vector3(size.x + 0.14f, size.y + 0.14f, 0.05f);
            frame.GetComponent<Renderer>().sharedMaterial = frameMat;
            var board = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(board.GetComponent<Collider>());
            board.transform.SetParent(holder, false);
            board.transform.localPosition = new Vector3(0f, 0f, -0.027f);
            board.transform.localScale = new Vector3(size.x, size.y, 1f);
            board.GetComponent<Renderer>().sharedMaterial = boardMat;
            var paint = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(paint.GetComponent<Collider>());
            paint.transform.SetParent(holder, false);
            paint.transform.localPosition = new Vector3(0f, 0f, -0.03f);
            paint.transform.localScale = new Vector3(size.x, size.y, 1f);
            paint.GetComponent<Renderer>().sharedMaterial = MuralMaterial(tex, t);
        }

        private static Material Lit(string name, Color c)
        {
            string path = $"{GenUtil.MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", c);
            mat.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material Unlit(string name, Color c)
        {
            string path = $"{GenUtil.MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", c);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static UnityEngine.SceneManagement.Scene SceneManager_Open()
        {
            var active = EditorSceneManager.GetActiveScene();
            return active.path == ScenePath ? active : EditorSceneManager.OpenScene(ScenePath);
        }

        private static void BuildMurals(Transform root)
        {
            var group = new GameObject("Murals").transform;
            group.SetParent(root, false);
            foreach (var m in Murals)
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{GenUtil.Root}/Art/Textures/Murals/{m.tex}.png");
                if (tex == null) { Debug.LogWarning($"Mural texture {m.tex} missing"); continue; }
                Vector3 origin = new Vector3(m.from.x, m.y, m.from.z);
                if (!Physics.Raycast(origin, m.dir, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore))
                {
                    Debug.LogWarning($"No wall found for {m.tex}");
                    continue;
                }
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.name = m.tex;
                quad.transform.SetParent(group, false);
                quad.transform.SetPositionAndRotation(hit.point + hit.normal * 0.02f, Quaternion.LookRotation(-hit.normal, Vector3.up));
                quad.transform.localScale = new Vector3(m.size.x, m.size.y, 1f);
                var r = quad.GetComponent<MeshRenderer>();
                r.sharedMaterial = MuralMaterial(m.tex, tex);
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private static Material MuralMaterial(string name, Texture2D tex)
        {
            string path = $"{GenUtil.MaterialDir}/M_{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.3f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetFloat("_Smoothness", 0.05f);
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material ShadowMaterial()
        {
            string path = $"{GenUtil.MaterialDir}/M_ShadowFigure.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", new Color(0.012f, 0.008f, 0.014f));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static ShadowFigureDriver Figure(string name, Transform parent, Material mat)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GenUtil.Root + "/Prefabs/Characters/PF_Villager_Man.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = parent.position + Vector3.down * 50f; // parked out of sight until it's needed
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            var driver = go.AddComponent<ShadowFigureDriver>();
            GenUtil.Set(driver, "shadowMaterial", mat);
            return driver;
        }

        private static AudioSource Sfx(Transform parent)
        {
            var go = new GameObject("Sfx");
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0.85f;
            s.minDistance = 3f;
            s.maxDistance = 40f;
            s.rolloffMode = AudioRolloffMode.Linear;
            return s;
        }

        private static Vector3 Floor(Vector3 p)
        {
            return Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore) ? hit.point : p;
        }
    }
}
