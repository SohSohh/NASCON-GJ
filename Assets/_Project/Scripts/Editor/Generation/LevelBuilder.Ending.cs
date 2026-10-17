using System.IO;
using NasconGJ.Cutscenes;
using NasconGJ.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace NasconGJ.EditorTools.Generation
{
    public static partial class LevelBuilder
    {
        // ------------------------------------------------------------------------------------------------------------
        //  C02 — Ending, "The Cycle". Built on top of the C01b set (same town, rooftop, tomb), with the prologue's director
        //  swapped for EndingDirector (which inherits its camera/caption/atmosphere toolkit) and a few ending-only pieces:
        //  the dawn shots at the tomb, the ring of light, the old man who was the Void before, the storyteller close-up.
        // ------------------------------------------------------------------------------------------------------------
        private const string PrologueTownScenePath = GenUtil.Root + "/Scenes/Cutscenes/C01b_PrologueTown.unity";
        private const string EndingScenePath = GenUtil.Root + "/Scenes/Cutscenes/C02_Ending.unity";

        [MenuItem("NASCON/Cutscenes/Build Ending (C02)")]
        public static void BuildEndingOnly() => BuildEnding();

        private static void BuildEnding()
        {
            // the C02 file keeps its own GUID (build settings refer to it); only its contents are replaced
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            File.Copy(Path.GetFullPath(PrologueTownScenePath), Path.GetFullPath(EndingScenePath), true);
            AssetDatabase.ImportAsset(EndingScenePath, ImportAssetOptions.ForceUpdate);
            var scene = EditorSceneManager.OpenScene(EndingScenePath);

            // swap the director's script; every serialized reference (camera, marks, villagers, audio) carries over
            var old = Object.FindAnyObjectByType<PrologueDirector>();
            var so = new SerializedObject(old);
            so.FindProperty("m_Script").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MonoScript>(GenUtil.Root + "/Scripts/Cutscenes/EndingDirector.cs");
            so.ApplyModifiedPropertiesWithoutUndo();
            var director = Object.FindAnyObjectByType<EndingDirector>();
            director.gameObject.name = "EndingDirector";

            var memory = GameObject.Find("MemoryFall");
            if (memory != null) memory.SetActive(false);

            var root = new GameObject("--- ENDING ---").transform;
            var marks = new GameObject("Marks").transform;
            marks.SetParent(root, false);

            Transform P(string n, Vector3 p, Vector3 face)
            {
                var t = new GameObject(n).transform;
                t.SetParent(root, false);
                t.SetPositionAndRotation(p, Quaternion.LookRotation(face));
                return t;
            }

            // Ren out of the tomb, and back in
            var exitPath = new[] { P("Exit_0", new Vector3(200f, 0f, 34.2f), Vector3.back), P("Exit_1", new Vector3(200f, 0f, 31f), Vector3.back), P("Exit_2", new Vector3(200f, 0f, 27.6f), Vector3.back) };
            var returnPath = new[] { P("Return_0", new Vector3(200f, 0f, 33f), Vector3.forward), P("Return_1", new Vector3(200f, 0f, 40.2f), Vector3.forward) };
            GenUtil.SetArray(director, "exitPath", exitPath);
            GenUtil.SetArray(director, "returnPath", returnPath);
            GenUtil.Set(director, "dawnFrom", Mark("Dawn_From", marks, new Vector3(198.1f, 1.05f, 20.8f), new Vector3(200f, 2.3f, 34f)));
            GenUtil.Set(director, "dawnTo", Mark("Dawn_To", marks, new Vector3(199.1f, 1.45f, 23.6f), new Vector3(200f, 1.8f, 30f)));
            GenUtil.Set(director, "ringFrom", Mark("Ring_From", marks, new Vector3(-30f, 26f, -24f), new Vector3(120f, 4f, 25f)));
            GenUtil.Set(director, "ringTo", Mark("Ring_To", marks, new Vector3(-22f, 19f, -20f), new Vector3(120f, 2f, 25f)));
            GenUtil.Set(director, "streetFrom", GameObject.Find("Flood_To").transform);
            GenUtil.Set(director, "streetTo", GameObject.Find("Flood_From").transform);
            GenUtil.Set(director, "mouthFrom", Mark("Mouth_From", marks, new Vector3(201.9f, 1.55f, 24.6f), new Vector3(199.6f, 1.6f, 34f)));
            GenUtil.Set(director, "mouthTo", Mark("Mouth_To", marks, new Vector3(201.3f, 1.5f, 26.4f), new Vector3(199.8f, 1.6f, 34f)));
            // a warm fill at the tomb mouth: the dawn on Ren's face and hands
            var fill = new GameObject("MouthFill").AddComponent<Light>();
            fill.transform.SetParent(root, false);
            fill.transform.position = new Vector3(200.8f, 2.4f, 25.5f);
            fill.type = LightType.Point;
            fill.color = new Color(1f, 0.8f, 0.6f);
            fill.range = 11f;
            fill.intensity = 2.6f;
            fill.shadows = LightShadows.None;

            // the storyteller in the square, his shadow on the wall behind him (the one that stays)
            var villagersProp = new SerializedObject(director).FindProperty("villagers");
            var imprintsProp = new SerializedObject(director).FindProperty("imprints");
            Villager elder = null; ShadowFigure elderShadow = null;
            for (int i = 0; i < villagersProp.arraySize; i++)
            {
                var v = villagersProp.GetArrayElementAtIndex(i).objectReferenceValue as Villager;
                if (v == null || v.name != "Elder_Storyteller") continue;
                elder = v;
                if (i < imprintsProp.arraySize) elderShadow = imprintsProp.GetArrayElementAtIndex(i).objectReferenceValue as ShadowFigure;
            }
            if (elder != null && elderShadow == null)
            {
                // he never had one in the prologue: give him a shadow on the wall behind his bench, copied from another's
                ShadowFigure donor = null;
                for (int i = 0; i < imprintsProp.arraySize && donor == null; i++) donor = imprintsProp.GetArrayElementAtIndex(i).objectReferenceValue as ShadowFigure;
                Vector3 from = elder.transform.position + Vector3.up * 1.2f;
                if (donor != null && Physics.Raycast(from, -elder.transform.forward, out var wall, 8f, ~0, QueryTriggerInteraction.Ignore))
                {
                    var copy = Object.Instantiate(donor.gameObject, root);
                    copy.name = "Storyteller_Shadow";
                    // keep the donor's pose relative to its own wall: turn it from its wall's facing to this one's
                    Vector3 donorNormal = Physics.Raycast(donor.transform.position + donor.transform.forward * 0.5f, -donor.transform.forward, out var dw, 2f, ~0, QueryTriggerInteraction.Ignore) ? dw.normal : -donor.transform.forward;
                    copy.transform.rotation = Quaternion.FromToRotation(donorNormal, wall.normal) * donor.transform.rotation;
                    // beside him, not hidden behind him; and it isn't a man's shadow: it's the Void's shape
                    Vector3 side = Vector3.Cross(Vector3.up, wall.normal).normalized;
                    copy.transform.position = new Vector3(wall.point.x, donor.transform.position.y, wall.point.z) + side * 1.1f + wall.normal * 0.03f;
                    var cso = new SerializedObject(copy.GetComponent<ShadowFigure>());
                    cso.FindProperty("sheet").enumValueIndex = (int)ShadowFigure.Sheet.Creature;
                    cso.FindProperty("height").floatValue = 2.6f;
                    cso.FindProperty("animateIdle").boolValue = true;
                    cso.ApplyModifiedPropertiesWithoutUndo();
                    elderShadow = copy.GetComponent<ShadowFigure>();
                }
            }
            GenUtil.Set(director, "storyteller", elder);
            GenUtil.Set(director, "storytellerShadow", elderShadow);
            if (elder != null)
            {
                Vector3 e = elder.transform.position;
                GenUtil.Set(director, "storyFrom", Mark("Story_From", marks, e + new Vector3(0.5f, 1.25f, -3.6f), e + new Vector3(0f, 1.15f, 0f)));
                GenUtil.Set(director, "storyTo", Mark("Story_To", marks, e + new Vector3(0.15f, 1.3f, -2.2f), e + new Vector3(0f, 1.25f, 0f)));

                // the one who was the Void before: the same old man, waiting in the dark beyond the door
                var former = Object.Instantiate(elder.gameObject, root);
                former.name = "FormerVoid";
                var fso = new SerializedObject(former.GetComponent<Villager>());
                fso.FindProperty("activity").enumValueIndex = (int)Villager.Activity.Idle;
                fso.FindProperty("talkTo").objectReferenceValue = null;
                fso.FindProperty("strollOffset").vector3Value = Vector3.zero;
                fso.FindProperty("stoop").floatValue = 0.55f;
                fso.ApplyModifiedPropertiesWithoutUndo();
                GenUtil.Set(director, "formerVoid", former.GetComponent<Villager>());
                GenUtil.Set(director, "formerVoidStart", P("FormerVoid_Start", new Vector3(200.3f, 0f, 39.6f), Vector3.back));
                GenUtil.SetArray(director, "formerVoidPath", new[]
                {
                    P("FormerVoid_0", new Vector3(200.9f, 0f, 33.5f), Vector3.back),
                    P("FormerVoid_1", new Vector3(201.6f, 0f, 28.2f), Vector3.back),
                    P("FormerVoid_2", new Vector3(204.5f, 0f, 19f), Vector3.back),
                });
            }

            // the ring of light: a flat glowing band laid on the ground, grown outward from the tomb by the director
            var ringGo = new GameObject("LightRing");
            ringGo.transform.SetParent(root, false);
            ringGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var ring = ringGo.AddComponent<LineRenderer>();
            ring.useWorldSpace = true;
            ring.positionCount = 97;
            ring.alignment = LineAlignment.TransformZ;
            ring.widthMultiplier = 2f;
            ring.numCapVertices = 0;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            var ringMat = AssetDatabase.LoadAssetAtPath<Material>($"{GenUtil.MaterialDir}/M_LightRing.mat");
            if (ringMat == null) { ringMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(ringMat, $"{GenUtil.MaterialDir}/M_LightRing.mat"); }
            ringMat.SetColor("_BaseColor", new Color(4f, 3.5f, 2.6f));
            EditorUtility.SetDirty(ringMat);
            ring.sharedMaterial = ringMat;
            ring.enabled = false;
            GenUtil.Set(director, "lightRing", ring);

            // the crystal Ren brings out (L05's prize): a long six-sided bipyramid of warm light
            var crystal = new GameObject("Crystal");
            crystal.transform.SetParent(root, false);
            crystal.transform.position = new Vector3(200f, 1.1f, 38f);
            crystal.transform.localScale = Vector3.one * 1.5f;
            crystal.AddComponent<MeshFilter>().sharedMesh = CrystalMesh();
            var crystalRenderer = crystal.AddComponent<MeshRenderer>();
            var crystalMat = AssetDatabase.LoadAssetAtPath<Material>($"{GenUtil.MaterialDir}/M_EndingCrystal.mat");
            if (crystalMat == null) { crystalMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(crystalMat, $"{GenUtil.MaterialDir}/M_EndingCrystal.mat"); }
            crystalMat.SetColor("_BaseColor", new Color(3.2f, 3.0f, 2.6f));
            EditorUtility.SetDirty(crystalMat);
            crystalRenderer.sharedMaterial = crystalMat;
            crystalRenderer.shadowCastingMode = ShadowCastingMode.Off;
            var glow = new GameObject("CrystalLight").AddComponent<Light>();
            glow.transform.SetParent(crystal.transform, false);
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.95f, 0.86f);
            glow.range = 7f;
            glow.intensity = 2.2f;
            glow.shadows = LightShadows.None;
            GenUtil.Set(director, "crystal", crystal.transform);
            GenUtil.Set(director, "crystalLight", glow);
            GenUtil.Set(director, "crystalRenderer", crystalRenderer);
            GenUtil.Set(director, "raiseCam", Mark("Raise_Cam", marks, new Vector3(200.9f, 0.55f, 25.3f), new Vector3(200f, 2.05f, 27.5f)));

            // voice-over in speaking order (null = not recorded yet: caption only)
            AudioClip Vo(string n) => n == null ? null : AssetDatabase.LoadAssetAtPath<AudioClip>($"{GenUtil.Root}/Audio/Voice/{n}.ogg");
            GenUtil.SetArray(director, "voiceLines", new Object[]
            {
                Vo("VO_E_N1_FromTheTomb"), Vo("VO_E_N2_BroughtBack"), Vo("VO_E_N3_Themselves"), Vo("VO_E_Y01_Ren"),
                Vo("VO_E_N4_InReturn"), Vo("VO_E_V1_EveryAge"), null, null,
                Vo("VO_E_N5_SomeoneNew"), Vo("VO_E_N6_OldMan"), Vo("VO_E_E1_OnceAgain"),
            });

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("C02 ending built.");
        }

        /// <summary>A crystal: six-sided, long point up, short point down (flat-shaded facets).</summary>
        private static Mesh CrystalMesh()
        {
            const int sides = 6;
            const float r = 0.075f, top = 0.21f, bottom = -0.12f;
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a0 = i / (float)sides * Mathf.PI * 2f, a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * r, 0f, Mathf.Sin(a0) * r), p1 = new Vector3(Mathf.Cos(a1) * r, 0f, Mathf.Sin(a1) * r);
                foreach (var (tip, flip) in new[] { (new Vector3(0f, top, 0f), false), (new Vector3(0f, bottom, 0f), true) })
                {
                    int b = verts.Count;
                    verts.Add(p0); verts.Add(p1); verts.Add(tip);
                    if (flip) { tris.Add(b); tris.Add(b + 1); tris.Add(b + 2); }
                    else { tris.Add(b); tris.Add(b + 2); tris.Add(b + 1); }
                }
            }
            var mesh = new Mesh { name = "EndingCrystal" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            string path = $"{GenUtil.Root}/Art/Models/Props/EndingCrystal.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { existing.Clear(); EditorUtility.CopySerialized(mesh, existing); return existing; }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
